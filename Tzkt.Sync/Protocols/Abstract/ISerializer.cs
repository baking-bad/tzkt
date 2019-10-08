using System.IO;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface ISerializer
    {
        Task<IBlock> DeserializeBlock(Stream stram);
    }
}
