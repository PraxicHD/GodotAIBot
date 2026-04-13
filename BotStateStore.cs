using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GodotAIBot;

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
        var serialized = JsonSerializer.Serialize(memory, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = _filePath + ".tmp";

        File.WriteAllText(tempPath, serialized);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
