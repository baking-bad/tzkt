namespace Tzkt.Api.Websocket
{
    public interface IMessage
    {
        MessageType Type { get; }
    }

    public enum MessageType
    {
        State = 0,
        Data = 1,
        Reorg = 2,
    }
}
