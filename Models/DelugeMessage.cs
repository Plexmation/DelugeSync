using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        public Uri GetUrl(string searchRef, string baseUrl)
        {
            try
            {
                if (TorrentPath.Contains(searchRef))
                {
                    var startIndex = TorrentPath.IndexOf(searchRef) + searchRef.Length;
                    var lengthIndex = TorrentPath.Length - startIndex;
                    var location = TorrentPath.Substring(startIndex,lengthIndex);
                    //TODO: might need to add filename - will check on amqp-publish side
                    var fullLink = new Uri(SanitiseEnd(baseUrl) + $"/{searchRef}/" + location);
                    return fullLink;
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

        public string GetFilename(string baseLocation)
        {
            try
            {
                var startIndex = TorrentPath.LastIndexOf('/') + 1;
                return SanitiseEnd(baseLocation) + "/" + TorrentPath.Substring(startIndex, TorrentPath.Length - startIndex);
            } catch (Exception ex)
            {
                _logger.LogError("The following exception ocurred while trying to parse the filename:\n" + ex.Message);
                return null;
            }
        }
        public static string GetFilenameFromDownloadUrl(string Url, string baseLocation, string searchCriteria, bool includeFolder = false)
        {
            try
            {
                var subdir = "";
                var sanUrl = SanitiseEnd(Url);
                var startIndex = sanUrl.LastIndexOf('/') + 1;
                var UrlSegments = sanUrl.Split('/');
                var newUrl = "";
                if (includeFolder)
                {
                    string subDirectory = SanitiseEnd(baseLocation) + $"/{searchCriteria}/{UrlSegments[UrlSegments.Length - 2]}/";
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
