namespace WindowsSshServer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.updateStatusTimer = new System.Windows.Forms.Timer(this.components);
            this.clientCountLabel = new System.Windows.Forms.Label();
            this.generateKeysButton = new System.Windows.Forms.Button();
            this.sessionsGroupBox = new System.Windows.Forms.GroupBox();
            this.closeAllTerminalsButton = new System.Windows.Forms.Button();
            this.activeSessionsLabel = new System.Windows.Forms.Label();
            this.activeSessionsCaptionLabel = new System.Windows.Forms.Label();
            this.showAllTerminalsCheckBox = new System.Windows.Forms.CheckBox();
            this.sessionsListBox = new System.Windows.Forms.ListBox();
            this.sessionsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(12, 12);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "&Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(12, 41);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "&Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabel.Location = new System.Drawing.Point(252, 9);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 23);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "{Status}";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // updateStatusTimer
            // 
            this.updateStatusTimer.Enabled = true;
            this.updateStatusTimer.Tick += new System.EventHandler(this.updateStatusTimer_Tick);
            // 
            // clientCountLabel
            // 
            this.clientCountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clientCountLabel.Location = new System.Drawing.Point(252, 32);
            this.clientCountLabel.Name = "clientCountLabel";
            this.clientCountLabel.Size = new System.Drawing.Size(200, 13);
            this.clientCountLabel.TabIndex = 4;
            this.clientCountLabel.Text = "{client count}";
            this.clientCountLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // generateKeysButton
            // 
            this.generateKeysButton.Location = new System.Drawing.Point(93, 12);
            this.generateKeysButton.Name = "generateKeysButton";
            this.generateKeysButton.Size = new System.Drawing.Size(120, 23);
            this.generateKeysButton.TabIndex = 2;
            this.generateKeysButton.Text = "Generate &Keys";
            this.generateKeysButton.UseVisualStyleBackColor = true;
            this.generateKeysButton.Click += new System.EventHandler(this.generateKeysButton_Click);
            // 
            // sessionsGroupBox
            // 
            this.sessionsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sessionsGroupBox.Controls.Add(this.sessionsListBox);
            this.sessionsGroupBox.Controls.Add(this.closeAllTerminalsButton);
            this.sessionsGroupBox.Controls.Add(this.activeSessionsLabel);
            this.sessionsGroupBox.Controls.Add(this.activeSessionsCaptionLabel);
            this.sessionsGroupBox.Controls.Add(this.showAllTerminalsCheckBox);
            this.sessionsGroupBox.Location = new System.Drawing.Point(12, 70);
            this.sessionsGroupBox.Name = "sessionsGroupBox";
            this.sessionsGroupBox.Size = new System.Drawing.Size(440, 202);
            this.sessionsGroupBox.TabIndex = 5;
            this.sessionsGroupBox.TabStop = false;
            this.sessionsGroupBox.Text = "&Sessions";
            // 
            // closeAllTerminalsButton
            // 
            this.closeAllTerminalsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeAllTerminalsButton.Location = new System.Drawing.Point(334, 32);
            this.closeAllTerminalsButton.Name = "closeAllTerminalsButton";
            this.closeAllTerminalsButton.Size = new System.Drawing.Size(100, 23);
            this.closeAllTerminalsButton.TabIndex = 3;
            this.closeAllTerminalsButton.Text = "&Close All";
            this.closeAllTerminalsButton.UseVisualStyleBackColor = true;
            this.closeAllTerminalsButton.Click += new System.EventHandler(this.closeAllTerminalsButton_Click);
            // 
            // activeSessionsLabel
            // 
            this.activeSessionsLabel.AutoSize = true;
            this.activeSessionsLabel.Location = new System.Drawing.Point(49, 16);
            this.activeSessionsLabel.Name = "activeSessionsLabel";
            this.activeSessionsLabel.Size = new System.Drawing.Size(42, 13);
            this.activeSessionsLabel.TabIndex = 1;
            this.activeSessionsLabel.Text = "{count}";
            // 
            // activeSessionsCaptionLabel
            // 
            this.activeSessionsCaptionLabel.AutoSize = true;
            this.activeSessionsCaptionLabel.Location = new System.Drawing.Point(3, 16);
            this.activeSessionsCaptionLabel.Name = "activeSessionsCaptionLabel";
            this.activeSessionsCaptionLabel.Size = new System.Drawing.Size(40, 13);
            this.activeSessionsCaptionLabel.TabIndex = 0;
            this.activeSessionsCaptionLabel.Text = "&Active:";
            // 
            // showAllTerminalsCheckBox
            // 
            this.showAllTerminalsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.showAllTerminalsCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.showAllTerminalsCheckBox.Location = new System.Drawing.Point(334, 61);
            this.showAllTerminalsCheckBox.Name = "showAllTerminalsCheckBox";
            this.showAllTerminalsCheckBox.Size = new System.Drawing.Size(100, 23);
            this.showAllTerminalsCheckBox.TabIndex = 4;
            this.showAllTerminalsCheckBox.Text = "&Show All";
            this.showAllTerminalsCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.showAllTerminalsCheckBox.UseVisualStyleBackColor = true;
            this.showAllTerminalsCheckBox.CheckedChanged += new System.EventHandler(this.showAllTerminalsCheckBox_CheckedChanged);
            // 
            // sessionsListBox
            // 
            this.sessionsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sessionsListBox.FormattingEnabled = true;
            this.sessionsListBox.IntegralHeight = false;
            this.sessionsListBox.Location = new System.Drawing.Point(6, 32);
            this.sessionsListBox.Name = "sessionsListBox";
            this.sessionsListBox.Size = new System.Drawing.Size(322, 164);
            this.sessionsListBox.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 284);
            this.Controls.Add(this.sessionsGroupBox);
            this.Controls.Add(this.generateKeysButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.clientCountLabel);
            this.Controls.Add(this.statusLabel);
            this.Location = new System.Drawing.Point(-1, -1);
            this.Name = "MainForm";
            this.Text = "Windows SSH Server";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.sessionsGroupBox.ResumeLayout(false);
            this.sessionsGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Timer updateStatusTimer;
        private System.Windows.Forms.Label clientCountLabel;
        private System.Windows.Forms.Button generateKeysButton;
        private System.Windows.Forms.GroupBox sessionsGroupBox;
        private System.Windows.Forms.CheckBox showAllTerminalsCheckBox;
        private System.Windows.Forms.Label activeSessionsLabel;
        private System.Windows.Forms.Label activeSessionsCaptionLabel;
        private System.Windows.Forms.Button closeAllTerminalsButton;
        private System.Windows.Forms.ListBox sessionsListBox;
    }
}

