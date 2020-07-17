using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using MediaServicesV2.Library.RestSharp;
using MediaServicesV2.Library.RestSharp.Models;
using Microsoft.Azure.Management.BatchAI.Fluent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Storage.Helper;

namespace MediaServicesV2.Services.Encoding.Services.Media
{
    /// <summary>
    /// Implements the services that performs work using AMS V2 REST calls.
    /// </summary>
    public class MediaServicesV2EncodeOperations : IMediaServicesV2EncodeOperations
    {
        private readonly ILogger<MediaServicesV2EncodeOperations> _log;
        private readonly IAzureStorageOperations _storageService;
        private readonly IMediaServicesV2RestSharp _mediaServicesV2RestSharp;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2EncodeOperations"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="mediaServicesV2RestSharp">mediaServicesV2RestSharp.</param>
        public MediaServicesV2EncodeOperations(
            ILogger<MediaServicesV2EncodeOperations> log,
            IAzureStorageOperations storageService,
            IMediaServicesV2RestSharp mediaServicesV2RestSharp)
        {
            _log = log;
            _storageService = storageService;
            _mediaServicesV2RestSharp = mediaServicesV2RestSharp;
        }

        /// <inheritdoc/>
        public async Task<string> CopyFilesIntoNewAsset(IEnumerable<Uri> filesToCopy)
        {
            _ = filesToCopy ?? throw new ArgumentNullException(nameof(filesToCopy));
            _ = !filesToCopy.Any() ? throw new ArgumentOutOfRangeException(nameof(filesToCopy), "Count is zero") : 0;

            string newAssetId;
            var assetUriBuilder = new BlobUriBuilder(filesToCopy.First());
            string assetName = GetInputAssetName(assetUriBuilder);
            string assetAccountName = assetUriBuilder.AccountName;
            Uri assetUri;
            try
            {
                (newAssetId, assetUri) = await _mediaServicesV2RestSharp.CreateEmptyAssetAsync(assetName, assetAccountName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Error creating asset for {assetUriBuilder.ToUri()}");
                throw new Exception($"Failed to create asset for {assetUriBuilder.ToUri()}", e);
            }

            _log.LogInformation($"Created {newAssetId}, {assetName}");

            try
            {
                foreach (var fileToCopy in filesToCopy)
                {
                    var sourceUriBuilder = new BlobUriBuilder(fileToCopy);
                    var destUriBuilder = new BlobUriBuilder(assetUri)
                    {
                        BlobName = sourceUriBuilder.BlobName,
                    };
                    var exists = await _storageService.BlobExistsAsync(fileToCopy).ConfigureAwait(false);
                    if (!exists)
                    {
                        _log.LogError($"Attempted to use nonexistent blob: {fileToCopy} as input to encoding.");
                    }

                    var s = new Stopwatch();
                    s.Start();
                    var copyFromUriOperation = await _storageService.BlobCopyAsync(fileToCopy, destUriBuilder.ToUri()).ConfigureAwait(false);
                    var response = await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false);
                    s.Stop();
                    _log.LogInformation($"MediaServicesV2CopyFileCompleted {s.ElapsedMilliseconds.ToString("G", CultureInfo.InvariantCulture)}");
                }

                await _mediaServicesV2RestSharp.CreateFileInfosAsync(newAssetId).ConfigureAwait(false);
                _log.LogInformation($"MediaServicesV2CopyFileAndUpdateAssetSuccess {assetName}, {assetUri}");
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed while coping files");
                throw new Exception($"Failed to copy {assetName} to {newAssetId}", e);
            }

            return newAssetId;
        }

