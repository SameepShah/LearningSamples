using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace UrlShortenerApp
{
    public class UrlShortener
    {
        private readonly string _jsonFilePath;
        private readonly bool _useAzureStorage;
        private readonly AzureStorageHandler? _azureHandler;
        private readonly string _blobName;
        private Dictionary<string, ShortUrlEntry> _urlMap = new();

        public UrlShortener(string jsonFilePath, bool useAzureStorage, AzureStorageHandler? azureHandler = null, string blobName = "urlshortener.json")
        {
            _jsonFilePath = jsonFilePath;
            _useAzureStorage = useAzureStorage;
            _azureHandler = azureHandler;
            _blobName = blobName;
        }

        public async Task LoadAsync()
        {
            if (_useAzureStorage && _azureHandler != null)
            {
                if (await _azureHandler.BlobExistsAsync(_blobName))
                {
                    await _azureHandler.DownloadFileAsync(_blobName, _jsonFilePath);
                }
            }
            if (File.Exists(_jsonFilePath))
            {
                var json = await File.ReadAllTextAsync(_jsonFilePath);
                _urlMap = JsonSerializer.Deserialize<Dictionary<string, ShortUrlEntry>>(json) ?? new();
            }
        }

        public async Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(_urlMap, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_jsonFilePath, json);
            if (_useAzureStorage && _azureHandler != null)
            {
                await _azureHandler.UploadFileAsync(_blobName, _jsonFilePath);
            }
        }

        public string GenerateShortCode(int length = 6)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_urlMap.ContainsKey(code));
            return code;
        }

        public async Task<string> ShortenUrlAsync(string originalUrl, DateTime? expiry = null)
        {
            var code = GenerateShortCode();
            var entry = new ShortUrlEntry
            {
                ShortCode = code,
                OriginalUrl = originalUrl,
                AccessCount = 0,
                Expiry = expiry
            };
            _urlMap[code] = entry;
            await SaveAsync();
            return code;
        }

        public async Task<string?> RetrieveUrlAsync(string code)
        {
            if (_urlMap.TryGetValue(code, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    _urlMap.Remove(code);
                    await SaveAsync();
                    return null;
                }
                entry.AccessCount++;
                await SaveAsync();
                return entry.OriginalUrl;
            }
            return null;
        }

        public ShortUrlEntry? GetStats(string code)
        {
            return _urlMap.TryGetValue(code, out var entry) ? entry : null;
        }

        public IEnumerable<ShortUrlEntry> FindUrlsByOriginalUrl(string originalUrl)
        {
            return _urlMap.Values
                .Where(entry => entry.OriginalUrl.Contains(originalUrl, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
} 