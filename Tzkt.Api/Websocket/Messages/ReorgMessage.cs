namespace Tzkt.Api.Websocket
{
    public class ReorgMessage : IMessage
    {
        public MessageType Type => MessageType.Reorg;
        public object State { get; }

        public ReorgMessage(object state)
        {
            State = state;
        }
    }
}