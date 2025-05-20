namespace Tzkt.Sync
{
    public class ValidationException(string message, bool rebase = false)
        : BaseException($"Validation exception - {message}", rebase)
    {
    }
}
