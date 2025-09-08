using System;

namespace UrlShortenerApp
{
    public class ShortUrlEntry
    {
        public string ShortCode { get; set; }
        public string OriginalUrl { get; set; }
        public int AccessCount { get; set; }
        public DateTime? Expiry { get; set; }
    }
} 