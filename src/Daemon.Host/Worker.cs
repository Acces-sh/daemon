using Daemon.Application.Interfaces;
using Serilog;

namespace Daemon.Host;

public class Worker : BackgroundService, IDisposable
{
    private readonly IClientService _clientService;
    private readonly IDaemonService _daemonService;
    private readonly IFileService _fileService;

    public Worker(
        IFileService fileService, IClientService clientService, IDaemonService daemonService)
    {
        _fileService = fileService;
        _clientService = clientService;
        _daemonService = daemonService;
    }
    
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _daemonService.Worker();

        return Task.CompletedTask;
    }

    public async void Dispose()
    {
        Log.Information("Host: The daemon will close now");
        try
        {
            await _fileService.RemoveAll();
            _clientService.Dispose();
        }
        catch (Exception e)
        {
            Log.Warning("Daemon: The authorized key file could not be emptied. Please check the file");
            Log.Debug("Daemon: {Message}", e.Message);
        }
    }
}
