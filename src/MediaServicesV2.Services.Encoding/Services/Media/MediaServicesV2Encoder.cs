using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaServicesV2.Services.Encoding.Models;
using MediaServicesV2.Services.Encoding.Presets;
using MediaServicesV2.Services.Encoding.Services.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace MediaServicesV2.Services.Encoding.Media
{
    /// <summary>
    /// Manages the Azure Media Services Encoder business logic.
    /// </summary>
    public class MediaServicesV2Encoder : IMediaServicesV2Encoder
    {
        private readonly IMediaServicesV2EncodeOperations _mediaServicesV2EncodeOperations;
        private readonly IMediaServicesPreset _mediaServicesPreset;
        private readonly ILogger<MediaServicesV2Encoder> _log;
        private readonly Uri _callbackEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2Encoder"/> class.
        /// </summary>
        /// <remarks>
        /// This encoder will takes into consideration the input and ouput storage accounts.
        /// It first creates a new asset in the same account as the SourceUris, and copies the files into that asset.
        /// When requesting the job, it will ask for an output asset in the same account at OutputContainer.
        /// The error returned from AMS V2 when asking for an output asset in a storage account which is not linked
        /// with the AMS instance, is an error message regarding a missing record.  If you get such an error,
        /// first check that the requested OutputContainer is a linked storage account.
        /// </remarks>
        /// <param name="log">ILogger log.</param>
        /// <param name="mediaServicesV2EncodeOperations"><see cref="IMediaServicesV2EncodeOperations"/>.</param>
        /// <param name="mediaServicesPreset"><see cref="IMediaServicesPreset"/>To provide presets.</param>
        /// <param name="configuration">IConfiguration.</param>
        public MediaServicesV2Encoder(
            ILogger<MediaServicesV2Encoder> log,
            IMediaServicesV2EncodeOperations mediaServicesV2EncodeOperations,
            IMediaServicesPreset mediaServicesPreset,
            IConfiguration configuration)
        {
            _log = log;
            _mediaServicesV2EncodeOperations = mediaServicesV2EncodeOperations;
            _mediaServicesPreset = mediaServicesPreset;

            // Get the optional AmsV2CallbackEndpoint:
            var amsV2CallbackEndpoint = configuration.GetValue<string>("AmsV2CallbackEndpoint");
            Uri.TryCreate(amsV2CallbackEndpoint, UriKind.Absolute, out _callbackEndpoint);
        }

        /// <inheritdoc/>
        public async Task<string> EncodeCreateAsync(string presetName, List<Uri> sourceUris, string outputAssetStorageAccountName = null, JObject operationContext = null)
        {
            _ = presetName ?? throw new ArgumentNullException(nameof(presetName));
            _ = sourceUris ?? throw new ArgumentNullException(nameof(sourceUris));

            string inputAssetId = await _mediaServicesV2EncodeOperations.CopyFilesIntoNewAsset(sourceUris).ConfigureAwait(false);

            var preset = _mediaServicesPreset.GetPresetForPresetName(presetName, null);
            string outputAssetName = _mediaServicesV2EncodeOperations.GetOutputAssetName(sourceUris.First(), outputAssetStorageAccountName);

            string jobId;
            try
            {
                Dictionary<string, string> correlationData = null;
                if (operationContext != null)
                {
                    correlationData = new Dictionary<string, string>()
                     {
                            { "operationContext", operationContext.ToString() },
                     };
                }

                jobId = await _mediaServicesV2EncodeOperations.SubmitMesJobAsync(
                    inputAssetId,
                    preset,
                    outputAssetName,
                    outputAssetStorageAccountName,
                    _callbackEndpoint,
                    correlationData).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create job for {presetName}.", e);
            }

            _log.LogInformation($"{nameof(_mediaServicesV2EncodeOperations.SubmitMesJobAsync)} called.", jobId);

            return jobId;
        }


        /// <inheritdoc/>
        public async Task<string> UploadAndEncodeCreateAsync(string presetName, List<string> filesToUpload, string outputAssetStorageAccountName = null, JObject operationContext = null)
        {
            _ = presetName ?? throw new ArgumentNullException(nameof(presetName));
            _ = filesToUpload ?? throw new ArgumentNullException(nameof(filesToUpload));

            string inputAssetId = await _mediaServicesV2EncodeOperations.UploadFilesIntoNewAsset(filesToUpload).ConfigureAwait(false);

            var preset = _mediaServicesPreset.GetPresetForPresetName(presetName, null);
            string outputAssetName = _mediaServicesV2EncodeOperations.GetOutputAssetName(filesToUpload.First(), outputAssetStorageAccountName);

            string jobId;
            try
            {
                Dictionary<string, string> correlationData = null;
                if (operationContext != null)
                {
                    correlationData = new Dictionary<string, string>()
                     {
                            { "operationContext", operationContext.ToString() },
                     };
                }

                jobId = await _mediaServicesV2EncodeOperations.SubmitMesJobAsync(
                    inputAssetId,
                    preset,
                    outputAssetName,
                    outputAssetStorageAccountName,
                    _callbackEndpoint,
                    correlationData).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create job for {presetName}.", e);
            }

            _log.LogInformation($"{nameof(_mediaServicesV2EncodeOperations.SubmitMesJobAsync)} called.", jobId);

            return jobId;
        }

        /// <inheritdoc/>
        public async Task<(string JobId, string Status)> HandleNotificationAsync(MediaServicesV2NotificationMessage notificationMessage)
        {
            _ = notificationMessage ?? throw new ArgumentNullException(nameof(notificationMessage));

            var jobId = notificationMessage.Properties["jobId"];
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("notificationMessage.jobId is invalid");
            }

            string newState;
            try
            {
                switch (notificationMessage.EventType)
                {
                    case MediaServicesV2NotificationEventType.TaskStateChange:
                        newState = await ProcessTaskStateChangeAsync(notificationMessage, jobId).ConfigureAwait(false);
                        break;
                    case MediaServicesV2NotificationEventType.TaskProgress:
                        newState = $"Progress:{GetEncodeProgress(notificationMessage)}";
                        break;
                    default:
                        _log.LogInformation(notificationMessage.EventType.ToString());
                        throw new NotSupportedException($"Unsupported AMSv2 event type {notificationMessage.EventType}");
                }
            }
            catch (Exception e)
            {
                var msg = $"Failed while parsing notifiation message for {jobId}.";
                _log.LogError(msg, e);
                throw new Exception(msg, e);
            }

            return (jobId, newState);
        }

        /// <summary>
        /// Get the progress.
        /// </summary>
        /// <param name="notificationMessage">MediaServicesV2NotificationMessage.</param>
        /// <returns>Progress percentage.</returns>
        private static int GetEncodeProgress(MediaServicesV2NotificationMessage notificationMessage)
        {
            _ = notificationMessage ?? throw new ArgumentNullException(nameof(notificationMessage));

            if (!notificationMessage.Properties.TryGetValue("lastComputedProgress", out string progressString))
            {
                throw new Exception("Could not find LastComputedProgress property in notification message");
            }

            if (!int.TryParse(progressString, out int progress))
            {
                throw new Exception("Could not parse progress from message");
            }

            return progress;
        }

        /// <summary>
        /// Processes the task state change.
        /// </summary>
        /// <param name="notificationMessage">MediaServicesV2NotificationMessage.</param>
        /// <param name="jobId">The id of the job.</param>
        /// <returns>Successfully published message.</returns>
        private async Task<string> ProcessTaskStateChangeAsync(MediaServicesV2NotificationMessage notificationMessage, string jobId)
        {
            _ = notificationMessage ?? throw new ArgumentNullException(nameof(notificationMessage));

            if (!notificationMessage.Properties.TryGetValue("newState", out string newState))
            {
                throw new Exception("Could not find NewState property in notification message");
            }

            if (newState.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                newState.Equals("Processing", StringComparison.OrdinalIgnoreCase))
            {
                return newState;
            }

            if (newState.Equals("Finished", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var destinationUris = await _mediaServicesV2EncodeOperations.CopyOutputAssetToOutputContainerAsync(jobId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed while CopyOutputAssetToOutputContainerAsync", e);
                }

                try
                {
                    await _mediaServicesV2EncodeOperations.DeleteAssetsForJobAsync(jobId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed while DeleteAssetsForV2JobAsync", e);
                }

                return newState;
            }

            if (newState.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                await _mediaServicesV2EncodeOperations.DeleteAssetsForJobAsync(jobId).ConfigureAwait(false);
                throw new Exception("Encode Job Failed");
            }

            if (newState.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
            {
                await _mediaServicesV2EncodeOperations.DeleteAssetsForJobAsync(jobId).ConfigureAwait(false);
                return newState;
            }

            throw new NotSupportedException($"Unknown newState:{newState}");
        }
    }
}