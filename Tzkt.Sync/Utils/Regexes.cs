using System.Text.RegularExpressions;

namespace Tzkt.Sync
{
    static partial class Regexes
    {
        [GeneratedRegex(@"(?<=(^|[^\\])(\\\\)*)\\u0000")]
        public static partial Regex Metadata();
    }
}
