using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IPreprocessor
    {
        Task Run();
    }
}
