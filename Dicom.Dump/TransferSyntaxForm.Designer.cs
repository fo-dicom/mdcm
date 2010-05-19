namespace Dicom.Forms {
	partial class TransferSyntaxForm {
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
			this.cbTransferSyntax = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.nuQualityRate = new System.Windows.Forms.NumericUpDown();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.nuQualityRate)).BeginInit();
			this.SuspendLayout();
			// 
			// cbTransferSyntax
			// 
			this.cbTransferSyntax.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbTransferSyntax.FormattingEnabled = true;
			this.cbTransferSyntax.Location = new System.Drawing.Point(15, 25);
			this.cbTransferSyntax.Name = "cbTransferSyntax";
			this.cbTransferSyntax.Size = new System.Drawing.Size(275, 21);
			this.cbTransferSyntax.TabIndex = 0;
			this.cbTransferSyntax.SelectedIndexChanged += new System.EventHandler(this.OnSelectTransferSyntax);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(84, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Transfer Syntax:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 59);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(70, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Quality/Rate:";
			// 
			// nuQualityRate
			// 
			this.nuQualityRate.Location = new System.Drawing.Point(15, 75);
			this.nuQualityRate.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.nuQualityRate.Name = "nuQualityRate";
			this.nuQualityRate.Size = new System.Drawing.Size(100, 20);
			this.nuQualityRate.TabIndex = 3;
			this.nuQualityRate.ValueChanged += new System.EventHandler(this.OnChangeQualityRate);
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(218, 122);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "&Cancel";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button2.Location = new System.Drawing.Point(137, 122);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 5;
			this.button2.Text = "&OK";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// TransferSyntaxForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(305, 157);
			this.ControlBox = false;
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.nuQualityRate);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cbTransferSyntax);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "TransferSyntaxForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select Transfer Syntax";
			((System.ComponentModel.ISupportInitialize)(this.nuQualityRate)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox cbTransferSyntax;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown nuQualityRate;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
	}
}