using System;

namespace Tzkt.Sync
{
    public class BaseException : Exception
    {
        public bool RebaseRequired { get; }

        public BaseException(string message, bool rebase = false) : base(message) => RebaseRequired = rebase;
    }
}
