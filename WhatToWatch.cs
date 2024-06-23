using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WhatToWatch;

class WhatToWatch
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
        if (this.state.TryGetValue(url, out var result))
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
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                Console.Error.WriteLine($"Url {url} fetch returned {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Url {url} fetch failed: {ex}");
        }

        return null;
    }
}