using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GodotAIBot;

public sealed class GodotAssistantBot
{
    private static readonly string[] DirectAnswerLeadIns =
    {
        "Short version:",
        "Most likely:",
        "What I’d do:",
        "The key move here:"
    };

    private static readonly string[] SupportiveClosers =
    {
        "If you want, I can turn that into code next.",
        "If you paste the script, I can tighten this into line-by-line feedback.",
        "If helpful, I can break that into a smaller implementation plan.",
        "If you want to keep going, I can help with the next concrete step."
    };

    private static readonly string[] MemoryLeadIns =
    {
        "I’m also keeping these project details in mind:",
        "A couple of things I remember from earlier that may matter:",
        "This also connects with what you’ve told me before:"
    };

    private static readonly string[] DocsSectionTitles =
    {
        "Relevant docs:",
        "Useful docs context:",
        "Docs worth anchoring on:"
    };

    private static readonly string[] ReviewSectionTitles =
    {
        "What I’d fix first:",
        "Main issues I see:",
        "First pass review:"
    };

    private static readonly string[] PlanSectionTitles =
    {
        "Suggested path:",
        "A workable plan:",
        "How I’d build it:"
    };

    private static readonly string[] QuestionOpeners =
    {
        "Here’s the clearest answer I can give you right now:",
        "This is the path I’d take:",
        "Here’s the practical version:",
        "This is the strongest direction I see:"
    };

    private static readonly string[] DocsOpeners =
    {
        "I pulled the most relevant Godot context for this:",
        "These docs are the pieces that matter most here:",
        "Here’s the most useful guidance I found in the docs:",
        "This is the part of the docs I’d anchor on:"
    };

    private static readonly string[] ReviewOpeners =
    {
        "A few code-quality and behavior issues stand out first:",
        "I’d tighten these points before building further:",
        "Here are the first things I’d fix in that code:",
        "A couple of implementation risks jump out here:"
    };

    private static readonly string[] GenericFallbacks =
    {
        "Give me the feature goal, the Godot API you’re using, or a code snippet, and I’ll turn it into concrete next steps.",
        "Point me at the mechanic, scene flow, or script you’re wrestling with and I’ll help shape it.",
        "If you want, paste the code or describe the feature and I’ll help you move from idea to implementation.",
        "Bring me the bug, system, or design question and I’ll help break it into workable steps."
    };

    private static readonly string[] DebugGuidance =
    {
        "Your prompt reads like debugging work, so I’m prioritizing correctness, migration issues, and likely failure points.",
        "This looks like troubleshooting territory, so I’m focusing on behavior regressions and Godot-specific gotchas.",
        "I treated this like a code review pass first, because that’s the fastest way to unblock the next fix."
    };

    private static readonly string[] DocsGuidance =
    {
        "I trimmed the docs down to the sections most likely to unblock the decision in front of you.",
        "I focused on the highest-signal docs matches instead of broad coverage, so the answer stays usable.",
        "I pulled the parts of the docs that are most likely to affect implementation, not just terminology."
    };

