using System.Text.RegularExpressions;

namespace Tzkt.Api
{
    static partial class Regexes
    {
        [GeneratedRegex("^(tz1|tz2|tz3|tz4|KT1|txr1|sr1)[0-9A-Za-z]{33}$")]
        public static partial Regex Address();

        [GeneratedRegex("^tz[0-9A-Za-z]{34}$")]
        public static partial Regex TzAddress();

        [GeneratedRegex("^KT1[0-9A-Za-z]{33}$")]
        public static partial Regex Kt1Address();

        [GeneratedRegex("^sr1[0-9A-Za-z]{33}$")]
        public static partial Regex Sr1Address();

        [GeneratedRegex("^src1[0-9A-Za-z]{50}$")]
        public static partial Regex Src1Hash();

        [GeneratedRegex("^btz[0-9A-Za-z]{34}$")]
        public static partial Regex BtzAddress();

        [GeneratedRegex("^B[0-9A-Za-z]{50}$")]
        public static partial Regex Block();

        [GeneratedRegex("^expr[0-9A-Za-z]{50}$")]
        public static partial Regex Expression();

        [GeneratedRegex("^o[0-9A-Za-z]{50}$")]
        public static partial Regex Operation();

        [GeneratedRegex("^P[0-9A-Za-z]{50}$")]
        public static partial Regex Protocol();

        [GeneratedRegex("^[0-9a-f]{8}$")]
        public static partial Regex Software();

        [GeneratedRegex("^[0-9]+$")]
        public static partial Regex Number();

        [GeneratedRegex(@"^[\w\s,]+$")]
        public static partial Regex CommaSeparatedWords();

        [GeneratedRegex(@"^[\w:]+$")]
        public static partial Regex Field();

        [GeneratedRegex(@"^[\w:]+(\.[\w:]+)+$")]
        public static partial Regex FieldPath();

        [GeneratedRegex(@"^"".*""$")]
        public static partial Regex Quoted();

        [GeneratedRegex(@"^\[[0-9]+\]$")]
        public static partial Regex ArrayIndex();

        [GeneratedRegex(@"(?:""(?:(?:\\"")|(?:[^""]))*"")|(?:[^"".]+)")]
        public static partial Regex JsonPathParser();
    }
}
