using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using MediaServicesV2.Library.RestSharp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;
using RestSharp.Serializers.NewtonsoftJson;

namespace MediaServicesV2.Library.RestSharp
{
    /// <summary>
    /// Azure Media Service client provider.
    /// Ref:
    ///   https://docs.microsoft.com/en-us/rest/api/media/operations/azure-media-services-rest-api-reference
    ///   https://docs.microsoft.com/en-us/azure/media-services/previous/media-services-rest-connect-with-aad
    /// .
    /// </summary>
    public class MediaServicesV2RestSharp : IMediaServicesV2RestSharp
    {
        private readonly ILogger<MediaServicesV2RestSharp> _log;
        private readonly TokenCredential _tokenCredential;
        private readonly IRestClient _restClient;
        private readonly object configLock = new object();
        private bool isConfigured = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2RestSharp"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="tokenCredential">TokenCredential.</param>
        /// <param name="configuration">Configuration.</param>
        public MediaServicesV2RestSharp(
            ILogger<MediaServicesV2RestSharp> log,
            TokenCredential tokenCredential,
            IConfiguration configuration)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _tokenCredential = tokenCredential;

            var amsAccountName = configuration.GetValue<string>("AmsAccountName") ?? throw new Exception("'AmsAccountName' app setting is required.");
            var amsLocation = configuration.GetValue<string>("AmsLocation") ?? throw new Exception("'AmsLocation' app setting is required.");
            var baseUrl = configuration.GetValue<string>("AmsRestApiEndpoint") ?? throw new Exception("'AmsRestApiEndpoint' app setting is required.");
            _restClient = new RestClient(baseUrl);
        }

        /// <inheritdoc/>
        public async Task<string> GetOrCreateNotificationEndPointAsync(string notificationEndPointName, Uri callbackEndpoint)
        {
            ConfigureRestClient();

            // Try to get existing notification endpoint:
            var getRequest = new RestRequest($"NotificationEndPoints", Method.GET);
            var getResponse = await _restClient.ExecuteAsync<JObject>(getRequest, cancellationToken: default).ConfigureAwait(false);
            if (getResponse.IsSuccessful)
            {
                var existingNotificationEndPoint = getResponse.Data["d"]["results"]?.Where(r =>
                    ((string)r["Name"])?.ToUpperInvariant() == notificationEndPointName.ToUpperInvariant() &&
                    ((string)r["EndPointAddress"])?.ToUpperInvariant() == callbackEndpoint.ToString().ToUpperInvariant()).FirstOrDefault();

                var existingNotificationEndPointId = (string)existingNotificationEndPoint?.SelectToken("Id", false);
                if (!string.IsNullOrWhiteSpace(existingNotificationEndPointId))
                {
                    // We have a match, no need to create one.
                    return existingNotificationEndPointId;
                }
            }

            // Create a new notification endpoint:
            var request = new RestRequest($"NotificationEndPoints", Method.POST);

            JObject jsonBody = new JObject()
            {
                new JProperty("Name", notificationEndPointName),
                new JProperty("EndPointAddress", callbackEndpoint),
                new JProperty("EndPointType", 3),
                new JProperty("CredentialType", 0),
                new JProperty("ProtectionKeyType", 0),
            };
            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            return (string)restResponse.Data.SelectToken("d.Id", true);
        }

        /// <inheritdoc/>
        public async Task<(string AssetId, Uri AssetUri)> CreateEmptyAssetAsync(string assetName, string accountName)
        {
            ConfigureRestClient();

            var request = new RestRequest($"Assets", Method.POST);
            JObject jsonBody = new JObject()
            {
                new JProperty("Name", assetName),
                new JProperty("StorageAccountName", accountName),
            };
            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var assetId = (string)restResponse.Data.SelectToken("d.Id", true);
            var assetUri = (Uri)restResponse.Data.SelectToken("d.Uri", true);
            return (assetId, assetUri);
        }

