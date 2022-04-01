using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using Daemon.Application.Interfaces;
using Daemon.Application.Responses;
using Daemon.Application.Wrappers;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Daemon.Application.Services;

public class DaemonService : IDaemonService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IFileService _fileService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IClientService _clientService;

    public DaemonService(IHostApplicationLifetime applicationLifetime, IFileService fileService, IAuthenticationService authenticationService, IClientService clientService)
    {
        _applicationLifetime = applicationLifetime;
        _fileService = fileService;
        _authenticationService = authenticationService;
        _clientService = clientService;
    }

    public void Worker()
    {
        try
        {
            if (!_fileService.IsPermissionsCorrect())
            {
                Log.Fatal("The daemon does not have permissions to write to the authorized_key file ");
                _applicationLifetime.StopApplication();
            }

            BackgroundJob.Enqueue(() => StartAuthenticationTask());
        }
        catch (Exception e)
        {
            switch (e)
            {
                case DirectoryNotFoundException:
                case FileNotFoundException _:
                    Log.Fatal("Authorized key file don't exist !");
                    break;
                default:
                    Log.Fatal("An error has occurred ! {Message}", e.Message);
                    break;
            }

            _applicationLifetime.StopApplication();
        }
        
    }
    

    [AutomaticRetry(Attempts = 10000, DelaysInSeconds = new[] { 10, 30, 60, 300, 600, 1800 })]
    public async Task StartAuthenticationTask()
    {
        Log.Information("Daemon: Authentication attempt with the acces.sh API");

        var serializerOption = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var response = await _authenticationService.Authenticate();

            if (response.IsSuccessStatusCode == false)
            {
                var errorResponse = await JsonSerializer.DeserializeAsync
                    <ErrorResult<string[]>>(await response.Content.ReadAsStreamAsync(), serializerOption);

                if (errorResponse == null) throw new HttpRequestException();

                if (errorResponse.Messages is not null)
                    ShowErrors(errorResponse.Messages);
                else
                    Log.Warning("Daemon: {Error}", errorResponse.Exception);

                _applicationLifetime.StopApplication();

                return;
            }

            var content = await JsonSerializer.DeserializeAsync
                <AuthenticateServerResponse>(await response.Content.ReadAsStreamAsync(), serializerOption);

            if (content == null) throw new Exception("Daemon: Authentication succeeded ");

            _clientService.Jwt = content.Jwt;

            Log.Information("Daemon: The daemon has been authenticated");
            
            BackgroundJob.Enqueue(() => StartConnectionTask());
        }
        catch (Exception e)
        {
            Log.Information("Daemon: Authentication failed");
            Log.Debug("Daemon: {Name}", e.GetType().Name);
            Log.Debug("Daemon: {Message}", e.Message);

            if (e is HttpRequestException or JsonException ||
                (e is TaskCanceledException && e.InnerException is TimeoutException))
                throw;

            Log.Fatal("Daemon: A critical error has occurred");

            _applicationLifetime.StopApplication();
        }
    }
    
      
    /// <summary>
    ///     Attempt to connect to the remote server
    /// </summary>
    /// <returns></returns>
    [AutomaticRetry(Attempts = 10000, DelaysInSeconds = new[] { 10, 30, 60, 120, 300 })]
    public async Task StartConnectionTask()
    {
        Log.Information("Daemon: Connection attempt with the acces.sh API");

        try
        {
            await _clientService.Connect();
            Log.Information("Daemon: Success connection to acces.sh api");
            _clientService.Init();
        }
        catch (Exception e)
        {
            if (e is WebSocketException)
            {
                Log.Warning("Daemon: Connection failed ... Next attempt soon");
                throw;
            }

            Log.Fatal("Daemon: A critical error has occurred");
            Log.Warning("Daemon: {Message}", e.Message);
            
            _applicationLifetime.StopApplication();
        }
    }
    
    private static void ShowErrors(IEnumerable<string>? errors)
    {
        if (errors is null) return; 

        foreach (var error in errors) Log.Warning("Daemon: {Error}", error);
    }
}
