using System;
using System.IO;
using System.Text.Json;

internal static class Release
{
    public static void Main()
    {
        try
        {
            string configPath = "config.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine("config.json not found.");
                return;
            }

            Console.WriteLine("Reading config.json...");
            string jsonText = File.ReadAllText(configPath);
            JsonDocument doc = JsonDocument.Parse(jsonText);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("config", out var global))
            {
                Console.WriteLine("Missing 'config' section in config.json.");
                return;
            }

            string BRANCH = TryGetString(global, "branch");
            string CLIENT = TryGetString(global, "client");
            string OUTDIR = Path.GetFullPath(TryGetString(global, "outDir"));

            if (!root.TryGetProperty(BRANCH, out var branch))
            {
                Console.WriteLine($"Branch '{BRANCH}' not found.");
                return;
            }

            var (res_CODE, res_SUFFIX) = TrySplit(branch, "res");
            var (silence_CODE, silence_SUFFIX) = TrySplit(branch, "silence");
            var (data_CODE, data_SUFFIX) = TrySplit(branch, "data");

            string BRANCH_VERSION = BRANCH.Split('_')[0] + ".0";

            string targetDir = Path.Combine(OUTDIR, $"OSRELWin{BRANCH_VERSION}_R{res_CODE}_S{silence_CODE}_D{data_CODE}", "GenshinImpact_Data", "Persistent");
            Console.WriteLine("Creating target directory: " + targetDir);
            Directory.CreateDirectory(targetDir);

            string path1 = Path.Combine(OUTDIR, "client_game_res", BRANCH, $"output_{res_CODE}_{res_SUFFIX}", "client", CLIENT);
            string path2 = Path.Combine(OUTDIR, "client_design_data", BRANCH, $"output_{silence_CODE}_{silence_SUFFIX}", "client_silence", "General", "AssetBundles");
            string path3 = Path.Combine(OUTDIR, "client_design_data", BRANCH, $"output_{data_CODE}_{data_SUFFIX}", "client", "General", "AssetBundles");

            Console.WriteLine("Checking Version res...");
            if (!CopyVersion(path1, "release_res_versions_external", targetDir, "res_versions_persist"))
                CopyVersion(path1, "res_versions_external", targetDir, "res_versions_persist");

            CopyVersion(path2, "data_versions", targetDir, "silence_data_versions_persist");
            CopyVersion(path3, "data_versions", targetDir, "data_versions_persist");
            CopyVersion(path3, "data_versions_medium", targetDir, "data_versions_medium_persist");

            Console.WriteLine("Copying assets...");
            CopyAssets(path1, "AssetBundles", Path.Combine(targetDir, "AssetBundles"));
            CopyAssets(path1, "AudioAssets", Path.Combine(targetDir, "AudioAssets"), "audio_versions");
            CopyAssets(path1, "VideoAssets", Path.Combine(OUTDIR, $"OSRELWin{BRANCH_VERSION}_R{res_CODE}_S{silence_CODE}_D{data_CODE}", "GenshinImpact_Data", "StreamingAssets", "VideoAssets"), "video_versions");

            CopyAssets(path2, null, Path.Combine(targetDir, "AssetBundles"));
            CopyAssets(path3, null, Path.Combine(targetDir, "AssetBundles"));

            File.WriteAllText(Path.Combine(targetDir, "res_revision"), res_CODE);
            File.WriteAllText(Path.Combine(targetDir, "silence_revision"), silence_CODE);
            File.WriteAllText(Path.Combine(targetDir, "data_revision"), data_CODE);

            Console.WriteLine("SUCCESS: Completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("FATAL ERROR: " + ex.GetType().Name + " - " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        Console.ReadLine();
    }

    static string TryGetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var val))
            throw new Exception($"Missing key: {name}");
        if (val.ValueKind != JsonValueKind.String)
            throw new Exception($"Invalid string for key: {name}");
        return val.GetString()!;
    }

    static (string, string) TrySplit(JsonElement obj, string name)
    {
        string raw = TryGetString(obj, name);
        var parts = raw.Split('_');
        if (parts.Length != 2)
            throw new Exception($"Invalid format in key '{name}': expected 'CODE_SUFFIX'");
        return (parts[0], parts[1]);
    }

    static bool CopyVersion(string fromDir, string filename, string toDir, string newName)
    {
        string src = Path.Combine(fromDir, filename);
        string dst = Path.Combine(toDir, newName);
        Console.WriteLine($"Copying {src} → {dst}");

        if (!File.Exists(src))
        {
            Console.WriteLine("Not found: " + src);
            return false;
        }

        Directory.CreateDirectory(toDir);
        File.Copy(src, dst, overwrite: true);
        return true;
    }

    static void CopyAssets(string baseDir, string? subDir, string destDir, string? exclude = null)
    {
        string source = subDir == null ? baseDir : Path.Combine(baseDir, subDir);
        Console.WriteLine($"Copying from {source} to {destDir}");
        if (!Directory.Exists(source))
        {
            Console.WriteLine("Skip missing folder: " + source);
            return;
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            if (exclude != null && Path.GetFileName(file).Equals(exclude, StringComparison.OrdinalIgnoreCase))
                continue;

            string rel = Path.GetRelativePath(source, file);
            string dst = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(file, dst, overwrite: true);
        }
    }
}
