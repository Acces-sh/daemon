using System.Threading.Tasks;

namespace Daemon.Application.Interfaces;

public interface IClientService
{
    string Jwt { set; }
    Task Connect();
    void Dispose();
    void Init();
}
