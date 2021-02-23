using System.Collections.Generic;
using System.Threading.Tasks;

namespace Accessh.Configuration.Interfaces
{
    public interface IFileService
    {
        void CheckPermissions();
        Task RemoveAll();
        Task AddKeysJob(IList<string> keys);
        Task RemoveKeysJob(IList<string> keys);
    }
}
