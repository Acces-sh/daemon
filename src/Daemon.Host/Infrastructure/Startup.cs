using System.ComponentModel.DataAnnotations;
using Daemon.Application.Common;
using Daemon.Application.Interfaces;
using Daemon.Application.Services;
using Daemon.Application.Settings;
using Hangfire;
using Hangfire.MemoryStorage;

namespace Daemon.Host.Infrastructure;

public static class Startup
{
    private static AppConfiguration _appConfiguration = new();
    
    internal static IConfigurationRoot AddConfigurations(this IHostBuilder host)
    {
        const string configurationsDirectory = "Configurations";
        
        // Get config path
        var config = new ConfigurationBuilder()
            .AddJsonFile($"{configurationsDirectory}/core.json", false, true).Build();
        _appConfiguration = InitializeCoreConfiguration(config);
        var configPath = _appConfiguration.Core.Mode == Mode.Docker ? Directory.GetCurrentDirectory()
            : _appConfiguration.Core.ConfigurationFilePath;
            
        config = new ConfigurationBuilder()
            .AddJsonFile($"{configurationsDirectory}/core.json", false, true)
            .AddJsonFile($"{configurationsDirectory}/logger.json", false, true)
            .SetBasePath(configPath).AddJsonFile("config.json")
            .AddEnvironmentVariables().Build();
        
        host.ConfigureHostConfiguration(builder =>
        {
            builder.AddConfiguration(config);
        } );
        
        _appConfiguration= InitializeCoreConfiguration(config);

        return config;
    }
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Hangfire
        services.AddHangfire(configuration =>
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage()
        );
        services.AddHangfireServer();
        
        // Services
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IClientService, ClientService>();
        
        // Configuration
        services.AddSingleton(_appConfiguration);

        return services;
    }
    
    private static AppConfiguration InitializeCoreConfiguration(IConfiguration configurationRoot)
    {
        var modeConfiguration = new AppConfiguration();

        configurationRoot.Bind(modeConfiguration);
        Validator.ValidateObject(modeConfiguration, new ValidationContext(modeConfiguration),
            true);

        return modeConfiguration;
    }
}
