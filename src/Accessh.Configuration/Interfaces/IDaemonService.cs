using System.Threading.Tasks;

namespace Accessh.Configuration.Interfaces;

public interface IDaemonService
{
    void Worker();
    Task StartAuthenticationTask();
    Task StartConnectionTask();
    Task Dispose();
}