using Akinator.Api.Net.Enumerations;
using Akinator.Api.Net.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akinator.Api.Net
{
    public class DebugAkinatorClient : AkinatorClient, IAkinatorClient
    {
        private readonly IAkinatorLogger _logger;

        public DebugAkinatorClient(IAkinatorServer server, IAkinatorLogger logger, AkinatorUserSession existingSession = null, bool childMode = false)
            : base(server, logger, existingSession, childMode)
        {
            _logger = logger;
        }

        public new async Task<AkinatorQuestion> StartNewGame(CancellationToken cancellationToken = default)
        {
            return await Measure(base.StartNewGame, cancellationToken);
        }

        public new async Task<AkinatorQuestion> Answer(AnswerOptions option, CancellationToken cancellationToken = default)
        {
            return await Measure(base.Answer, cancellationToken, option);
        }

        public new async Task<AkinatorQuestion> UndoAnswer(CancellationToken cancellationToken = default)
        {
            return await Measure(base.UndoAnswer, cancellationToken);
        }

        public new async Task<AkinatorQuestion> ExclusionGame(CancellationToken cancellationToken = default)
        {
            return await Measure(base.ExclusionGame, cancellationToken);
        }

        public new async Task<AkinatorGuess[]> GetGuess(CancellationToken cancellationToken = default)
        {
            return await Measure(base.GetGuess, cancellationToken);
        }

        private async Task<TOut> Measure<TOut>(Func<CancellationToken, Task<TOut>> func, CancellationToken cancellationToken)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var res = await func(cancellationToken);

            watch.Stop();

            await _logger.Information($"[Akinator.Api] {func.Method.Name} took {watch.ElapsedMilliseconds} ms.");

            return res;
        }

        private async Task<TOut> Measure<TOut, TIn0>(Func<TIn0, CancellationToken, Task<TOut>> func,
            CancellationToken cancellationToken, TIn0 in0)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var res = await func(in0, cancellationToken);

            watch.Stop();

            await _logger.Information($"[Akinator.Api] {func.Method.Name} took {watch.ElapsedMilliseconds} ms.");

            return res;
        }
    }
}
