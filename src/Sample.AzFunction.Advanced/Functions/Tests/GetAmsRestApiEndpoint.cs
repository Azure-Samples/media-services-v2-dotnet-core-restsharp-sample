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
    /// The rest api libary needs to be able to determine the api endpoint, which will vary based on region.
    /// This function also serves as a test for role based access control on azure resource manager.
    /// Usage:
    ///   http://localhost:7071/api/Tests/GetAmsRestApiEndpoint
    /// .
    /// </summary>
    public class GetAmsRestApiEndpoint
    {
        private readonly IMediaServicesV2RestSharp _mediaServicesV2RestSharp;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAmsRestApiEndpoint"/> class.
        /// </summary>
        /// <param name="mediaServicesV2RestSharp">The instance of <see cref="IMediaServicesV2RestSharp"/>.</param>
        public GetAmsRestApiEndpoint(IMediaServicesV2RestSharp mediaServicesV2RestSharp)
        {
            _mediaServicesV2RestSharp = mediaServicesV2RestSharp;
        }

        /// <summary>
        /// Gets the AmsRestApiEndpoint.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="log">ILogger.</param>
        /// <returns>The AmsRestApiEndpoint.</returns>
#pragma warning disable CA1801
        [FunctionName("GetAmsRestApiEndpoint")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Tests/GetAmsRestApiEndpoint")] HttpRequest req,
            ILogger log)
#pragma warning restore CA1801
        {
            string amsRestApiEndpoint;
            try
            {
                amsRestApiEndpoint = _mediaServicesV2RestSharp.GetAmsRestApiEndpoint();
            }
            catch (Azure.RequestFailedException e) when (e.ErrorCode == "AuthorizationPermissionMismatch")
            {
                return new BadRequestObjectResult(new { error = $"{nameof(GetAmsRestApiEndpoint)} requires the identity principal to have appropriate rights.\n\n\nException.Message:\n\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { error = $"Exception.Message:\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }

            log.LogInformation($"amsRestApiEndpoint: {amsRestApiEndpoint}");

            return await Task.FromResult(new OkObjectResult(new { amsRestApiEndpoint })).ConfigureAwait(false);
        }
    }
}
