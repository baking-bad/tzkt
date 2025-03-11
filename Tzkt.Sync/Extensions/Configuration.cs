namespace Tzkt.Sync
{
    static class ConfigurationExtension
    {
        public static string GetDefaultConnectionString(this IConfiguration config)
        {
            return config.GetConnectionString("DefaultConnection") ?? throw new Exception("ConnectionString is missed");
        }
    }
}
