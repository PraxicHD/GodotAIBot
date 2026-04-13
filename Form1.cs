using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GodotAIBot;

public partial class Form1 : Form
{
    private static readonly Color AppBackground = Color.FromArgb(18, 24, 33);
    private static readonly Color SurfaceBackground = Color.FromArgb(24, 31, 43);
    private static readonly Color SurfaceBorder = Color.FromArgb(49, 63, 85);
    private static readonly Color PrimaryAccent = Color.FromArgb(94, 129, 255);
    private static readonly Color SecondaryAccent = Color.FromArgb(55, 194, 145);
    private static readonly Color TextPrimary = Color.FromArgb(234, 239, 248);
    private static readonly Color TextMuted = Color.FromArgb(139, 154, 177);
    private static readonly Color UserBubble = Color.FromArgb(82, 110, 219);
    private static readonly Color BotBubble = Color.FromArgb(44, 57, 79);
    private static readonly Color SystemBubble = Color.FromArgb(49, 84, 78);

    private readonly GodotAssistantBot _bot = new();

    public Form1()
    {
        InitializeComponent();
        ApplyVisualStyle();

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
            "Welcome in. I can help with Godot APIs, architecture, debugging, and feature planning.\n\n" +
            "Quick start:\n" +
            "1. Click \"Load Docs\" to index the official Godot docs.\n" +
            "2. Ask about nodes, signals, physics, UI, or scene structure.\n" +
            "3. Paste GDScript or C# when you want a review or a fix.\n\n" +
            "Useful commands:\n" +
            "/help\n" +
            "/plan <feature>\n" +
            "/remember <fact>\n" +
            "/memory\n" +
            "/clear-memory";

        AppendBotMessage(welcome);
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
        lblStatus.Text = statusText;
    }

    private void ApplyVisualStyle()
    {
        BackColor = AppBackground;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI Variable Text", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(14);

        lblTitle.Font = new Font("Segoe UI Variable Display", 18F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.ForeColor = TextPrimary;
        lblSubtitle.Font = new Font("Segoe UI Variable Text", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        lblSubtitle.ForeColor = TextMuted;
        lblStatus.Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblStatus.ForeColor = SecondaryAccent;

        StyleButton(btnLoadDocs, SurfaceBackground, TextPrimary, SurfaceBorder);
        StyleButton(btnClearChat, SurfaceBackground, TextPrimary, SurfaceBorder);
        StyleButton(btnSend, PrimaryAccent, Color.White, PrimaryAccent);

        txtConversation.BackColor = SurfaceBackground;
        txtConversation.ForeColor = TextPrimary;
        txtConversation.Font = new Font("Segoe UI Variable Text", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        txtConversation.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtConversation.DetectUrls = true;

        txtInput.BackColor = SurfaceBackground;
        txtInput.ForeColor = TextPrimary;
        txtInput.Font = new Font("Segoe UI Variable Text", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private static void StyleButton(Button button, Color background, Color foreground, Color border)
    {
        button.BackColor = background;
        button.ForeColor = foreground;
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(background, 0.08f);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(background, 0.08f);
        button.Font = new Font("Segoe UI Variable Text", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
        button.Cursor = Cursors.Hand;
    }

    private void AppendUserMessage(string message) => AppendMessage("You", message, UserBubble, Color.White, HorizontalAlignment.Right);

    private void AppendBotMessage(string message) => AppendMessage("GodotBot", message, BotBubble, TextPrimary, HorizontalAlignment.Left);

    private void AppendSystemMessage(string message) => AppendMessage("System", message, SystemBubble, TextPrimary, HorizontalAlignment.Left);

    private void AppendMessage(string author, string message, Color bubbleColor, Color textColor, HorizontalAlignment alignment)
    {
        txtConversation.SelectionStart = txtConversation.TextLength;
        txtConversation.SelectionLength = 0;
        txtConversation.SelectionAlignment = alignment;
        txtConversation.SelectionBackColor = bubbleColor;
        txtConversation.SelectionColor = textColor;
        txtConversation.SelectionFont = new Font(txtConversation.Font, FontStyle.Bold);
        txtConversation.AppendText($"{author}\n");
        txtConversation.SelectionFont = txtConversation.Font;
        txtConversation.AppendText($"{message}\n\n");
        txtConversation.SelectionBackColor = txtConversation.BackColor;
        txtConversation.SelectionColor = txtConversation.ForeColor;
        txtConversation.SelectionAlignment = HorizontalAlignment.Left;
        txtConversation.SelectionStart = txtConversation.Text.Length;
        txtConversation.ScrollToCaret();
    }
}
