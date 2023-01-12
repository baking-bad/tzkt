using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class PeriodDictatorStatuses
    {
        public const string None = "none";
        public const string Abort = "abort";
        public const string Reset = "reset";
        public const string Submit = "submit";

        public static string ToString(int value) => value switch
        {
            (int)DictatorStatus.None => None,
            (int)DictatorStatus.Abort => Abort,
            (int)DictatorStatus.Reset => Reset,
            (int)DictatorStatus.Submit => Submit,
            _ => throw new Exception("invalid dictator status value")
        };
    }
}
