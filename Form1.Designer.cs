namespace GodotAIBot;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer? components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        btnLoadDocs = new System.Windows.Forms.Button();
        btnSend = new System.Windows.Forms.Button();
        txtConversation = new System.Windows.Forms.RichTextBox();
        txtInput = new System.Windows.Forms.TextBox();
        btnClearChat = new System.Windows.Forms.Button();
        lblStatus = new System.Windows.Forms.Label();
        lblTitle = new System.Windows.Forms.Label();
        lblSubtitle = new System.Windows.Forms.Label();
        SuspendLayout();
        // 
        // btnLoadDocs
        // 
        btnLoadDocs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnLoadDocs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnLoadDocs.Location = new System.Drawing.Point(674, 19);
        btnLoadDocs.Name = "btnLoadDocs";
        btnLoadDocs.Size = new System.Drawing.Size(138, 34);
        btnLoadDocs.TabIndex = 1;
        btnLoadDocs.Text = "Load Docs";
        btnLoadDocs.UseVisualStyleBackColor = true;
        // 
        // btnSend
        // 
        btnSend.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnSend.Location = new System.Drawing.Point(689, 582);
        btnSend.Name = "btnSend";
        btnSend.Size = new System.Drawing.Size(123, 42);
        btnSend.TabIndex = 5;
        btnSend.Text = "Send";
        btnSend.UseVisualStyleBackColor = true;
        // 
        // txtConversation
        // 
        txtConversation.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtConversation.BorderStyle = System.Windows.Forms.BorderStyle.None;
        txtConversation.Location = new System.Drawing.Point(18, 98);
        txtConversation.Name = "txtConversation";
        txtConversation.ReadOnly = true;
        txtConversation.Size = new System.Drawing.Size(794, 395);
        txtConversation.TabIndex = 3;
        txtConversation.Text = "";
        // 
        // txtInput
        // 
        txtInput.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        txtInput.Location = new System.Drawing.Point(18, 526);
        txtInput.Multiline = true;
        txtInput.Name = "txtInput";
        txtInput.Size = new System.Drawing.Size(665, 98);
        txtInput.TabIndex = 4;
        // 
        // btnClearChat
        // 
        btnClearChat.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnClearChat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnClearChat.Location = new System.Drawing.Point(542, 19);
        btnClearChat.Name = "btnClearChat";
        btnClearChat.Size = new System.Drawing.Size(118, 34);
        btnClearChat.TabIndex = 2;
        btnClearChat.Text = "Clear Chat";
        btnClearChat.UseVisualStyleBackColor = true;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Location = new System.Drawing.Point(18, 501);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new System.Drawing.Size(65, 15);
        lblStatus.TabIndex = 6;
        lblStatus.Text = "Status: idle";
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Location = new System.Drawing.Point(18, 19);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new System.Drawing.Size(80, 15);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Godot Codex";
        // 
        // lblSubtitle
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Location = new System.Drawing.Point(18, 47);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new System.Drawing.Size(291, 15);
        lblSubtitle.TabIndex = 7;
        lblSubtitle.Text = "A focused Godot pairing assistant for docs, code, and plans";
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(830, 643);
        Controls.Add(lblSubtitle);
        Controls.Add(lblTitle);
        Controls.Add(lblStatus);
        Controls.Add(btnClearChat);
        Controls.Add(txtInput);
        Controls.Add(txtConversation);
        Controls.Add(btnSend);
        Controls.Add(btnLoadDocs);
        MinimumSize = new System.Drawing.Size(846, 682);
        Name = "Form1";
        Text = "Godot AI Bot";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Button btnLoadDocs;
    private System.Windows.Forms.Button btnSend;
    private System.Windows.Forms.RichTextBox txtConversation;
    private System.Windows.Forms.TextBox txtInput;
    private System.Windows.Forms.Button btnClearChat;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;
}
