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
        SuspendLayout();
        // 
        // btnLoadDocs
        // 
        btnLoadDocs.Location = new System.Drawing.Point(12, 12);
        btnLoadDocs.Name = "btnLoadDocs";
        btnLoadDocs.Size = new System.Drawing.Size(127, 32);
        btnLoadDocs.TabIndex = 0;
        btnLoadDocs.Text = "Load Godot Docs";
        btnLoadDocs.UseVisualStyleBackColor = true;
        // 
        // btnSend
        // 
        btnSend.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        btnSend.Location = new System.Drawing.Point(687, 406);
        btnSend.Name = "btnSend";
        btnSend.Size = new System.Drawing.Size(101, 32);
        btnSend.TabIndex = 4;
        btnSend.Text = "Send";
        btnSend.UseVisualStyleBackColor = true;
        // 
        // txtConversation
        // 
        txtConversation.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtConversation.Location = new System.Drawing.Point(12, 50);
        txtConversation.Name = "txtConversation";
        txtConversation.ReadOnly = true;
        txtConversation.Size = new System.Drawing.Size(776, 283);
        txtConversation.TabIndex = 1;
        txtConversation.Text = "";
        // 
        // txtInput
        // 
        txtInput.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtInput.Location = new System.Drawing.Point(12, 363);
        txtInput.Multiline = true;
        txtInput.Name = "txtInput";
        txtInput.Size = new System.Drawing.Size(776, 37);
        txtInput.TabIndex = 3;
        // 
        // btnClearChat
        // 
        btnClearChat.Location = new System.Drawing.Point(145, 12);
        btnClearChat.Name = "btnClearChat";
        btnClearChat.Size = new System.Drawing.Size(88, 32);
        btnClearChat.TabIndex = 2;
        btnClearChat.Text = "Clear Chat";
        btnClearChat.UseVisualStyleBackColor = true;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Location = new System.Drawing.Point(12, 339);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new System.Drawing.Size(65, 15);
        lblStatus.TabIndex = 5;
        lblStatus.Text = "Status: idle";
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(800, 450);
        Controls.Add(lblStatus);
        Controls.Add(btnClearChat);
        Controls.Add(txtInput);
        Controls.Add(txtConversation);
        Controls.Add(btnSend);
        Controls.Add(btnLoadDocs);
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
}
