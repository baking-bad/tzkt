﻿using System.Numerics;
using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class TicketBalanceShort
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Ticket info.  
        /// Click on the field to expand more details.
        /// </summary>
        public required TicketInfoShort Ticket { get; set; }

        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public required Alias Account { get; set; }

        /// <summary>
        /// Balance.  
        /// </summary>
        [JsonSchemaType(typeof(string), IsNullable = false)]
        public BigInteger Balance { get; set; }
    }
}
