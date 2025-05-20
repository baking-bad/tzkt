namespace Tzkt.Sync
{
    public class BaseException(string message, bool rebase = false) : Exception(message)
    {
        public bool RebaseRequired { get; } = rebase;
    }
}
