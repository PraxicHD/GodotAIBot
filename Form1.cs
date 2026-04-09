using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GodotAIBot;

public partial class Form1 : Form
{
    private readonly GodotAssistantBot _bot = new();

    public Form1()
    {
        InitializeComponent();

        btnLoadDocs.Click += async (_, _) => await LoadDocsAsync();
        btnSend.Click += async (_, _) => await SendMessageAsync();
        btnClearChat.Click += (_, _) =>
        {
            txtConversation.Clear();
            ShowWelcomeMessage();
        };
        txtInput.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendMessageAsync();
            }
        };

        ShowWelcomeMessage();
    }


    private void ShowWelcomeMessage()
    {
        var welcome =
            "👋 Welcome to GodotBot!\n" +
            "I am your personal Godot engine assistant.\n\n" +
            "Quick start:\n" +
            "1) Click \"Load Godot Docs\" to index official docs.\n" +
            "2) Ask questions about Godot systems, nodes, signals, or APIs.\n" +
            "3) Paste GDScript/C# code for bug-finding and implementation help.\n\n" +
            "Built-in functions:\n" +
            "- /help : show all commands\n" +
            "- /plan <feature> : generate a step-by-step implementation plan\n" +
            "- /remember <fact> : save project preferences/details\n" +
            "- /memory : list saved preferences\n" +
            "- /clear-memory : wipe saved memory\n\n" +
            "Example prompts:\n" +
            "- \"Help me make a 2D dash mechanic with cooldown.\"\n" +
            "- \"Review this _physics_process movement code.\"\n" +
            "- \"Plan an inventory system with drag and drop.\"";

        AppendLine(welcome + Environment.NewLine);
        SetStatus("Ready");
    }

    private async Task LoadDocsAsync()
    {
        btnLoadDocs.Enabled = false;
        SetStatus("Loading docs...");
        AppendSystemMessage("Loading Godot documentation index. Please wait...");

        try
        {
            var chunkCount = await _bot.LoadKnowledgeBaseAsync();
            AppendSystemMessage($"Documentation loaded: {chunkCount} chunks indexed.");
            SetStatus($"Docs loaded ({chunkCount} chunks)");
        }
        catch (Exception ex)
        {
            AppendSystemMessage($"Failed to load docs: {ex.Message}");
            SetStatus("Doc load failed");
        }
        finally
        {
            btnLoadDocs.Enabled = true;
        }
    }

    private async Task SendMessageAsync()
    {
        var userMessage = txtInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        txtInput.Clear();
        AppendUserMessage(userMessage);
        btnSend.Enabled = false;
        SetStatus("Thinking...");

        try
        {
            var response = await _bot.RespondAsync(userMessage);
            AppendBotMessage(response);
            SetStatus("Ready");
        }
        catch (Exception ex)
        {
            AppendSystemMessage($"Bot error: {ex.Message}");
            SetStatus("Error during response");
        }
        finally
        {
            btnSend.Enabled = true;
        }
    }

    private void SetStatus(string statusText)
    {
        lblStatus.Text = $"Status: {statusText}";
    }

    private void AppendUserMessage(string message) => AppendLine($"You: {message}\n");

    private void AppendBotMessage(string message) => AppendLine($"GodotBot: {message}\n");

    private void AppendSystemMessage(string message) => AppendLine($"[system] {message}\n");

    private void AppendLine(string text)
    {
        txtConversation.AppendText(text + Environment.NewLine);
        txtConversation.SelectionStart = txtConversation.Text.Length;
        txtConversation.ScrollToCaret();
    }
}

public sealed class GodotAssistantBot
{
    private readonly GodotKnowledgeBase _knowledgeBase = new();
    private readonly BotStateStore _stateStore = new();

    private readonly List<MemoryEntry> _memory;

    public GodotAssistantBot()
    {
        _memory = _stateStore.LoadMemory();
    }

    public Task<int> LoadKnowledgeBaseAsync() => _knowledgeBase.LoadAsync();

