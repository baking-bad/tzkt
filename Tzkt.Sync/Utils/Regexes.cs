using System.Text.RegularExpressions;

namespace Tzkt.Sync
{
    static partial class Regexes
    {
        public const char NullEscapeChar = '\uFFFD';

        [GeneratedRegex(@"(?<=(^|[^\\])(\\\\)*)\\u0000")]
        public static partial Regex RestrictedUnicode();
    }
}
