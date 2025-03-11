namespace Tzkt.Api
{
    class ConfigurationException(string message) : Exception($"Bad configuration: {message}")
    {
    }
}