        /// <inheritdoc/>
        public async Task<string> UploadFilesIntoNewAsset(IEnumerable<string> filesToUpload)
        {
            _ = filesToUpload ?? throw new ArgumentNullException(nameof(filesToUpload));
            _ = !filesToUpload.Any() ? throw new ArgumentOutOfRangeException(nameof(filesToUpload), "Count is zero") : 0;

            string newAssetId;
            string assetName = Path.GetFileName(filesToUpload.First());
            string assetAccountName = null; // assetUriBuilder.AccountName;
            Uri assetUri;
            try
            {
                (newAssetId, assetUri) = await _mediaServicesV2RestSharp.CreateEmptyAssetAsync(assetName, assetAccountName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Error creating asset for {assetName}");
                throw new Exception($"Failed to create asset for {assetName}", e);
            }

            _log.LogInformation($"Created {newAssetId}, {assetName}");

            try
            {
                foreach (var fileToUpload in filesToUpload)
                {
                    if (!System.IO.File.Exists(fileToUpload))
                    {
                        _log.LogError($"Attempted to use nonexistent file: {fileToUpload} as input to encoding.");
                    }

                    var s = new Stopwatch();
                    s.Start();
                    var copyFromUriOperation = await _storageService.BlobUploadAsync(fileToUpload, assetUri).ConfigureAwait(false);
                    s.Stop();
                    _log.LogInformation($"MediaServicesV2UploadFileCompleted {s.ElapsedMilliseconds.ToString("G", CultureInfo.InvariantCulture)}");
                }

                await _mediaServicesV2RestSharp.CreateFileInfosAsync(newAssetId).ConfigureAwait(false);
                _log.LogInformation($"MediaServicesV2UploadFileAndUpdateAssetSuccess {assetName}, {assetUri}");
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed while uploading files");
                throw new Exception($"Failed to upload {assetName} to {newAssetId}", e);
            }

            return newAssetId;
        }

        /// <inheritdoc/>
        public async Task<string> SubmitMesJobAsync(string inputAssetId, string preset, string outputAssetName, string outputAssetStorageAccountName = null, Uri callbackEndpoint = null, IDictionary<string, string> correlationData = null)
        {
            if (string.IsNullOrWhiteSpace(inputAssetId))
            {
                throw new ArgumentException($@"{nameof(inputAssetId)} is invalid", nameof(inputAssetId));
            }

            if (string.IsNullOrWhiteSpace(preset))
            {
                throw new ArgumentException($@"{nameof(preset)} is invalid", nameof(preset));
            }

            _ = outputAssetName ?? throw new ArgumentNullException(nameof(outputAssetName));

            string jobName;
            try
            {
                jobName = GenerateJobName(outputAssetName);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not define job name from {outputAssetName}.", e);
            }

            string processorId;
            try
            {
                processorId = await _mediaServicesV2RestSharp.GetLatestMediaProcessorAsync("Media Encoder Standard").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Could not get media processor.");
                throw new Exception($"Could not get media processor.", e);
            }

            string base64UrlEncodedCorrelationDataJsonString;
            try
            {
                var correlationDataJsonString = JsonConvert.SerializeObject(correlationData);
                base64UrlEncodedCorrelationDataJsonString = Base64UrlEncoder.Encode(correlationDataJsonString);
                if (base64UrlEncodedCorrelationDataJsonString.Length > 4000)
                {
                    const string ErrorMsg = "UrlEncoded and serialized correlationData is larger than 4000";
                    _log.LogError(ErrorMsg);
                    throw new ArgumentException(ErrorMsg, nameof(correlationData));
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Could not convert correlationData.");
                throw new Exception($"Could not convert correlationData.", e);
            }

            string notificationEndPointId = string.Empty;
            if (callbackEndpoint != null)
            {
                try
                {
                    notificationEndPointId = await _mediaServicesV2RestSharp.GetOrCreateNotificationEndPointAsync("AmsV2Callback", callbackEndpoint).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _log.LogError(e, callbackEndpoint.ToString());
                    throw new Exception($"Could not create notification endpoint for {callbackEndpoint}", e);
                }
            }

            string jobId;
            string jobParams = $"{jobName} {processorId} {inputAssetId} {preset} {outputAssetName} {outputAssetStorageAccountName} {base64UrlEncodedCorrelationDataJsonString} {notificationEndPointId}";
            try
            {
                jobId = await _mediaServicesV2RestSharp.CreateJobAsync(
                    jobName,
                    processorId,
                    inputAssetId,
                    preset,
                    outputAssetName,
                    outputAssetStorageAccountName,
                    correlationData: base64UrlEncodedCorrelationDataJsonString,
                    notificationEndPointId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogError(e, jobParams);
                throw new Exception($"Could not start media encoder standard job.", e);
            }

            _log.LogInformation($"Started {jobId} with {jobParams}");
            return jobId;
        }

        /// <inheritdoc/>
        public async Task<JObject> GetOperationContextForJobAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary = await GetCorrelationDataDictionaryAsync(jobId).ConfigureAwait(false);
            if (correlationDataDictionary == null || !correlationDataDictionary.ContainsKey("operationContext"))
            {
                _log.LogError($"Could not get operationContext. {jobId}");
                throw new Exception($"Could not get operationContext. {jobId}");
            }

            var operationContextJsonString = correlationDataDictionary["operationContext"];
            var operationContext = JObject.Parse(operationContextJsonString);
            return operationContext;
        }

        /// <inheritdoc/>
        public async Task<Uri[]> CopyOutputAssetToOutputContainerAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary = await GetCorrelationDataDictionaryAsync(jobId).ConfigureAwait(false);
            if (correlationDataDictionary == null || !correlationDataDictionary.ContainsKey("outputAssetContainer"))
            {
                _log.LogError($"Expected outputAssetContainer in correlationDataDictionary from {jobId}.");
                throw new Exception($"Expected outputAssetContainer in correlationDataDictionary from {jobId}.");
            }

            var outputAssetContainer = correlationDataDictionary["outputAssetContainer"];
            var outputAssetContainerUri = new Uri(outputAssetContainer);
            (string firstOutputAssetId, _) = await _mediaServicesV2RestSharp.GetFirstOutputAssetAsync(jobId).ConfigureAwait(false);

            (_, Uri outputAssetUri) = await _mediaServicesV2RestSharp.GetAssetNameAndUriAsync(firstOutputAssetId).ConfigureAwait(false);

            IEnumerable<string> outputAssetFileNames = await _mediaServicesV2RestSharp.GetAssetFilesNames(firstOutputAssetId).ConfigureAwait(false);

            List<Uri> outputUris = new List<Uri> { };

            foreach (var assetFileName in outputAssetFileNames)
            {
                try
                {
                    var sourceUriBuilder = new BlobUriBuilder(outputAssetUri)
                    {
                        BlobName = assetFileName,
                    };

                    var destUriBuilder = new BlobUriBuilder(outputAssetContainerUri)
                    {
                        BlobName = assetFileName,
                    };

                    var s = new Stopwatch();
                    s.Start();
                    var copyFromUriOperation = await _storageService.BlobCopyAsync(sourceUriBuilder.ToUri(), destUriBuilder.ToUri()).ConfigureAwait(false);
                    var response = await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false);
                    s.Stop();
                    outputUris.Add(destUriBuilder.ToUri());
                    DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
                    _log.LogInformation($"MediaServicesV2CopyFileCompleted {s.ElapsedMilliseconds.ToString("G", CultureInfo.InvariantCulture)}");
                }
                catch (Exception e)
                {
                    _log.LogError($"Failed to copy {assetFileName} {outputAssetContainer} for {jobId}");
                    throw new Exception($"Failed to copy {assetFileName} {outputAssetContainer} for {jobId}", e);
                }

                _log.LogInformation($"Copied output asset to {outputAssetContainer} for {jobId}");
            }

            return outputUris.ToArray();
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAssetsForJobAsync(string jobId)
        {
            try
            {
                (string firstInputAssetId, _) = await _mediaServicesV2RestSharp.GetFirstInputAssetAsync(jobId).ConfigureAwait(false);
                await _mediaServicesV2RestSharp.DeleteAssetAsync(firstInputAssetId).ConfigureAwait(false);

                (string firstOutputAssetId, _) = await _mediaServicesV2RestSharp.GetFirstOutputAssetAsync(jobId).ConfigureAwait(false);
                await _mediaServicesV2RestSharp.DeleteAssetAsync(firstOutputAssetId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogError(e, jobId);
                throw new Exception($"Could not delete asset for {jobId}.", e);
            }

            return true;
        }

        /// <summary>
        /// Generates the input asset name from the source Uri.
        /// </summary>
        /// <param name="sourceUriBuilder">Source Uri.</param>
        /// <returns>The input asset name generated.</returns>
        public string GetInputAssetName(BlobUriBuilder sourceUriBuilder)
        {
            _ = sourceUriBuilder ?? throw new ArgumentNullException(nameof(sourceUriBuilder));
            return $"V2-{sourceUriBuilder.AccountName}-{sourceUriBuilder.BlobContainerName}-Input";
        }

        /// <summary>
        /// Generates the output asset name from the output Uri.
        /// </summary>
        /// <param name="blobUri">Output blob Uri.</param>
        /// <param name="outputAssetStorageAccountName">Output asset storage account.</param>
        /// <returns>The output asset name generated.</returns>
        public string GetOutputAssetName(Uri blobUri, string outputAssetStorageAccountName)
        {
            BlobUriBuilder outputContainerUriBuilder = new BlobUriBuilder(blobUri);
#pragma warning disable CA1308 // Normalize strings to uppercase
            if (outputContainerUriBuilder.BlobContainerName != outputContainerUriBuilder.BlobContainerName.ToLower(CultureInfo.InvariantCulture))
#pragma warning restore CA1308 // Normalize strings to uppercase
            {
                throw new ArgumentException($"ContainerName {outputContainerUriBuilder.BlobContainerName} must be lowercase.");
            }

            return $"V2-{outputAssetStorageAccountName}-{outputContainerUriBuilder.BlobContainerName}-Output";
        }

        /// <summary>
        /// Generates the output asset name from the local file path to be uploaded.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="outputAssetStorageAccountName">Output asset storage account.</param>
        /// <returns>The output asset name generated.</returns>
        public string GetOutputAssetName(string filePath, string outputAssetStorageAccountName)
        {
            string fileName = Path.GetFileName(filePath).ToLower(CultureInfo.InvariantCulture);
            return $"V2-{outputAssetStorageAccountName}-{fileName}-Output";
        }


        private static string GenerateJobName(string outputAssetName)
        {
            var provider = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
            string uniqueness = Guid.NewGuid().ToString("N", provider).Substring(0, 11);
            return $"{outputAssetName}-{uniqueness}";
        }

        private async Task<IDictionary<string, string>> GetCorrelationDataDictionaryAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary;
            try
            {
                (_, string firstTaskName) = await _mediaServicesV2RestSharp.GetFirstTaskAsync(jobId).ConfigureAwait(false);
                var base64UrlEncodedCorrelationDataJsonString = firstTaskName;
                var correlationDataJsonString = Base64UrlEncoder.Decode(base64UrlEncodedCorrelationDataJsonString);
                correlationDataDictionary = JsonConvert.DeserializeObject<IDictionary<string, string>>(correlationDataJsonString);
            }
            catch (Exception e)
            {
                _log.LogError(e, jobId);
                throw new Exception($"Could not get correlationDataDictionary from {jobId}.", e);
            }

            return correlationDataDictionary;
        }
    }
}
