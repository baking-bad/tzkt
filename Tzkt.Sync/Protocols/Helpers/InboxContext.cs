namespace Tzkt.Sync.Protocols
{
    public class InboxContext
    {
        public List<(long, byte[])> Messages { get; } = new();

        public void Push(long operationId, byte[] payload = null)
        {
            Messages.Add((operationId, payload));
        }
    }
}
