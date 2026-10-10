using Xunit;

namespace Tzkt.Api.Tests.Utils;

public class RegexesTests
{
    [Theory]
    [InlineData("tz1KqTpEZ7Yob7QbPE4Hy4Wo8fHG8LhKxZSx")]
    [InlineData("tz2BFTyPeYRzxd5aiBchbXN3WCZhx7BqbMBq")]
    [InlineData("tz3WXYtyDUNL91qfiCJtVUX746QpNv5i5ve5")]
    [InlineData("tz4DWZXsrP3bdPaZ5B3M3iLVoRMAyxw9oKLH")]
    [InlineData("tz5M7PdBGU7rHfCBmX21EHq8tsHRYS3ZXVdp")]
    [InlineData("KT1GBZmSxmnKJXGMdMLbugPfLyUPmuLSMwKS")]
    [InlineData("sr1Ghq66tYK9y3r8CC1Tf8i8m5nxh8nTvZEf")]
    public void Address_MatchesAllImplicitAndOriginatedPrefixes(string address)
    {
        Assert.Matches(Regexes.Address(), address);
        Assert.Matches(Regexes.AddressWithEntrypoint(), address);
        Assert.Matches(Regexes.AddressWithEntrypoint(), $"{address}%transfer");
    }

    [Theory]
    [InlineData("tz6M7PdBGU7rHfCBmX21EHq8tsHRYS3ZXVdp")]
    [InlineData("tz5M7PdBGU7rHfCBmX21EHq8tsHRYS3ZXVd")]
    [InlineData("tz5M7PdBGU7rHfCBmX21EHq8tsHRYS3ZXVdpp")]
    [InlineData("mdpk")]
    public void Address_RejectsInvalidValues(string address)
    {
        Assert.DoesNotMatch(Regexes.Address(), address);
        Assert.DoesNotMatch(Regexes.AddressWithEntrypoint(), address);
    }

    [Fact]
    public void TzAddress_MatchesTz5()
    {
        Assert.Matches(Regexes.TzAddress(), "tz5M7PdBGU7rHfCBmX21EHq8tsHRYS3ZXVdp");
    }
}
