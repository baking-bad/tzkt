namespace Tzkt.Api.Websocket
{
    public interface IHubProcessor
    {
        Task OnStateChanged();
    }
}
