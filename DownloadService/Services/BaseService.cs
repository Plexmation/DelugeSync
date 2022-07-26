using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadService.Services
{
    public class BaseService
    {
        protected ILogger _logger;

        public BaseService(ILogger logger)
        {
            this._logger = logger;
        }
    }
}
