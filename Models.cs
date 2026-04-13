using System;

namespace GodotAIBot;

public sealed record DocChunk(string Source, string Title, string Text);

public sealed record MemoryEntry(string Text, DateTime Timestamp, MemoryType Type);

public enum MemoryType
{
    Conversation,
    UserPreference
}
