using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Accessh.Configuration;
using Accessh.Configuration.Interfaces;

namespace Accessh.Daemon.Services
{
    /// <summary>
    /// Authentication manager 
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private const string ServerAuthUri = "servers/authentication";
        private readonly string _serverUrl;
        private readonly string _apiToken;

        public AuthenticationService(AppConfiguration configuration, KeyConfiguration keyConfiguration)
        {
            _apiToken = keyConfiguration.ApiToken;
            _serverUrl = configuration.ServerUrl;
        }

        /// <summary>
        /// Performs an authentication request on the server 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task<HttpResponseMessage> Try()
        {
            var client = new HttpClient();
            var bodyData = new { token = _apiToken };

            client.DefaultRequestHeaders.Add("User-Agent", "accessh-daemon-client");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_serverUrl + ServerAuthUri),
                Content = new StringContent(JsonSerializer.Serialize(bodyData), Encoding.UTF8,
                    "application/json"),
            };

            return await client.SendAsync(request);
        }
    }
}
