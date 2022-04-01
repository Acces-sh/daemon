using System.Net.Http;
using System.Threading.Tasks;

namespace Daemon.Application.Interfaces;

public interface IAuthenticationService
{
    Task<HttpResponseMessage> Authenticate();
}
