using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private const string AmsRestApiResource = "https://rest.media.azure.net";
        private readonly ILogger<GetJobById> _log;

        private readonly string _armAmsAccoutGetPath;
        private readonly string _armManagementUrl;
        private readonly object configLock = new object();
        private bool isConfigured = false;

        private IRestClient _restClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobById"/> class.
        /// </summary>
        /// <param name="log">The injected <see cref="ILogger"/>.</param>
        /// <param name="configuration">The injected <see cref="IConfiguration"/>.</param>
        public GetJobById(
            ILogger<GetJobById> log,
            IConfiguration configuration)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));

            var azureSubscriptionId = configuration.GetValue<string>("AZURE_SUBSCRIPTION_ID") ?? throw new Exception("'AZURE_SUBSCRIPTION_ID' app setting is required.");
            _armManagementUrl = configuration.GetValue<string>("ArmManagementUrl") ?? throw new Exception("'ArmManagementUrl' app setting is required.");
            var amsAccountName = configuration.GetValue<string>("AmsAccountName") ?? throw new Exception("'AmsAccountName' app setting is required.");
            var amsResourceGroup = configuration.GetValue<string>("AmsResourceGroup") ?? throw new Exception("'AmsResourceGroup' app setting is required.");
            _armAmsAccoutGetPath = $"/subscriptions/{azureSubscriptionId}/resourceGroups/{amsResourceGroup}/providers/microsoft.media/mediaservices/{amsAccountName}?api-version=2015-10-01";
        }

        /// <summary>
        /// Gets the JobState using the jobId.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <returns>The Job REST response, ref: https://docs.microsoft.com/en-us/rest/api/media/operations/job#job_entity_properties.</returns>
        [FunctionName("GetJobById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
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
                _log.LogError(argumentMsg);
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
                _log.LogError(e, $"Error in {nameof(GetJobById)} for {jobId}.");
                return new BadRequestObjectResult(new { error = e.Message });
            }

            return actionResult;
        }

        /// <summary>
        /// Gets the AMS V2 API endpoint.
        /// </summary>
        /// <returns>The AMS V2 API endpoint.</returns>
        private string GetAmsRestApiEndpoint()
        {
            string amsRestApiEndpoint;
            try
            {
                // Create a rest client for access the Azure Resource Management API Endpoint:
                var restManagementClient = new RestClient(_armManagementUrl);

                // Acquire a token for arm.
                // Ref: https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet#asal
                var azureServiceTokenProvider = new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider();
                string armAccessToken = azureServiceTokenProvider.GetAccessTokenAsync(_armManagementUrl).Result;

                // Add the bearer authentication token to all requests:
                restManagementClient.Authenticator = new JwtAuthenticator(armAccessToken);

                // Enforce a known serializer:
                restManagementClient.UseNewtonsoftJson(
                    new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        ContractResolver = new DefaultContractResolver(),
                    });

                // Add default headers
                restManagementClient.AddDefaultHeader("Accept", $"{ContentType.Json};odata=verbose");
                restManagementClient.AddDefaultHeader("Content-Type", ContentType.Json);

                // GET ams account details:
                var request = new RestRequest(_armAmsAccoutGetPath, Method.GET);
                var restResponse = restManagementClient.Execute<JObject>(request);
                if (!restResponse.IsSuccessful)
                {
                    string expMsg = "Failed to get ams account details.";
                    throw new Exception(expMsg);
                }

                // Parse endpoint information from arm response.
                amsRestApiEndpoint = (string)restResponse.Data.SelectToken("properties.apiEndpoints[0].endpoint");
            }
            catch (Exception e)
            {
                // Just log, let the caller catch the original exception:
                _log.LogError(e, $"Failed in {nameof(GetAmsRestApiEndpoint)}.\nException.Message:\n{e.Message}\nInnerException.Message:\n{e.InnerException?.Message}");
                throw;
            }

            return amsRestApiEndpoint;
        }

        /// <summary>
        /// This should be called before any call to <see cref="IRestClient"/>,
        /// it sets up the auth token and default headers per the AMS V2 API specification.
        /// </summary>
        private void ConfigureRestClient()
        {
            if (!isConfigured)
            {
                Exception exceptionInLock = null;
                lock (configLock)
                {
                    if (!isConfigured)
                    {
                        try
                        {
                            string amsRestApiEndpoint = GetAmsRestApiEndpoint();
                            _restClient = new RestClient(amsRestApiEndpoint);

                            // Acquire a token for AMS.
                            // Ref: https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet#asal
                            var azureServiceTokenProvider = new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider();
                            string amsAccessToken = azureServiceTokenProvider.GetAccessTokenAsync(AmsRestApiResource).Result;

                            _restClient.Authenticator = new JwtAuthenticator(amsAccessToken);

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
                        }
                        catch (Exception e)
                        {
                            exceptionInLock = e;
                            _log.LogError(e, $"Failed in {nameof(ConfigureRestClient)}.\nException.Message:\n{e.Message}\nInnerException.Message:\n{e.InnerException?.Message}");
                        }
                        finally
                        {
                            isConfigured = true;
                        }
                    }
                }

                if (exceptionInLock != null)
                {
                    throw exceptionInLock;
                }
            }

            return;
        }
    }
}
