using System.Threading.Tasks;

namespace Tzkt.Api.Websocket
{
    public interface IHubProcessor
    {
        Task OnStateChanged();
    }
}
