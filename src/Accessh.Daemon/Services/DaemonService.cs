﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Exception;
using Accessh.Configuration.Interfaces;
using Accessh.Configuration.Wrappers;
using Hangfire;
using Serilog;

namespace Accessh.Daemon.Services
{
    /// <summary>
    ///     Daemon service
    /// </summary>
    public class DaemonService : IDaemonService
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IAuthenticationService _authenticationService;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly IClientService _clientService;
        private readonly IFileService _fileService;
        private readonly KeyConfiguration _keyConfiguration;

        public DaemonService(CancellationTokenSource cancellationToken,
            AppConfiguration appConfiguration,
            KeyConfiguration keyConfiguration,
            IClientService clientService,
            IFileService fileService)
        {
            _cancellationToken = cancellationToken;
            _appConfiguration = appConfiguration;
            _keyConfiguration = keyConfiguration;
            _authenticationService = new AuthenticationService(appConfiguration, keyConfiguration);
            _clientService = clientService;
            _fileService = fileService;
        }

        /// <summary>
        ///     Entrypoint of daemon
        /// </summary>
        public void Worker()
        {
            Log.Information("Starting..");
            Log.Information("Acces.sh Daemon, Version : {Version} ", _appConfiguration.Version);

            if (string.IsNullOrEmpty(_keyConfiguration.ApiToken) || _keyConfiguration.ApiToken.Length < 50)
            {
                Log.Warning("No token provided");
                _cancellationToken.Cancel();
                return;
            }

            try
            {
                _fileService.CheckPermissions();
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
                    case FilePermissionException _:
                        Log.Fatal("The daemon does not have permissions to write to the authorized_key file ");
                        break;
                    default:
                        Log.Fatal("An error has occurred ! {Message}", e.Message);
                        break;
                }

                _cancellationToken.Cancel();
            }
        }

        /// <summary>
        ///     Attempt to authentication to the remote server
        /// </summary>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 10000, DelaysInSeconds = new[] { 10, 30, 60, 120, 300 })]
        public async Task StartAuthenticationTask()
        {
            Log.Information("Daemon: Authentication attempt with the acces.sh API");
            var serializerOption = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var response = await _authenticationService.Try();

                if (response.IsSuccessStatusCode == false)
                {
                    var errorResponse = await JsonSerializer.DeserializeAsync
                        <ErrorResult<string[]>>(await response.Content.ReadAsStreamAsync(), serializerOption);

                    if (errorResponse == null) throw new HttpRequestException();

                    if (errorResponse.Messages is not null)
                        ShowErrors(errorResponse.Messages);
                    else
                        Log.Warning("Daemon: {Error}", errorResponse.Exception);

                    _cancellationToken.Cancel();

                    throw new HttpRequestException();
                }

                var content = await JsonSerializer.DeserializeAsync
                    <Result<string>>(await response.Content.ReadAsStreamAsync(), serializerOption);

                if (content == null) throw new Exception("Daemon: Authentication succeeded ");

                _clientService.Jwt = content.Data;

                Log.Information("Daemon: The daemon has been authenticated");
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
                _cancellationToken.Cancel();
                return;
            }

            BackgroundJob.Enqueue(() => StartConnectionTask());
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
                _clientService.InitRoute();
                // _clientService.AskGetKeys();
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
                _cancellationToken.Cancel();
            }
        }

        /// <summary>
        ///     Dispose daemon service
        /// </summary>
        /// <returns></returns>
        public async Task Dispose()
        {
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

        /// <summary>
        ///     Display errors in the console
        /// </summary>
        /// <param name="errors"></param>
        private static void ShowErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors) Log.Warning("Daemon: {Error}", error);
        }
    }
}