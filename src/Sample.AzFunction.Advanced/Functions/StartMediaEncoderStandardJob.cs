using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaServicesV2.Services.Encoding.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sample.AzFunction.Advanced.Models;

namespace Sample.AzFunction.Advanced.Functions
{
    /// <summary>
    /// Starts a MediaEncoderStandard Job using <see cref="IMediaServicesV2Encoder"/>, which uses RestSharp to call AMS V2 APIs.
    /// </summary>
    public class StartMediaEncoderStandardJob
    {
        private readonly IMediaServicesV2Encoder _mediaServicesV2Encoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartMediaEncoderStandardJob"/> class.
        /// </summary>
        /// <param name="mediaServicesV2Encoder">Manages the Azure Media Services Encoder business logic.</param>
        public StartMediaEncoderStandardJob(
            IMediaServicesV2Encoder mediaServicesV2Encoder)
        {
            _mediaServicesV2Encoder = mediaServicesV2Encoder;
        }

        /// <summary>
        /// Starts a job.
        /// </summary>
        /// <param name="req">The request that should be parsed for parameters.</param>
        /// <param name="log">log.</param>
        /// <returns>JobId.</returns>
        [FunctionName("StartMediaEncoderStandardJob")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string jobId;
            try
            {
                string requestBody;
                using (var sr = new StreamReader(req?.Body))
                {
                    requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                }

                var parameters = JsonConvert.DeserializeObject<EncodingFunctionInputDTO>(requestBody);

                string presetName = parameters?.PresetName ?? throw new Exception("Property 'presetName' is missing in the json body.");
                List<Uri> sourceUris = parameters?.Inputs?.Select(i => i.BlobUri).ToList() ?? throw new Exception("Property 'inputs' is missing in the json body.");
                string outputAssetStorage = parameters?.OutputAssetStorage;

                var operationContext = parameters?.OperationContext;

                jobId = await _mediaServicesV2Encoder.EncodeCreateAsync(
                    presetName,
                    sourceUris,
                    outputAssetStorage,
                    operationContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { error = $"Failed.\n\nException.Message:\n{e.Message}\n\nInnerException.Message:\n{e.InnerException?.Message}" });
            }

            var msg = $"Started: {jobId}";
            log.LogInformation(msg);
            return new OkObjectResult(msg);
        }
    }
}


/*
Curl sample:

    curl -X POST \
                'https://deploymentname.azurewebsites.net/api/StartMediaEncoderStandardJob?code=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX==' \
                -H 'Content-Type: application/json' \
                -H 'cache-control: no-cache' \
                -d '
                    {
                        "presetName" :"SpriteOnlySetting",
                        "inputs": [
                                {
                                    "blobUri": "https://storageacount.blob.core.windows.net/input/BigBuckBunny.mp4"
                                }
                            ],
                        "operationContext": {
                                "test": "mediaServicesV2test01",
                                "processId": 1002
                            }
                    }
                    ' -v
*/
