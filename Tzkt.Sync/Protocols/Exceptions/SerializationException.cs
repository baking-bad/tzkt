namespace Tzkt.Sync.Protocols
{
    public class SerializationException : BaseException
    {
        public SerializationException(string message)
            : base($"Serialization exception - {message}", false) { }
    }
}
