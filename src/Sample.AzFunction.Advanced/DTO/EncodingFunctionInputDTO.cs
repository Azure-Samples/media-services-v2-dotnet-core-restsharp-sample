using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sample.AzFunction.Advanced.Models
{
    /// <summary>
    /// Data transfer object used as input for the functions.
    /// </summary>
    public class EncodingFunctionInputDTO
    {
        /// <summary>
        /// Gets or sets inputs
        /// </summary>
        [JsonProperty("inputs")]
        public IEnumerable<InputItem> Inputs { get; set; }

        /// <summary>
        /// Gets or sets Media Encoder Standard preset name. For example "Sprites" or "Adaptive Streaming"
        /// </summary>
        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        /// <summary>
        /// Gets or sets storage account name of the output asset.
        /// Optional parameter.
        /// </summary>
        [JsonProperty("outputAssetStorage")]
        public string OutputAssetStorage { get; set; }

        /// <summary>
        /// Gets or sets operation context.
        /// Optional parameter.
        /// </summary>
        [JsonProperty("operationContext")]
#pragma warning disable CA2227 // Collection properties should be read only
        public JObject OperationContext { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }

    /// <summary>
    /// Input object.
    /// </summary>
    public class InputItem
    {
        /// <summary>
        /// Gets or sets source blob Uris
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }
    }
}