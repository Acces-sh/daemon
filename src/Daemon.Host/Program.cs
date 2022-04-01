using Daemon.Host;
using Daemon.Host.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

Log.Information("Acces.sh : Daemon booting Up...");

try
{
    var builder = Host.CreateDefaultBuilder(args);
    var configuration = builder.AddConfigurations();
    
    builder.UseSerilog((_, config) =>
    {
        config.WriteTo.Console()
            .ReadFrom.Configuration(configuration);
    });
    builder.ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddInfrastructure();
        services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));
    });

    await builder.RunConsoleAsync();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("TaskCanceledException", StringComparison.Ordinal))
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Acces.sh : Daemon shutting down...");
    Log.CloseAndFlush();
}
