using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Azure.Core;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Storage.Helper;

namespace Sample.AzFunction.Advanced.Functions.Tests
{
    /// <summary>
    /// Most media projects involve using Azure Storage, this functions demonstrates how to create and use a BlobBaseClient.
    /// This function also serves as a test for role based access control on storage account data.
    /// Usage:
    ///   http://localhost:7071/api/Tests/GetContentLength?blobUri=https%3A%2F%2Fmyaccount.blob.core.windows.net%2Ftest00%2Fbbb.mp4
    /// .
    /// </summary>
    public class GetContentLength
    {
        private readonly TokenCredential _tokenCredential;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetContentLength"/> class.
        /// </summary>
        /// <param name="tokenCredential">TokenCredential from <see cref="ServiceCollectionExtensions"/>.</param>
        public GetContentLength(TokenCredential tokenCredential)
        {
            _tokenCredential = tokenCredential;
        }

        /// <summary>
        /// Gets the content length of a file.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="log">ILogger.</param>
        /// <returns>Content length of blobUri, or -1.</returns>
        [FunctionName("GetContentLength")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Tests/GetContentLength")] HttpRequest req,
            ILogger log)
        {
            // Try to get the blobUri from the request parameters:
            string blobUri = req?.Query["blobUri"];
            blobUri ??= HttpUtility.UrlDecode(blobUri);

            // Replace with the blobUri from the body, if it exists:
            using (var sr = new StreamReader(req?.Body))
            {
                string requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                blobUri ??= data?.blobUri;
            }

            // If we have a blobUri, try to get the ContentLength:
            long contentLength = -1;
            try
            {
                if (Uri.TryCreate(blobUri, UriKind.Absolute, out Uri requestedUri))
                {
                    var blobBaseClient = new BlobBaseClient(requestedUri, _tokenCredential);
                    var props = await blobBaseClient.GetPropertiesAsync().ConfigureAwait(false);
                    contentLength = props.Value.ContentLength;
                }
            }
            catch (Azure.RequestFailedException e) when (e.ErrorCode == "AuthorizationPermissionMismatch")
            {
                return new BadRequestObjectResult(new { error = $"BlobBaseClient.GetPropertiesAsync requires the identity principal to have role 'Storage Blob Data Reader' on resource (file, container, resource-group, or subscription).\n\n\nException.Message:\n\n{e.Message}" });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { error = $"Exception.Message:\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }

            log.LogInformation($"BlobUri: {blobUri}, ContentLength: {contentLength}");

            return new OkObjectResult(new { blobUri, contentLength });
        }
    }
}
