namespace Tzkt.Sync
{
    public class SerializationException(string message)
        : BaseException($"Serialization exception - {message}", false)
    {
    }
}
