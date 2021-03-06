using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Enums;
using Accessh.Configuration.Interfaces;
using Accessh.Configuration.Parameters;
using Hangfire;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Accessh.Daemon.Services
{
    /// <summary>
    /// Client service manages the websocket connection between the API and the daemon. 
    /// </summary>
    public class ClientService : IClientService
    {
        private readonly CancellationTokenSource _cancellationToken;
        private HubConnection _connection;
        private readonly IFileService _fileService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerConfiguration _serverConfiguration;

        public string Jwt { get; set; }

        public ClientService(CancellationTokenSource cancellationToken, IFileService fileService,
            ServerConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _cancellationToken = cancellationToken;
            _fileService = fileService;
            _serviceProvider = serviceProvider;
            Jwt = "";
            _serverConfiguration = configuration;
        }

        /// <summary>
        /// Attempt to connect to the Acces.sh API 
        /// </summary>
        public async Task Connect()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_serverConfiguration.HubUrl, options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                    options.AccessTokenProvider = () => Task.FromResult(Jwt);
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
        /// End the connection with the remote server
        /// </summary>
        public async Task Dispose()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Initialize route
        /// </summary>
        public void InitRoute()
        {
            _connection.Closed += async error => await ConnectionHandler(error);
            _connection.On<bool, string[]>("AuthenticationStatus", AuthenticationHandler);
            _connection.On<ServerAction>("ServerActions", ServerActionHandler);
            _connection.On<List<string>>("AddKeys", AddKeysHandler);
            _connection.On<List<string>>("RemoveKeys", RemoveKeysHandler);
            _connection.On<Response<string>>("Error", ServerErrorHandler);
        }

        /// <summary>
        /// Ask api to get ssh keys
        /// </summary>
        public async void AskGetKeys()
        {
            Log.Information("Request for ssh key synchronization");
            await _connection.InvokeAsync("GetKeys");
        }

        #region Handler

        /// <summary>
        /// Handle connection loss 
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private async Task ConnectionHandler(Exception error)
        {
            Log.Warning(
                "The connection between the server and the client has been disrupted. Attempt to reconnect ...");
            Log.Debug(error.Message);

            try
            {
                await Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("The authorized key file could not be cleaned...");
                Log.Debug(e.Message);
            }

            RestartAuthentication();
        }

        /// <summary>
        /// Handle authentication response from api
        /// </summary>
        /// <param name="status"></param>
        /// <param name="messages"></param>
        private void AuthenticationHandler(bool status, string[] messages)
        {
            if (status) return;

            Log.Error("Authentication failed");
            foreach (var message in messages)
            {
                Log.Error(message);
            }

            _cancellationToken.Cancel();
        }

        /// <summary>
        /// Add key job
        /// </summary>
        /// <param name="keys"></param>
        private void AddKeysHandler(IList<string> keys)
        {
            Log.Information("Receive action -> Add keys");
            BackgroundJob.Enqueue(() => _fileService.AddKeysJob(keys));
        }

        /// <summary>
        /// Run a key deletion job
        /// </summary>
        /// <param name="keys"></param>
        [AutomaticRetry(Attempts = 0)]
        private void RemoveKeysHandler(IList<string> keys)
        {
            Log.Information("Receive action -> Remove keys");
            BackgroundJob.Enqueue(() => _fileService.RemoveKeysJob(keys));
        }

        /// <summary>
        /// Handler for server action
        /// </summary>
        /// <param name="serverAction"></param>
        [AutomaticRetry(Attempts = 0)]
        private async Task ServerActionHandler(ServerAction serverAction)
        {
            Log.Information("Receive server action request -> " + serverAction);

            switch (serverAction)
            {
                case ServerAction.Removed:
                    Log.Information("Daemon has been removed from the api.");
                    _cancellationToken.Cancel();
                    break;
                case ServerAction.Reconnect:
                case ServerAction.Authentication:
                    Log.Information("The daemon will reconnect ");
                    await Dispose();
                    RestartAuthentication();
                    break;
                case ServerAction.Logout:
                    Log.Information("The daemon receive logout request.");
                    _cancellationToken.Cancel();
                    break;
            }
        }

        /// <summary>
        /// Handler server errors
        /// </summary>
        /// <param name="response"></param>
        private void ServerErrorHandler(Response<string> response)
        {
            Log.Error(response.Message);

            foreach (var error in response.Errors)
            {
                Log.Error(error);
            }

            // Is daemon need to stop
            if (response.RedirectNeeded == false) return;

            _cancellationToken.Cancel();
        }

        #endregion
        
        /// <summary>
        /// Performs a new authentication
        /// </summary>
        private void RestartAuthentication()
        {
            using var scope = _serviceProvider.CreateScope();
            var daemon = scope.ServiceProvider.GetService<IDaemonService>();

            BackgroundJob.Enqueue(() => daemon.StartAuthenticationTask());
        }
    }
}
