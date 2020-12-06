using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Akinator.Api.Net.Enumerations;
using Akinator.Api.Net.Model;
using Akinator.Api.Net.Model.External;
using Akinator.Api.Net.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Akinator.Api.Net
{
    public class AkinatorClient : IAkinatorClient
    {
        private readonly Regex _mRegexSession = new Regex("var uid_ext_session = '(.*)'\\;\\n.*var frontaddr = '(.*)'\\;");
        private readonly Regex _mRegexStartGameResult = new Regex(@"^jQuery3410014644797238627216_\d+\((.+)\)$");
        private readonly AkiWebClient _mWebClient;
        private readonly IAkinatorServer _mServer;
        private readonly bool _mChildMode;
        private string _mSession;
        private string _mSignature;
        private int _mStep;
        private int _mLastGuessStep;

        public AkinatorClient(IAkinatorServer server, AkinatorUserSession existingSession = null, bool childMode = false)
        {
            _mWebClient = new AkiWebClient();
            _mServer = server;
            _mChildMode = childMode;
            Attach(existingSession);
        }

        public async Task<AkinatorQuestion> StartNewGame(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = await GetSession(cancellationToken).ConfigureAwait(false);
            
            var url = AkiUrlBuilder.NewGame(apiKey, _mServer, _mChildMode);
            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var match = _mRegexStartGameResult.Match(content);
            if (!match.Success && match.Groups.Count != 2)
            {
                throw new InvalidCastException($"Invalid result received from Akinator. Result was {response}");
            }

            var result = JsonConvert.DeserializeObject<BaseResponse<NewGameParameters>>(match.Groups[1].Value,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            _mSession = result.Parameters.Identification.Session;
            _mSignature = result.Parameters.Identification.Signature;
            _mStep = result.Parameters.StepInformation.Step;
            CurrentQuestion = ToAkinatorQuestion(result.Parameters.StepInformation);
            return ToAkinatorQuestion(result.Parameters.StepInformation);
        }

        public async Task<AkinatorQuestion> Answer(AnswerOptions answer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AkiUrlBuilder.Answer(BuildAnswerRequest(answer), _mServer);

            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<BaseResponse<Question>>(content,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            _mStep = result.Parameters.Step;
            CurrentQuestion = ToAkinatorQuestion(result.Parameters);
            return ToAkinatorQuestion(result.Parameters);
        }

        public async Task<AkinatorQuestion> UndoAnswer(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_mStep == 0)
            {
                return null;
            }

            var url = AkiUrlBuilder.UndoAnswer(_mSession, _mSignature, _mStep, _mServer);

            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<BaseResponse<Question>>(content,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            _mStep = result.Parameters.Step;
            CurrentQuestion = ToAkinatorQuestion(result.Parameters);
            return ToAkinatorQuestion(result.Parameters);
        }

        public async Task<AkinatorQuestion> ExclusionGame(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_mStep == 0)
            {
                return null;
            }

            var url = AkiUrlBuilder.Exclusion(_mSession, _mSignature, _mStep, _mServer);

            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<BaseResponse<Question>>(content,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            _mStep = result.Parameters.Step;
            CurrentQuestion = ToAkinatorQuestion(result.Parameters);
            return ToAkinatorQuestion(result.Parameters);
        }



        public async Task<AkinatorGuess[]> SearchCharacter(string search, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AkiUrlBuilder.SearchCharacter(search, _mSession, _mSignature, _mStep, _mServer);

            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<BaseResponse<Characters>>(content,
                new JsonSerializerSettings()
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            return result.Parameters.AllCharacters.Select(p =>
                new AkinatorGuess(p.Name, p.Description)
                {
                    ID = p.IdBase,
                    PhotoPath = p.PhotoPath,
                }).ToArray();
        }

        public AkinatorQuestion CurrentQuestion { get; private set; }

        public async Task<AkinatorHallOfFameEntries[]> GetHallOfFame(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hallOfFameRequestUrl = AkiUrlBuilder.MapHallOfFame(_mServer);
            var response = await _mWebClient.GetAsync(hallOfFameRequestUrl, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = XmlConverter.ToClass<HallOfFame>(content);
            return ToHallOfFameEntry(data.Awards.Award);
        }

        public async Task<AkinatorGuess[]> GetGuess(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AkiUrlBuilder.GetGuessUrl(BuildGuessRequest(), _mServer);
            var response = await _mWebClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<BaseResponse<Guess>>(content,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

            _mLastGuessStep = _mStep;

            return result.Parameters.Characters.Select(p =>
                new AkinatorGuess(p.Name, p.Description)
                {
                    ID = p.Id,
                    PhotoPath = p.PhotoPath,
                    Probabilty = p.Probabilty
                }).ToArray();
        }

        public bool GuessIsDue(Platform platform = Platform.Android) =>
            GuessDueChecker.GuessIsDue(CurrentQuestion, _mLastGuessStep, platform);

        private async Task<ApiKey> GetSession(CancellationToken cancellationToken)
        {
            var response = await _mWebClient.GetAsync("https://en.akinator.com/game", cancellationToken).ConfigureAwait(false);
            if (response?.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Cannot connect to Akinator.com");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var match = _mRegexSession.Match(content);
            if (!match.Success)
            {
                throw new InvalidOperationException("Cannot retrieve the Api-Key from Akinator.com");
            }

            var apiKey = new ApiKey
            {
                SessionUid = match.Groups[1].Value,
                FrontAdress = match.Groups[2].Value
            };

            return apiKey;
        }

        public AkinatorUserSession GetUserSession() =>
            new AkinatorUserSession(_mSession, _mSignature, _mStep, _mLastGuessStep);

        private static AkinatorQuestion ToAkinatorQuestion(Question question) =>
            new AkinatorQuestion(question.Text, question.Progression, question.Step);

        private static AkinatorHallOfFameEntries[] ToHallOfFameEntry(List<Award> awardsAward) =>
            awardsAward
                .Select(p => new AkinatorHallOfFameEntries(
                    p.AwardId,
                    p.CharacterName,
                    p.Description,
                    p.Type,
                    p.WinnerName,
                    p.Delai,
                    p.Pos))
                .ToArray();

        private GuessRequest BuildGuessRequest() =>
            new GuessRequest(_mStep, _mSession, _mSignature);

        private AnswerRequest BuildAnswerRequest(AnswerOptions choice) =>
            new AnswerRequest(choice, _mStep, _mSession, _mSignature);

        private void Attach(AkinatorUserSession existingSession)
        {
            if (existingSession != null)
            {
                _mStep = existingSession.Step;
                _mLastGuessStep = existingSession.LastGuessStep;
                _mSession = existingSession.Session;
                _mSignature = existingSession.Signature;
            }
        }

        public void Dispose()
        {
            _mWebClient?.Dispose();
        }
    }
}
