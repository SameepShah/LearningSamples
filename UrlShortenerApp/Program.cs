using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UrlShortenerApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            bool useAzureStorage = config.GetValue<bool>("UseAzureStorage");
            string jsonFilePath = config["JsonFilePath"] ?? "urlshortener.json";
            AzureStorageHandler? azureHandler = null;
            string blobName = Path.GetFileName(jsonFilePath);

            if (useAzureStorage)
            {
                string connStr = config["Azure:ConnectionString"] ?? string.Empty;
                string container = config["Azure:ContainerName"] ?? "urlshortener-data";
                azureHandler = new AzureStorageHandler(connStr, container);
            }

            var shortener = new UrlShortener(jsonFilePath, useAzureStorage, azureHandler, blobName);
            await shortener.LoadAsync();

            while (true)
            {
                Console.WriteLine("\n--- URL Shortener Menu ---");
                Console.WriteLine("1. Shorten a URL");
                Console.WriteLine("2. Retrieve a URL");
                Console.WriteLine("3. View URL Statistics");
                Console.WriteLine("4. Exit");
                Console.Write("Select an option: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.Write("Enter the original URL: ");
                        var url = Console.ReadLine();
                        Console.Write("Set expiry? (y/n): ");
                        var expiryChoice = Console.ReadLine();
                        DateTime? expiry = null;
                        if (expiryChoice?.Trim().ToLower() == "y")
                        {
                            Console.Write("Enter expiry in days from now: ");
                            if (int.TryParse(Console.ReadLine(), out int days))
                            {
                                expiry = DateTime.UtcNow.AddDays(days);
                            }
                        }
                        var code = await shortener.ShortenUrlAsync(url!, expiry);
                        Console.WriteLine($"Shortened URL code: {code}");
                        break;
                    case "2":
                        Console.Write("Enter the short code: ");
                        var codeToRetrieve = Console.ReadLine();
                        var originalUrl = await shortener.RetrieveUrlAsync(codeToRetrieve!);
                        if (originalUrl != null)
                        {
                            Console.WriteLine($"Original URL: {originalUrl}");
                        }
                        else
                        {
                            Console.WriteLine("Short code not found or expired.");
                        }
                        break;
                    case "3":
                        Console.Write("Enter the short code: ");
                        var codeForStats = Console.ReadLine();
                        var stats = shortener.GetStats(codeForStats!);
                        if (stats != null)
                        {
                            Console.WriteLine($"Short Code: {stats.ShortCode}");
                            Console.WriteLine($"Original URL: {stats.OriginalUrl}");
                            Console.WriteLine($"Access Count: {stats.AccessCount}");
                            Console.WriteLine($"Expiry: {(stats.Expiry.HasValue ? stats.Expiry.Value.ToString("u") : "None")}");
                        }
                        else
                        {
                            Console.WriteLine("Short code not found.");
                        }
                        break;
                    case "4":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        break;
                }
            }
        }
    }
}
