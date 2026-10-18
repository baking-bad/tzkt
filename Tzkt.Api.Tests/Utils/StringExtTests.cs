using Xunit;

namespace Tzkt.Api.Tests.Utils;

public class StringExtTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ledger")]
    [InlineData("tab\there\nnewline")]
    [InlineData("\u0001control")]
    [InlineData("юникод ✓ 😀")]
    [InlineData("{\"a\":\"\\ud83d\\ude00\"}")]   // valid surrogate pair escape
    [InlineData("{\"a\":\"\\uD83D\\uDE00\"}")]
    [InlineData("a\\\\u0000b")]                 // escaped backslash followed by text, not an escape
    public void IsValidText_AcceptsRegularStrings(string value)
    {
        Assert.True(value.IsValidText());
    }

    [Theory]
    [InlineData("a\0b")]
    [InlineData("\\u0000")]                     // NUL escape, rejected by jsonb
    [InlineData("{\"a\":\"x\\u0000y\"}")]
    [InlineData("a\\\\\\u0000b")]               // escaped backslash followed by a real escape
    [InlineData("\\ud800")]                     // lone high surrogate escape
    [InlineData("\\udc00")]                     // lone low surrogate escape
    [InlineData("\"\\ud83d\\ud83d\\ude00\"")]   // lone high before a pair
    [InlineData("\"\\ud83d\\ude00\\ude00\"")]   // lone low after a pair
    public void IsValidText_RejectsNulAndUnpairedSurrogates(string value)
    {
        Assert.False(value.IsValidText());
    }
}
