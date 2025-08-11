using System;
using System.Linq;
using System.Threading.Tasks;
using Models;
using Utils;

class Program
{
    static async Task Main(string[] args)
    {
        WindowUtils.TrySetWideConsole();
        WindowUtils.CenterConsole();
		
        var configPath = "config.json";
        var fetchArgsList = ConfigLoader.Load(configPath);

        if (CliHandler.TryParseArgs(args, out FetchArgs? cliArgs))
        {
            await Fetcher.RunAsync(cliArgs!);
            Console.WriteLine("\nDone.\nPress Enter to exit...");
            Console.ReadLine();
            return;
        }

        foreach (var fetchArgs in fetchArgsList)
        {
            await Fetcher.RunAsync(fetchArgs);
            Console.WriteLine();
        }

        Console.WriteLine("Done.\nPress Enter to exit...");
        Console.ReadLine();
    }
}
