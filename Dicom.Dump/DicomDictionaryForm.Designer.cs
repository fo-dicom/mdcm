namespace Dicom.Forms {
	partial class DicomDictionaryForm {
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
			this.tbSearch = new System.Windows.Forms.TextBox();
			this.lblElementTagLabel = new System.Windows.Forms.Label();
			this.lbElements = new System.Windows.Forms.ListBox();
			this.lblElementTag = new System.Windows.Forms.Label();
			this.lblElementTagMask = new System.Windows.Forms.Label();
			this.lblElementName = new System.Windows.Forms.Label();
			this.lbValueRepresentations = new System.Windows.Forms.ListBox();
			this.lblValueMultiplicity = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.bttnClose = new System.Windows.Forms.Button();
			this.lblPrivateCreator = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// tbSearch
			// 
			this.tbSearch.Location = new System.Drawing.Point(12, 12);
			this.tbSearch.Name = "tbSearch";
			this.tbSearch.Size = new System.Drawing.Size(250, 20);
			this.tbSearch.TabIndex = 0;
			this.tbSearch.TextChanged += new System.EventHandler(this.tbSearch_TextChanged);
			// 
			// lblElementTagLabel
			// 
			this.lblElementTagLabel.AutoSize = true;
			this.lblElementTagLabel.Location = new System.Drawing.Point(330, 15);
			this.lblElementTagLabel.Name = "lblElementTagLabel";
			this.lblElementTagLabel.Size = new System.Drawing.Size(70, 13);
			this.lblElementTagLabel.TabIndex = 1;
			this.lblElementTagLabel.Text = "Element Tag:";
			// 
			// lbElements
			// 
			this.lbElements.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.lbElements.FormattingEnabled = true;
			this.lbElements.Location = new System.Drawing.Point(12, 38);
			this.lbElements.Name = "lbElements";
			this.lbElements.Size = new System.Drawing.Size(250, 290);
			this.lbElements.TabIndex = 2;
			this.lbElements.SelectedIndexChanged += new System.EventHandler(this.lbElements_SelectedIndexChanged);
			// 
			// lblElementTag
			// 
			this.lblElementTag.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblElementTag.Location = new System.Drawing.Point(406, 12);
			this.lblElementTag.Name = "lblElementTag";
			this.lblElementTag.Size = new System.Drawing.Size(204, 23);
			this.lblElementTag.TabIndex = 3;
			// 
			// lblElementTagMask
			// 
			this.lblElementTagMask.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblElementTagMask.Location = new System.Drawing.Point(406, 45);
			this.lblElementTagMask.Name = "lblElementTagMask";
			this.lblElementTagMask.Size = new System.Drawing.Size(204, 23);
			this.lblElementTagMask.TabIndex = 4;
			// 
			// lblElementName
			// 
			this.lblElementName.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblElementName.Location = new System.Drawing.Point(406, 78);
			this.lblElementName.Name = "lblElementName";
			this.lblElementName.Size = new System.Drawing.Size(204, 23);
			this.lblElementName.TabIndex = 5;
			// 
			// lbValueRepresentations
			// 
			this.lbValueRepresentations.BackColor = System.Drawing.SystemColors.Control;
			this.lbValueRepresentations.FormattingEnabled = true;
			this.lbValueRepresentations.Location = new System.Drawing.Point(406, 114);
			this.lbValueRepresentations.Name = "lbValueRepresentations";
			this.lbValueRepresentations.SelectionMode = System.Windows.Forms.SelectionMode.None;
			this.lbValueRepresentations.Size = new System.Drawing.Size(201, 43);
			this.lbValueRepresentations.TabIndex = 6;
			// 
			// lblValueMultiplicity
			// 
			this.lblValueMultiplicity.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblValueMultiplicity.Location = new System.Drawing.Point(406, 170);
			this.lblValueMultiplicity.Name = "lblValueMultiplicity";
			this.lblValueMultiplicity.Size = new System.Drawing.Size(204, 23);
			this.lblValueMultiplicity.TabIndex = 7;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(364, 46);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(36, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Mask:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(362, 79);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(38, 13);
			this.label2.TabIndex = 9;
			this.label2.Text = "Name:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(283, 114);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(117, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Value Representations:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(312, 171);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(88, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "Value Multiplicity:";
			// 
			// bttnClose
			// 
			this.bttnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.bttnClose.Location = new System.Drawing.Point(500, 296);
			this.bttnClose.Name = "bttnClose";
			this.bttnClose.Size = new System.Drawing.Size(107, 32);
			this.bttnClose.TabIndex = 12;
			this.bttnClose.Text = "&Close";
			this.bttnClose.UseVisualStyleBackColor = true;
			// 
			// lblPrivateCreator
			// 
			this.lblPrivateCreator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblPrivateCreator.Location = new System.Drawing.Point(406, 206);
			this.lblPrivateCreator.Name = "lblPrivateCreator";
			this.lblPrivateCreator.Size = new System.Drawing.Size(204, 23);
			this.lblPrivateCreator.TabIndex = 13;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(320, 207);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(80, 13);
			this.label6.TabIndex = 14;
			this.label6.Text = "Private Creator:";
			// 
			// DicomDictionaryForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(622, 342);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.lblPrivateCreator);
			this.Controls.Add(this.bttnClose);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lblValueMultiplicity);
			this.Controls.Add(this.lbValueRepresentations);
			this.Controls.Add(this.lblElementName);
			this.Controls.Add(this.lblElementTagMask);
			this.Controls.Add(this.lblElementTag);
			this.Controls.Add(this.lbElements);
			this.Controls.Add(this.lblElementTagLabel);
			this.Controls.Add(this.tbSearch);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "DicomDictionaryForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "DICOM Dictionary";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox tbSearch;
		private System.Windows.Forms.Label lblElementTagLabel;
		private System.Windows.Forms.ListBox lbElements;
		private System.Windows.Forms.Label lblElementTag;
		private System.Windows.Forms.Label lblElementTagMask;
		private System.Windows.Forms.Label lblElementName;
		private System.Windows.Forms.ListBox lbValueRepresentations;
		private System.Windows.Forms.Label lblValueMultiplicity;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button bttnClose;
		private System.Windows.Forms.Label lblPrivateCreator;
		private System.Windows.Forms.Label label6;
	}
}