using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Daemon.Application.Interfaces;
using Daemon.Application.Settings;

namespace Daemon.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private const string ServerAuthUri = "servers/authentication";
    private readonly AppConfiguration _configuration;
    
    public AuthenticationService(AppConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<HttpResponseMessage> Authenticate()
    {
        var client = new HttpClient();
        var bodyData = new { Token = _configuration.ApiToken.Trim(), Version = _configuration.Core.Version! };

        client.DefaultRequestHeaders.Add("User-Agent", "accessh-daemon-client");

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_configuration.Core.ServerUrl + ServerAuthUri),
            Content = new StringContent(JsonSerializer.Serialize(bodyData), Encoding.UTF8,
                "application/json")
        };

        return await client.SendAsync(request);
    }
}
