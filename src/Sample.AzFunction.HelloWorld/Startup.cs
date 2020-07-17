using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Sample.AzFunction.HelloWorld;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Sample.AzFunction.HelloWorld
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
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.
            builder?.Services.AddSingleton<TokenCredential>(sp => new Azure.Identity.DefaultAzureCredential(includeInteractiveCredentials: true));
        }
    }
#pragma warning restore CA1812 // Internal class is never instantiated, make static
}