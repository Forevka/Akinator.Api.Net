using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Akinator.Api.Net.Utils
{
    public class AkiWebClient : IDisposable
    {
        private readonly IAkinatorLogger _logger;
        private readonly HttpClient _mWebClient;

        public AkiWebClient(IAkinatorLogger logger)
        {
            _logger = logger;
            _mWebClient = new HttpClient(new HttpClientHandler
            {
                UseCookies = false
            });
            _mWebClient.DefaultRequestHeaders.Add("Accept", "text/javascript, application/javascript, application/ecmascript, application/x-ecmascript, */*; q=0.01");
            _mWebClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,ar;q=0.8");
            _mWebClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            _mWebClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            _mWebClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            _mWebClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            _mWebClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _mWebClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.92 Safari/537.36");
            _mWebClient.DefaultRequestHeaders.Add("Referer", "https://en.akinator.com/game");
        }

        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var res = await _mWebClient.GetAsync(url, cancellationToken);

            watch.Stop();

            await _logger.Information($"[Akinator.Api] Request to {url} took {watch.ElapsedMilliseconds} ms.");

            return res;
        }

        public void Dispose()
        {
            _mWebClient?.Dispose();
        }
    }
    
}
