using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Storage.Helper
{
    /// <summary>
    /// Provides access to storage operations.
    /// </summary>
    public class AzureStorageOperations : IAzureStorageOperations
    {
        private readonly TimeSpan _blobCopyTimeout = TimeSpan.FromHours(4.0);

        private readonly TokenCredential _tokenCredential;
        private readonly ILogger<AzureStorageOperations> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageOperations"/> class.
        /// </summary>
        /// <param name="tokenCredential">tokenCredential.</param>
        /// <param name="log">log.</param>
        public AzureStorageOperations(
            TokenCredential tokenCredential,
            ILogger<AzureStorageOperations> log)
        {
            _tokenCredential = tokenCredential;
            _log = log;
        }

        /// <inheritdoc/>
        public async Task<CopyFromUriOperation> BlobCopyAsync(Uri sourceUri, Uri destinationUri)
        {
            _ = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
            _ = destinationUri ?? throw new ArgumentNullException(nameof(destinationUri));

            var destinationBlobBaseClient = new BlobBaseClient(destinationUri, _tokenCredential);

            CopyFromUriOperation blobCopyInfo;
            try
            {
                // 1. Get SAS for Source
                string sasUri = await GetSasUrlAsync(sourceUri, _blobCopyTimeout).ConfigureAwait(false);

                // 2. Copy Async to Dest
                blobCopyInfo = await destinationBlobBaseClient.StartCopyFromUriAsync(new Uri(sasUri)).ConfigureAwait(false);
            }
            catch (Azure.RequestFailedException e) when (e.ErrorCode == "AuthorizationPermissionMismatch")
            {
                var msg = $"BlobBaseClient.StartCopyFromUriAsync requires the identity principal to have role 'Storage Blob Data Contributor' on resource (file, container, resource-group, or subscription).  " +
                          $"Could not copy blob from {sourceUri} to {destinationUri}.";
                _log.LogError(e, msg);
                throw new Exception(msg, e);
            }
            catch (Exception e)
            {
                var msg = $"Could not copy blob from {sourceUri} to {destinationUri}.";
                _log.LogError(e, msg);
                throw new Exception(msg, e);
            }

            return blobCopyInfo;
        }

        /// <inheritdoc/>
        public async Task<Response<BlobContentInfo>> BlobUploadAsync(string filePath, Uri destinationUri)
        {
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _ = destinationUri ?? throw new ArgumentNullException(nameof(destinationUri));

            BlobUriBuilder uriBuilder = new BlobUriBuilder(destinationUri)
            {
                BlobName = Path.GetFileName(filePath)
            };

            var destinationBlobBaseClient = new BlobClient(uriBuilder.ToUri(), _tokenCredential);

            Response<BlobContentInfo> blobContentInfo;
            try
            {
                _log.LogInformation("Uploading to Blob storage as blob:\n\t {0}\n", filePath);
                blobContentInfo = await destinationBlobBaseClient.UploadAsync(filePath, true);
            }
            catch (Azure.RequestFailedException e) when (e.ErrorCode == "AuthorizationPermissionMismatch")
            {
                var msg = $"BlobBaseClient.StartCopyFromUriAsync requires the identity principal to have role 'Storage Blob Data Contributor' on resource (file, container, resource-group, or subscription).  " +
                          $"Could not upload blob from {filePath} to {destinationUri}.";
                _log.LogError(e, msg);
                throw new Exception(msg, e);
            }
            catch (Exception e)
            {
                var msg = $"Could not upload blob from {filePath} to {destinationUri}.";
                _log.LogError(e, msg);
                throw new Exception(msg, e);
            }

            return blobContentInfo;
        }


        /// <inheritdoc/>
        public async Task<bool> BlobExistsAsync(Uri blobUri)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            var destinationBlobBaseClient = new BlobBaseClient(blobUri, _tokenCredential);
            return await destinationBlobBaseClient.ExistsAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<string> GetSasUrlAsync(Uri blobUri, TimeSpan ttl)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));

            try
            {
                var blobUriBuilder = new BlobUriBuilder(blobUri);

                // Create a SAS token that's valid for the TimeSpan, plus a back-off start for clock skew.
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobUriBuilder.BlobContainerName,
                    BlobName = blobUriBuilder.BlobName,
                    Resource = "b", // "b" is for blob
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow + ttl,
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read); // read permissions only for the SAS.

                var blobServiceClient = new BlobServiceClient(new UriBuilder(blobUriBuilder.Scheme, blobUriBuilder.Host).Uri, _tokenCredential, null);

                var userDelegation = (await blobServiceClient.GetUserDelegationKeyAsync(sasBuilder.StartsOn, sasBuilder.ExpiresOn).ConfigureAwait(false))?.Value;

                if (userDelegation == null)
                {
                    var msg = $@"Unable to get a user delegation key from the Storage service for blob {blobUri}";
                    _log.LogError(msg);
                    throw new Exception(msg);
                }

                var sasToken = sasBuilder.ToSasQueryParameters(userDelegation, blobUriBuilder.AccountName);
                blobUriBuilder.Sas = sasToken;

                // Construct the full URI, including the SAS token.
                return blobUriBuilder.ToUri().ToString();
            }
            catch (RequestFailedException e)
            {
                var msg = $@"Unable to get a user delegation key from the Storage service for blob {blobUri}";
                _log.LogError(msg);
                throw new Exception(msg, e);
            }
            catch (Exception e)
            {
                var msg = $@"Failed to generate the SAS url for blob {blobUri}";
                _log.LogError(msg);
                throw new Exception(msg, e);
            }
        }
    }
}
