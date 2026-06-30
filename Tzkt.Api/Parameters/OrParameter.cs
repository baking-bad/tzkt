using System.Text.Json.Serialization;
using NSwag.Annotations;

namespace Tzkt.Api
{
    public class OrParameter(params (string, List<int>?)[] colsAndVals)
    {
        [JsonIgnore]
        [OpenApiIgnore]
        public (string, List<int>?)[] ColsAndVals { get; } = colsAndVals;
    }
}
