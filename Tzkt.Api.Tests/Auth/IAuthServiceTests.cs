using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Netezos.Keys;
using Tzkt.Api.Services.Auth;
using Xunit;

namespace Tzkt.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void PubKeyAuthTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                $"Auth/Samples/ReadPubKey.json")
            .Build();

        var auth = new PubKeyAuth(configuration);
        
        var date = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        var key = Key.FromBase58(configuration.GetSection("PrivKey").Value);
        var sign = key.Sign($"{date}").ToBase58();

        var authConfig = configuration.GetAuthConfig().Credentials.FirstOrDefault();
        var headers = new AuthHeaders()
        {
            User = authConfig.User,
            Signature = sign,
            Nonce = date
        };
        
        var expectedError = $"User {headers.User} doesn't have required permissions. {AuthRights.Write} required. {authConfig.AuthRights} granted";


        Assert.True(auth.TryAuthenticate(headers, AuthRights.Read, out _));
        
        date = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        sign = key.Sign($"{date}").ToBase58();

        headers = new AuthHeaders()
        {
            User = authConfig.User,
            Signature = sign,
            Nonce = date
        };
        
        Assert.False(auth.TryAuthenticate(headers, AuthRights.Write, out var error));
        Assert.Equal(expectedError, error);
    }
}