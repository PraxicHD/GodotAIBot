using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GodotAIBot;

public sealed class GodotKnowledgeBase
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "this", "that", "from", "into", "about", "have", "your",
        "you", "are", "how", "what", "when", "where", "why", "can", "should", "does", "using",
        "use", "make", "build", "create", "help", "need", "want", "godot"
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly List<DocChunk> _chunks = new();

    public async Task<int> LoadAsync()
    {
        if (_chunks.Count > 0)
        {
            return _chunks.Count;
        }

        const string rootUrl = "https://docs.godotengine.org/en/stable/";
        var html = await _httpClient.GetStringAsync(rootUrl);
        var loadedChunks = new List<DocChunk>();

        var links = Regex.Matches(html, "href=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .Where(link => link.StartsWith("/en/stable/", StringComparison.OrdinalIgnoreCase) && link.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Take(100)
            .ToList();

        foreach (var link in links)
        {
            var fullUrl = "https://docs.godotengine.org" + link;
            var content = await _httpClient.GetStringAsync(fullUrl);
            var title = ExtractTitle(content, fullUrl);
            var plainText = ConvertHtmlToPlainText(content);

            foreach (var chunk in CreateChunks(fullUrl, title, plainText))
            {
                loadedChunks.Add(chunk);
            }
        }

        _chunks.AddRange(loadedChunks);
        return _chunks.Count;
    }

    public Task<List<DocChunk>> SearchAsync(string query, int maxResults)
    {
        if (_chunks.Count == 0)
        {
            return Task.FromResult(new List<DocChunk>());
        }

        var terms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2)
            .Where(term => !StopWords.Contains(term))
            .Distinct()
            .ToList();

        if (terms.Count == 0)
        {
            return Task.FromResult(new List<DocChunk>());
        }

        var ranked = _chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = ScoreChunk(chunk, terms, query)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.Source)
            .Take(maxResults)
            .Select(result => result.Chunk with { Text = CreateFocusedSnippet(result.Chunk.Text, terms) })
            .ToList();

        return Task.FromResult(ranked);
    }

    private static string ExtractTitle(string html, string fallbackUrl)
    {
        var match = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return fallbackUrl;
        }

        var title = Regex.Replace(match.Groups[1].Value, "\\s+", " ").Trim();
        return WebUtility.HtmlDecode(title);
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        var withoutScripts = Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var withoutStyles = Regex.Replace(withoutScripts, "<style.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var plainText = Regex.Replace(withoutStyles, "<.*?>", " ");
        plainText = WebUtility.HtmlDecode(plainText);
        return Regex.Replace(plainText, "\\s+", " ").Trim();
    }

    private static IEnumerable<DocChunk> CreateChunks(string source, string title, string plainText)
    {
        const int chunkSize = 520;
        const int overlap = 120;

        if (string.IsNullOrWhiteSpace(plainText))
        {
            yield break;
        }

        for (var index = 0; index < plainText.Length; index += chunkSize - overlap)
        {
            var length = Math.Min(chunkSize, plainText.Length - index);
            var chunk = plainText.Substring(index, length).Trim();
            if (chunk.Length < 120)
            {
                continue;
            }

            yield return new DocChunk(source, title, chunk);
        }
    }

    private static int ScoreChunk(DocChunk chunk, List<string> terms, string rawQuery)
    {
        var score = terms.Sum(term => ScoreTermMatch(chunk.Text, term) + (chunk.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ? 5 : 0));

        if (chunk.Text.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
        }

        return score;
    }

    private static string CreateFocusedSnippet(string text, List<string> terms)
    {
        const int radius = 150;
        var firstMatchingTerm = terms.FirstOrDefault(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(firstMatchingTerm))
        {
            return text.Length <= radius * 2 ? text : text[..(radius * 2)].Trim() + "...";
        }

        var index = text.IndexOf(firstMatchingTerm, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, index - radius);
        var length = Math.Min(radius * 2, text.Length - start);
        var snippet = text.Substring(start, length).Trim();

        if (start > 0)
        {
            snippet = "..." + snippet;
        }

        if (start + length < text.Length)
        {
            snippet += "...";
        }

        return snippet;
    }

    private static int ScoreTermMatch(string text, string term)
    {
        var occurrences = Regex.Matches(text, Regex.Escape(term), RegexOptions.IgnoreCase).Count;
        return occurrences switch
        {
            0 => 0,
            1 => 3,
            2 => 5,
            _ => 6
        };
    }
}
