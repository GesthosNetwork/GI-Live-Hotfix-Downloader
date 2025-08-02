using System.Text.Json;
using Models;
using Core;

namespace Utils;

public static class ConfigLoader
{
    public static List<FetchArgs> Load(string configPath)
    {
        try
        {
            const string DEFAULT_URL = "https://autopatchhk.yuanshen.com";

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("config", out var global))
                ErrorExit("[ERROR] Missing 'config' section.");

            string client = global.GetProperty("client").GetString()!;
            string userDefinedUrl = global.GetProperty("url").GetString()!;
            string outDir = global.GetProperty("outDir").GetString()!;
            bool verbose = global.TryGetProperty("verbose", out var verboseProp) && verboseProp.GetBoolean();
            bool defaultBase = global.TryGetProperty("defaultBase", out var baseProp) && baseProp.GetBoolean();

            string? branchRaw = global.TryGetProperty("branch", out var branchVal) ? branchVal.GetString() : null;
            if (string.IsNullOrWhiteSpace(branchRaw))
                ErrorExit("[ERROR] Missing \"branch\" in config.json.");

            var targetBranches = branchRaw!.Split(',').Select(b => b.Trim()).Where(b => b != "").ToList();
            if (targetBranches.Count == 0)
                ErrorExit("[ERROR] No branch specified in config.json.");

            if (!root.TryGetProperty("system", out var system))
                ErrorExit("[ERROR] Missing 'system' section.");

            Constants.ResFiles = system.GetProperty("resFiles").Deserialize<Dictionary<string, bool>>()!;
            Constants.ResUnlistedFiles = system.GetProperty("resUnlistedFiles").Deserialize<Dictionary<string, bool>>()!;
            Constants.ResLegacyFiles = system.GetProperty("resLegacyFiles").Deserialize<Dictionary<string, bool>>()!;
            Constants.DirMappings = system.GetProperty("dirMappings").Deserialize<Dictionary<string, List<string>>>()!;
            Constants.NameMappings = system.GetProperty("nameMappings").Deserialize<Dictionary<string, List<string>>>()!;
            Constants.SilenceFiles = system.GetProperty("silenceFiles").Deserialize<Dictionary<string, bool>>()!;
            Constants.DataFiles = system.GetProperty("dataFiles").Deserialize<Dictionary<string, bool>>()!;

            var result = new List<FetchArgs>();
            bool urlChanged = false;
            string updatedUrl = userDefinedUrl;

            foreach (var branch in targetBranches)
            {
                if (!root.TryGetProperty(branch, out var branchObj))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Branch '{branch}' not found in config.json.");
                    Console.ResetColor();
                    continue;
                }

                string resolvedUrl = branch switch
                {
                    "3.2_live" => "https://ps.yuuki.me/data_game/genshin",
                    "3.4_live" => "https://mirror.autopatch-reversedrooms.workers.dev/anime-cn",
                    _ => DEFAULT_URL
                };

                if (resolvedUrl != userDefinedUrl)
                {
                    updatedUrl = resolvedUrl;
                    urlChanged = true;
                }

                var clients = new List<string> { client };

                string? GetRevision(JsonElement parent, string key)
                {
                    if (!parent.TryGetProperty(key, out var node)) return null;
                    return node.ValueKind switch
                    {
                        JsonValueKind.String => node.GetString(),
                        JsonValueKind.Object when node.TryGetProperty("revision", out var revProp) => revProp.GetString(),
                        _ => null
                    };
                }

                string? audio = null;
                bool isBase = defaultBase;

                if (branchObj.TryGetProperty("res", out var resNode))
                {
                    if (resNode.ValueKind == JsonValueKind.Object)
                    {
                        if (resNode.TryGetProperty("audio", out var audioNode))
                            audio = audioNode.GetString();

                        if (resNode.TryGetProperty("base", out var baseNodeVal) &&
                            (baseNodeVal.ValueKind == JsonValueKind.True || baseNodeVal.ValueKind == JsonValueKind.False))
                            isBase = baseNodeVal.GetBoolean();
                    }
                }

                void AddTask(string command, string? revision, bool isBaseFlag = false, string? audioDiff = null)
                {
                    if (!string.IsNullOrEmpty(revision))
                    {
                        result.Add(new FetchArgs
                        {
                            Command = command,
                            Branch = branch,
                            Clients = clients,
                            Url = resolvedUrl,
                            OutDir = outDir,
                            Revision = revision,
                            IsBase = isBaseFlag,
                            AudioDiff = audioDiff,
                            Verbose = verbose
                        });
                    }
                }

                AddTask("res", GetRevision(branchObj, "res"), isBase, audio);
                AddTask("silence", GetRevision(branchObj, "silence"));
                AddTask("data", GetRevision(branchObj, "data"));
            }

            if (result.Count == 0)
                ErrorExit("[ERROR] No valid branches found to process.");

            if (urlChanged || updatedUrl != userDefinedUrl)
            {
                var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
                var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(global.GetRawText())!;
                configDict["url"] = updatedUrl;
                rootDict["config"] = configDict;
                var newJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, newJson);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Exception while loading config.json: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
            return null!;
        }
    }

    private static void ErrorExit(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(1);
    }
}
