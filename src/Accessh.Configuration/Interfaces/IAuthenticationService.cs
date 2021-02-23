using System.Net.Http;
using System.Threading.Tasks;

namespace Accessh.Configuration.Interfaces
{
    public interface IAuthenticationService
    {
        Task<HttpResponseMessage> Try();
    }
}
