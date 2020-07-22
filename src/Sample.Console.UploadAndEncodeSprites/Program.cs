using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaServicesV2.Library.RestSharp;
using MediaServicesV2.Services.Encoding.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Helper;

namespace Sample.Console.UploadAndEncodeSprites
{
    /// <summary>
    /// Program class.
    /// </summary>
    internal class Program
    {
        private const string InputMP4FileName = @"ignite.mp4";

        /// <summary>
        /// Main method.
        /// </summary>
        /// <returns>Task.</returns>
        public static async Task Main()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // do the actual work here
            var consoleServ = serviceProvider.GetService<IConsoleService>();
            await consoleServ.UploadAndEncodeAsync(new List<string>() { InputMP4FileName }).ConfigureAwait(false);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

            services.AddLogging(configure => configure.AddConsole())
               .AddTransient<ConsoleService>();
            services.AddDefaultAzureTokenCredential();
            services.AddAzureFluentManagement();
            services.AddAzureStorageManagement();
            services.AddAzureStorageOperations();
            services.AddMediaServicesV2();
            services.AddMediaServicesV2RestSharp();
            services.AddSingleton<IConsoleService, ConsoleService>();
            services.AddSingleton<IConfiguration>(configuration);
        }
    }
}
