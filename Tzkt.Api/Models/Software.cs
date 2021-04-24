using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Tzkt.Api.Models
{
    public class Software
    {
        /// <summary>
        /// Software ID (short commit hash)
        /// </summary>
        public string ShortHash { get; set; }

        /// <summary>
        /// Level of the first block baked with this software
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Datetime of the first block baked with this software
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the last block baked with this software
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Datetime of the last block baked with this software
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of blocks baked with this software
        /// </summary>
        public int BlocksCount { get; set; }

        /// <summary>
        /// Offchain metadata
        /// </summary>
        public RawJson Metadata { get; set; }

        /// <summary>
        /// **DEPRECATED**. Use `metadata` instead.
        /// </summary>
        public DateTime? CommitDate
        {
            get
            {
                if (Metadata?.Json == null) return null;
                using var doc = JsonDocument.Parse(Metadata.Json);
                return doc.RootElement.TryGetProperty("commitDate", out var v) && v.TryGetDateTime(out var dt) ? dt : null;
            }
        }

        /// <summary>
        /// **DEPRECATED**. Use `metadata` instead.
        /// </summary>
        public string CommitHash
        {
            get
            {
                if (Metadata?.Json == null) return null;
                using var doc = JsonDocument.Parse(Metadata.Json);
                return doc.RootElement.TryGetProperty("commitHash", out var v) ? v.GetString() : null;
            }
        }

        /// <summary>
        /// **DEPRECATED**. Use `metadata` instead.
        /// </summary>
        public string Version
        {
            get
            {
                if (Metadata?.Json == null) return null;
                using var doc = JsonDocument.Parse(Metadata.Json);
                return doc.RootElement.TryGetProperty("version", out var v) ? v.GetString() : null;
            }
        }

        /// <summary>
        /// **DEPRECATED**. Use `metadata` instead.
        /// </summary>
        public List<string> Tags
        {
            get
            {
                if (Metadata?.Json == null) return null;
                using var doc = JsonDocument.Parse(Metadata.Json);
                return doc.RootElement.TryGetProperty("tags", out var v) && v.ValueKind == JsonValueKind.Array
                    ? v.EnumerateArray().Select(x => x.GetString()).ToList() : null;
            }
        }
    }
}
