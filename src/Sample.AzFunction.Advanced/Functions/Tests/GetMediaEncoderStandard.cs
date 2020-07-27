using System;
using System.Threading.Tasks;
using MediaServicesV2.Library.RestSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Sample.AzFunction.Advanced.Functions.Tests
{
    /// <summary>
    /// The rest api libary needs to be able to access Media Services APIs.
    /// This function also serves as a test for role based access control on azure media services.
    /// Usage:
    ///   http://localhost:7071/api/Tests/GetMediaEncoderStandard
    /// .
    /// </summary>
    public class GetMediaEncoderStandard
    {
        private readonly IMediaServicesV2RestSharp _mediaServicesV2RestSharp;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMediaEncoderStandard"/> class.
        /// </summary>
        /// <param name="mediaServicesV2RestSharp">The instance of <see cref="IMediaServicesV2RestSharp"/>.</param>
        public GetMediaEncoderStandard(IMediaServicesV2RestSharp mediaServicesV2RestSharp)
        {
            _mediaServicesV2RestSharp = mediaServicesV2RestSharp;
        }

        /// <summary>
        /// Gets the Media Encoder Standard ProcessorId.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="log">ILogger.</param>
        /// <returns>The processorId.</returns>
#pragma warning disable CA1801
        [FunctionName("GetMediaEncoderStandard")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Tests/GetMediaEncoderStandard")] HttpRequest req,
            ILogger log)
#pragma warning restore CA1801
        {
            var processorName = "Media Encoder Standard";

            string processorId;
            try
            {
                processorId = await _mediaServicesV2RestSharp.GetLatestMediaProcessorAsync(processorName).ConfigureAwait(false);
            }
            catch (Azure.RequestFailedException e) when (e.ErrorCode == "AuthorizationPermissionMismatch")
            {
                return new BadRequestObjectResult(new { error = $"{nameof(GetMediaEncoderStandard)} requires the identity principal to have appropriate rights.\n\n\nException.Message:\n\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { error = $"Exception.Message:\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }

            log.LogInformation($"processorName: {processorName}, processorId: {processorId}");

            return new OkObjectResult(new { processorName, processorId });
        }
    }
}
