using System;

namespace Tzkt.Api.Models
{
    public class SoftwareAlias
    {
        /// <summary>
        /// Software version (commit tag)
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Date of the commit or when the software was first seen
        /// </summary>
        public DateTime Date { get; set; }
    }
}
