using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class BigMapInterface
    {
        /// <summary>
        /// Full path to the Big_map in the contract storage
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Big_map name, if exists (field annotation)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// JSON Schema of the Big_map key in humanified format (as returned by API)
        /// </summary>
        public RawJson KeySchema { get; set; }

        /// <summary>
        /// JSON Schema of the Big_map value in humanified format (as returned by API)
        /// </summary>
        public RawJson ValueSchema { get; set; }
    }

    public class EntrypointInterface
    {
        /// <summary>
        /// Entrypoint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// JSON Schema of the entrypoint parameter in humanified format (as returned by API)
        /// </summary>
        public RawJson ParameterSchema { get; set; }
    }

    public class EventInterface
    {
        /// <summary>
        /// Event tag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// JSON Schema of the event type in humanified format (as returned by API)
        /// </summary>
        public RawJson EventSchema { get; set; }
    }

    public class ContractInterface
    {
        /// <summary>
        /// JSON Schema of the contract storage in humanified format (as returned by API)
        /// </summary>
        public RawJson StorageSchema { get; set; }

        /// <summary>
        /// List of terminal entrypoints
        /// </summary>
        public List<EntrypointInterface> Entrypoints { get; set; }

        /// <summary>
        /// List of currently available Big_maps
        /// </summary>
        public List<BigMapInterface> BigMaps { get; set; }

        /// <summary>
        /// List of events extractable from the code ("static")
        /// </summary>
        public List<EventInterface> Events { get; set; }
    }
}
