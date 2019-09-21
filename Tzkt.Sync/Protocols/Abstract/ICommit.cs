using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface ICommit<T> : ICommit
    {
        T Content { get; }
    }

    public interface ICommit
    {
        Task Apply();
        Task Revert();
    }
}
