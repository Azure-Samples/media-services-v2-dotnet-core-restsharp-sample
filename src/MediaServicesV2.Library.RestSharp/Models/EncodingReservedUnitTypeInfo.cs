using System;
using Newtonsoft.Json;

namespace MediaServicesV2.Library.RestSharp.Models
{
    /// <summary>
    /// Class to manage reserved unit type.
    /// </summary>
    public class EncodingReservedUnitTypeInfo
    {
        /// <summary>
        /// Gets or sets the reserved unit type (S1, S2 or S3).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ReservedUnitType? ReservedUnitType { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of units that can be reserved for the account.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxReservableUnits { get; set; }

        /// <summary>
        /// Gets or sets the current reserved units.
        /// The number of the encoding reserved units that you want to be provisioned for this account.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? CurrentReservedUnits { get; set; }

        /// <summary>
        /// Gets or sets the Media Services account Id.
        /// </summary>
        public Guid AccountId { get; set; }
    }
}