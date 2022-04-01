using System.Threading.Tasks;

namespace Daemon.Application.Interfaces;

public interface IDaemonWorker
{
    Task StartAuthenticationTask();
    void StopApplication();
}
