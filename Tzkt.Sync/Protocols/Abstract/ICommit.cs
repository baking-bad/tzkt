using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface ICommit
    {
        Task Apply();
        Task Revert();
    }
}
