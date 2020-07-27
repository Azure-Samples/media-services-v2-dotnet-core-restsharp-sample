using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaServicesV2.Library.RestSharp.Models;

namespace MediaServicesV2.Library.RestSharp
{
    /// <summary>
    /// Azure Media Service V2 API wrapper.
    /// This wrapper does not seek to replicate the .NetFramework SDK project.
    /// It provides a subset of the API, and can be extended, or could be updated to return models instead of Tuples.
    /// </summary>
    public interface IMediaServicesV2RestSharp
    {
        /// <summary>
        /// Creates an empty asset in the requested storage account.
        /// </summary>
        /// <param name="assetName">The desired assetName.</param>
        /// <param name="accountName">The desired accountName.</param>
        /// <returns>string AssetId, Uri AssetUri.</returns>
        Task<(string AssetId, Uri AssetUri)> CreateEmptyAssetAsync(string assetName, string accountName);

        /// <summary>
        /// GetAssetNameAndUriAsync.
        /// </summary>
        /// <param name="assetId">assetId.</param>
        /// <returns>string AssetName, Uri AssetUri.</returns>
        Task<(string AssetName, Uri AssetUri)> GetAssetNameAndUriAsync(string assetId);

        /// <summary>
        /// CreateFileInfosAsync.
        /// </summary>
        /// <param name="assetId">assetId.</param>
        /// <returns>void Task.</returns>
        Task CreateFileInfosAsync(string assetId);

        /// <summary>
        /// GetLatestMediaProcessorAsync.
        /// </summary>
        /// <param name="mediaProcessorName">mediaProcessorName.</param>
        /// <returns>mediaProcessorId.</returns>
        Task<string> GetLatestMediaProcessorAsync(string mediaProcessorName);

        /// <summary>
        /// GetEncodingReservedUnitTypeAsync.
        /// </summary>
        /// <returns>Info object about reserved units.</returns>
        Task<EncodingReservedUnitTypeInfo> GetEncodingReservedUnitTypeAsync();

        /// <summary>
        /// Get information of a specific job.
        /// </summary>
        /// <param name="jobId">The Job Id.</param>
        /// <returns>JobInfo object.</returns>
        Task<JobInfo> GetJobInfoAsync(string jobId);

        /// <summary>
        /// Updates encoding reserved units.
        /// </summary>
        /// <param name="data">Encoding reserved unit data.</param>
        /// <returns>True if successful.</returns>
        Task<bool> UpdateEncodingReservedUnitTypeAsync(EncodingReservedUnitTypeInfo data);

        /// <summary>
        /// Starts a job.
        /// </summary>
        /// <param name="jobName">jobName.</param>
        /// <param name="processorId">processorId.</param>
        /// <param name="inputAssetId">inputAssetId.</param>
        /// <param name="preset">preset.</param>
        /// <param name="outputAssetName">outputAssetName.</param>
        /// <param name="outputAssetStorageAccountName">outputAssetStorageAccountName.</param>
        /// <param name="correlationData">correlationData.</param>
        /// <param name="notificationEndPointId">notificationEndPointId.</param>
        /// <returns>jobId.</returns>
        Task<string> CreateJobAsync(
            string jobName,
            string processorId,
            string inputAssetId,
            string preset,
            string outputAssetName,
            string outputAssetStorageAccountName,
            string correlationData,
            string notificationEndPointId);

        /// <summary>
        /// Creates or updates the notification endpoint used as a webhook for task state changes.
        /// </summary>
        /// <param name="notificationEndPointName">The name to use.</param>
        /// <param name="callbackEndpoint">The azure function endpoint, that calls DefaultMediaServicesV2CallbackService.</param>
        /// <returns>notificationEndPointId.</returns>
        Task<string> GetOrCreateNotificationEndPointAsync(
            string notificationEndPointName,
            Uri callbackEndpoint);

        /// <summary>
        /// Gets the AssetFileNames in an Asset.
        /// </summary>
        /// <param name="assetId">assetId.</param>
        /// <returns>An enumerable of file paths.</returns>
        Task<IEnumerable<string>> GetAssetFilesNames(string assetId);

        /// <summary>
        /// Gets the Id and Name of the first task in a job.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>string FirstTaskId, string FirstTaskName.</returns>
        Task<(string FirstTaskId, string FirstTaskName)> GetFirstTaskAsync(string jobId);

        /// <summary>
        /// Gets the Id and Name of the first input asset in a job.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>string FirstInputAssetId, string FirstInputAssetName.</returns>
        Task<(string FirstInputAssetId, string FirstInputAssetName)> GetFirstInputAssetAsync(string jobId);

        /// <summary>
        /// Gets the Id and Name of the first output asset in a job.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>string FirstOutputAssetId, string FirstOutputAssetName.</returns>
        Task<(string FirstOutputAssetId, string FirstOutputAssetName)> GetFirstOutputAssetAsync(string jobId);

        /// <summary>
        /// Deletes an asset by id.
        /// </summary>
        /// <param name="assetId">assetId.</param>
        /// <returns>void Task.</returns>
        Task DeleteAssetAsync(string assetId);

        /// <summary>
        /// Gets the AMS V2 API endpoint.
        /// </summary>
        /// <returns>The AMS V2 API endpoint.</returns>
        string GetAmsRestApiEndpoint();
    }
}