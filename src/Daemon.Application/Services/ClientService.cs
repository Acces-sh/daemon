using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration.Enums;
using Daemon.Application.Interfaces;
using Daemon.Application.Settings;
using Daemon.Application.Wrappers;
using Hangfire;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Daemon.Application.Services;

public class ClientService : IClientService
{
    private readonly AppConfiguration _configuration;
    private readonly IFileService _fileService;
    private readonly IServiceProvider _serviceProvider;
    private HubConnection? _connection;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public string Jwt { get; set; }
 
    public ClientService(AppConfiguration configuration, IFileService fileService, IServiceProvider serviceProvider,IHostApplicationLifetime hostApplicationLifetime)
    {
        _configuration = configuration;
        _fileService = fileService;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        Jwt = "";
    }
    
    /// <summary>
    ///     Attempt to connect to the Acces.sh API
    /// </summary>
    public async Task Connect()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_configuration.Core.HubUrl!, options =>
            {
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
                options.AccessTokenProvider = () => Task.FromResult(Jwt)!;
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Critical);
                logging.AddConsole();
            })
            .Build();

        await _connection.StartAsync();
    }
    
    /// <summary>
    ///     End the connection with the remote server
    /// </summary>
    public void Dispose()
    {
        if (_connection == null) return;

        try
        {
            _connection.StopAsync().Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception e)
        {
            Log.Information("Client: The connection cannot be properly stopped");
            Log.Debug("Client: {Message}", e.Message);
            throw;
        }
    }
    
    /// <summary>
    ///     Initialize route
    /// </summary>
    public void Init()
    {
        _connection!.Closed += async error => await ConnectionHandler(error);
        _connection!.On<bool, ErrorResult<string>>("AuthenticationStatus", AuthenticationHandler);
        
        // Add KEYS
        _connection!.On<List<string>>("InitKeys", InitKeysHandler);
        _connection!.On<List<string>>("AddKeys", AddKeysHandler);
        _connection.On<List<string>>("RemoveKeys", RemoveKeysHandler);

        _connection.On<ServerAction>("ServerActions", ServerActionHandler);
        _connection.On<ErrorResult<string[]>>("Error", ServerErrorHandler);
    }

    /// <summary>
    ///     Performs a new authentication
    /// </summary>
    private void RestartAuthentication()
    {
        using var scope = _serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetService<IDaemonWorker>();

        BackgroundJob.Schedule(() => worker!.StartAuthenticationTask(), TimeSpan.FromMinutes(1));
    }
    
    #region Handler
    
    /// <summary>
    ///     Handle connection loss
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    private Task ConnectionHandler(Exception? error)
    {
        Log.Warning(
            "Client: The connection between the server and the client has been disrupted");

        if (error != null) Log.Debug("Client: {Message}", error.Message);

        try
        {
            Dispose();
        }
        catch (Exception e)
        {
            Log.Debug("Client: Connection can't be disposed {Message}", e.Message);
        }

        Log.Information("Client: Attempt to reconnect ...");
        RestartAuthentication();

        return Task.CompletedTask;
    }
    
    /// <summary>
    ///     Handle authentication response from api
    /// </summary>
    /// <param name="status"></param>
    /// <param name="error"></param>
    private void AuthenticationHandler(bool status, ErrorResult<string> error)
    {
        Log.Error("Client: Authentication failed");
        Log.Error("Client: {Message}", error.Exception);

        if (status) return;
        
        using var scope = _serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetService<IDaemonWorker>();

        worker!.StopApplication();
    }
    
    /// <summary>
    ///     Init key Handler
    /// </summary>
    /// <param name="keys"></param>
    [AutomaticRetry(Attempts = 0)]
    private void InitKeysHandler(IList<string> keys)
    {
        Log.Information("Client: Receive action -> Init keys");
        BackgroundJob.Enqueue(() => _fileService.RemoveKeysJob(keys));
    }
    
    /// <summary>
    ///     Add key Handler
    /// </summary>
    /// <param name="keys"></param>
    private void AddKeysHandler(IList<string> keys)
    {
        Log.Information("Client: Receive action -> Add keys");
        BackgroundJob.Enqueue(() => _fileService.AddKeysJob(keys));
    }
    
    /// <summary>
    ///     Remove key Handler
    /// </summary>
    /// <param name="keys"></param>
    [AutomaticRetry(Attempts = 0)]
    private void RemoveKeysHandler(IList<string> keys)
    {
        Log.Information("Client: Receive action -> Remove keys");
        BackgroundJob.Enqueue(() => _fileService.RemoveKeysJob(keys));
    }
    
    /// <summary>
    ///     Handler for server action
    /// </summary>
    /// <param name="serverAction"></param>
    [AutomaticRetry(Attempts = 0)]
    private Task ServerActionHandler(ServerAction serverAction)
    {
        Log.Information("Client: Receive server action request {ServerAction} ", serverAction);
        
        using var scope = _serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetService<IDaemonWorker>();

        switch (serverAction)
        {
            case ServerAction.Removed:
                Log.Information("Client: Daemon has been removed from the api");
                // worker!.StopApplication();
                _hostApplicationLifetime.StopApplication();
                // _cancellationToken.Cancel();
                break;
            case ServerAction.Reconnect:
            case ServerAction.Authentication:
                Log.Information("Client: The daemon will reconnect");
                Dispose();
                RestartAuthentication();
                break;
            case ServerAction.Logout:
                Log.Information("Client: The daemon receive logout request");
                _hostApplicationLifetime.StopApplication();
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handler server errors
    /// </summary>
    /// <param name="response"></param>
    private void ServerErrorHandler(ErrorResult<string[]> response)
    {
        Log.Error("Client: {Message}", response.Exception);

        if (response.Messages != null)
            foreach (var error in response.Messages)
                Log.Error("Client {Error}", error);

        using var scope = _serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetService<IDaemonWorker>();

        worker!.StopApplication();
    }
    #endregion
}
