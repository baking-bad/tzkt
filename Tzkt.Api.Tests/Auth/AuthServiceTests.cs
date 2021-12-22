using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;
using Tzkt.Api.Services.Auth;
using Xunit;

namespace Tzkt.Api.Tests.Auth;

public class AuthServiceTests
{
    [Fact]
    public void PubKeyAuthTest()
    {
        string expectedError = null;
        string error = null;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                $"Auth/Samples/PubKeyAuthSample.json")
            .Build();

        var headers = new AuthHeaders();
        var auth = new PubKeyAuth(configuration);
        var config = configuration.GetAuthConfig();
        var credentials = config.Credentials.FirstOrDefault();
        var key = Key.FromBase58(configuration.GetSection("PrivKey").Value);

        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "The X-TZKT-USER header is required";
        Assert.Equal(expectedError, error);
        
        headers.User = "wrongName";
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "The X-TZKT-NONCE header is required";
        Assert.Equal(expectedError, error);

        headers.Nonce = (long)(DateTime.UtcNow.AddSeconds(-config.NonceLifetime - 1) - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "The X-TZKT-SIGNATURE header is required";
        Assert.Equal(expectedError, error);

        headers.Signature = "wrongSignature";
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = $"User {headers.User} doesn't exist";
        Assert.Equal(expectedError, error);
        
        headers.User = credentials.User;
        
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "Nonce too old.";
        Assert.StartsWith(expectedError, error);
        
        Assert.False(auth.TryAuthenticate(headers, AccessRights.Write, out error));
        expectedError = $"User {headers.User} doesn't have required permissions. {AccessRights.Write} required. {credentials.Access} granted";
        Assert.Equal(expectedError, error);    
        
        headers.Nonce = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "Invalid signature";
        Assert.Equal(expectedError, error);

        headers.Signature = key.Sign($"{headers.Nonce}").ToBase58();

        Assert.True(auth.TryAuthenticate(headers, AccessRights.Read, out error));
        
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = $"Nonce {headers.Nonce} has already been used";
        Assert.Equal(expectedError, error);

        string json = null;
        headers.Nonce = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, json, out error));
        expectedError = "The body is empty";
        Assert.Equal(expectedError, error);

        json = "{\"test\": \"test\"}";
        var hash =  Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
        headers.Signature = key.Sign($"{headers.Nonce}{hash}").ToBase58();
        Assert.True(auth.TryAuthenticate(headers, AccessRights.Read, json, out error));
    }
    
    [Fact]
    public void PasswordAuthTest()
    {
        string expectedError = null;
        string error = null;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Auth/Samples/PasswordAuthSample.json")
            .Build();

        var headers = new AuthHeaders();
        var auth = new PasswordAuth(configuration);
        var config = configuration.GetAuthConfig();
        var credentials = config.Credentials.FirstOrDefault();
        
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "The X-TZKT-USER header is required";
        Assert.Equal(expectedError, error);
        
        headers.User = "wrongName";
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "The X-TZKT-PASSWORD header is required";
        Assert.Equal(expectedError, error);

        headers.Password = "wrongPassword";
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = $"User {headers.User} doesn't exist";
        Assert.Equal(expectedError, error);
        
        headers.User = credentials.User;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, out error));
        expectedError = "Invalid password";
        Assert.StartsWith(expectedError, error);

        headers.Password = credentials.Password;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.Write, out error));
        expectedError = $"User {headers.User} doesn't have required permissions. {AccessRights.Write} required. {credentials.Access} granted";
        Assert.Equal(expectedError, error);    
        
        Assert.True(auth.TryAuthenticate(headers, AccessRights.Read, out error));
        
        string json = null;
        headers.Nonce = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        Assert.False(auth.TryAuthenticate(headers, AccessRights.None, json, out error));
        expectedError = "The body is empty";
        Assert.Equal(expectedError, error);

        json = "{\"test\": \"test\"}";
        Assert.True(auth.TryAuthenticate(headers, AccessRights.Read, json, out error));
    }
}