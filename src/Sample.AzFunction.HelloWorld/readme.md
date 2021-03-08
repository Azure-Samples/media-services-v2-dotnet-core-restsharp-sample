
**Important : The Media Services v2 SDK is deprecated and will be retired after 29 February 2024.** Please migrate to Azure Media Services v3 API. For functions and logic apps samples using the v3 API, please go [to this repo](https://aka.ms/ams3functions).

The project includes several folders of sample Azure Functions for use with Azure Media Services v2 that show workflows related
to ingesting content directly from blob storage, encoding, and writing content back to blob storage. It also includes examples of
how to monitor job notifications via WebHooks and Azure Queues.

## IMPORTANT! Update your Azure Media Services REST API and SDKs to v3 by 29 February 2024

Because version 3 of Azure Media Services REST API and client SDKs for .NET and Java offers more capabilities than version 2, we’re retiring version 2 of the Azure Media Services REST API and client SDKs for .NET and Java. We encourage you to make the switch sooner to gain the richer benefits of version 3 of Azure Media Services REST API and client SDKs for .NET and Java. Version 3 provides: 

### Action Required:
To minimize disruption to your workloads, review the migration guide to transition your code from the version 2 to version 3 API and SDK before 29 February 2024. 

After 29 February 2024, Azure Media Services will no longer accept traffic on the version 2 REST API, the ARM account management API version 2015-10-01, or from the version 2 .NET client SDKs. This includes any 3rd party open-source client SDKS that may call the version 2 API.  

See [Update your Azure Media Services REST API and SDKs to v3 by 29 February 2024](https://azure.microsoft.com/en-us/updates/update-your-azure-media-services-rest-api-and-sdks-to-v3-by-29-february-2024)


# (DEPRECATED) "Simple Azure Media Services V2 REST API call using RestSharp."

This function app demonstrates the minimum amount of code to get an auth token and use it with the [Azure Media Services V2 REST API](https://docs.microsoft.com/en-us/azure/media-services/previous/media-services-rest-how-to-use), using the [RestSharp library](https://restsharp.dev/).

The Azure Function App uses a [Startup class](Startup) and [dependency injection](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection) to provide a [TokenCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) to the instance of the [Azure Function GetJobById](./GetJobById.cs).

Auth Flow:

 1. In `GetAmsRestApiEndpoint()` the `Microsoft.Azure.Services.AppAuthentication` is used to request a bearer token for the resource: `https://management.azure.com`
 2. That bearer token is then used to get details on the Azure Media Services account, in particular the `amsRestApiEndpoint`.
 3. In `ConfigureRestClient()` the `Microsoft.Azure.Services.AppAuthentication` is used to request a bearer token for the resource:`https://rest.media.azure.net`
 4. That bearer token is then used for calls to the base url for AMS V2 REST API, `amsRestApiEndpoint`, which has the form: `https://{amsAccountName}.restv2.{location-based-instance}.media.azure.net/api/`

![AzFxnWithRestSharpUsingAmsV2](./docs/img/AzFxnWithRestSharpUsingAmsV2.drawio.svg)

We use a [DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) as a TokenCredential, with ```includeInteractiveCredentials=true```, this will prompt for a credential when running locally.
It expects a Managed Identity with adequate rights to the Azure Media Services resource uri, when running in Azure.  See the Advanced sample for more details on deploying such an app.
