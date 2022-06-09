using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelugeSync.Models
{
    public class DelugeMessage
    {
        public static ILogger _logger;
        public string TorrentId { get; set; }
        public string TorrentName { get; set; }
        public string TorrentPath { get; set; }
        public bool? IsSingle { get; set; }

        public Uri GetUrl(string searchRef, string baseUrl)
        {
            try
            {
                if (TorrentPath.Contains(searchRef))
                {
                    var splitPath = TorrentPath.Split('/').ToList();
                    var urlList = new List<string>();
                    if (splitPath.Count > 0)
                    {
                        var foundIndex = splitPath.IndexOf(searchRef);
                        var location = "";
                        foreach (var (item, index) in splitPath.Select((value, i) => (value, i)))
                        {
                            if (index > foundIndex)
                            {
                                urlList.Add(item);
                            }
                        }

                        urlList.ForEach(item => location += item + "/");

                        location = location.Remove(location.Count() - 1, 1);

                        //still need the filename to be append here

                        var fullLink = new Uri(SanitiseEnd(baseUrl) + $"/{searchRef}/" + location);
                        return fullLink;
                    } else
                    {
                        _logger.LogError("Could not split recieved path");
                        return null;
                    }
                    
                }
                else
                {
                    _logger.LogError($"Url does not contain search reference \"{searchRef}\"");
                    return null;
                }
            } catch (Exception ex)
            {
                _logger.LogError("The following exception ocurred while tring to parse the URL:\n" + ex.Message);
                return null;
            }
        }
        private static string SanitiseEnd(string url)
        {
            if (url.EndsWith('/'))
            {
                url = url.Remove(url.Length - 1);
            }
            return url;
        }
        public static string GetFilenameFromDownloadUrl(string Url, string baseLocation, string searchCriteria, bool includeFolder = false)
        {
            try
            {
                var sanUrl = SanitiseEnd(Url);
                var startIndex = sanUrl.LastIndexOf('/') + 1;
                var UrlSegments = sanUrl.Split('/');
                var newUrl = "";
                if (includeFolder)
                {
                    string subDirectory = SanitiseEnd(baseLocation) + $"/{searchCriteria}/";
                    newUrl = subDirectory + sanUrl.Substring(startIndex, sanUrl.Length - startIndex);
                    if (!Directory.Exists(subDirectory)) Directory.CreateDirectory(subDirectory); 
                }
                else
                {
                    newUrl = SanitiseEnd(baseLocation) + $"/{searchCriteria}/" + sanUrl.Substring(startIndex, sanUrl.Length - startIndex);
                }
                
                return newUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError("The following exception ocurred while trying to parse the filename:\n" + ex.Message);
                return null;
            }
        }
    }
}
