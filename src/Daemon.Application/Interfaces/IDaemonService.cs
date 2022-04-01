using System.Threading.Tasks;

namespace Daemon.Application.Interfaces;

public interface IDaemonService
{
    void Worker();
    Task StartAuthenticationTask();
    Task StartConnectionTask();
}
