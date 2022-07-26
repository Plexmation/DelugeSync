using DownloadService.Models.Settings;
using DownloadService.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DelugeSync
{
    public class DownloadService : BaseService
    {
        private DownloadSettings _settings;

        public DownloadService(ILogger logger, DownloadSettings downloadSettings) : base(logger)
        {
            _settings = downloadSettings;
            _settings.ServicePointSettings.InitServicePointSettings();
        }
        public int DownloadChunks { get; set; } = 4;

        public async Task<DownloadResult> DownloadAsync(string fileUrl, string destinationFolderPath, NetworkCredential credentials = null, string tempFolderPath = null)
        {
            try
            {
                Uri uri = new Uri(fileUrl);
                string destinationFilePath;
                if (!string.IsNullOrEmpty(tempFolderPath))
                {
                    destinationFilePath = tempFolderPath;
                } else
                {
                    destinationFilePath = destinationFolderPath;
                }

                DownloadResult result = new DownloadResult() { FilePath = destinationFilePath };

                _logger.LogInformation("Requesting file information from server");
                #region Get file size  
                WebRequest webRequest = HttpWebRequest.Create(fileUrl);
                webRequest.Method = "HEAD";
                if (credentials != null) webRequest.Credentials = credentials;
                long responseLength;
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    responseLength = long.Parse(webResponse.Headers.Get("Content-Length"));
                    result.Size = responseLength;
                }
                #endregion

                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    ConcurrentDictionary<long, string> tempFilesDictionary = new ConcurrentDictionary<long, string>();

                    _logger.LogInformation("Calculating ranges");
                    #region Calculate ranges  
                    List<Range> readRanges = new List<Range>();
                    for (int chunk = 0; chunk < DownloadChunks - 1; chunk++)
                    {
                        var range = new Range()
                        {
                            Start = chunk * (responseLength / DownloadChunks),
                            End = ((chunk + 1) * (responseLength / DownloadChunks)) - 1
                        };
                        readRanges.Add(range);
                    }


                    readRanges.Add(new Range()
                    {
                        Start = readRanges.Any() ? readRanges.Last().End + 1 : 0,
                        End = responseLength
                    });

                    #endregion

                    DateTime startTime = DateTime.Now;

                    _logger.LogInformation("Starting download...");
                    #region Parallel download  

                    int index = 0;
                    await Parallel.ForEachAsync(readRanges, new ParallelOptions() { MaxDegreeOfParallelism = DownloadChunks }, async (readRange, cancellationToken) =>
                    {
                        try
                        {
                            HttpWebRequest httpWebRequest = HttpWebRequest.Create(fileUrl) as HttpWebRequest;
                            httpWebRequest.Method = "GET";
                            httpWebRequest.Proxy = null;
                            if (_settings.EnableBetaOptions)
                            {
                                httpWebRequest.UnsafeAuthenticatedConnectionSharing = _settings.BetaOptions.UnsafeAuthenticatedConnectionSharing;
                                httpWebRequest.PreAuthenticate = _settings.BetaOptions.PreAuthenticate;
                                httpWebRequest.AllowWriteStreamBuffering = _settings.BetaOptions.AllowWriteStreamBuffering;
                                httpWebRequest.Pipelined = _settings.BetaOptions.Pipelined;
    }
                            if (credentials != null)
                            {
                                httpWebRequest.Credentials = credentials;
                            }
                            httpWebRequest.AddRange(readRange.Start, readRange.End);
                            using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                            {
                                //TODO: Create own temp file config
                                string tempFilePath = Path.GetTempFileName();
                                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    await httpWebResponse.GetResponseStream().CopyToAsync(fileStream);
                                    var result = tempFilesDictionary.TryAdd(readRange.Start, tempFilePath);
                                    if (!result) _logger.LogError("Key already exists");
                                }
                            }
                        } catch (Exception ex)
                        {
                            _logger.LogError(ex.ToString());
                        }
                        index++;
                    });
                    result.ParallelDownloads = index;

                    #endregion

                    result.TimeTaken = DateTime.Now.Subtract(startTime);

                    _logger.LogInformation("Merging chunks");
                    #region Merge to single file  
                    foreach (var tempFile in tempFilesDictionary.OrderBy(b => b.Key))
                    {
                        byte[] tempFileBytes = File.ReadAllBytes(tempFile.Value);
                        destinationStream.Write(tempFileBytes, 0, tempFileBytes.Length);
                        File.Delete(tempFile.Value);
                    }
                    #endregion

                    _logger.LogInformation("Moving file from temporary location");
                    #region move to real location if temp file location is specified
                    if (!string.IsNullOrEmpty(tempFolderPath))
                    {
                        try
                        {
                            File.Move(destinationFilePath, destinationFolderPath, true);
                        } catch (Exception ex)
                        {
                            _logger.LogError($"Failed to move file: {ex.ToString()}");
                        }
                    }
                    #endregion

                    await destinationStream.FlushAsync();
                    await destinationStream.DisposeAsync();
                    
                    return result;
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return null;
            }
        }
    }
    internal class Range
    {
        public long Start { get; set; }
        public long End { get; set; }
    }
    public class DownloadResult
    {
        public long Size { get; set; }
        public string FilePath { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public int ParallelDownloads { get; set; }
    }
}
