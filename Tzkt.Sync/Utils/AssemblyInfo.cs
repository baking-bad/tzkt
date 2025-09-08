using System.Reflection;

namespace Tzkt.Sync
{
    public static class AssemblyInfo
    {
        public static string Name { get; }
        public static string Version { get; }

        static AssemblyInfo()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            Name = assembly.Name ?? string.Empty;
            Version = assembly.Version?.ToString() ?? string.Empty;
        }
    }
}
