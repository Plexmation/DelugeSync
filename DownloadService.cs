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
    public static class DownloadService
    {
        public static ILogger _logger;
        public static int IdleConnectionSeconds = 10;
        public static int DownloadChunks = 4;
        public static int MaxConnections = 8;
        static DownloadService()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = MaxConnections;
            ServicePointManager.MaxServicePointIdleTime = (IdleConnectionSeconds * 1000);
        }
        public static DownloadResult Download(string fileUrl, string destinationFolderPath, int numberOfParallelDownloads = 0, bool validateSSL = false, NetworkCredential credentials = null)
        {
            try
            {
                _logger.LogInformation("Starting download...");
                if (!validateSSL)
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                }

                Uri uri = new Uri(fileUrl);
                //string destinationFilePath = Path.Combine(destinationFolderPath, uri.Segments.Last());
                string destinationFilePath = destinationFolderPath;

                DownloadResult result = new DownloadResult() { FilePath = destinationFilePath };

                //Basically parallel downloads must = chunks
                if (numberOfParallelDownloads <= 0)
                {
                    numberOfParallelDownloads = DownloadChunks;
                }

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

                using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Append))
                {
                    ConcurrentDictionary<int, string> tempFilesDictionary = new ConcurrentDictionary<int, string>();

                    #region Calculate ranges  
                    List<Range> readRanges = new List<Range>();
                    for (int chunk = 0; chunk < numberOfParallelDownloads - 1; chunk++)
                    {
                        var range = new Range()
                        {
                            Start = chunk * (responseLength / numberOfParallelDownloads),
                            End = ((chunk + 1) * (responseLength / numberOfParallelDownloads)) - 1
                        };
                        readRanges.Add(range);
                    }


                    readRanges.Add(new Range()
                    {
                        Start = readRanges.Any() ? readRanges.Last().End + 1 : 0,
                        End = responseLength - 1
                    });

                    #endregion

                    DateTime startTime = DateTime.Now;

                    #region Parallel download  

                    int index = 0;
                    Parallel.ForEach(readRanges, new ParallelOptions() { MaxDegreeOfParallelism = numberOfParallelDownloads }, readRange =>
                    {
                        HttpWebRequest httpWebRequest = HttpWebRequest.Create(fileUrl) as HttpWebRequest;
                        httpWebRequest.Method = "GET";
                        if (credentials != null)
                        {
                            httpWebRequest.Credentials = credentials;
                        }
                        httpWebRequest.AddRange(readRange.Start, readRange.End);
                        using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
                        {
                            string tempFilePath = Path.GetTempFileName();
                            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                httpWebResponse.GetResponseStream().CopyTo(fileStream);
                                tempFilesDictionary.TryAdd((int)index, tempFilePath);
                                fileStream.Close();
                            }
                            httpWebResponse.Close();
                        }
                        index++;
                    });

                    result.ParallelDownloads = index;

                    #endregion

                    result.TimeTaken = DateTime.Now.Subtract(startTime);

                    #region Merge to single file  
                    foreach (var tempFile in tempFilesDictionary.OrderBy(b => b.Key))
                    {
                        byte[] tempFileBytes = File.ReadAllBytes(tempFile.Value);
                        destinationStream.Write(tempFileBytes, 0, tempFileBytes.Length);
                        File.Delete(tempFile.Value);
                    }
                    #endregion

                    destinationStream.Flush();
                    destinationStream.Close();
                    destinationStream.Dispose();
                    
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
