using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Core
{
    public static class Downloader
    {
        private static readonly HttpClient Client = new HttpClient();

        public static async Task DownloadFileAsync(string remotePath, string baseUrl, string outDir)
        {
            var url = $"{baseUrl}/{remotePath}";
            var localPath = Path.Combine(outDir, remotePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(localPath))
            {
                Console.WriteLine($"[SKIP] {remotePath} already exists.");
                return;
            }

            try
            {
                var response = await Client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SKIP] {remotePath} optional file not found.");
                    return;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                await File.WriteAllBytesAsync(localPath, bytes);
                Console.WriteLine($"[GET] {remotePath}");
            }
            catch (HttpRequestException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Unable to connect to the internet.");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch {remotePath}; reason={ex.Message}");
            }
        }
    }
}
