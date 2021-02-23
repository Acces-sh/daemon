using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration;
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
        public static async Task Main(string[] args)
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables()
                .Build();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .ReadFrom.Configuration(configurationRoot)
                .CreateLogger();

            Log.Information("Starting..");

            var cts = new CancellationTokenSource();
            var serverConfiguration = InitializeConfiguration(configurationRoot);
            var builder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<HostService>();
                    services.AddSingleton(serverConfiguration);
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
        }

        private static ServerConfiguration InitializeConfiguration(IConfigurationRoot configurationRoot)
        {
            var token = Environment.GetEnvironmentVariable("API_TOKEN");
            var serverConfiguration = new ServerConfiguration();

            configurationRoot.Bind(serverConfiguration);
            Validator.ValidateObject(serverConfiguration, new ValidationContext(serverConfiguration),
                true);
            if (!string.IsNullOrEmpty(token))
                serverConfiguration.ApiToken = token;

            return serverConfiguration;
        }
    }
}
