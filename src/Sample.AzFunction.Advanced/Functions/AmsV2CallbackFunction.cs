using System;
using System.IO;
using System.Threading.Tasks;
using MediaServicesV2.Services.Encoding.Models;
using MediaServicesV2.Services.Encoding.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Sample.AzFunction.Advanced.Functions
{
    /// <summary>
    /// AmsV2CallbackFunction get the job status from Azure Media Services V2.
    /// </summary>
    public class AmsV2CallbackFunction
    {
        private readonly ILogger<AmsV2CallbackFunction> _logger;
        private readonly IMediaServicesV2Encoder _mediaServicesV2Encoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmsV2CallbackFunction"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="mediaServicesV2Encoder">mediaServicesV2Encoder.</param>
        public AmsV2CallbackFunction(
            ILogger<AmsV2CallbackFunction> logger,
            IMediaServicesV2Encoder mediaServicesV2Encoder)
        {
            _logger = logger;
            _mediaServicesV2Encoder = mediaServicesV2Encoder;
        }

        /// <summary>
        /// The webhook used for AMS V2 notifications.
        /// </summary>
        /// <param name="req">web request.</param>
        /// <returns>web response.</returns>
        [FunctionName("AmsV2Callback")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req)
        {
            MediaServicesV2NotificationMessage notificationMessage;
            try
            {
                string requestBody;
                using (var sr = new StreamReader(req?.Body))
                {
                    requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                }

                if (req.Headers.TryGetValue("ms-signature", out _))
                {
                    // TODO: Could verify ms-signature here.
                    notificationMessage = JsonConvert.DeserializeObject<MediaServicesV2NotificationMessage>(requestBody);
                    _logger.LogInformation("parsed notification message");
                }
                else
                {
                    var errorMsg = "VerifyWebHookRequestSignature failed.";
                    _logger.LogError(errorMsg);
                    return new BadRequestObjectResult(new { error = errorMsg });
                }
            }
            catch (Exception e)
            {
                var exceptionMsg = "Failed to extract MediaServicesV2NotificationMessage.";
                _logger.LogError(exceptionMsg, e);
                return new BadRequestObjectResult(new { error = exceptionMsg });
            }

            if (notificationMessage != null)
            {
                var (jobId, status) = await _mediaServicesV2Encoder.HandleNotificationAsync(notificationMessage).ConfigureAwait(false);
                _logger.LogInformation($"{jobId} {status}");
            }

            return new OkResult();
        }
    }
}