using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;
using RestSharp.Serializers.NewtonsoftJson;

namespace Sample.AzFunction.HelloWorld
{
    /// <summary>
    /// Uses Media Services V2 API via RestSharp directly.
    /// Usage:
    ///   http://localhost:7071/api/GetJobById?id=nb:jid:UUID:f2781a3a-0d00-a813-4e02-f1eab4bb5972
    ///   or
    ///   http://localhost:7071/api/GetJobById
    ///   with a body of: { "id"="nb:jid:UUID:f2781a3a-0d00-a813-4e02-f1eab4bb5972" }
    /// Ref:
    ///   https://docs.microsoft.com/en-us/rest/api/media/operations/azure-media-services-rest-api-reference
    ///   https://docs.microsoft.com/en-us/azure/media-services/previous/media-services-rest-connect-with-aad
    ///
    /// This example demonstrates how to use RestSharp with AMS V2 directly by doing a GET on the /Jobs('{jobId}') API endpoint.
    /// For an example that uses a service, see advanced sample project.
    /// </summary>
    public class GetJobById
    {
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _tokenCredential;

        private readonly IRestClient _restClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobById"/> class.
        /// </summary>
        /// <param name="configuration">The injected <see cref="IConfiguration"/>.</param>
        /// <param name="tokenCredential">The injected <see cref="TokenCredential"/>.</param>
        public GetJobById(
            IConfiguration configuration,
            TokenCredential tokenCredential)
        {
            _configuration = configuration;
            _tokenCredential = tokenCredential;

            var amsAccountName = configuration.GetValue<string>("AmsAccountName") ?? throw new Exception("'AmsAccountName' app setting is required.");
            var amsLocation = configuration.GetValue<string>("AmsLocation") ?? throw new Exception("'AmsLocation' app setting is required.");
            var baseUrl = $"https://{amsAccountName}.restv2.{amsLocation}.media.azure.net/api/";
            _restClient = new RestClient(baseUrl);
        }

        /// <summary>
        /// Gets the JobState using the jobId.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="log">ILogger.</param>
        /// <returns>The Job REST response, ref: https://docs.microsoft.com/en-us/rest/api/media/operations/job#job_entity_properties.</returns>
        [FunctionName("GetJobById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Try to get the jobId from the request parameters:
            string jobId = req?.Query["id"];
            jobId = HttpUtility.UrlDecode(jobId);

            // Replace with the jobId from the body, if it exists:
            using (var sr = new StreamReader(req?.Body))
            {
                string requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                jobId ??= data?.id;
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                var argumentMsg = $"Error in {nameof(GetJobById)}, job id must be in the body or as a query param.";
                log.LogError(argumentMsg);
                return new BadRequestObjectResult(new { error = argumentMsg });
            }

            ActionResult actionResult;
            try
            {
                ConfigureRestClient();

                var request = new RestRequest($"Jobs('{jobId}')", Method.GET);
                var response = _restClient.Execute(request);
                var statusCode = response.StatusCode;
                var content = response.Content;

                if (response.IsSuccessful)
                {
                    actionResult = new OkObjectResult(content);
                }
                else
                {
                    actionResult = new BadRequestObjectResult(new { error = content });
                }
            }
            catch (Exception e)
            {
                log.LogError(e, $"Error in {nameof(GetJobById)} for {jobId}.");
                return new BadRequestObjectResult(new { error = e.Message });
            }

            return actionResult;
        }

        /// <summary>
        /// This should be called before any call to <see cref="IRestClient"/>,
        /// it sets up the auth token and default headers per the AMS V2 API specification.
        /// </summary>
        private void ConfigureRestClient()
        {
            var amsAccessToken = _tokenCredential.GetToken(
                new TokenRequestContext(
                    scopes: new[] { "https://rest.media.azure.net/.default" },
                    parentRequestId: null),
                default);

            _restClient.Authenticator = new JwtAuthenticator(amsAccessToken.Token);

            _restClient.UseNewtonsoftJson(
                new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver(),
                });

            _restClient.AddDefaultHeader("Content-Type", $"{ContentType.Json};odata=verbose");
            _restClient.AddDefaultHeader("Accept", $"{ContentType.Json};odata=verbose");
            _restClient.AddDefaultHeader("DataServiceVersion", "3.0");
            _restClient.AddDefaultHeader("MaxDataServiceVersion", "3.0");
            _restClient.AddDefaultHeader("x-ms-version", "2.19");

            return;
        }
    }
}
