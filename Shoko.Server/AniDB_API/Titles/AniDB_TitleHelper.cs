using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Shoko.Commons.Utils;

namespace Shoko.Server.AniDB_API.Titles
{
    public class AniDB_TitleHelper
    {
        // ensure that it doesn't try to download at the same time
        private static readonly object accessLock = new object();

        private static readonly string CacheFilePath = Path.Combine(ServerSettings.ApplicationPath, "anime-titles.xml");
        
        private static readonly string CacheFilePathTemp =
            Path.Combine(ServerSettings.ApplicationPath, "anime-titles.xml") + ".temp";

        private static readonly string CacheFilePathBak =
            Path.Combine(ServerSettings.ApplicationPath, "anime-titles.xml") + ".bak";

        private AniDBRaw_AnimeTitles cache;

        private static AniDB_TitleHelper instance;

        public static AniDB_TitleHelper Instance => instance ?? (instance = new AniDB_TitleHelper());

        public List<AniDBRaw_AnimeTitle_Anime> SearchTitle(string query)
        {
            try
            {
                if (cache == null) CreateCache();
                if (cache != null)
                {
                    // TODO Maybe sort this or something one day
                    ConcurrentBag<AniDBRaw_AnimeTitle_Anime> matches = new ConcurrentBag<AniDBRaw_AnimeTitle_Anime>();
                    Parallel.ForEach(cache.Animes.ToList(), anime =>
                    {
                        foreach (var animeTitle in anime.Titles)
                        {
                            if (animeTitle.TitleLanguage != ServerSettings.LanguagePreference &&
                                animeTitle.TitleLanguage != "en" && animeTitle.TitleLanguage != "x-jay") continue;
                            if (!animeTitle.Title.FuzzyMatches(query)) continue;
                            matches.Add(anime);
                            break;
                        }
                    });
                    return matches.OrderBy(a => a.AnimeID).ToList();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<AniDBRaw_AnimeTitle_Anime>();
        }

        private void CreateCache()
        {
            if (!File.Exists(CacheFilePath))
            {
                // first check if there's a temp file
                if (File.Exists(CacheFilePathTemp))
                {
                    File.Move(CacheFilePathTemp, CacheFilePath);
                }
                if (!File.Exists(CacheFilePath))
                    DownloadCache();
            }
            
            if (!File.Exists(CacheFilePath)) return;

            lock (accessLock)
            {
                // If data is stale, then redownload
                DateTime lastWriteTime = File.GetLastWriteTime(CacheFilePath);
                if (DateTime.Now - lastWriteTime > TimeSpan.FromHours(24))
                {
                    DownloadCache();
                }
            }

            // Load the file
            using (var stream = new FileStream(CacheFilePath, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AniDBRaw_AnimeTitles));
                if (serializer.Deserialize(stream) is AniDBRaw_AnimeTitles rawData)
                {
                    cache = rawData;
                }
            }
        }

        private void DownloadCache()
        {
            try
            {
                if (File.Exists(CacheFilePathTemp)) File.Delete(CacheFilePathTemp);
                // Download the file
                using (var client = new WebClient())
                {
                    client.Headers.Add("Accept-Encoding", "gzip");
                    client.DownloadFile("http://anidb.net/api/anime-titles.xml.gz", CacheFilePathTemp);
                }
                
                // backup the old one
                if (File.Exists(CacheFilePath)) File.Move(CacheFilePath, CacheFilePathBak);

                // rename new one
                File.Move(CacheFilePathTemp, CacheFilePath);

                // remove old one
                if (File.Exists(CacheFilePathBak)) File.Delete(CacheFilePathBak);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}