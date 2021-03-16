using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Enums;
using Accessh.Configuration.Interfaces;
using Accessh.Daemon.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Accessh.Daemon
{
    public static class Program
    {
        private const int ERROR_FILE_NOT_FOUND = -0x2;
        private const int ERROR_INVALID_DATA = -0xD;

        public static async Task<int> Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            AppConfiguration appConfiguration;
            KeyConfiguration keyConfiguration;
            IConfigurationRoot configurationRoot;

            try
            {
                configurationRoot = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true)
                    .AddEnvironmentVariables()
                    .Build();
                appConfiguration = InitializeAppConfiguration(configurationRoot);
                var appConfigurationRoot = new ConfigurationBuilder()
                    .SetBasePath(appConfiguration.ConfigurationFilePath)
                    .AddJsonFile("config.json", false, true)
                    .AddEnvironmentVariables()
                    .Build();
                keyConfiguration = InitializeKeyConfiguration(appConfigurationRoot, appConfiguration);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case DirectoryNotFoundException:
                    case FileNotFoundException:
                    case FormatException:
                        Console.WriteLine(
                            "The configuration file (config.json) cannot be found.");
                        return ERROR_FILE_NOT_FOUND;
                    case ValidationException:
                        Console.WriteLine("Please check the information in the config.json file.");
                        return ERROR_INVALID_DATA;
                    default:
                        throw;
                }
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .ReadFrom.Configuration(configurationRoot)
                .CreateLogger();

            var builder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<HostService>();
                    services.AddSingleton(appConfiguration);
                    services.AddSingleton(keyConfiguration);
                    services.AddSingleton(cts);
                    services.AddSingleton<IDaemonService, DaemonService>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<IClientService, ClientService>();
                    services.AddHangfire(configuration =>
                        configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                            .UseRecommendedSerializerSettings()
                            .UseMemoryStorage()
                    );
                    services.AddHangfireServer();

                    services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));
                });

            await builder.RunConsoleAsync(cts.Token);

            return 0;
        }

        private static AppConfiguration InitializeAppConfiguration(IConfiguration configurationRoot)
        {
            var serverConfiguration = new AppConfiguration();

            configurationRoot.Bind(serverConfiguration);
            Validator.ValidateObject(serverConfiguration, new ValidationContext(serverConfiguration),
                true);

            return serverConfiguration;
        }

        private static KeyConfiguration InitializeKeyConfiguration(IConfiguration configurationRoot,
            AppConfiguration appConfiguration)
        {
            var keyConfiguration = new KeyConfiguration();
            var tokenEnv = Environment.GetEnvironmentVariable("API_TOKEN");
            
            configurationRoot.Bind(keyConfiguration);
            Validator.ValidateObject(keyConfiguration, new ValidationContext(keyConfiguration),
                true);

            if (appConfiguration.Mode == Mode.Docker)
            {
                keyConfiguration.ApiToken = tokenEnv;
            }
            
            return keyConfiguration;
        }
    }
}
