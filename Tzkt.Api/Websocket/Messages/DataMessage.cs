namespace Tzkt.Api.Websocket
{
    public class DataMessage : IMessage
    {
        public MessageType Type => MessageType.Data;
        public object Data { get; }
        public object State { get; }

        public DataMessage(object data, object state)
        {
            Data = data;
            State = state;
        }
    }
}