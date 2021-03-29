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
using Accessh.Configuration.Parameters;
using Hangfire;
using Serilog;

namespace Accessh.Daemon.Services
{
    public class DaemonService : IDaemonService
    {
        private readonly CancellationTokenSource _cancellationToken;
        private readonly AppConfiguration _appConfiguration;
        private readonly KeyConfiguration _keyConfiguration;
        private readonly IAuthenticationService _authenticationService;
        private readonly IClientService _clientService;
        private readonly IFileService _fileService;

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
        /// Entrypoint of daemon
        /// </summary>
        public void Worker()
        {
            Log.Information("Starting..");
            Log.Information("Acces.sh Daemon, Version : " + _appConfiguration.Version);

            if (string.IsNullOrEmpty(_keyConfiguration.ApiToken) || _keyConfiguration.ApiToken.Length < 50)
            {
                Log.Warning("No token provided !");
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
                        Log.Fatal($"An error has occurred ! {e.Message}");
                        break;
                }

                _cancellationToken.Cancel();
            }
        }

        /// <summary>
        /// Attempt to authentication to the remote server
        /// </summary>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 10000, DelaysInSeconds = new[] {10, 30, 60, 120, 300})]
        public async Task StartAuthenticationTask()
        {
            Log.Information("Authentication attempt with the acces.sh API.");
            var serializerOption = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};

            try
            {
                var response = await _authenticationService.Try();

                if (response.IsSuccessStatusCode == false)
                {
                    var errorResponse = await JsonSerializer.DeserializeAsync
                        <Response<string[]>>(await response.Content.ReadAsStreamAsync(), serializerOption);
                    if (errorResponse != null)
                    {
                        ShowErrors(errorResponse.Errors);
                        _cancellationToken.Cancel();
                    }

                    throw new HttpRequestException();
                }

                var content = await JsonSerializer.DeserializeAsync
                    <Response<string>>(await response.Content.ReadAsStreamAsync(), serializerOption);

                if (content == null) throw new Exception("Authentication failed");

                _clientService.Jwt = content.Data;

                Log.Information("The daemon has been authenticated");
            }
            catch (Exception e)
            {
                Log.Information("Authentication failed");
                Log.Debug(e.GetType().Name);
                Log.Debug(e.Message);
                
                if (e is HttpRequestException or JsonException ||
                    e is TaskCanceledException && e.InnerException is TimeoutException)
                {
                    throw;
                }

                Log.Fatal("A critical error has occurred. ");
                _cancellationToken.Cancel();
                return;
            }

            BackgroundJob.Enqueue(() => StartConnectionTask());
        }

        /// <summary>
        /// Attempt to connect to the remote server
        /// </summary>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 10000, DelaysInSeconds = new[] {10, 30, 60, 120, 300})]
        public async Task StartConnectionTask()
        {
            Log.Information("Connection attempt with the acces.sh API.");

            try
            {
                await _clientService.Connect();
                Log.Information("Success connection to server api.");
                _clientService.InitRoute();
                _clientService.AskGetKeys();
            }
            catch (Exception e)
            {
                if (e is WebSocketException)
                {
                    Log.Warning("Connection failed ... Next attempt soon");
                }
                else
                {
                    Log.Fatal("A critical error has occurred. ");
                    Log.Warning(e.Message);
                    _cancellationToken.Cancel();
                }
            }
        }

        /// <summary>
        /// Dispose daemon service
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
                Log.Warning("The authorized key file could not be emptied. Please check the file.");
                Log.Debug(e.Message);
            }
        }

        /// <summary>
        /// Display errors in the console
        /// </summary>
        /// <param name="errors"></param>
        private static void ShowErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                Log.Warning(error);
            }
        }
    }
}
