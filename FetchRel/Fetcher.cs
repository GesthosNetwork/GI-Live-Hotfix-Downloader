using System.Text;
using System.Text.Json;
using Core;
using Models;
using Utils;

public static class Fetcher
{
    public static async Task RunAsync(FetchArgs args)
    {
        string cleanedOutDir = args.OutDir.StartsWith("./") ? args.OutDir.Substring(2) : args.OutDir;
        Console.WriteLine($"Executing: fetchrel.exe --branch {args.Branch} --client {string.Join(",", args.Clients)} --out {cleanedOutDir} --url {args.Url} {args.Command} {args.Revision}\n");
        Console.WriteLine($"> {args.Command.ToUpper()} | {args.Branch} | {args.Revision}\n");

        switch (args.Command)
        {
            case "res":
                await ProcessRes(args);
                Console.WriteLine();
                break;
            case "silence":
                await ProcessSilence(args);
                Console.WriteLine();
                break;
            case "data":
                await ProcessData(args);
                break;
            default:
                Console.WriteLine($"[ERROR] Unsupported command: {args.Command}");
                break;
        }
    }

    private static async Task ProcessRes(FetchArgs args)
    {
        foreach (var client in args.Clients)
        {
            var basePath = $"client_game_res/{args.Branch}/output_{args.Revision}/client/{client}";

            foreach (var entry in Constants.ResFiles)
            {
                if (!entry.Value) continue;
                await Downloader.DownloadFileAsync($"{basePath}/{entry.Key}", args.Url, args.OutDir);
                await ParseResVersions(entry.Key, basePath, args.Url, args.OutDir, args.IsBase, entry.Value);
            }

            foreach (var entry in Constants.ResLegacyFiles.Concat(Constants.ResUnlistedFiles))
            {
                if (!entry.Value) continue;
                await Downloader.DownloadFileAsync($"{basePath}/{entry.Key}", args.Url, args.OutDir);
            }

            if (!string.IsNullOrEmpty(args.AudioDiff))
            {
                var audioFile = $"audio_diff_versions_{args.AudioDiff}";
                await Downloader.DownloadFileAsync($"{basePath}/{audioFile}", args.Url, args.OutDir);
                await ParseAudioVersions(audioFile, basePath, args.Url, args.OutDir, args.AudioDiff);
            }
        }
    }

    private static async Task ProcessData(FetchArgs args)
    {
        var basePath = $"client_design_data/{args.Branch}/output_{args.Revision}/client/General";

        foreach (var entry in Constants.DataFiles)
        {
            if (!entry.Value) continue;
            await Downloader.DownloadFileAsync($"{basePath}/{entry.Key}", args.Url, args.OutDir);
            await ParseDataVersions(entry.Key, basePath, args.Url, args.OutDir);
        }
    }

    private static async Task ProcessSilence(FetchArgs args)
    {
        var basePath = $"client_design_data/{args.Branch}/output_{args.Revision}/client_silence/General";

        foreach (var entry in Constants.SilenceFiles)
        {
            if (!entry.Value) continue;
            await Downloader.DownloadFileAsync($"{basePath}/{entry.Key}", args.Url, args.OutDir);
            await ParseDataVersions(entry.Key, basePath, args.Url, args.OutDir);
        }
    }

    private static async Task ParseResVersions(string name, string relPath, string baseUrl, string outDir, bool isBase, bool isAudio)
    {
        var fullPath = Path.Combine(outDir, relPath.Replace('/', Path.DirectorySeparatorChar), name);
        if (!File.Exists(fullPath))
            return;

        foreach (var line in File.ReadLines(fullPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string remoteName;
            bool isPatch;

            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(line);
                remoteName = json != null && json.TryGetValue("remoteName", out var rn) ? rn?.ToString() ?? "" : "";
                isPatch = json != null && json.TryGetValue("isPatch", out var ip) && ip?.ToString() == "True";
            }
            catch
            {
                var split = line.Split(' ');
                remoteName = split[0];
                isPatch = split.Length >= 3 && split[2] == "P";
            }

            if (string.IsNullOrEmpty(remoteName)) continue;

            var remoteDir = Constants.DirMappings.FirstOrDefault(kv => kv.Value.Contains(Path.GetExtension(remoteName))).Key
                            ?? Constants.NameMappings.FirstOrDefault(kv => kv.Value.Contains(remoteName)).Key
                            ?? "";

            if (!isBase && !isPatch) continue;
            if (!isAudio && (remoteDir == "AudioAssets" || remoteDir == "VideoAssets")) continue;

            var relFile = UrlHelper.Join(relPath, remoteDir, remoteName);
            await Downloader.DownloadFileAsync(relFile, baseUrl, outDir);
        }
    }

    private static async Task ParseDataVersions(string name, string relPath, string baseUrl, string outDir)
    {
        var fullPath = Path.Combine(outDir, relPath.Replace('/', Path.DirectorySeparatorChar), name);
        if (!File.Exists(fullPath))
            return;

        foreach (var line in File.ReadLines(fullPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string remoteName;

            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(line);
                remoteName = json != null && json.TryGetValue("remoteName", out var rn) ? rn?.ToString() ?? "" : "";
            }
            catch
            {
                remoteName = line.Split(' ')[0];
            }

            if (string.IsNullOrEmpty(remoteName)) continue;

            var remoteDir = Constants.DirMappings.FirstOrDefault(kv => kv.Value.Contains(Path.GetExtension(remoteName))).Key
                            ?? Constants.NameMappings.FirstOrDefault(kv => kv.Value.Contains(remoteName)).Key
                            ?? "";

            var relFile = UrlHelper.Join(relPath, remoteDir, remoteName);
            await Downloader.DownloadFileAsync(relFile, baseUrl, outDir);
        }
    }

    private static async Task ParseAudioVersions(string name, string relPath, string baseUrl, string outDir, string diff)
    {
        var fullPath = Path.Combine(outDir, relPath.Replace('/', Path.DirectorySeparatorChar), name);
        if (!File.Exists(fullPath))
            return;

        foreach (var line in File.ReadLines(fullPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string remoteName;

            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(line);
                remoteName = json != null && json.TryGetValue("remoteName", out var rn) ? rn?.ToString() ?? "" : "";
            }
            catch
            {
                remoteName = line.Split(' ')[0];
            }

            if (string.IsNullOrEmpty(remoteName)) continue;

            var remoteDir = $"AudioDiff_{diff}";
            var relFile = UrlHelper.Join(relPath, remoteDir, remoteName);
            await Downloader.DownloadFileAsync(relFile, baseUrl, outDir);
        }
    }
}
