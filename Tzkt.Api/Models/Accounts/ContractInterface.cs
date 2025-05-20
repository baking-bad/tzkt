namespace Tzkt.Api.Models
{
    public class BigMapInterface
    {
        /// <summary>
        /// Full path to the Big_map in the contract storage
        /// </summary>
        public required string Path { get; set; }

        /// <summary>
        /// Big_map name, if exists (field annotation)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// JSON Schema of the Big_map key in humanified format (as returned by API)
        /// </summary>
        public required RawJson KeySchema { get; set; }

        /// <summary>
        /// JSON Schema of the Big_map value in humanified format (as returned by API)
        /// </summary>
        public required RawJson ValueSchema { get; set; }
    }

    public class EntrypointInterface
    {
        /// <summary>
        /// Entrypoint name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// JSON Schema of the entrypoint parameter in humanified format (as returned by API)
        /// </summary>
        public required RawJson ParameterSchema { get; set; }
    }

    public class EventInterface
    {
        /// <summary>
        /// Event tag
        /// </summary>
        public required string Tag { get; set; }

        /// <summary>
        /// JSON Schema of the event type in humanified format (as returned by API)
        /// </summary>
        public required RawJson EventSchema { get; set; }
    }

    public class ContractInterface
    {
        /// <summary>
        /// JSON Schema of the contract storage in humanified format (as returned by API)
        /// </summary>
        public required RawJson StorageSchema { get; set; }

        /// <summary>
        /// List of terminal entrypoints
        /// </summary>
        public required List<EntrypointInterface> Entrypoints { get; set; }

        /// <summary>
        /// List of currently available Big_maps
        /// </summary>
        public required List<BigMapInterface> BigMaps { get; set; }

        /// <summary>
        /// List of events extractable from the code ("static")
        /// </summary>
        public required List<EventInterface> Events { get; set; }
    }
}
