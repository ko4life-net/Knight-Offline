namespace Knight_Offline
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ServerIPLabel = new System.Windows.Forms.Label();
            this.ServerIP = new System.Windows.Forms.TextBox();
            this.PortLabel = new System.Windows.Forms.Label();
            this.Port = new System.Windows.Forms.TextBox();
            this.NumberOfBotsLabel = new System.Windows.Forms.Label();
            this.NumberOfBots = new System.Windows.Forms.TextBox();
            this.SpawnBotsButton = new System.Windows.Forms.Button();
            this.ArrestBotsButton = new System.Windows.Forms.Button();
            this.EventLogGroupBox = new System.Windows.Forms.GroupBox();
            this.EventLogList = new System.Windows.Forms.ListBox();
            this.Command = new System.Windows.Forms.TextBox();
            this.SendCommandButton = new System.Windows.Forms.Button();
            this.EventLogGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ServerIPLabel
            // 
            this.ServerIPLabel.AutoSize = true;
            this.ServerIPLabel.Location = new System.Drawing.Point(12, 9);
            this.ServerIPLabel.Name = "ServerIPLabel";
            this.ServerIPLabel.Size = new System.Drawing.Size(51, 13);
            this.ServerIPLabel.TabIndex = 0;
            this.ServerIPLabel.Text = "Server IP";
            // 
            // ServerIP
            // 
            this.ServerIP.Location = new System.Drawing.Point(69, 6);
            this.ServerIP.Name = "ServerIP";
            this.ServerIP.Size = new System.Drawing.Size(100, 20);
            this.ServerIP.TabIndex = 1;
            this.ServerIP.Text = "192.168.1.10";
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(175, 9);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(26, 13);
            this.PortLabel.TabIndex = 2;
            this.PortLabel.Text = "Port";
            // 
            // Port
            // 
            this.Port.Location = new System.Drawing.Point(207, 5);
            this.Port.Name = "Port";
            this.Port.Size = new System.Drawing.Size(54, 20);
            this.Port.TabIndex = 3;
            this.Port.Text = "15001";
            // 
            // NumberOfBotsLabel
            // 
            this.NumberOfBotsLabel.AutoSize = true;
            this.NumberOfBotsLabel.Location = new System.Drawing.Point(267, 8);
            this.NumberOfBotsLabel.Name = "NumberOfBotsLabel";
            this.NumberOfBotsLabel.Size = new System.Drawing.Size(79, 13);
            this.NumberOfBotsLabel.TabIndex = 4;
            this.NumberOfBotsLabel.Text = "Number of bots";
            // 
            // NumberOfBots
            // 
            this.NumberOfBots.Location = new System.Drawing.Point(365, 6);
            this.NumberOfBots.Name = "NumberOfBots";
            this.NumberOfBots.Size = new System.Drawing.Size(50, 20);
            this.NumberOfBots.TabIndex = 5;
            this.NumberOfBots.Text = "1";
            // 
            // SpawnBotsButton
            // 
            this.SpawnBotsButton.Location = new System.Drawing.Point(420, 5);
            this.SpawnBotsButton.Name = "SpawnBotsButton";
            this.SpawnBotsButton.Size = new System.Drawing.Size(120, 22);
            this.SpawnBotsButton.TabIndex = 6;
            this.SpawnBotsButton.Text = "Spawn bots";
            this.SpawnBotsButton.UseVisualStyleBackColor = true;
            this.SpawnBotsButton.Click += new System.EventHandler(this.SpawnBotsClick);
            // 
            // ArrestBotsButton
            // 
            this.ArrestBotsButton.Enabled = false;
            this.ArrestBotsButton.Location = new System.Drawing.Point(544, 5);
            this.ArrestBotsButton.Name = "ArrestBotsButton";
            this.ArrestBotsButton.Size = new System.Drawing.Size(120, 22);
            this.ArrestBotsButton.TabIndex = 7;
            this.ArrestBotsButton.Text = "Arrest bots";
            this.ArrestBotsButton.UseVisualStyleBackColor = true;
            this.ArrestBotsButton.Click += new System.EventHandler(this.ArrestBotsClick);
            // 
            // EventLogGroupBox
            // 
            this.EventLogGroupBox.Controls.Add(this.EventLogList);
            this.EventLogGroupBox.Location = new System.Drawing.Point(15, 32);
            this.EventLogGroupBox.Name = "EventLogGroupBox";
            this.EventLogGroupBox.Size = new System.Drawing.Size(648, 371);
            this.EventLogGroupBox.TabIndex = 8;
            this.EventLogGroupBox.TabStop = false;
            this.EventLogGroupBox.Text = "Event log";
            // 
            // EventLogList
            // 
            this.EventLogList.FormattingEnabled = true;
            this.EventLogList.Location = new System.Drawing.Point(6, 20);
            this.EventLogList.Name = "EventLogList";
            this.EventLogList.Size = new System.Drawing.Size(636, 342);
            this.EventLogList.TabIndex = 9;
            this.EventLogList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.CommunicationListDrawItem);
            // 
            // Command
            // 
            this.Command.Enabled = false;
            this.Command.Location = new System.Drawing.Point(21, 409);
            this.Command.Name = "Command";
            this.Command.Size = new System.Drawing.Size(561, 20);
            this.Command.TabIndex = 10;
            this.Command.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CommandKeyDown);
            // 
            // SendCommandButton
            // 
            this.SendCommandButton.Enabled = false;
            this.SendCommandButton.Location = new System.Drawing.Point(588, 409);
            this.SendCommandButton.Name = "SendCommandButton";
            this.SendCommandButton.Size = new System.Drawing.Size(75, 20);
            this.SendCommandButton.TabIndex = 11;
            this.SendCommandButton.Text = "Send";
            this.SendCommandButton.UseVisualStyleBackColor = true;
            this.SendCommandButton.Click += new System.EventHandler(this.SendCommandClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 435);
            this.Controls.Add(this.ServerIPLabel);
            this.Controls.Add(this.ServerIP);
            this.Controls.Add(this.PortLabel);
            this.Controls.Add(this.Port);
            this.Controls.Add(this.NumberOfBotsLabel);
            this.Controls.Add(this.NumberOfBots);
            this.Controls.Add(this.SpawnBotsButton);
            this.Controls.Add(this.ArrestBotsButton);
            this.Controls.Add(this.EventLogGroupBox);
            this.Controls.Add(this.Command);
            this.Controls.Add(this.SendCommandButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Knight Offline";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormClosing);
            this.EventLogGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label ServerIPLabel;
        private System.Windows.Forms.TextBox ServerIP;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.TextBox Port;
        private System.Windows.Forms.Label NumberOfBotsLabel;
        private System.Windows.Forms.TextBox NumberOfBots;
        private System.Windows.Forms.Button SpawnBotsButton;
        private System.Windows.Forms.Button ArrestBotsButton;
        private System.Windows.Forms.GroupBox EventLogGroupBox;
        private System.Windows.Forms.ListBox EventLogList;
        private System.Windows.Forms.TextBox Command;
        private System.Windows.Forms.Button SendCommandButton;
    }
}