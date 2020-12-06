using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akinator.Api.Net.Model.External
{
    internal class BaseResponse
    {
        [JsonProperty("completion")]
        public string Completion { get; set; }

        [JsonProperty("parameters")]
        public JObject Parameters { get; set; }

        public TParametersType DeserializeParameters<TParametersType>(JsonSerializerSettings settings) where TParametersType : IBaseParameters
        {
            return Parameters.ToObject<TParametersType>(JsonSerializer.Create(settings));
        }
    }
}