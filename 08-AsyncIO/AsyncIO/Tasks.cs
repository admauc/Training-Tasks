using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncIO
{
    public static class Tasks
    {


        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the synchronous way and can be used to compare the performace of sync \ async approaches. 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContent(this IEnumerable<Uri> uris)
        {
            return uris.Select(x => new WebClient().DownloadString(x));
        }

        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the asynchronous way and can be used to compare the performace of sync \ async approaches. 
        /// 
        /// maxConcurrentStreams parameter should control the maximum of concurrent streams that are running at the same time (throttling). 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <param name="maxConcurrentStreams">Max count of concurrent request streams</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContentAsync(this IEnumerable<Uri> uris, int maxConcurrentStreams)
        {
            var content = uris.Take(maxConcurrentStreams).Select(x => DownloadContentAsync(x));
            var contentQueue = new Queue<Task<string>>(content);
            while (contentQueue.Count != 0)
            {
                yield return contentQueue.Dequeue().Result;
                if (maxConcurrentStreams < uris.Count())
                {
                    contentQueue.Enqueue(DownloadContentAsync(uris.ElementAt(maxConcurrentStreams)));
                    maxConcurrentStreams++;
                }
            }
        }

        private static Task<string> DownloadContentAsync(Uri url)
        {
            return (new WebClient()).DownloadStringTaskAsync(url);
        }

        /// <summary>
        /// Calculates MD5 hash of required resource.
        /// 
        /// Method has to run asynchronous. 
        /// Resource can be any of type: http page, ftp file or local file.
        /// </summary>
        /// <param name="resource">Uri of resource</param>
        /// <returns>MD5 hash</returns>
        public async static Task<string> GetMD5Async(this Uri resource)
        {
            MD5 hash = MD5.Create();
            return await new WebClient().DownloadDataTaskAsync(resource)
            .ContinueWith(result => string.Join("", hash.ComputeHash(result.Result)
            .Select(str => str.ToString("X2"))));
        }

    }

}
