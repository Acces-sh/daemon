using System.Threading.Tasks;

namespace Accessh.Configuration.Interfaces
{
    public interface IClientService
    {
        string Jwt { set; }
        Task Connect();
        Task Dispose();
        void InitRoute();
        void AskGetKeys();
    }
}
