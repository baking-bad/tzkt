namespace Tzkt.Api.Websocket
{
    public class StateMessage : IMessage
    {
        public MessageType Type => MessageType.State;
        public object State { get; }

        public StateMessage(object state)
        {
            State = state;
        }
    }
}