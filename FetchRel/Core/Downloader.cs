using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public static class Downloader
    {
        private static readonly HttpClient Client = new HttpClient();

        public static async Task DownloadFileAsync(string remotePath, string baseUrl, string outDir, int timeoutSeconds = 300)
        {
            var url = $"{baseUrl}/{remotePath}";
            var localPath = Path.Combine(outDir, remotePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(localPath))
            {
                Console.WriteLine($"[SKIP] {remotePath} already exists.");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                using var response = await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token
                );

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SKIP] {remotePath} optional file not found.");
                    return;
                }

                var dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                await using var file = File.Create(localPath);

                await stream.CopyToAsync(file, cts.Token);

                Console.WriteLine($"[GET] {remotePath}");
            }
            catch (OperationCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[TIMEOUT] {remotePath}");
                Console.ResetColor();
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
