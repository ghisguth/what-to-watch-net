using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WhatToWatch
{
    public class WhatToWatch
    {
        private static string stateFileName = "state.txt";

        private Dictionary<string, string> state;

        public async Task Run(string[] args)
        {
            Console.WriteLine("what?!");

            this.state = this.ReadState();

            var urls = File.ReadAllLines("interests.txt");

            foreach (var url in urls)
            {
                await this.ProcessUrl(url);
            }

            this.WriteState();
        }

        private Dictionary<string, string> ReadState()
        {
            if (!File.Exists(stateFileName))
            {
                return new Dictionary<string, string>();
            }

            return File.ReadAllLines(stateFileName)
                .Select(s => s.Split('\t'))
                .Where(s => s.Length == 2)
                .GroupBy(s => s[0])
                .ToDictionary(s => s.Key, t => t.First()[1]);
        }

        private void WriteState()
        {
            File.WriteAllLines(stateFileName,
                this.state
                .Select(s => s.Key + "\t" + s.Value)
                .ToList());
        }

        private string GetState(string url)
        {
            string result;

            if (this.state.TryGetValue(url, out result))
            {
                return result;
            }

            return string.Empty;
        }

        private void SetState(string url, string state)
        {
            this.state[url] = state;
        }

        private async Task ProcessUrl(string url)
        {
            Console.WriteLine($"\x1b[94m{url}\x1b[0m");

            var html = await this.FetchTextAsync(url, 5000, Encoding.UTF8);

            if (string.IsNullOrEmpty(html))
            {
                Console.WriteLine("???");
            }
            else
            {
                string title = ParseTitle(html);

                var oldState = this.GetState(url);
                this.SetState(url, title);

                var updated = !string.Equals(title, oldState);

                title = ColorizeProgress(title, updated);

                Console.WriteLine(title);
            }
        }

        private static string ColorizeProgress(string title, bool updated)
        {
            title = title.Replace("[", "\x1b[92m[").Replace("]", "]\x1b[0m");

            if (updated)
            {
                title += " \x1b[93m(NEW!)\x1b[0m";
            }

            return title;
        }

        private static string ParseTitle(string html)
        {
            var titleRegex = new Regex("<span id=\"news-title\">(?<title>.*)</span>");
            var match = titleRegex.Match(html);
            var title = match.Success ? match.Groups["title"].Value : "?";
            return title;
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
