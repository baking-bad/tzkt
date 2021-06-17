using System;

namespace Tzkt.Api.Services.Auth
{
    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }
}
