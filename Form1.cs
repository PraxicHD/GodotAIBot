using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GodotAIBot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Attach the GetDocs method to the button's click event
            btnLoadDocs.Click += GetDocs;
        }

        // This method runs when the user clicks the "Load Docs" button
        private async void GetDocs(object? sender, EventArgs e)
        {
            // Step 1: Main Godot documentation page URL
            string url = "https://docs.godotengine.org/en/stable/";

            // Step 2: Create an HttpClient to download web pages
            HttpClient client = new HttpClient();

            // Step 3: Download the main page HTML
            var html = await client.GetStringAsync(url);

            // Optional: confirm the main page was downloaded
            MessageBox.Show("Downloaded " + html.Length + " characters from the main page");

            // Step 4: Prepare a list to store all discovered documentation links
            List<string> urlStrings = new List<string>();

            // Step 5: Regex pattern to find all href attributes
            Regex regex = new Regex("href=\"([^\"]+)\"");

            // Step 6: Find all matches in the HTML
            MatchCollection matches = regex.Matches(html);

            // Step 7: Loop through matches and filter for valid doc pages
            foreach (Match match in matches)
            {
                string link = match.Groups[1].Value;

                // Only keep links that start with "/en/stable/" and end with ".html"
                if (link.StartsWith("/en/stable/") && link.EndsWith(".html"))
                {
                    urlStrings.Add(link);
                }
            }

            // Step 8: Create a dictionary to store chunks in memory
            // Key = "URL#chunkIndex", Value = chunk text
            Dictionary<string, string> godotChunksDict = new Dictionary<string, string>();

            // Step 9: Loop through each documentation URL
            for (int i = 0; i < urlStrings.Count; i++)
            {
                string currentUrl = urlStrings[i];

                // Prepend base URL since links are relative
                string fullUrl = "https://docs.godotengine.org" + currentUrl;

                // Download the page HTML
                string htmlContent = await client.GetStringAsync(fullUrl);

                // Convert HTML to plain text (strip all tags)
                string plainText = Regex.Replace(htmlContent, "<.*?>", String.Empty);

                // Step 9a: Split page into 500-character chunks
                int chunkIndex = 0;           // Tracks chunk number
                int start = 0;                // Start index for substring

                while (start < plainText.Length)
                {
                    // Calculate length of this chunk (handle last chunk shorter than 500)
                    int length = Math.Min(500, plainText.Length - start);

                    // Extract chunk
                    string chunk = plainText.Substring(start, length);

                    // Add chunk to dictionary with key = URL + "#" + chunkIndex
                    godotChunksDict.Add(fullUrl + "#" + chunkIndex, chunk);

                    // Move to next chunk
                    start += 500;
                    chunkIndex++;
                }
            }

            // Step 10: Inform user of total chunks downloaded
            MessageBox.Show("Downloaded " + godotChunksDict.Count + " documentation chunks from Godot!");
        }
    }
}