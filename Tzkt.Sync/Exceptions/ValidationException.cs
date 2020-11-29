namespace Tzkt.Sync
{
    public class ValidationException : BaseException
    {
        public ValidationException(string message, bool rebase = false)
            : base($"Validation exception - {message}", rebase) { }
    }
}
