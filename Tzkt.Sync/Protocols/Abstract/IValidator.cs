using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IValidator
    {
        Task<IBlock> ValidateBlock(IBlock block);
    }
}
