using System.Collections.Generic;

namespace DelugeSync.Models
{
    public class HttpProfileSetting
    {
        public bool Enabled { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseUrl { get; set; }
        public int DownloadChunks { get; set; }
        public int MaxConnections { get; set; }
        public bool NagleAlgorithm { get; set; }
        public bool BetaOptions { get; set; }
        public int ConnectionIdleTimeout { get; set; }
        public List<FileProfileSetting>? FileProfiles { get; set; }
    }

    public class FileProfileSetting
    {
        public string searchCriteria { get; set; }
        public string saveLocationRelative { get; set; }
    }
}