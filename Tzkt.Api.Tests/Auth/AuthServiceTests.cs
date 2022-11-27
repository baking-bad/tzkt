using System;
using System.Linq;
using System.Threading.Tasks;
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
        string? error;
        string? expectedError;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Auth/Samples/PubKeyAuthSample.json")
            .Build();

        var auth = new PubKeyAuth(configuration);
        var config = configuration.GetAuthConfig();
        var key = Key.FromBase58(configuration.GetSection("PrivKey").Value);

        foreach (var credentials in config.Users)
        {
            var rights = new AccessRights()
            {
                Table = "WrongTable",
                Access = Access.Write
            };
            
            var headers = new AuthHeaders();

            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "The X-TZKT-USER header is required";
            Assert.Equal(expectedError, error);
        
            headers.User = "wrongName";
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "The X-TZKT-NONCE header is required";
            Assert.Equal(expectedError, error);

            headers.Nonce = 253402300800000;
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "The X-TZKT-SIGNATURE header is required";
            Assert.Equal(expectedError, error);

            headers.Signature = "wrongSignature";
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = $"User {headers.User} doesn't exist";
            Assert.Equal(expectedError, error);
            
            headers.User = credentials.Name;
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "Nonce out of range.";
            Assert.Equal(expectedError, error);
        
            headers.Nonce = (long)(DateTime.UtcNow.AddSeconds(-config.NonceLifetime - 1) - DateTime.UnixEpoch).TotalMilliseconds;
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "Nonce too old.";
            Assert.StartsWith(expectedError, error);
        
            headers.Nonce = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

            if (credentials.Rights != null)
            {
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {rights.Table} required.";
                Assert.StartsWith(expectedError, error);

                rights.Table = credentials.Rights.FirstOrDefault(x => x.Section == null)?.Table;
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {Access.Write} required.";
                Assert.StartsWith(expectedError, error);

                rights.Section = "wrongSection";
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {rights.Section} required.";
                Assert.StartsWith(expectedError, error);
                
                rights.Section = credentials.Rights.FirstOrDefault(x => x.Section != null)?.Section;
                rights.Access = Access.Write;
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {rights.Access} required for section {rights.Section}.";
                Assert.StartsWith(expectedError, error);
            }

            rights.Access = Access.Read;

            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "Invalid signature";
            Assert.Equal(expectedError, error);

            headers.Signature = key.Sign($"{headers.Nonce}").ToBase58();

            Assert.True(auth.TryAuthenticate(headers, rights, out error));
        
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = $"Nonce {headers.Nonce} has already been used";
            Assert.Equal(expectedError, error);

            Task.Delay(10);
            string? json = null;
            headers.Nonce = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
            Assert.False(auth.TryAuthenticate(headers, rights, json, out error));
            expectedError = "Request body is empty";
            Assert.Equal(expectedError, error);

            json = "{\"test\": \"test\"}";
            var hash =  Hex.Convert(Blake2b.GetDigest(Utf8.Parse(json)));
            headers.Signature = key.Sign($"{headers.Nonce}{hash}").ToBase58();
            Assert.True(auth.TryAuthenticate(headers, rights, json, out error));
        }
    }
    
    [Fact]
    public void PasswordAuthTest()
    {
        string? error;
        string? expectedError;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("Auth/Samples/PasswordAuthSample.json")
            .Build();

        var auth = new PasswordAuth(configuration);
        var config = configuration.GetAuthConfig();

        foreach (var credentials in config.Users)
        {
            var rights = new AccessRights()
            {
                Table = "WrongTable",
                Section = "WrongSection",
                Access = Access.Write
            };
        
            var headers = new AuthHeaders();
            
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "The X-TZKT-USER header is required";
            Assert.Equal(expectedError, error);
        
            headers.User = "wrongName";
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "The X-TZKT-PASSWORD header is required";
            Assert.Equal(expectedError, error);

            headers.Password = "wrongPassword";
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = $"User {headers.User} doesn't exist";
            Assert.Equal(expectedError, error);
        
            headers.User = credentials.Name;
            Assert.False(auth.TryAuthenticate(headers, rights, out error));
            expectedError = "Invalid password";
            Assert.StartsWith(expectedError, error);

            headers.Password = credentials.Password;

            if (credentials.Rights != null)
            {
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {rights.Table} required.";
                Assert.StartsWith(expectedError, error);

                rights.Table = credentials.Rights.FirstOrDefault()?.Table;
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {rights.Section} required.";
                Assert.StartsWith(expectedError, error);   
        
                rights.Section = credentials.Rights.FirstOrDefault()?.Section;
                Assert.False(auth.TryAuthenticate(headers, rights, out error));
                expectedError = $"User {headers.User} doesn't have required permissions. {Access.Write} required.";
                Assert.StartsWith(expectedError, error);    
            }

            rights.Access = Access.Read;
            Assert.True(auth.TryAuthenticate(headers, rights, out error));
        
            string? json = null;
            Assert.False(auth.TryAuthenticate(headers, rights, json, out error));
            expectedError = "Request body is empty";
            Assert.Equal(expectedError, error);

            json = "{\"test\": \"test\"}";
            Assert.True(auth.TryAuthenticate(headers, rights, json, out error));
        }
        

    }
}