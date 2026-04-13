using System;
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
