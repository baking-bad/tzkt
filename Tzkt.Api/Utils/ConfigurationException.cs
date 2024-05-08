﻿namespace Tzkt.Api
{
    class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base($"Bad configuration: {message}") { }
    }
}
