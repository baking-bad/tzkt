namespace Tzkt.Api
{
    public static class StringExtension
    {
        /// <summary>
        /// Checks that the string can be safely passed to the database.
        /// </summary>
        public static bool IsValidText(this string value)
        {
            return !value.Contains('\0') && !Regexes.RestrictedUnicode().IsMatch(value);
        }
    }
}
