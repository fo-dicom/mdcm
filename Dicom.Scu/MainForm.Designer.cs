namespace Dicom.Scu {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.nuQuality = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.nuMaxPdu = new System.Windows.Forms.NumericUpDown();
			this.label6 = new System.Windows.Forms.Label();
			this.cbTransferSyntax = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.nuTimeout = new System.Windows.Forms.NumericUpDown();
			this.bttnTest = new System.Windows.Forms.Button();
			this.cbUseTls = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.nuRemotePort = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.tbRemoteHost = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.tbRemoteAE = new System.Windows.Forms.TextBox();
			this.tbLocalAE = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.bttnSendClear = new System.Windows.Forms.Button();
			this.bttnSend = new System.Windows.Forms.Button();
			this.bttnSendAddImage = new System.Windows.Forms.Button();
			this.bttnSendRemoveImage = new System.Windows.Forms.Button();
			this.lvSendImages = new System.Windows.Forms.ListView();
			this.colSendFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSendSopClass = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSendTransfer = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colSendStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.statusImageList = new System.Windows.Forms.ImageList(this.components);
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.tabStatus = new System.Windows.Forms.TabPage();
			this.rtbLog = new System.Windows.Forms.RichTextBox();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nuQuality)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nuMaxPdu)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nuTimeout)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nuRemotePort)).BeginInit();
			this.tabPage2.SuspendLayout();
			this.tabStatus.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Controls.Add(this.tabStatus);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.HotTrack = true;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(592, 386);
			this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.groupBox1);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(584, 360);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Network Settings";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.nuQuality);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.nuMaxPdu);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.cbTransferSyntax);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.nuTimeout);
			this.groupBox1.Controls.Add(this.bttnTest);
			this.groupBox1.Controls.Add(this.cbUseTls);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.nuRemotePort);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.tbRemoteHost);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.tbRemoteAE);
			this.groupBox1.Controls.Add(this.tbLocalAE);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(6, 6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(437, 273);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "DICOM Network";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(88, 207);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(67, 13);
			this.label8.TabIndex = 15;
			this.label8.Text = "Quality/Rate";
			// 
			// nuQuality
			// 
			this.nuQuality.Location = new System.Drawing.Point(161, 205);
			this.nuQuality.Name = "nuQuality";
			this.nuQuality.Size = new System.Drawing.Size(150, 20);
			this.nuQuality.TabIndex = 14;
			this.nuQuality.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(79, 128);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(76, 13);
			this.label7.TabIndex = 13;
			this.label7.Text = "Max PDU Size";
			// 
			// nuMaxPdu
			// 
			this.nuMaxPdu.Location = new System.Drawing.Point(161, 126);
			this.nuMaxPdu.Maximum = new decimal(new int[] {
            4194304,
            0,
            0,
            0});
			this.nuMaxPdu.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.nuMaxPdu.Name = "nuMaxPdu";
			this.nuMaxPdu.Size = new System.Drawing.Size(150, 20);
			this.nuMaxPdu.TabIndex = 12;
			this.nuMaxPdu.Value = new decimal(new int[] {
            32768,
            0,
            0,
            0});
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(74, 181);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(81, 13);
			this.label6.TabIndex = 11;
			this.label6.Text = "Transfer Syntax";
			// 
			// cbTransferSyntax
			// 
			this.cbTransferSyntax.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbTransferSyntax.FormattingEnabled = true;
			this.cbTransferSyntax.Location = new System.Drawing.Point(161, 178);
			this.cbTransferSyntax.Name = "cbTransferSyntax";
			this.cbTransferSyntax.Size = new System.Drawing.Size(150, 21);
			this.cbTransferSyntax.TabIndex = 10;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(110, 154);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(45, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "Timeout";
			// 
			// nuTimeout
			// 
			this.nuTimeout.Location = new System.Drawing.Point(161, 152);
			this.nuTimeout.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.nuTimeout.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nuTimeout.Name = "nuTimeout";
			this.nuTimeout.Size = new System.Drawing.Size(150, 20);
			this.nuTimeout.TabIndex = 8;
			this.nuTimeout.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// bttnTest
			// 
			this.bttnTest.Location = new System.Drawing.Point(236, 231);
			this.bttnTest.Name = "bttnTest";
			this.bttnTest.Size = new System.Drawing.Size(75, 23);
			this.bttnTest.TabIndex = 5;
			this.bttnTest.Text = "Test";
			this.bttnTest.UseVisualStyleBackColor = true;
			this.bttnTest.Click += new System.EventHandler(this.OnClickTest);
			// 
			// cbUseTls
			// 
			this.cbUseTls.AutoSize = true;
			this.cbUseTls.Location = new System.Drawing.Point(161, 235);
			this.cbUseTls.Name = "cbUseTls";
			this.cbUseTls.Size = new System.Drawing.Size(46, 17);
			this.cbUseTls.TabIndex = 4;
			this.cbUseTls.Text = "TLS";
			this.cbUseTls.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(91, 102);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(64, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "DICOM Port";
			// 
			// nuRemotePort
			// 
			this.nuRemotePort.Location = new System.Drawing.Point(161, 100);
			this.nuRemotePort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.nuRemotePort.Name = "nuRemotePort";
			this.nuRemotePort.Size = new System.Drawing.Size(150, 20);
			this.nuRemotePort.TabIndex = 3;
			this.nuRemotePort.Value = new decimal(new int[] {
            104,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(60, 77);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(95, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Host or IP Address";
			// 
			// tbRemoteHost
			// 
			this.tbRemoteHost.Location = new System.Drawing.Point(161, 74);
			this.tbRemoteHost.Name = "tbRemoteHost";
			this.tbRemoteHost.Size = new System.Drawing.Size(150, 20);
			this.tbRemoteHost.TabIndex = 2;
			this.tbRemoteHost.Text = "localhost";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(71, 51);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(84, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Remote AE Title";
			// 
			// tbRemoteAE
			// 
			this.tbRemoteAE.Location = new System.Drawing.Point(161, 48);
			this.tbRemoteAE.Name = "tbRemoteAE";
			this.tbRemoteAE.Size = new System.Drawing.Size(150, 20);
			this.tbRemoteAE.TabIndex = 1;
			this.tbRemoteAE.Text = "ANY_SCP";
			// 
			// tbLocalAE
			// 
			this.tbLocalAE.Location = new System.Drawing.Point(161, 22);
			this.tbLocalAE.Name = "tbLocalAE";
			this.tbLocalAE.Size = new System.Drawing.Size(150, 20);
			this.tbLocalAE.TabIndex = 0;
			this.tbLocalAE.Text = "TEST_SCU";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(82, 25);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(73, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Local AE Title";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.bttnSendClear);
			this.tabPage2.Controls.Add(this.bttnSend);
			this.tabPage2.Controls.Add(this.bttnSendAddImage);
			this.tabPage2.Controls.Add(this.bttnSendRemoveImage);
			this.tabPage2.Controls.Add(this.lvSendImages);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(584, 360);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "C-Store";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// bttnSendClear
			// 
			this.bttnSendClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.bttnSendClear.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bttnSendClear.Location = new System.Drawing.Point(104, 309);
			this.bttnSendClear.Name = "bttnSendClear";
			this.bttnSendClear.Size = new System.Drawing.Size(75, 43);
			this.bttnSendClear.TabIndex = 4;
			this.bttnSendClear.Text = "Clear";
			this.bttnSendClear.UseVisualStyleBackColor = true;
			this.bttnSendClear.Click += new System.EventHandler(this.OnSendClear);
			// 
			// bttnSend
			// 
			this.bttnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.bttnSend.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bttnSend.Location = new System.Drawing.Point(491, 309);
			this.bttnSend.Name = "bttnSend";
			this.bttnSend.Size = new System.Drawing.Size(85, 43);
			this.bttnSend.TabIndex = 3;
			this.bttnSend.Text = "Send";
			this.bttnSend.UseVisualStyleBackColor = true;
			this.bttnSend.Click += new System.EventHandler(this.OnSend);
			// 
			// bttnSendAddImage
			// 
			this.bttnSendAddImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.bttnSendAddImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bttnSendAddImage.Location = new System.Drawing.Point(55, 309);
			this.bttnSendAddImage.Name = "bttnSendAddImage";
			this.bttnSendAddImage.Size = new System.Drawing.Size(43, 43);
			this.bttnSendAddImage.TabIndex = 2;
			this.bttnSendAddImage.Text = "+";
			this.bttnSendAddImage.UseVisualStyleBackColor = true;
			this.bttnSendAddImage.Click += new System.EventHandler(this.OnSendAddImage);
			// 
			// bttnSendRemoveImage
			// 
			this.bttnSendRemoveImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.bttnSendRemoveImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bttnSendRemoveImage.Location = new System.Drawing.Point(6, 309);
			this.bttnSendRemoveImage.Name = "bttnSendRemoveImage";
			this.bttnSendRemoveImage.Size = new System.Drawing.Size(43, 43);
			this.bttnSendRemoveImage.TabIndex = 1;
			this.bttnSendRemoveImage.Text = "-";
			this.bttnSendRemoveImage.UseVisualStyleBackColor = true;
			this.bttnSendRemoveImage.Click += new System.EventHandler(this.OnSendRemoveImage);
			// 
			// lvSendImages
			// 
			this.lvSendImages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lvSendImages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lvSendImages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSendFile,
            this.colSendSopClass,
            this.colSendTransfer,
            this.colSendStatus});
			this.lvSendImages.FullRowSelect = true;
			this.lvSendImages.GridLines = true;
			this.lvSendImages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvSendImages.Location = new System.Drawing.Point(0, 0);
			this.lvSendImages.Name = "lvSendImages";
			this.lvSendImages.Size = new System.Drawing.Size(584, 300);
			this.lvSendImages.SmallImageList = this.statusImageList;
			this.lvSendImages.TabIndex = 0;
			this.lvSendImages.UseCompatibleStateImageBehavior = false;
			this.lvSendImages.View = System.Windows.Forms.View.Details;
			// 
			// colSendFile
			// 
			this.colSendFile.Text = "File";
			this.colSendFile.Width = 228;
			// 
			// colSendSopClass
			// 
			this.colSendSopClass.Text = "SOP Class";
			this.colSendSopClass.Width = 129;
			// 
			// colSendTransfer
			// 
			this.colSendTransfer.Text = "Transfer Syntax";
			this.colSendTransfer.Width = 131;
			// 
			// colSendStatus
			// 
			this.colSendStatus.Text = "Status";
			this.colSendStatus.Width = 72;
			// 
			// statusImageList
			// 
			this.statusImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("statusImageList.ImageStream")));
			this.statusImageList.TransparentColor = System.Drawing.Color.Magenta;
			this.statusImageList.Images.SetKeyName(0, "Input.bmp");
			this.statusImageList.Images.SetKeyName(1, "OK.bmp");
			this.statusImageList.Images.SetKeyName(2, "Critical.bmp");
			// 
			// tabPage3
			// 
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(584, 360);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "C-Find";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// tabPage4
			// 
			this.tabPage4.Location = new System.Drawing.Point(4, 22);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage4.Size = new System.Drawing.Size(584, 360);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "C-Move";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// tabStatus
			// 
			this.tabStatus.Controls.Add(this.rtbLog);
			this.tabStatus.Location = new System.Drawing.Point(4, 22);
			this.tabStatus.Name = "tabStatus";
			this.tabStatus.Padding = new System.Windows.Forms.Padding(3);
			this.tabStatus.Size = new System.Drawing.Size(584, 360);
			this.tabStatus.TabIndex = 4;
			this.tabStatus.Text = "Status";
			this.tabStatus.UseVisualStyleBackColor = true;
			// 
			// rtbLog
			// 
			this.rtbLog.BackColor = System.Drawing.Color.White;
			this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.rtbLog.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.rtbLog.ForeColor = System.Drawing.Color.White;
			this.rtbLog.Location = new System.Drawing.Point(3, 3);
			this.rtbLog.Name = "rtbLog";
			this.rtbLog.ReadOnly = true;
			this.rtbLog.Size = new System.Drawing.Size(578, 354);
			this.rtbLog.TabIndex = 0;
			this.rtbLog.Text = "";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(592, 386);
			this.Controls.Add(this.tabControl1);
			this.Name = "MainForm";
			this.Text = "DICOM SCU";
			this.Load += new System.EventHandler(this.OnLoad);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nuQuality)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nuMaxPdu)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nuTimeout)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nuRemotePort)).EndInit();
			this.tabPage2.ResumeLayout(false);
			this.tabStatus.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.TextBox tbRemoteAE;
		private System.Windows.Forms.TextBox tbLocalAE;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.NumericUpDown nuRemotePort;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox tbRemoteHost;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button bttnTest;
		private System.Windows.Forms.CheckBox cbUseTls;
		private System.Windows.Forms.Button bttnSendAddImage;
		private System.Windows.Forms.Button bttnSendRemoveImage;
		private System.Windows.Forms.ListView lvSendImages;
		private System.Windows.Forms.Button bttnSend;
		private System.Windows.Forms.ColumnHeader colSendFile;
		private System.Windows.Forms.ColumnHeader colSendSopClass;
		private System.Windows.Forms.ColumnHeader colSendTransfer;
		private System.Windows.Forms.TabPage tabStatus;
		private System.Windows.Forms.RichTextBox rtbLog;
		private System.Windows.Forms.Button bttnSendClear;
		private System.Windows.Forms.ColumnHeader colSendStatus;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.NumericUpDown nuTimeout;
		private System.Windows.Forms.ComboBox cbTransferSyntax;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.NumericUpDown nuMaxPdu;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.NumericUpDown nuQuality;
		private System.Windows.Forms.ImageList statusImageList;
	}
}

