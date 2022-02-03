using System.Threading.Tasks;

namespace Accessh.Configuration.Interfaces
{
    public interface IClientService
    {
        string Jwt { set; }
        Task Connect();
        void Dispose();
        void InitRoute();
        void AskGetKeys();
    }
}