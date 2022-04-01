using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Enums;
using Accessh.Configuration.Interfaces;
using Accessh.Configuration.Wrappers;
using Hangfire;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Accessh.Daemon.Services
{
    /// <summary>
    ///     Client service manages the websocket connection between the API and the daemon.
    /// </summary>
    public class ClientService : IClientService
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly IFileService _fileService;
        private readonly IServiceProvider _serviceProvider;
        private HubConnection _connection;

        public ClientService(CancellationTokenSource cancellationToken, IFileService fileService,
            AppConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _cancellationToken = cancellationToken;
            _fileService = fileService;
            _serviceProvider = serviceProvider;
            Jwt = "";
            _appConfiguration = configuration;
        }

        public string Jwt { get; set; }

        /// <summary>
        ///     Attempt to connect to the Acces.sh API
        /// </summary>
        public async Task Connect()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_appConfiguration.HubUrl, options =>
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
        public void InitRoute()
        {
            _connection.Closed += async error => await ConnectionHandler(error);
            _connection.On<bool, ErrorResult<string>>("AuthenticationStatus", AuthenticationHandler);
            _connection.On<ServerAction>("ServerActions", ServerActionHandler);
            _connection.On<List<string>>("AddKeys", AddKeysHandler);
            _connection.On<List<string>>("RemoveKeys", RemoveKeysHandler);
            _connection.On<ErrorResult<string[]>>("Error", ServerErrorHandler);
        }

        /// <summary>
        ///     Ask api to get ssh keys
        /// </summary>
        public async void AskGetKeys()
        {
            Log.Information("Client: Request for ssh key synchronization");
            await _connection.InvokeAsync("GetKeys");
        }

        /// <summary>
        ///     Performs a new authentication
        /// </summary>
        private void RestartAuthentication()
        {
            using var scope = _serviceProvider.CreateScope();
            var daemon = scope.ServiceProvider.GetService<IDaemonService>();

            BackgroundJob.Schedule(() => daemon.StartAuthenticationTask(), TimeSpan.FromMinutes(1));
        }

        #region Handler

        /// <summary>
        ///     Handle connection loss
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private Task ConnectionHandler(Exception error)
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

            _cancellationToken.Cancel();
        }

        /// <summary>
        ///     Add key job
        /// </summary>
        /// <param name="keys"></param>
        private void AddKeysHandler(IList<string> keys)
        {
            Log.Information("Client: Receive action -> Add keys");
            BackgroundJob.Enqueue(() => _fileService.AddKeysJob(keys));
        }

        /// <summary>
        ///     Run a key deletion job
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

            switch (serverAction)
            {
                case ServerAction.Removed:
                    Log.Information("Client: Daemon has been removed from the api");
                    _cancellationToken.Cancel();
                    break;
                case ServerAction.Reconnect:
                case ServerAction.Authentication:
                    Log.Information("Client: The daemon will reconnect");
                    Dispose();
                    RestartAuthentication();
                    break;
                case ServerAction.Logout:
                    Log.Information("Client: The daemon receive logout request");
                    _cancellationToken.Cancel();
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

            foreach (var error in response.Messages) Log.Error("Client {Error}", error);

            // TODO: HANDLE SERVER STOP
            // Is daemon need to stop
            // if (response.RedirectNeeded == false) return;

            _cancellationToken.Cancel();
        }

        #endregion
    }
}