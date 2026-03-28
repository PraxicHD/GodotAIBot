namespace GodotAIBot;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

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
        this.SuspendLayout();
        this.btnLoadDocs = new System.Windows.Forms.Button();
        this.btnLoadDocs.Location = new System.Drawing.Point(248, 232);
        this.btnLoadDocs.Size = new System.Drawing.Size(120, 40);
        this.btnLoadDocs.Text = "Load Docs";
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Text = "Godot AI Bot";
        Controls.Add(this.btnLoadDocs);
        this.ResumeLayout(false);
    }

    #endregion
    private System.Windows.Forms.Button btnLoadDocs;
}