namespace QMSTool
{
    partial class QMSTool
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
            this.groupBoxCommunication = new System.Windows.Forms.GroupBox();
            this.buttonUpdateFirmware = new System.Windows.Forms.Button();
            this.buttonVersion = new System.Windows.Forms.Button();
            this.textBoxRegValue = new System.Windows.Forms.TextBox();
            this.textBoxRegAddr = new System.Windows.Forms.TextBox();
            this.labelRegValue = new System.Windows.Forms.Label();
            this.labelRegOffset = new System.Windows.Forms.Label();
            this.buttonWriteRegister = new System.Windows.Forms.Button();
            this.buttonReadRegister = new System.Windows.Forms.Button();
            this.buttonReadAllRegisters = new System.Windows.Forms.Button();
            this.richTextBoxInfo = new System.Windows.Forms.RichTextBox();
            this.comboBoxFtdiDevice = new System.Windows.Forms.ComboBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonDisconnect = new System.Windows.Forms.Button();
            this.groupBoxIo = new System.Windows.Forms.GroupBox();
            this.buttonClearLog = new System.Windows.Forms.Button();
            this.groupBoxCommunication.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxCommunication
            // 
            this.groupBoxCommunication.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxCommunication.Controls.Add(this.buttonUpdateFirmware);
            this.groupBoxCommunication.Controls.Add(this.buttonVersion);
            this.groupBoxCommunication.Controls.Add(this.textBoxRegValue);
            this.groupBoxCommunication.Controls.Add(this.textBoxRegAddr);
            this.groupBoxCommunication.Controls.Add(this.labelRegValue);
            this.groupBoxCommunication.Controls.Add(this.labelRegOffset);
            this.groupBoxCommunication.Controls.Add(this.buttonWriteRegister);
            this.groupBoxCommunication.Controls.Add(this.buttonReadRegister);
            this.groupBoxCommunication.Controls.Add(this.buttonReadAllRegisters);
            this.groupBoxCommunication.Enabled = false;
            this.groupBoxCommunication.Location = new System.Drawing.Point(12, 93);
            this.groupBoxCommunication.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxCommunication.Name = "groupBoxCommunication";
            this.groupBoxCommunication.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBoxCommunication.Size = new System.Drawing.Size(204, 422);
            this.groupBoxCommunication.TabIndex = 3;
            this.groupBoxCommunication.TabStop = false;
            this.groupBoxCommunication.Text = "Communication";
            // 
            // buttonUpdateFirmware
            // 
            this.buttonUpdateFirmware.Location = new System.Drawing.Point(56, 306);
            this.buttonUpdateFirmware.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonUpdateFirmware.Name = "buttonUpdateFirmware";
            this.buttonUpdateFirmware.Size = new System.Drawing.Size(89, 61);
            this.buttonUpdateFirmware.TabIndex = 9;
            this.buttonUpdateFirmware.Text = "Update Firmware";
            this.buttonUpdateFirmware.UseVisualStyleBackColor = true;
            this.buttonUpdateFirmware.Click += new System.EventHandler(this.buttonUpdateFirmware_Click);
            // 
            // buttonVersion
            // 
            this.buttonVersion.Location = new System.Drawing.Point(56, 28);
            this.buttonVersion.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonVersion.Name = "buttonVersion";
            this.buttonVersion.Size = new System.Drawing.Size(89, 28);
            this.buttonVersion.TabIndex = 8;
            this.buttonVersion.Text = "Version";
            this.buttonVersion.UseVisualStyleBackColor = true;
            this.buttonVersion.Click += new System.EventHandler(this.buttonVersion_Click);
            // 
            // textBoxRegValue
            // 
            this.textBoxRegValue.Location = new System.Drawing.Point(105, 223);
            this.textBoxRegValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxRegValue.Name = "textBoxRegValue";
            this.textBoxRegValue.Size = new System.Drawing.Size(89, 22);
            this.textBoxRegValue.TabIndex = 7;
            // 
            // textBoxRegAddr
            // 
            this.textBoxRegAddr.Location = new System.Drawing.Point(11, 223);
            this.textBoxRegAddr.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxRegAddr.Name = "textBoxRegAddr";
            this.textBoxRegAddr.Size = new System.Drawing.Size(89, 22);
            this.textBoxRegAddr.TabIndex = 6;
            // 
            // labelRegValue
            // 
            this.labelRegValue.AutoSize = true;
            this.labelRegValue.Location = new System.Drawing.Point(133, 203);
            this.labelRegValue.Name = "labelRegValue";
            this.labelRegValue.Size = new System.Drawing.Size(42, 17);
            this.labelRegValue.TabIndex = 5;
            this.labelRegValue.Text = "value";
            // 
            // labelRegOffset
            // 
            this.labelRegOffset.AutoSize = true;
            this.labelRegOffset.Location = new System.Drawing.Point(25, 203);
            this.labelRegOffset.Name = "labelRegOffset";
            this.labelRegOffset.Size = new System.Drawing.Size(68, 17);
            this.labelRegOffset.TabIndex = 4;
            this.labelRegOffset.Text = "reg offset";
            // 
            // buttonWriteRegister
            // 
            this.buttonWriteRegister.Location = new System.Drawing.Point(105, 173);
            this.buttonWriteRegister.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonWriteRegister.Name = "buttonWriteRegister";
            this.buttonWriteRegister.Size = new System.Drawing.Size(93, 28);
            this.buttonWriteRegister.TabIndex = 3;
            this.buttonWriteRegister.Text = "Write";
            this.buttonWriteRegister.UseVisualStyleBackColor = true;
            this.buttonWriteRegister.Click += new System.EventHandler(this.buttonWriteRegister_Click);
            // 
            // buttonReadRegister
            // 
            this.buttonReadRegister.Location = new System.Drawing.Point(11, 173);
            this.buttonReadRegister.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonReadRegister.Name = "buttonReadRegister";
            this.buttonReadRegister.Size = new System.Drawing.Size(89, 28);
            this.buttonReadRegister.TabIndex = 2;
            this.buttonReadRegister.Text = "Read";
            this.buttonReadRegister.UseVisualStyleBackColor = true;
            this.buttonReadRegister.Click += new System.EventHandler(this.buttonReadRegister_Click);
            // 
            // buttonReadAllRegisters
            // 
            this.buttonReadAllRegisters.Location = new System.Drawing.Point(28, 129);
            this.buttonReadAllRegisters.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonReadAllRegisters.Name = "buttonReadAllRegisters";
            this.buttonReadAllRegisters.Size = new System.Drawing.Size(148, 28);
            this.buttonReadAllRegisters.TabIndex = 1;
            this.buttonReadAllRegisters.Text = "Read All Registers";
            this.buttonReadAllRegisters.UseVisualStyleBackColor = true;
            this.buttonReadAllRegisters.Click += new System.EventHandler(this.buttonReadAllRegisters_Click);
            // 
            // richTextBoxInfo
            // 
            this.richTextBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxInfo.Enabled = false;
            this.richTextBoxInfo.Location = new System.Drawing.Point(222, 43);
            this.richTextBoxInfo.Name = "richTextBoxInfo";
            this.richTextBoxInfo.ReadOnly = true;
            this.richTextBoxInfo.Size = new System.Drawing.Size(698, 472);
            this.richTextBoxInfo.TabIndex = 4;
            this.richTextBoxInfo.Text = "";
            // 
            // comboBoxFtdiDevice
            // 
            this.comboBoxFtdiDevice.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxFtdiDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFtdiDevice.FormattingEnabled = true;
            this.comboBoxFtdiDevice.Location = new System.Drawing.Point(107, 12);
            this.comboBoxFtdiDevice.Name = "comboBoxFtdiDevice";
            this.comboBoxFtdiDevice.Size = new System.Drawing.Size(808, 24);
            this.comboBoxFtdiDevice.TabIndex = 5;
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(12, 8);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(89, 28);
            this.buttonConnect.TabIndex = 6;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonDisconnect
            // 
            this.buttonDisconnect.Enabled = false;
            this.buttonDisconnect.Location = new System.Drawing.Point(12, 40);
            this.buttonDisconnect.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonDisconnect.Name = "buttonDisconnect";
            this.buttonDisconnect.Size = new System.Drawing.Size(89, 28);
            this.buttonDisconnect.TabIndex = 7;
            this.buttonDisconnect.Text = "Disconnect";
            this.buttonDisconnect.UseVisualStyleBackColor = true;
            this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);
            // 
            // groupBoxIo
            // 
            this.groupBoxIo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxIo.Enabled = false;
            this.groupBoxIo.Location = new System.Drawing.Point(12, 521);
            this.groupBoxIo.Name = "groupBoxIo";
            this.groupBoxIo.Size = new System.Drawing.Size(903, 280);
            this.groupBoxIo.TabIndex = 8;
            this.groupBoxIo.TabStop = false;
            this.groupBoxIo.Text = "IO";
            // 
            // buttonClearLog
            // 
            this.buttonClearLog.Enabled = false;
            this.buttonClearLog.Location = new System.Drawing.Point(127, 43);
            this.buttonClearLog.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonClearLog.Name = "buttonClearLog";
            this.buttonClearLog.Size = new System.Drawing.Size(89, 28);
            this.buttonClearLog.TabIndex = 9;
            this.buttonClearLog.Text = "Clear Log";
            this.buttonClearLog.UseVisualStyleBackColor = true;
            this.buttonClearLog.Click += new System.EventHandler(this.buttonClearLog_Click);
            // 
            // QMSTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(927, 813);
            this.Controls.Add(this.buttonClearLog);
            this.Controls.Add(this.groupBoxIo);
            this.Controls.Add(this.buttonDisconnect);
            this.Controls.Add(this.buttonConnect);
            this.Controls.Add(this.comboBoxFtdiDevice);
            this.Controls.Add(this.richTextBoxInfo);
            this.Controls.Add(this.groupBoxCommunication);
            this.Name = "QMSTool";
            this.Text = "Form1";
            this.groupBoxCommunication.ResumeLayout(false);
            this.groupBoxCommunication.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxCommunication;
        private System.Windows.Forms.TextBox textBoxRegValue;
        private System.Windows.Forms.TextBox textBoxRegAddr;
        private System.Windows.Forms.Label labelRegValue;
        private System.Windows.Forms.Label labelRegOffset;
        private System.Windows.Forms.Button buttonWriteRegister;
        private System.Windows.Forms.Button buttonReadRegister;
        private System.Windows.Forms.Button buttonReadAllRegisters;
        private System.Windows.Forms.RichTextBox richTextBoxInfo;
        private System.Windows.Forms.ComboBox comboBoxFtdiDevice;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonDisconnect;
        private System.Windows.Forms.Button buttonVersion;
        private System.Windows.Forms.Button buttonUpdateFirmware;
        private System.Windows.Forms.GroupBox groupBoxIo;
        private System.Windows.Forms.Button buttonClearLog;

    }
}

