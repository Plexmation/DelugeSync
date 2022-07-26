using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadService.Models.Settings
{
    public class DownloadSettings
    {
        public int DownloadChunks { get; set; } = 4;
        public bool ValidateSSL { get; set; } = false;
        public bool EnableBetaOptions { get; set; } = false;
        public BetaOptions BetaOptions { get; set; } = new BetaOptions();
        public ServicePointSettings ServicePointSettings { get; set; } = new ServicePointSettings();
    }
    public class BetaOptions
    {
        public bool UnsafeAuthenticatedConnectionSharing { get; set; } = true;
        public bool PreAuthenticate { get; set; } = true;
        public bool AllowWriteStreamBuffering { get; set; } = false;
        public bool Pipelined { get; set; } = false;
    }
}
