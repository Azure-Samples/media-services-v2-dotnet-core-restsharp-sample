using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Newtonsoft.Json.Linq;

namespace MediaServicesV2.Services.Encoding.Services.Media
{
    /// <summary>
    /// Manages the Azure Media Services operations.
    /// </summary>
    public interface IMediaServicesV2EncodeOperations
    {
        /// <summary>
        /// Copies files and updates properties.
        /// </summary>
        /// <param name="filesToCopy">Uris of files to copy.</param>
        /// <returns>The inputAssetId.</returns>
        public Task<string> CopyFilesIntoNewAsset(IEnumerable<Uri> filesToCopy);

        /// <summary>
        /// Upload files and updates properties.
        /// </summary>
        /// <param name="filesToUpload">Path of files to upload.</param>
        /// <returns>The inputAssetId.</returns>
        public Task<string> UploadFilesIntoNewAsset(IEnumerable<string> filesToUpload);

        /// <summary>
        /// Create a Azure Media Services job with the MediaEncoderStandard processor.
        /// </summary>
        /// <param name="inputAssetId">inputAssetId.</param>
        /// <param name="preset">The preset to be used, a known preset, or json.</param>
        /// <param name="outputAssetName">The output asset name.</param>
        /// <param name="outputAssetStorageAccountName">The storage account name of the output asset.</param>
        /// <param name="callbackEndpoint">The callback endpoint url for V2 notifications.</param>
        /// <param name="correlationData">The correlation data to be base64UrlEncoded into the task name.</param>
        /// <returns>JobId.</returns>
        public Task<string> SubmitMesJobAsync(string inputAssetId, string preset, string outputAssetName, string outputAssetStorageAccountName = null, Uri callbackEndpoint = null, IDictionary<string, string> correlationData = null);

        /// <summary>
        /// Provides the OperationContext that was set for the Job.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>OperationContext sent by the caller when starting this job.</returns>
        public Task<JObject> GetOperationContextForJobAsync(string jobId);

        /// <summary>
        /// Copies the AMS.V2 created asset to the desired output container.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>The array of the destination output.</returns>
        public Task<Uri[]> CopyOutputAssetToOutputContainerAsync(string jobId);

        /// <summary>
        /// Deletes both the input and output assets associated with a jobId.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>The deletion has completed successfully.</returns>
        public Task<bool> DeleteAssetsForJobAsync(string jobId);

        /// <summary>
        /// Generates the input asset name.
        /// </summary>
        /// <param name="sourceUriBuilder">Uri of the blob.</param>
        /// <returns>Input asset name.</returns>
        public string GetInputAssetName(BlobUriBuilder sourceUriBuilder);

        /// <summary>
        /// Generates the output asset name.
        /// </summary>
        /// <param name="blobUri">Uri of the source blob.</param>
        /// <param name="outputAssetStorageAccountName">Storage account name of the output asset.</param>
        /// <returns>Output asset name.</returns>
        public string GetOutputAssetName(Uri blobUri, string outputAssetStorageAccountName);

        /// <summary>
        /// Generates the output asset name.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        /// <param name="outputAssetStorageAccountName">Storage account name of the output asset.</param>
        /// <returns>Output asset name.</returns>
        public string GetOutputAssetName(string filePath, string outputAssetStorageAccountName);
    }
}