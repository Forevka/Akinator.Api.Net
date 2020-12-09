using Akinator.Api.Net.Model;
using Newtonsoft.Json;

namespace Akinator.Api.Net.Exceptions
{
    public class AkinatorTimeoutException : AkinatorBaseException
    {
        public AkinatorTimeoutException(string url, string response) : base(url, response) { }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(new ExceptionDataModel
            {
                Content = Response,
                Name = "Timeout",
                Url = Url,
            });
        }
    }
}