    public async Task<string> RespondAsync(string userMessage)
    {
        if (TryHandleCommands(userMessage, out var commandResponse))
        {
            return commandResponse;
        }

        var suggestions = AnalyzeCodeIfPresent(userMessage);
        var featurePlan = BuildFeaturePlan(userMessage);
        var knowledge = await _knowledgeBase.SearchAsync(userMessage, 3);
        var memoryHints = RecallRelevantMemory(userMessage);

        var builder = new StringBuilder();
        builder.AppendLine("I am your personal Godot expert bot. Here is the best next step:");

        if (!string.IsNullOrWhiteSpace(featurePlan))
        {
            builder.AppendLine();
            builder.AppendLine("Suggested implementation plan:");
            builder.AppendLine(featurePlan);
        }

        if (suggestions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Code review findings:");
            foreach (var suggestion in suggestions)
            {
                builder.AppendLine($"- {suggestion}");
            }
        }

        if (knowledge.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Godot docs context:");
            foreach (var item in knowledge)
            {
                builder.AppendLine($"- {item.Source}");
                builder.AppendLine($"  {item.Text}");
            }
        }

        if (memoryHints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("What I remember about your project:");
            foreach (var hint in memoryHints)
            {
                builder.AppendLine($"- {hint}");
            }
        }

        if (string.IsNullOrWhiteSpace(featurePlan) && suggestions.Count == 0 && knowledge.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("Tell me your goal (movement, combat, UI, save/load, inventory, multiplayer), or paste GDScript/C# and I will provide concrete implementation steps + fixes.");
        }

        Remember($"Q: {Truncate(userMessage, 220)}", MemoryType.Conversation);
        Remember($"A: {Truncate(builder.ToString(), 300)}", MemoryType.Conversation);

        return builder.ToString().Trim();
    }

    private bool TryHandleCommands(string userMessage, out string response)
    {
        response = string.Empty;

        if (string.Equals(userMessage, "/help", StringComparison.OrdinalIgnoreCase))
        {
            response = "Commands:\n" +
                       "- /help\n" +
                       "- /remember <fact or preference>\n" +
                       "- /memory (show learned facts)\n" +
                       "- /clear-memory\n" +
                       "- /plan <feature request>";
            return true;
        }

        if (userMessage.StartsWith("/plan ", StringComparison.OrdinalIgnoreCase))
        {
            var request = userMessage[6..].Trim();
            response = string.IsNullOrWhiteSpace(request)
                ? "Use /plan <feature request>."
                : "Implementation plan:\n" + BuildFeaturePlan(request);
            return true;
        }

        if (userMessage.StartsWith("/remember ", StringComparison.OrdinalIgnoreCase))
        {
            var note = userMessage[10..].Trim();
            if (string.IsNullOrEmpty(note))
            {
                response = "Use /remember <preference or project detail>.";
                return true;
            }

            Remember(note, MemoryType.UserPreference);
            response = "Saved. I will reuse this in future responses.";
            return true;
        }

        if (string.Equals(userMessage, "/memory", StringComparison.OrdinalIgnoreCase))
        {
            var learned = _memory
                .Where(m => m.Type == MemoryType.UserPreference)
                .OrderByDescending(m => m.Timestamp)
                .Take(10)
                .ToList();

            response = learned.Count == 0
                ? "I have no saved preferences yet. Use /remember <text>."
                : "Learned preferences:\n- " + string.Join("\n- ", learned.Select(m => m.Text));

            return true;
        }

        if (string.Equals(userMessage, "/clear-memory", StringComparison.OrdinalIgnoreCase))
        {
            _memory.Clear();
            _stateStore.SaveMemory(_memory);
            response = "Memory cleared for this bot profile.";
            return true;
        }

        return false;
    }

    private static string BuildFeaturePlan(string prompt)
    {
        var p = prompt.ToLowerInvariant();

        if (p.Contains("inventory"))
        {
            return "1) Create Item Resource scripts (id, icon, stack_size).\n" +
                   "2) Add Inventory singleton/autoload with add/remove/query APIs.\n" +
                   "3) Build UI GridContainer bound to inventory slots and refresh on signal.\n" +
                   "4) Add drag/drop and stack merge logic with edge-case checks.\n" +
                   "5) Save inventory data in SaveGame resource/json and restore on load.";
        }

        if (p.Contains("save") || p.Contains("load"))
        {
            return "1) Define serializable save model (player stats, position, world state).\n" +
                   "2) Create SaveService with save/load API and version field.\n" +
                   "3) Capture node state via groups (e.g., saveable) and custom methods.\n" +
                   "4) Write to user:// with backup file and corruption fallback.\n" +
                   "5) Hook save/load flow to menu + autosave checkpoints.";
        }

        if (p.Contains("multiplayer") || p.Contains("network"))
        {
            return "1) Choose authority model (host-authoritative recommended).\n" +
                   "2) Set up ENetMultiplayerPeer and connection lifecycle UI.\n" +
                   "3) Mark state updates with RPC modes and validate authority.\n" +
                   "4) Implement input prediction/reconciliation for player movement.\n" +
                   "5) Add disconnect recovery and latency simulation tests.";
        }

        if (p.Contains("ui") || p.Contains("hud"))
        {
            return "1) Separate gameplay and UI scenes.\n" +
                   "2) Create a UI controller that listens to gameplay signals.\n" +
                   "3) Use theme resources for consistent styling.\n" +
                   "4) Add input focus rules for keyboard/controller.\n" +
                   "5) Test scaling anchors across multiple resolutions.";
        }

        return "1) Define target behavior and acceptance criteria.\n" +
               "2) Choose nodes/scenes/resources needed in Godot.\n" +
               "3) Implement minimal vertical slice in one scene.\n" +
               "4) Add signals/tests/logging for validation.\n" +
               "5) Refactor into reusable scripts and save data model.";
    }

