using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace DownloadService.Models.Settings
{
    public class ServicePointSettings
    {
        public static bool Expect100Continue { get; set; } = false;
        public static bool UseNagleAlgorithm { get; set; } = true;
        public static int DefaultConnectionLimit { get; set; } = 8;
        public static int IdleConnectionSeconds { get; set; } = 10;
        private static int MaxServicePointIdleTime { get; set; } = (IdleConnectionSeconds * 1000);
        public static RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; } = delegate { return true; };

        public Task InitServicePointSettings()
        {
            try
            {
                ServicePointManager.Expect100Continue = Expect100Continue;
                ServicePointManager.UseNagleAlgorithm = UseNagleAlgorithm;
                ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;
                ServicePointManager.MaxServicePointIdleTime = MaxServicePointIdleTime;
            } catch (Exception ex)
            {
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
