---
page_type: sample
languages:
- csharp
products:
- azure
- azure-media-services
- azure-functions
- dotnet-core
description: "Simple Azure Media Services V2 REST API call using RestSharp."
---

# "Simple Azure Media Services V2 REST API call using RestSharp."

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
