using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WhatToWatch
{
    public class WhatToWatch
    {
        public async Task Run(string[] args)
        {
            Console.WriteLine("what?!");

            var urls = File.ReadAllLines("interests.txt");

            foreach(var url in urls) 
            {
                await this.ProcessUrl(url);
            }
        }

        private async Task ProcessUrl(string url)
        {
            Console.WriteLine(url);

            var html = await this.FetchTextAsync(url, 5000, Encoding.UTF8);

            if(string.IsNullOrEmpty(html)) {
                Console.WriteLine("???");
            } else {
                var titleRegex = new Regex("<span id=\"news-title\">(?<title>.*)</span>");
                var match = titleRegex.Match(html);
                var title = match.Success ? match.Groups["title"].Value : "?";
                title = title.Replace("[", "\x1b[92m[").Replace("]", "]\x1b[0m");
                Console.WriteLine(title);
            }
        }
        
        public async Task<string> FetchTextAsync(string url, int timeoutInMilliseconds, Encoding encoding)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                var responseTask = await GetResponseAsync(request, timeoutInMilliseconds);
                if (responseTask == null)
                {
                    Console.Error.WriteLine("Url {0} fetch returned null", url);

                    return null;
                }

                using (var webResponse = responseTask)
                {
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (var reader = new StreamReader(responseStream, encoding))
                            {
                                return await reader.ReadToEndAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Url {0} fetch failed: {1}", url, ex);
            }

            return null;
        }

        private static async Task<WebResponse> GetResponseAsync(HttpWebRequest request, int timeoutInMilliseconds)
        {
            var responseTask = request.GetResponseAsync();

            var completedTask = await Task.WhenAny(responseTask, Task.Delay(timeoutInMilliseconds));
            if (completedTask != responseTask)
            {
                Console.Error.WriteLine("Url {0} fetch timed out", request.RequestUri.ToString());

                return null;
            }

            return await responseTask;
        }
    }
}
