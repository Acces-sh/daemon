using System.Collections.Generic;
using System.Threading.Tasks;

namespace Daemon.Application.Interfaces;

public interface IFileService
{
    bool IsPermissionsCorrect();
    Task RemoveAll();
    Task InitKeysJob(IList<string> keys);
    Task AddKeysJob(IList<string> keys);
    Task RemoveKeysJob(IList<string> keys);
}
