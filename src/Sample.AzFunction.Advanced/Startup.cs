using System.Diagnostics.CodeAnalysis;
using MediaServicesV2.Library.RestSharp;
using MediaServicesV2.Services.Encoding.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Sample.AzFunction.Advanced;
using Storage.Helper;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Sample.AzFunction.Advanced
{
#pragma warning disable CA1812 // Internal class is never instantiated, make static

    /// <summary>
    /// This Azure Function project uses dependency injection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Use the Configure function to inject all required services into the ServiceCollection.
        /// Note: Service instances cannot be used in this function, as the hosting application is not fully loaded.
        /// </summary>
        /// <param name="builder">builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder?.Services.AddDefaultAzureTokenCredential();
            builder?.Services.AddAzureFluentManagement();
            builder?.Services.AddAzureStorageManagement();
            builder?.Services.AddAzureStorageOperations();
            builder?.Services.AddMediaServicesV2();
            builder?.Services.AddMediaServicesV2RestSharp();
        }
    }
#pragma warning restore CA1812 // Internal class is never instantiated, make static
}