    private static readonly HashSet<string> PlanningKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "build", "create", "implement", "plan", "design", "make", "architecture"
    };

    private static readonly Queue<string> RecentPhrases = new();

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
        var intent = ClassifyIntent(userMessage, suggestions.Count, knowledge.Count, !string.IsNullOrWhiteSpace(featurePlan));

        var builder = new StringBuilder();
        builder.AppendLine(BuildOpeningLine(userMessage, knowledge.Count, suggestions.Count));

        var directAnswer = BuildDirectAnswer(userMessage, intent, suggestions.Count, knowledge.Count, !string.IsNullOrWhiteSpace(featurePlan));
        if (!string.IsNullOrWhiteSpace(directAnswer))
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(DirectAnswerLeadIns));
            builder.AppendLine(directAnswer);
        }

        var guidance = BuildGuidanceSummary(userMessage, knowledge.Count > 0, suggestions.Count > 0);
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            builder.AppendLine();
            builder.AppendLine(guidance);
        }

        if (suggestions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(ReviewSectionTitles));
            foreach (var suggestion in suggestions)
            {
                builder.AppendLine($"- {suggestion}");
            }
        }

        if (!string.IsNullOrWhiteSpace(featurePlan))
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(PlanSectionTitles));
            builder.AppendLine(featurePlan);
        }

        if (knowledge.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(DocsSectionTitles));
            foreach (var item in knowledge)
            {
                builder.AppendLine($"- {item.Title} ({item.Source})");
                builder.AppendLine($"  {item.Text}");
            }
        }

        if (memoryHints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(MemoryLeadIns));
            foreach (var hint in memoryHints)
            {
                builder.AppendLine($"- {hint}");
            }
        }

        if (string.IsNullOrWhiteSpace(featurePlan) && suggestions.Count == 0 && knowledge.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(GenericFallbacks));
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine(PickPhrase(SupportiveClosers));
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
            return PickPhrase(ReviewOpeners);
        }

        if (knowledgeCount > 0 && userMessage.Contains('?', StringComparison.Ordinal))
        {
            return PickPhrase(DocsOpeners);
        }

        if (knowledgeCount > 0)
        {
            return PickPhrase(DocsOpeners);
        }

        return PickPhrase(QuestionOpeners);
    }

    private static string BuildGuidanceSummary(string userMessage, bool hasKnowledge, bool hasSuggestions)
    {
        if (hasSuggestions)
        {
            return PickPhrase(DebugGuidance);
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

        return PickPhrase(DocsGuidance);
    }

    private static BotIntent ClassifyIntent(string userMessage, int suggestionCount, int knowledgeCount, bool hasPlan)
    {
        var lower = userMessage.ToLowerInvariant();

        if (suggestionCount > 0 || lower.Contains("bug") || lower.Contains("error") || lower.Contains("broken") || lower.Contains("why"))
        {
            return BotIntent.Debug;
        }

        if (hasPlan)
        {
            return BotIntent.Plan;
        }

        if (knowledgeCount > 0 || lower.Contains("what is") || lower.Contains("how do") || userMessage.Contains('?', StringComparison.Ordinal))
        {
            return BotIntent.Explain;
        }

        return BotIntent.Chat;
    }

    private static string BuildDirectAnswer(string userMessage, BotIntent intent, int suggestionCount, int knowledgeCount, bool hasPlan)
    {
        var lower = userMessage.ToLowerInvariant();

        return intent switch
        {
            BotIntent.Debug => "Start by checking the behavior closest to the failure point, then validate the Godot-specific assumptions around node access, timing, and movement callbacks.",
            BotIntent.Plan => "Treat this as a vertical slice first: get one complete happy-path working, then generalize once the flow feels right.",
            BotIntent.Explain when lower.Contains("signal") => "Signals are usually easiest to reason about when one node owns emission and another owns connection setup.",
            BotIntent.Explain when lower.Contains("physics") || lower.Contains("movement") => "Keep movement math and collision resolution in the same physics flow so the behavior stays predictable.",
            BotIntent.Explain when lower.Contains("ui") || lower.Contains("control") => "For Godot UI, the biggest wins usually come from getting containers and sizing rules right before styling details.",
            BotIntent.Explain when knowledgeCount > 0 => "The docs point to the implementation constraints first, so it’s worth anchoring on those before you commit to a structure.",
            BotIntent.Chat when hasPlan => "There’s enough signal here to move from a broad idea into a concrete implementation path.",
            _ => "There’s a workable next step here, and it doesn’t need to be overly complicated to get moving."
        };
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
                response = "Use /remember <preference or project detail> and I’ll keep it in mind.";
                return true;
            }

            Remember(note, MemoryType.UserPreference);
            response = "Saved. I’ll keep that in mind in future replies.";
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
            response = "Memory cleared. We’re back to a clean slate.";
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

    private static string PickPhrase(IReadOnlyList<string> options)
    {
        foreach (var option in options)
        {
            if (!RecentPhrases.Contains(option))
            {
                RememberPhrase(option);
                return option;
            }
        }

        var fallback = options[Random.Shared.Next(options.Count)];
        RememberPhrase(fallback);
        return fallback;
    }

    private static void RememberPhrase(string phrase)
    {
        RecentPhrases.Enqueue(phrase);
        while (RecentPhrases.Count > 6)
        {
            RecentPhrases.Dequeue();
        }
    }
}

public enum BotIntent
{
    Chat,
    Explain,
    Debug,
    Plan
}
