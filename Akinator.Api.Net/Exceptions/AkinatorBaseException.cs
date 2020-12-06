using System;

namespace Akinator.Api.Net.Exceptions
{
    public class AkinatorBaseException : Exception
    {
        public readonly string Url;
        public readonly string Response;

        public AkinatorBaseException(string url, string response)
        {
            Url = url;
            Response = response;
        }
    }
}
