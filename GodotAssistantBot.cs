using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GodotAIBot;

public sealed class GodotAssistantBot
{
    private static readonly HashSet<string> PlanningKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "build", "create", "implement", "plan", "design", "make", "architecture"
    };

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
        var featurePlan = ShouldBuildFeaturePlan(userMessage) ? BuildFeaturePlan(userMessage) : string.Empty;
        var knowledge = await _knowledgeBase.SearchAsync(userMessage, 3);
        var memoryHints = RecallRelevantMemory(userMessage);

        var builder = new StringBuilder();
        builder.AppendLine(BuildOpeningLine(userMessage, knowledge.Count, suggestions.Count));

        var guidance = BuildGuidanceSummary(userMessage, knowledge.Count > 0, suggestions.Count > 0);
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            builder.AppendLine();
            builder.AppendLine(guidance);
        }

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
            builder.AppendLine("Relevant Godot docs:");
            foreach (var item in knowledge)
            {
                builder.AppendLine($"- {item.Title} ({item.Source})");
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
            builder.AppendLine("Tell me your goal (movement, combat, UI, save/load, inventory, multiplayer), ask about a Godot API, or paste GDScript/C# and I will provide concrete implementation steps and fixes.");
        }

        Remember($"Q: {Truncate(userMessage, 220)}", MemoryType.Conversation);
        Remember($"A: {Truncate(builder.ToString(), 300)}", MemoryType.Conversation);

        return builder.ToString().Trim();
    }

    private static bool ShouldBuildFeaturePlan(string userMessage)
    {
        var normalized = userMessage.ToLowerInvariant();
        return PlanningKeywords.Any(normalized.Contains);
    }

    private static string BuildOpeningLine(string userMessage, int knowledgeCount, int suggestionCount)
    {
        if (suggestionCount > 0)
        {
            return "I found a few implementation and code-quality points worth fixing first:";
        }

        if (knowledgeCount > 0 && userMessage.Contains('?', StringComparison.Ordinal))
        {
            return "Here’s the most relevant Godot guidance I found for your question:";
        }

        if (knowledgeCount > 0)
        {
            return "Here’s the strongest Godot context I found for that request:";
        }

        return "Here’s the clearest next step I can give you:";
    }

    private static string BuildGuidanceSummary(string userMessage, bool hasKnowledge, bool hasSuggestions)
    {
        if (hasSuggestions)
        {
            return "Your prompt looks like code or debugging work, so I prioritized correctness and migration issues.";
        }

        if (!hasKnowledge)
        {
            return string.Empty;
        }

        var lower = userMessage.ToLowerInvariant();
        if (lower.Contains("signal"))
        {
            return "Focus on signal ownership, connection points, and when the receiving node enters the scene tree.";
        }

        if (lower.Contains("physics") || lower.Contains("movement"))
        {
            return "For movement questions, validate the callback you are using first, then keep velocity updates and collision resolution in the same flow.";
        }

        if (lower.Contains("ui") || lower.Contains("control"))
        {
            return "For UI work, pay attention to container layout rules and input/focus behavior before styling.";
        }

        return "I pulled the highest-scoring docs matches and trimmed them down to the parts most likely to answer the request.";
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
        var normalizedPrompt = prompt.ToLowerInvariant();

        if (normalizedPrompt.Contains("inventory"))
        {
            return "1) Create Item Resource scripts (id, icon, stack_size).\n" +
                   "2) Add Inventory singleton/autoload with add/remove/query APIs.\n" +
                   "3) Build UI GridContainer bound to inventory slots and refresh on signal.\n" +
                   "4) Add drag/drop and stack merge logic with edge-case checks.\n" +
                   "5) Save inventory data in SaveGame resource/json and restore on load.";
        }

        if (normalizedPrompt.Contains("save") || normalizedPrompt.Contains("load"))
        {
            return "1) Define serializable save model (player stats, position, world state).\n" +
                   "2) Create SaveService with save/load API and version field.\n" +
                   "3) Capture node state via groups (e.g., saveable) and custom methods.\n" +
                   "4) Write to user:// with backup file and corruption fallback.\n" +
                   "5) Hook save/load flow to menu + autosave checkpoints.";
        }

        if (normalizedPrompt.Contains("multiplayer") || normalizedPrompt.Contains("network"))
        {
            return "1) Choose authority model (host-authoritative recommended).\n" +
                   "2) Set up ENetMultiplayerPeer and connection lifecycle UI.\n" +
                   "3) Mark state updates with RPC modes and validate authority.\n" +
                   "4) Implement input prediction/reconciliation for player movement.\n" +
                   "5) Add disconnect recovery and latency simulation tests.";
        }

        if (normalizedPrompt.Contains("ui") || normalizedPrompt.Contains("hud"))
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

    private static List<string> AnalyzeCodeIfPresent(string input)
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
