using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MediaServicesV2.Library.RestSharp
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> to register the services in this folder.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IMediaServicesV2RestSharp"/> implementation using RestSharp.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddMediaServicesV2RestSharp(this IServiceCollection services)
        {
            services.AddSingleton<IMediaServicesV2RestSharp, MediaServicesV2RestSharp>();
        }

        /// <summary>
        /// Adds a <see cref="TokenCredential"/> for use in token-auth.
        /// This is required by the constructor of <see cref="MediaServicesV2RestSharp"/>.
        /// Use this if your main app has not already inserted a TokenCredential in the service collection.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> object.</param>
        public static void AddMediaServicesV2RestSharpDefaultAzureTokenCredential(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.
            services.AddSingleton<TokenCredential>(sp => new Azure.Identity.DefaultAzureCredential(includeInteractiveCredentials: true));
        }
    }
}
