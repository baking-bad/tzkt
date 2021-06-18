namespace Tzkt.Api.Models
{
    public class ProposalMetadata
    {
        /// <summary>
        /// Alias name of the proposal
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Hash of the proposal, which representing a tarball of concatenated .ml/.mli source files
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Link to the proposal on Tezos Agora portall with full details
        /// </summary>
        public string Agora { get; set; }

        /// <summary>
        /// Reward for the proposal developers, that will be generated if the proposal is accepted
        /// </summary>
        public long Invoice { get; set; }
    }
}
