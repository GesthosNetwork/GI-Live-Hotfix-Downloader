using System;
using Models;

namespace Utils;

public static class CliHandler
{
    public static bool TryParseArgs(string[] args, out FetchArgs? parsedArgs)
    {
        parsedArgs = null;

        if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
        {
            PrintHelp();
            return false;
        }

        if (args.Length < 6) return false;

        try
        {
            string? branch = null, client = null, outDir = null, url = null, command = null, revision = null, audio = null;
            bool isBase = false;
            bool verbose = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--branch":
                        branch = args[++i];
                        break;
                    case "--client":
                        client = args[++i];
                        break;
                    case "--out":
                        outDir = args[++i];
                        break;
                    case "--url":
                        url = args[++i];
                        break;
                    case "--base":
                        isBase = true;
                        break;
                    case "--audio":
                        audio = args[++i];
                        break;
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        if (command == null)
                            command = args[i];
                        else if (revision == null)
                            revision = args[i];
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(branch) ||
                string.IsNullOrWhiteSpace(client) ||
                string.IsNullOrWhiteSpace(outDir) ||
                string.IsNullOrWhiteSpace(url) ||
                string.IsNullOrWhiteSpace(command) ||
                string.IsNullOrWhiteSpace(revision))
            {
                return false;
            }

            parsedArgs = new FetchArgs
            {
                Branch = branch,
                Clients = new List<string> { client },
                OutDir = outDir,
                Url = url,
                Command = command,
                Revision = revision,
                IsBase = isBase,
                AudioDiff = audio,
                Verbose = verbose
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  fetchrel.exe --branch <branch> --client <client> --out <outDir> --url <url> <command> <revision> [--base] [--audio <region>] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  fetchrel.exe --branch 1.0_live --client StandaloneWindows64 --out Downloads --url https://autopatchhk.yuanshen.com res 1284249_ba7ad33643");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --branch      Target branch (e.g. 1.0_live)");
        Console.WriteLine("  --client      Game client (e.g. StandaloneWindows64)");
        Console.WriteLine("  --out         Output directory (e.g. Downloads)");
        Console.WriteLine("  --url         Base URL to fetch from");
        Console.WriteLine("  --base        Mark the res file as base");
        Console.WriteLine("  --audio       Specify audio diff region (e.g. cn)");
        Console.WriteLine("  --verbose     Enable verbose output");
        Console.WriteLine("  -h, --help    Show this help message");
    }
}
