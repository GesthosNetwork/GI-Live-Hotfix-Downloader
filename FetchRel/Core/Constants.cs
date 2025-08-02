using System.Collections.Generic;

namespace Core
{
    public static class Constants
    {
        public static Dictionary<string, bool> ResFiles { get; set; } = new();
        public static Dictionary<string, bool> ResLegacyFiles { get; set; } = new();
        public static Dictionary<string, bool> ResUnlistedFiles { get; set; } = new();
        public static Dictionary<string, List<string>> DirMappings { get; set; } = new();
        public static Dictionary<string, List<string>> NameMappings { get; set; } = new();
        public static Dictionary<string, bool> SilenceFiles { get; set; } = new();
        public static Dictionary<string, bool> DataFiles { get; set; } = new();
    }
}
