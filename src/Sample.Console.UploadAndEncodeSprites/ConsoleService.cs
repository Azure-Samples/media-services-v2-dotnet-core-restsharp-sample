using System.Collections.Generic;
using System.Threading.Tasks;
using MediaServicesV2.Library.RestSharp;
using MediaServicesV2.Library.RestSharp.Models;
using MediaServicesV2.Services.Encoding.Services.Media;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Sample.Console.UploadAndEncodeSprites
{
    /// <summary>
    /// Console service implementation.
    /// </summary>
    public class ConsoleService : IConsoleService
    {
        private readonly IMediaServicesV2Encoder _mediaServicesV2Encoder;
        private readonly IMediaServicesV2RestSharp _mediaServicesV2RestSharp;
        private readonly ILogger<ConsoleService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleService"/> class.
        /// </summary>
        /// <param name="mediaServicesV2Encoder">Encoder object.</param>
        /// <param name="mediaServicesV2RestSharp">AMSv2 rest shap object.</param>
        /// <param name="logger">Logger.</param>
        public ConsoleService(IMediaServicesV2Encoder mediaServicesV2Encoder, IMediaServicesV2RestSharp mediaServicesV2RestSharp, ILogger<ConsoleService> logger)
        {
            _mediaServicesV2Encoder = mediaServicesV2Encoder;
            _mediaServicesV2RestSharp = mediaServicesV2RestSharp;
            _logger = logger;
        }

        /// <summary>
        /// Method that uploads and encodes a list of local files.
        /// </summary>
        /// <param name="sourceFiles">List of source files.</param>
        /// <returns>The job id.</returns>
        public async Task<string> UploadAndEncodeAsync(List<string> sourceFiles)
        {
            bool mediaUnitChanged = false;

            // Meanwhile, a hard-coded example:
            string presetName = "Sprites";

            var operationContext = new JObject()
                    {
                        new JProperty("someKey1", "That you need during job status or after completion."),
                        new JProperty("someKey2", "It will be encoded into the first Task name, so it can be easily parsed later."),
                    };

            // let's get media reserved unit info
            var mediaU = await _mediaServicesV2RestSharp.GetEncodingReservedUnitTypeAsync().ConfigureAwait(false);

            if (mediaU.CurrentReservedUnits == 0)
            {
                _logger.LogInformation("There is no media reserved unit. Let's provision one S2 unit...");

                mediaU.CurrentReservedUnits = 1;
                mediaU.ReservedUnitType = ReservedUnitType.S2;
                await _mediaServicesV2RestSharp.UpdateEncodingReservedUnitTypeAsync(mediaU).ConfigureAwait(false);
                mediaUnitChanged = true;
            }

            _logger.LogInformation("Starting upload and encoding...");

            // let's upload an encode the file
            var jobId = await _mediaServicesV2Encoder.UploadAndEncodeCreateAsync(
                presetName,
                sourceFiles,
                null,
                operationContext).ConfigureAwait(false);

            _logger.LogInformation("Job submitted. Monitoring it...");

            const int SleepInterval = 10 * 1000;
            bool exit = false;
            do
            {
                var jobInfo = await _mediaServicesV2RestSharp.GetJobInfoAsync(jobId).ConfigureAwait(false);

                if (jobInfo.State == JobState.Finished || jobInfo.State == JobState.Error || jobInfo.State == JobState.Canceled)
                {
                    exit = true;
                }
                else
                {
                    System.Threading.Thread.Sleep(SleepInterval);
                }
                _logger.LogInformation($"Job state is {jobInfo.State}.");
            }
            while (!exit);

            if (mediaUnitChanged)
            {
                _logger.LogInformation("Let's revert back to 0 S1 unit...");

                mediaU.CurrentReservedUnits = 0;
                mediaU.ReservedUnitType = ReservedUnitType.S1;
                await _mediaServicesV2RestSharp.UpdateEncodingReservedUnitTypeAsync(mediaU).ConfigureAwait(false);
            }

            _logger.LogInformation("Work done !");

            return jobId;
        }
    }
}