using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaServicesV2.Services.Encoding.Models;
using Newtonsoft.Json.Linq;

namespace MediaServicesV2.Services.Encoding.Services.Media
{
    /// <summary>
    /// Manages the Azure Media Services Encoder business logic.
    /// </summary>
    public interface IMediaServicesV2Encoder
    {
        /// <summary>
        /// Manages the Azure Media Services Encoder business logic.
        /// </summary>
        /// <param name="presetName">presetName to be used as-is, or replaced with a preset.</param>
        /// <param name="sourceUris">sourceUris to be copied into an Asset.</param>
        /// <param name="outputAssetStorageAccountName">Storage account of the ouput asset.</param>
        /// <param name="operationContext">operationContext, an opaque blob of data to encoded into the task name.</param>
        /// <returns>Job id.</returns>
        public Task<string> EncodeCreateAsync(string presetName, List<Uri> sourceUris, string outputAssetStorageAccountName = null, JObject operationContext = null);

        /// <summary>
        /// Upload and encode the uploaded file.
        /// </summary>
        /// <param name="presetName">presetName to be used as-is, or replaced with a preset.</param>
        /// <param name="filesToUpload">List of files to upload.</param>
        /// <param name="outputAssetStorageAccountName">Storage account of the ouput asset.</param>
        /// <param name="operationContext">operationContext, an opaque blob of data to encoded into the task name.</param>
        /// <returns>The job id.</returns>
        public Task<string> UploadAndEncodeCreateAsync(string presetName, List<string> filesToUpload, string outputAssetStorageAccountName = null, JObject operationContext = null);

        /// <summary>
        /// Executes the business logic surounding the reception of a notification message.
        /// </summary>
        /// <param name="notificationMessage">The <see cref="MediaServicesV2NotificationMessage"/> to handle.</param>
        /// <returns>string JobId, string Status.</returns>
        public Task<(string JobId, string Status)> HandleNotificationAsync(MediaServicesV2NotificationMessage notificationMessage);
    }
}