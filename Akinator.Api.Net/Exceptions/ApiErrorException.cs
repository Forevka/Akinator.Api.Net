using Akinator.Api.Net.Model;
using Newtonsoft.Json;

namespace Akinator.Api.Net.Exceptions
{
    public class ApiErrorException : AkinatorBaseException
    {
        private readonly string _description;
        public ApiErrorException(string url, string response, string description) : base(url, response)
        {
            _description = description;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(new ExceptionDataModel
            {
                Content = Response,
                Name = _description,
                Url = Url,
            });
        }
    }
}
