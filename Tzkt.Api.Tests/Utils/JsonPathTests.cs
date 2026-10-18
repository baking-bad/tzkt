using Tzkt.Api.Utils;
using Xunit;

namespace Tzkt.Api.Tests.Utils;

public class JsonPathTests
{
    [Theory]
    [InlineData("a", 1)]
    [InlineData("a.b.c", 3)]
    [InlineData("\"a b\".c", 2)]
    [InlineData("\"a\\\"b\"", 1)]   // escaped quote inside a quoted key
    [InlineData("\"юникод\"", 1)]
    [InlineData("a.[0].[*]", 3)]
    public void TryParse_AcceptsValidPaths(string path, int count)
    {
        Assert.True(JsonPath.TryParse(path, out var res));
        Assert.Equal(count, res.Length);
    }

    [Theory]
    [InlineData("\"a\0b\"")]        // NUL inside a quoted key
    [InlineData("\"a\\u0000b\"")]   // NUL escape inside a quoted key
    [InlineData("\"a\\ud800b\"")]   // unpaired surrogate escape inside a quoted key
    [InlineData("\"a\\qb\"")]       // invalid JSON escape inside a quoted key
    public void TryParse_RejectsInvalidPaths(string path)
    {
        Assert.False(JsonPath.TryParse(path, out _));
    }
}