    private void Remember(string text, MemoryType type)
    {
        _memory.Add(new MemoryEntry(text, DateTime.UtcNow, type));

        if (_memory.Count > 250)
        {
            _memory.RemoveRange(0, _memory.Count - 250);
        }

        _stateStore.SaveMemory(_memory);
    }

    private List<string> AnalyzeCodeIfPresent(string input)
    {
        var results = new List<string>();

        if (input.Contains("_process(") && !input.Contains("delta"))
        {
            results.Add("In _process, movement/math should usually use delta to avoid frame-rate dependent behavior.");
        }

        if (input.Contains("get_node(") && !input.Contains("@onready") && input.Contains("_process("))
        {
            results.Add("Avoid repeated get_node inside _process. Cache node references (@onready in GDScript, private fields in C#).");
        }

        if (input.Contains("yield(", StringComparison.OrdinalIgnoreCase))
        {
            results.Add("In Godot 4, replace yield() with await signal/coroutine patterns.");
        }

        if (input.Contains("KinematicBody2D") || input.Contains("KinematicBody3D"))
        {
            results.Add("In Godot 4, migrate KinematicBody* to CharacterBody* and adapt motion API usage.");
        }

        if (Regex.IsMatch(input, @"\bInput\.is_action_pressed\("))
        {
            results.Add("Input action usage detected. Ensure actions exist in Project Settings > Input Map.");
        }

        if (input.Contains("_physics_process(") && !input.Contains("move_and_slide"))
        {
            results.Add("If this is character movement, verify velocity integration and move_and_slide/move_and_collide usage in _physics_process.");
        }

        return results;
    }

    private List<string> RecallRelevantMemory(string prompt)
    {
        var keywords = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 3)
            .Select(token => token.ToLowerInvariant())
            .ToHashSet();

        return _memory
            .OrderByDescending(m => m.Timestamp)
            .Where(m => m.Type == MemoryType.UserPreference)
            .Where(m => keywords.Count == 0 || keywords.Any(keyword => m.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(m => m.Text)
            .Distinct()
            .Take(3)
            .ToList();
    }

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];
}

public sealed class BotStateStore
{
    private readonly string _filePath;

    public BotStateStore()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GodotAIBot");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "memory.json");
    }

    public List<MemoryEntry> LoadMemory()
    {
        if (!File.Exists(_filePath))
        {
            return new List<MemoryEntry>();
        }

        try
        {
            var raw = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<MemoryEntry>>(raw) ?? new List<MemoryEntry>();
        }
        catch
        {
            return new List<MemoryEntry>();
        }
    }

    public void SaveMemory(List<MemoryEntry> memory)
    {
        var serialized = JsonSerializer.Serialize(memory);
        File.WriteAllText(_filePath, serialized);
    }
}

public sealed class GodotKnowledgeBase
{
    private readonly HttpClient _httpClient = new();
    private readonly List<DocChunk> _chunks = new();

    public async Task<int> LoadAsync()
    {
        if (_chunks.Count > 0)
        {
            return _chunks.Count;
        }

        const string rootUrl = "https://docs.godotengine.org/en/stable/";
        var html = await _httpClient.GetStringAsync(rootUrl);

        var links = Regex.Matches(html, "href=\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value)
            .Where(link => link.StartsWith("/en/stable/", StringComparison.OrdinalIgnoreCase) && link.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Take(100)
            .ToList();

        foreach (var link in links)
        {
            var fullUrl = "https://docs.godotengine.org" + link;
            var content = await _httpClient.GetStringAsync(fullUrl);
            var plain = Regex.Replace(content, "<.*?>", " ");
            plain = Regex.Replace(plain, "\\s+", " ").Trim();

            for (var index = 0; index < plain.Length; index += 650)
            {
                var length = Math.Min(650, plain.Length - index);
                var chunk = plain.Substring(index, length);
                _chunks.Add(new DocChunk(fullUrl, chunk));
            }
        }

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
            .Where(t => t.Length > 2)
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
                Score = terms.Sum(term => ScoreTermMatch(chunk.Text, term))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.Source)
            .Take(maxResults)
            .Select(x => x.Chunk)
            .ToList();

        return Task.FromResult(ranked);
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

public sealed record DocChunk(string Source, string Text);

public sealed record MemoryEntry(string Text, DateTime Timestamp, MemoryType Type);

public enum MemoryType
{
    Conversation,
    UserPreference
}
