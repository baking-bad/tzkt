using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class RawOperation
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("contents")]
        public List<IOperationContent> Contents { get; set; }

        #region validation
        public bool IsValidFormat() =>
            !string.IsNullOrEmpty(Hash) &&
            Contents?.Count > 0 &&
            Contents.All(x => x.IsValidFormat());
        #endregion
    }

    public interface IManagerOperationContent : IOperationContent
    {
        [JsonIgnore]
        int GlobalCounter { get; set; }
    }

    public interface IOperationContent
    {
        bool IsValidFormat();
    }
    
    public interface IInternalOperationResult
    {
        bool IsValidFormat();
    }    
}
