namespace Mvkt.Api.Websocket
{
    public interface IHubProcessor
    {
        Task OnStateChanged();
    }
}
