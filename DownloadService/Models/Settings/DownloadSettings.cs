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
        public bool EnableBetaOptions { get; set; } = false;
        public BetaOptions BetaOptions { get; set; } = new BetaOptions();
        public DefaultServicePointSettings DefaultServicePointSettings { get; set; } = new DefaultServicePointSettings();
    }
    public class BetaOptions
    {
        public bool UnsafeAuthenticatedConnectionSharing { get; set; } = true;
        public bool PreAuthenticate { get; set; } = true;
        public bool AllowWriteStreamBuffering { get; set; } = false;
        public bool Pipelined { get; set; } = false;
    }
}