        /// <inheritdoc/>
        public async Task<(string AssetName, Uri AssetUri)> GetAssetNameAndUriAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var assetName = (string)restResponse.Data.SelectToken("d.Name", true);
            var assetUri = (Uri)restResponse.Data.SelectToken("d.Uri", true);
            return (assetName, assetUri);
        }

        /// <inheritdoc/>
        public async Task CreateFileInfosAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"CreateFileInfos", Method.GET);
            request.AddParameter("assetid", $"'{assetId}'");
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);

            // e.g.: status 204, no content.
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            // Note:
            //  CreateFileInfos asks Azure Media Services to enumerate files in the asset container and add them as AssetFiles,
            //  without auto-selecting a primary file.  A primary file is needed for encodes that need to diambiguate which file
            //  should be used as the master timeline, or primary video track, etc.  This code base is not expected to need
            //  primary files, so does not attempt to itterate over the created AssetFiles to then set one as Primary.
            //  Should this be needed in the future, the following operations should be considered:
            //  new RestRequest($"Assets('{assetId}')/Files", Method.GET);
            //  foreach(var assetFile in restResponse.Data["d"]["results"].ToList())
            //      var uriString = (string)assetFile.SelectToken("Uri", true);
            //      Use some selection criteria to find the file that you want as primary.
            //      var assetFileIdToUseAsPrimary = (string)assetFile.SelectToken("Id", true); break;
            //  new RestRequest($"Files('{assetFileIdToUseAsPrimary}')", Method.MERGE);
            //  request.AddJsonBody(new {IsPrimary = true});
            //  Ref: https://docs.microsoft.com/en-us/rest/api/media/operations/assetfile#Update_a_file
        }

        /// <inheritdoc/>
        public async Task<string> GetLatestMediaProcessorAsync(string mediaProcessorName)
        {
            ConfigureRestClient();
            var request = new RestRequest($"MediaProcessors", Method.GET);
            request.AddParameter("$filter", $"Name eq '{mediaProcessorName}'");
            request.AddHeader("Content-Type", ContentType.Json);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }
            var result = restResponse.Data.SelectToken("d.results", true).FirstOrDefault();
            if (result == null)
            {
                string expMsg = $"Media processor '{mediaProcessorName}' not found";
                throw new Exception(expMsg);
            }
            else
            {
                return (string)restResponse.Data.SelectToken("d.results", true).FirstOrDefault()?.SelectToken("Id", true);
            }
        }


        /// <inheritdoc/>
        public async Task<EncodingReservedUnitTypeInfo> GetEncodingReservedUnitTypeAsync()
        {
            // https://docs.microsoft.com/en-us/rest/api/media/operations/encodingreservedunittype

            ConfigureRestClient();
            var request = new RestRequest($"EncodingReservedUnitTypes", Method.GET);
            request.AddHeader("Content-Type", ContentType.Json);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var reservedUnitSection = restResponse.Data.SelectToken("d.results", true).FirstOrDefault();
            return (EncodingReservedUnitTypeInfo)reservedUnitSection.ToObject(typeof(EncodingReservedUnitTypeInfo));
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateEncodingReservedUnitTypeAsync(EncodingReservedUnitTypeInfo reservedUnitInfo)
        {
            // https://docs.microsoft.com/en-us/rest/api/media/operations/encodingreservedunittype

            _ = reservedUnitInfo ?? throw new ArgumentNullException(nameof(reservedUnitInfo));

            ConfigureRestClient();
            var request = new RestRequest($"EncodingReservedUnitTypes(guid'{reservedUnitInfo.AccountId}')", Method.PUT);
            request.AddHeader("Content-Type", ContentType.Json);
            reservedUnitInfo.MaxReservableUnits = null;
            request.AddJsonBody(reservedUnitInfo);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            return restResponse.IsSuccessful && restResponse.StatusCode == System.Net.HttpStatusCode.NoContent;
        }

        /// <inheritdoc/>
        public async Task<JobInfo> GetJobInfoAsync(string jobId)
        {
            // https://docs.microsoft.com/en-us/rest/api/media/operations/job

            // https://<accountname>.restv2.<location>.media.azure.net/api/Jobs('jobid')

            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')", Method.GET);
            request.AddHeader("Content-Type", ContentType.Json);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var myJobInfo = restResponse.Data.SelectToken("d", true);
            return (JobInfo)myJobInfo.ToObject(typeof(JobInfo));
        }

        /// <remarks>
        /// Ref:
        ///     https://docs.microsoft.com/en-us/rest/api/media/operations/job#create_jobs_with_notifications
        ///     https://social.msdn.microsoft.com/Forums/en-US/cc69a85f-74b0-4d52-8e69-629ff5007169/create-an-encoding-job-with-jobnotificationsubscriptions-by-using-rest-api-got-a-response-with-400
        /// In this implementation we use a single POST, versus the documented /$batch method, by forcing the header to "application/json" and using "InputMediaAssets@odata.bind" instead of "InputMediaAssets".
        /// </remarks>
        /// <inheritdoc/>
        public async Task<string> CreateJobAsync(string jobName, string processorId, string inputAssetId, string preset, string outputAssetName, string outputAssetStorageAccountName, string correlationData, string notificationEndPointId)
        {
            ConfigureRestClient();

            var request = new RestRequest($"Jobs", Method.POST);

            // The use of '@odata.bind' in the body means that we should not use 'odata=verbose'
            request.AddHeader("Content-Type", ContentType.Json);

            JObject jsonBody;
            // If no callback Url
            if (string.IsNullOrEmpty(notificationEndPointId))
            {
                jsonBody = new JObject()
            {
                new JProperty("Name", jobName),

                new JProperty(
                    "InputMediaAssets@odata.bind",
                    new JArray(
                        $"{_restClient.BaseUrl}Assets('{inputAssetId}')")),

                new JProperty(
                    "Tasks",
                    new JArray(
                        new JObject()
                        {
                            new JProperty("Name", correlationData),
                            new JProperty("Configuration", preset),
                            new JProperty("MediaProcessorId", processorId),
                            new JProperty("TaskBody", $"<?xml version=\"1.0\" encoding=\"utf-8\"?><taskBody><inputAsset>JobInputAsset(0)</inputAsset><outputAsset assetName=\"{outputAssetName}\" storageAccountName=\"{outputAssetStorageAccountName}\" assetCreationOptions=\"0\" assetFormatOption=\"0\" >JobOutputAsset(0)</outputAsset></taskBody>"),
                        })),
            };
            }
            // With a callback Url
            else
            {
                jsonBody = new JObject()
            {
                new JProperty("Name", jobName),

                new JProperty(
                    "InputMediaAssets@odata.bind",
                    new JArray(
                        $"{_restClient.BaseUrl}Assets('{inputAssetId}')")),

                new JProperty(
                    "Tasks",
                    new JArray(
                        new JObject()
                        {
                            new JProperty("Name", correlationData),
                            new JProperty("Configuration", preset),
                            new JProperty("MediaProcessorId", processorId),
                            new JProperty("TaskBody", $"<?xml version=\"1.0\" encoding=\"utf-8\"?><taskBody><inputAsset>JobInputAsset(0)</inputAsset><outputAsset assetName=\"{outputAssetName}\" storageAccountName=\"{outputAssetStorageAccountName}\" assetCreationOptions=\"0\" assetFormatOption=\"0\" >JobOutputAsset(0)</outputAsset></taskBody>"),
                            new JProperty("TaskNotificationSubscriptions",
                                new JArray(
                                    new JObject()
                                    {
                                        new JProperty("IncludeTaskProgress", true),
                                        new JProperty("NotificationEndPointId", notificationEndPointId),
                                        new JProperty("TargetTaskState", 2),
                                    }))
                        })),
            };
            }

            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var jobId = (string)restResponse.Data.SelectToken("d.Id", true);
            return jobId;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetAssetFilesNames(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')/Files", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);

            // e.g.: {"d":{"results":[{"__metadata":{"id":"https://yourams.restv2.westus.media.azure.net/api/Files('nb%3Acid%3AUUID%3A08091296-b368-4444-9ce9-878798ef482e')","uri":"https://yourams.restv2.westus.media.azure.net/api/Files('nb%3Acid%3AUUID%3A08091296-b368-4444-9ce9-878798ef482e')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.AssetFile"},"Id":"nb:cid:UUID:08091296-b368-4444-9ce9-878798ef482e","Name":"Unikitty_Clip_small.mov","ContentFileSize":"122992491","ParentAssetId":"nb:cid:UUID:3682f1d9-05f5-442a-832b-afe25cfec903","EncryptionVersion":null,"EncryptionScheme":null,"IsEncrypted":false,"EncryptionKeyId":null,"InitializationVector":null,"IsPrimary":false,"LastModified":"\/Date(1587097903363)\/","Created":"\/Date(1587097903363)\/","MimeType":null,"ContentChecksum":null,"Options":0}]}}
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            return restResponse.Data["d"]["results"]?.Select(r => (string)r.SelectToken("Name")).ToList();
        }

        /// <inheritdoc/>
        public async Task<(string FirstTaskId, string FirstTaskName)> GetFirstTaskAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/Tasks", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firstTaskId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstTaskName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firstTaskId, firstTaskName);
        }

        /// <inheritdoc/>
        public async Task<(string FirstInputAssetId, string FirstInputAssetName)> GetFirstInputAssetAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/InputMediaAssets", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firsInputAssetId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstInputAssetName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firsInputAssetId, firstInputAssetName);
        }

        /// <inheritdoc/>
        public async Task<(string FirstOutputAssetId, string FirstOutputAssetName)> GetFirstOutputAssetAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/OutputMediaAssets", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firsOutputAssetId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstOutputAssetName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firsOutputAssetId, firstOutputAssetName);
        }

        /// <inheritdoc/>
        public async Task DeleteAssetAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')", Method.DELETE);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }
        }

        private string CreateExceptionMessage(IRestResponse<JObject> restResponse)
        {
            var responseHeaders = restResponse.Headers?.Select(h => new JProperty(h.Name, (string)h.Value));
            JObject requestBody = restResponse.Request.Body != null && restResponse.Request.Body.ContentType.Contains(ContentType.Json, StringComparison.InvariantCultureIgnoreCase) ? JObject.Parse((string)restResponse.Request.Body.Value) : null;
            var msg = new JObject
                {
                    { "method", restResponse.Request.Method.ToString() },
                    { "host", _restClient.BaseUrl.ToString() },
                    { "path", restResponse.Request.Resource },
                    { "requestBody", requestBody },
                    { "responseUri", restResponse.ResponseUri },
                    { "responseHeaders",  new JObject(responseHeaders) },
                    { "responseStatusCode", restResponse.StatusCode.ToString() },
                    { "responseBody", restResponse.Data },
                };
            _log.LogError(msg.ToString(Newtonsoft.Json.Formatting.None));
            return msg.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// Configures the RestSharp RestClient.
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
                        }
                        catch (Exception e)
                        {
                            exceptionInLock = e;
                            _log.LogError(e, $"Failed to {nameof(ConfigureRestClient)}");
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