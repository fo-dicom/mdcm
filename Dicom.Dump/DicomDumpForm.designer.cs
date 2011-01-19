namespace Dicom.Forms {
	partial class DicomDumpForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DicomDumpForm));
			this.treeDump = new Aga.Controls.Tree.TreeViewAdv();
			this.ElementColumn = new Aga.Controls.Tree.TreeColumn();
			this.VrColumn = new Aga.Controls.Tree.TreeColumn();
			this.LengthColumn = new Aga.Controls.Tree.TreeColumn();
			this.ValueColumn = new Aga.Controls.Tree.TreeColumn();
			this.nvIcon = new Aga.Controls.Tree.NodeControls.NodeIcon();
			this.ncTag = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.ncName = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.nvVR = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.nvLength = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.nvValue = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.tsbOpenFile = new System.Windows.Forms.ToolStripButton();
			this.tsbClose = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbDictionary = new System.Windows.Forms.ToolStripButton();
			this.tsbViewImage = new System.Windows.Forms.ToolStripButton();
			this.tsbAdvanced = new System.Windows.Forms.ToolStripDropDownButton();
			this.tsbExtractPixels = new System.Windows.Forms.ToolStripMenuItem();
			this.tsbSaveTS = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbPrev = new System.Windows.Forms.ToolStripButton();
			this.tsbNext = new System.Windows.Forms.ToolStripButton();
			this.lblCount = new System.Windows.Forms.ToolStripLabel();
			this.tsbPixelDataMD5 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// treeDump
			// 
			this.treeDump.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.treeDump.BackColor = System.Drawing.SystemColors.Window;
			this.treeDump.Columns.Add(this.ElementColumn);
			this.treeDump.Columns.Add(this.VrColumn);
			this.treeDump.Columns.Add(this.LengthColumn);
			this.treeDump.Columns.Add(this.ValueColumn);
			this.treeDump.Cursor = System.Windows.Forms.Cursors.Default;
			this.treeDump.DefaultToolTipProvider = null;
			this.treeDump.DragDropMarkColor = System.Drawing.Color.Black;
			this.treeDump.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.treeDump.FullRowSelect = true;
			this.treeDump.LineColor = System.Drawing.SystemColors.ControlDark;
			this.treeDump.Location = new System.Drawing.Point(12, 12);
			this.treeDump.Model = null;
			this.treeDump.Name = "treeDump";
			this.treeDump.NodeControls.Add(this.nvIcon);
			this.treeDump.NodeControls.Add(this.ncTag);
			this.treeDump.NodeControls.Add(this.ncName);
			this.treeDump.NodeControls.Add(this.nvVR);
			this.treeDump.NodeControls.Add(this.nvLength);
			this.treeDump.NodeControls.Add(this.nvValue);
			this.treeDump.RowHeight = 18;
			this.treeDump.SelectedNode = null;
			this.treeDump.Size = new System.Drawing.Size(711, 367);
			this.treeDump.TabIndex = 0;
			this.treeDump.UseColumns = true;
			this.treeDump.NodeMouseDoubleClick += new System.EventHandler<Aga.Controls.Tree.TreeNodeAdvMouseEventArgs>(this.treeDump_NodeMouseDoubleClick);
			this.treeDump.SizeChanged += new System.EventHandler(this.treeDump_SizeChanged);
			// 
			// ElementColumn
			// 
			this.ElementColumn.Header = "Element";
			this.ElementColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			this.ElementColumn.TooltipText = null;
			this.ElementColumn.Width = 300;
			// 
			// VrColumn
			// 
			this.VrColumn.Header = "VR";
			this.VrColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			this.VrColumn.TooltipText = null;
			// 
			// LengthColumn
			// 
			this.LengthColumn.Header = "Length";
			this.LengthColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			this.LengthColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.LengthColumn.TooltipText = null;
			this.LengthColumn.Width = 80;
			// 
			// ValueColumn
			// 
			this.ValueColumn.Header = "Value";
			this.ValueColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			this.ValueColumn.TooltipText = null;
			this.ValueColumn.Width = 250;
			// 
			// nvIcon
			// 
			this.nvIcon.DataPropertyName = "Icon";
			this.nvIcon.LeftMargin = 1;
			this.nvIcon.ParentColumn = this.ElementColumn;
			this.nvIcon.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Clip;
			// 
			// ncTag
			// 
			this.ncTag.DataPropertyName = "ElementTag";
			this.ncTag.IncrementalSearchEnabled = true;
			this.ncTag.LeftMargin = 3;
			this.ncTag.ParentColumn = this.ElementColumn;
			// 
			// ncName
			// 
			this.ncName.DataPropertyName = "Name";
			this.ncName.IncrementalSearchEnabled = true;
			this.ncName.LeftMargin = 3;
			this.ncName.ParentColumn = this.ElementColumn;
			this.ncName.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
			// 
			// nvVR
			// 
			this.nvVR.DataPropertyName = "VR";
			this.nvVR.IncrementalSearchEnabled = true;
			this.nvVR.LeftMargin = 3;
			this.nvVR.ParentColumn = this.VrColumn;
			// 
			// nvLength
			// 
			this.nvLength.DataPropertyName = "Length";
			this.nvLength.IncrementalSearchEnabled = true;
			this.nvLength.LeftMargin = 3;
			this.nvLength.ParentColumn = this.LengthColumn;
			this.nvLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// nvValue
			// 
			this.nvValue.DataPropertyName = "Value";
			this.nvValue.IncrementalSearchEnabled = true;
			this.nvValue.LeftMargin = 3;
			this.nvValue.ParentColumn = this.ValueColumn;
			this.nvValue.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.Controls.Add(this.treeDump);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(735, 391);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(735, 416);
			this.toolStripContainer1.TabIndex = 1;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbOpenFile,
            this.tsbClose,
            this.toolStripSeparator1,
            this.tsbDictionary,
            this.tsbViewImage,
            this.tsbAdvanced,
            this.toolStripSeparator2,
            this.tsbPrev,
            this.tsbNext,
            this.lblCount});
			this.toolStrip1.Location = new System.Drawing.Point(3, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(571, 25);
			this.toolStrip1.TabIndex = 0;
			// 
			// tsbOpenFile
			// 
			this.tsbOpenFile.Image = ((System.Drawing.Image)(resources.GetObject("tsbOpenFile.Image")));
			this.tsbOpenFile.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbOpenFile.Name = "tsbOpenFile";
			this.tsbOpenFile.Size = new System.Drawing.Size(77, 22);
			this.tsbOpenFile.Text = "&Open File";
			this.tsbOpenFile.Click += new System.EventHandler(this.tsbOpenFile_Click);
			// 
			// tsbClose
			// 
			this.tsbClose.Image = ((System.Drawing.Image)(resources.GetObject("tsbClose.Image")));
			this.tsbClose.ImageTransparentColor = System.Drawing.Color.Black;
			this.tsbClose.Name = "tsbClose";
			this.tsbClose.Size = new System.Drawing.Size(56, 22);
			this.tsbClose.Text = "Close";
			this.tsbClose.Click += new System.EventHandler(this.OnClickClose);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbDictionary
			// 
			this.tsbDictionary.Image = ((System.Drawing.Image)(resources.GetObject("tsbDictionary.Image")));
			this.tsbDictionary.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbDictionary.Name = "tsbDictionary";
			this.tsbDictionary.Size = new System.Drawing.Size(81, 22);
			this.tsbDictionary.Text = "Dictionary";
			this.tsbDictionary.Click += new System.EventHandler(this.tsbDictionary_Click);
			// 
			// tsbViewImage
			// 
			this.tsbViewImage.Enabled = false;
			this.tsbViewImage.Image = ((System.Drawing.Image)(resources.GetObject("tsbViewImage.Image")));
			this.tsbViewImage.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbViewImage.Name = "tsbViewImage";
			this.tsbViewImage.Size = new System.Drawing.Size(88, 22);
			this.tsbViewImage.Text = "View Image";
			this.tsbViewImage.Click += new System.EventHandler(this.OnViewImage);
			// 
			// tsbAdvanced
			// 
			this.tsbAdvanced.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbExtractPixels,
            this.tsbPixelDataMD5,
            this.tsbSaveTS});
			this.tsbAdvanced.Image = ((System.Drawing.Image)(resources.GetObject("tsbAdvanced.Image")));
			this.tsbAdvanced.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbAdvanced.Name = "tsbAdvanced";
			this.tsbAdvanced.Size = new System.Drawing.Size(89, 22);
			this.tsbAdvanced.Text = "Advanced";
			// 
			// tsbExtractPixels
			// 
			this.tsbExtractPixels.Enabled = false;
			this.tsbExtractPixels.Name = "tsbExtractPixels";
			this.tsbExtractPixels.Size = new System.Drawing.Size(198, 22);
			this.tsbExtractPixels.Text = "Extract Pixel Data";
			this.tsbExtractPixels.Click += new System.EventHandler(this.OnClickExtractPixels);
			// 
			// tsbSaveTS
			// 
			this.tsbSaveTS.Enabled = false;
			this.tsbSaveTS.Name = "tsbSaveTS";
			this.tsbSaveTS.Size = new System.Drawing.Size(198, 22);
			this.tsbSaveTS.Text = "Save /w Transfer Syntax";
			this.tsbSaveTS.Click += new System.EventHandler(this.OnClickSaveWithTransferSyntax);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbPrev
			// 
			this.tsbPrev.Enabled = false;
			this.tsbPrev.Image = ((System.Drawing.Image)(resources.GetObject("tsbPrev.Image")));
			this.tsbPrev.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbPrev.Name = "tsbPrev";
			this.tsbPrev.Size = new System.Drawing.Size(50, 22);
			this.tsbPrev.Text = "Prev";
			this.tsbPrev.Click += new System.EventHandler(this.OnClickPrev);
			// 
			// tsbNext
			// 
			this.tsbNext.Enabled = false;
			this.tsbNext.Image = ((System.Drawing.Image)(resources.GetObject("tsbNext.Image")));
			this.tsbNext.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbNext.Name = "tsbNext";
			this.tsbNext.Size = new System.Drawing.Size(51, 22);
			this.tsbNext.Text = "Next";
			this.tsbNext.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
			this.tsbNext.Click += new System.EventHandler(this.OnClickNext);
			// 
			// lblCount
			// 
			this.lblCount.Name = "lblCount";
			this.lblCount.Size = new System.Drawing.Size(24, 22);
			this.lblCount.Text = "0/0";
			// 
			// tsbPixelDataMD5
			// 
			this.tsbPixelDataMD5.Enabled = false;
			this.tsbPixelDataMD5.Name = "tsbPixelDataMD5";
			this.tsbPixelDataMD5.Size = new System.Drawing.Size(198, 22);
			this.tsbPixelDataMD5.Text = "Pixel Data MD5";
			this.tsbPixelDataMD5.Click += new System.EventHandler(this.OnClickPixelDataMD5);
			// 
			// DicomDumpForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(735, 416);
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "DicomDumpForm";
			this.Text = "DICOM Dump";
			this.Shown += new System.EventHandler(this.OnFormShown);
			this.Click += new System.EventHandler(this.OnClickSaveWithTransferSyntax);
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private Aga.Controls.Tree.TreeViewAdv treeDump;
		private Aga.Controls.Tree.TreeColumn ElementColumn;
		private Aga.Controls.Tree.TreeColumn VrColumn;
		private Aga.Controls.Tree.TreeColumn LengthColumn;
		private Aga.Controls.Tree.TreeColumn ValueColumn;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private Aga.Controls.Tree.NodeControls.NodeTextBox ncTag;
		private Aga.Controls.Tree.NodeControls.NodeTextBox nvVR;
		private Aga.Controls.Tree.NodeControls.NodeTextBox nvLength;
		private Aga.Controls.Tree.NodeControls.NodeTextBox nvValue;
		private Aga.Controls.Tree.NodeControls.NodeTextBox ncName;
		private Aga.Controls.Tree.NodeControls.NodeIcon nvIcon;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton tsbOpenFile;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton tsbViewImage;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton tsbPrev;
		private System.Windows.Forms.ToolStripButton tsbNext;
		private System.Windows.Forms.ToolStripLabel lblCount;
		private System.Windows.Forms.ToolStripDropDownButton tsbAdvanced;
        private System.Windows.Forms.ToolStripMenuItem tsbExtractPixels;
		private System.Windows.Forms.ToolStripButton tsbDictionary;
		private System.Windows.Forms.ToolStripButton tsbClose;
		private System.Windows.Forms.ToolStripMenuItem tsbSaveTS;
		private System.Windows.Forms.ToolStripMenuItem tsbPixelDataMD5;

	}
}