using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Storage.Helper;

namespace Sample.AzFunction.Advanced
{
    /// <summary>
    /// In many projects, Azure resources need to be managed.
    /// This function uses the IAzureStorageManagement service to get data.
    /// Usage:
    ///   http://localhost:7071/api/GetStorageAccountKeyLength?name=myaccount
    /// .
    /// </summary>
    public class GetStorageAccountKey
    {
        private readonly IAzureStorageManagement _azureStorageManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetStorageAccountKey"/> class.
        /// </summary>
        /// <param name="azureStorageManagement">The <see cref="IAzureStorageManagement"/>.</param>
        public GetStorageAccountKey(IAzureStorageManagement azureStorageManagement)
        {
            _azureStorageManagement = azureStorageManagement;
        }

        /// <summary>
        /// Gets the storage key length for an account.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="log">ILogger.</param>
        /// <returns>Key length of the storage account name, or -1.</returns>
        [FunctionName("GetStorageAccountKeyLength")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Try to get the name from the request parameters:
            string name = req?.Query["name"];

            // Replace with the name from the body, if it exists:
            using (var sr = new StreamReader(req.Body))
            {
                string requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name ??= data?.name;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                var argumentMsg = $"Error in {nameof(GetStorageAccountKey)}, account name must be in the body or as a query param.";
                log.LogError(argumentMsg);
                return new BadRequestObjectResult(new { error = argumentMsg });
            }

            // Try to get some data from IAzure via IAzureStorageManagement:
            string storageKey = string.Empty;
            try
            {
                storageKey = _azureStorageManagement.GetAccountKey(name);
            }
            catch (Exception e)
            {
                var exceptionMsg = $"Error in {nameof(GetStorageAccountKey)} for {name}.";
                log.LogError(e, exceptionMsg);
                return new BadRequestObjectResult(new { error = $"{exceptionMsg} {e.Message}" });
            }

            if (storageKey == null)
            {
                var errorMsg = $"Could not get storage key for {name}.";
                log.LogError(errorMsg);
            }

            // Do something, like build a SAS url, but don't actually output a storage key!
            var outputMsg = $"StorageKey.Length for {name} is: {storageKey.Length}";
            log.LogInformation(outputMsg);
            return new OkObjectResult(outputMsg);
        }
    }
}
