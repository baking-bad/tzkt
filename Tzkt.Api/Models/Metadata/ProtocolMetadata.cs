namespace Tzkt.Api.Models
{
    public class ProtocolMetadata
    {
        /// <summary>
        /// Protocol name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Protocol hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Link to the documentation with explanation of the protocol and changes
        /// </summary>
        public string Docs { get; set; }
    }
}
