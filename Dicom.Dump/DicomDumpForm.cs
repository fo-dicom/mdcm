using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Dicom;
using Dicom.Data;
using Dicom.Forms;

using Aga.Controls.Tree;
using DicomDump;

namespace Dicom.Forms {
	public partial class DicomDumpForm : Form {
		private delegate void SelectFileDelegate(int selection);

		private List<string> _files;
		private int _selected;

		public DicomDumpForm() {
			InitializeComponent();
			_files = new List<string>();
			_selected = -1;
		}

		public void AddFile(string file) {
			_files.Add(file);
			if (IsHandleCreated) {
				if (_selected == -1 || _files.Count > 0)
					Invoke(new SelectFileDelegate(SelectFile), 0);
				else {
					tsbPrev.Enabled = (_selected > 0);
					tsbNext.Enabled = (_selected < (_files.Count - 1));
					lblCount.Text = String.Format("{0}/{1}", _selected + 1, _files.Count);
				}
			}
		}

		private void SelectFile(int selection) {
			if (selection < 0 || selection >= _files.Count) {
				Text = "DICOM Dump";
				ClearDump();
				return;
			}

			string tag = null;
			if (treeDump.Model != null && treeDump.SelectedNode != null) {
				TreeNodeAdv node = treeDump.SelectedNode;
				while (node.Parent != null && node.Parent != treeDump.Root)
					node = node.Parent;
				if (node != null && node != treeDump.Root) {
					object dt = treeDump.SelectedNode.Tag;
					if (dt is DicomNode) {
						tag = ((DicomNode)dt).ElementTag;
					}
				}
			}

			_selected = selection;
			LoadFile(_files[selection]);
			tsbPrev.Enabled = (selection > 0);
			tsbNext.Enabled = (selection < (_files.Count - 1));
			lblCount.Text = String.Format("{0}/{1}", selection + 1, _files.Count);
			Text = "DICOM Dump [" + _files[selection] + "]";

			if (tag != null && treeDump.Model != null) {
				foreach (TreeNodeAdv node in treeDump.Root.Children) {
					if (node.Tag != null && node.Tag is DicomNode) {
						if (tag == ((DicomNode)node.Tag).ElementTag) {
							treeDump.SelectedNode = node;
							treeDump.EnsureVisible(node);
							return;
						}
					}
				}
			}
		}

		private bool LoadFile(string file) {
			bool success = false;
			DicomFileFormat ff = new DicomFileFormat();
			try {
				ClearDump();

				ff.Load(file, DicomReadOptions.Default | DicomReadOptions.KeepGroupLengths);

				success = true;
			}
			catch (Exception e) {
				MessageBox.Show(e.Message + "\n\n" + file, "Error parsing file!", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			treeDump.Model = LoadFileFormat(ff);
			treeDump.ExpandAll();

			if (success) {
				if (ff.Dataset.Contains(DicomTags.PixelData)) {
					tsbViewImage.Enabled = true;
					tsbExtractPixels.Enabled = true;
					tsbPixelDataMD5.Enabled = true;
				}
				tsbSaveTS.Enabled = true;
			} else {
				tsbViewImage.Enabled = false;
				tsbExtractPixels.Enabled = false;
				tsbSaveTS.Enabled = false;
				tsbPixelDataMD5.Enabled = false;
			}

			return success;
		}

		private void ClearDump() {
			treeDump.Model = null;
			tsbViewImage.Enabled = false;
			tsbExtractPixels.Enabled = false;
			tsbPixelDataMD5.Enabled = false;
			tsbSaveTS.Enabled = false;

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		private class DicomNode : Node {
			private DcmItem _elem;
			private Image _image;

			public DicomNode(Image image, DcmItem elem)
				: base() {
				_image = image;
				_elem = elem;
			}

			public Image Icon {
				get { return _image; }
				set { _image = value; }
			}

			public String ElementTag {
				get { return _elem.Tag.ToString().ToUpper(); }
			}

			public String Name {
				get { return _elem.Tag.Entry.Name; }
			}

			public String VR {
				get { return _elem.VR.VR; }
			}

			public String Length {
				get {
					uint len = 0xffffffff;
					if (_elem is DcmItemSequence) {
						DcmItemSequence sq = _elem as DcmItemSequence;
						len = sq.StreamLength;
					}
					else if (_elem is DcmFragmentSequence) {
						//DcmFragmentSequence fs = _elem as DcmFragmentSequence;
						//len = (uint)fs.Fragments.Count;
					}
					else {
						DcmElement el = _elem as DcmElement;
						len = (uint)el.Length;
					}
					if (len == 0xffffffff)
						return "UNDEFINED  ";
					return len.ToString() + "  ";
				}
			}

			public String Value {
				get {
					if (_elem is DcmItemSequence) {
						DcmItemSequence sq = _elem as DcmItemSequence;
						return "";
					}
					else if (_elem is DcmFragmentSequence) {
						DcmFragmentSequence fs = _elem as DcmFragmentSequence;
						return String.Format("  (binary data)  [{0} fragments]", fs.Fragments.Count);
					}
					else if (_elem is DcmUniqueIdentifier) {
						DcmUniqueIdentifier ui = _elem as DcmUniqueIdentifier;
						if (ui.Length > 0) {
							DicomUID uid = ui.GetUID();
							string val = uid.UID;
							if (uid.Type != DicomUidType.Unknown) {
								val += String.Format(" ({0})", uid.Description);
							}
							return "  " + val;
						}
						return String.Empty;
					}
					else {
						DcmElement el = _elem as DcmElement;
						if (el.VR == DicomVR.OB || el.VR == DicomVR.OW)
							return "  (binary data)";
						String val = el.GetValueString();
						if (val.Length >= 200) {
							val = val.Substring(0, 197);
							val += "...";
						}
						return "  " + val;
					}
				}
			}
		}

		private class DicomTagNode : Node {
			private DicomTag _tag;
			private uint _len;
			private Image _image;

			public DicomTagNode(Image image, DicomTag tag, uint len)
				: base() {
				_image = image;
				_tag = tag;
				_len = len;
			}

			public Image Icon {
				get { return _image; }
				set { _image = value; }
			}

			public String ElementTag {
				get { return _tag.ToString().ToUpper(); }
			}

			public String Name {
				get { return _tag.Entry.Name; }
			}

			public String VR {
				get { return "(none)  "; }
			}

			public String Length {
				get {
					if (_len == 0xffffffff)
						return "UNDEFINED  ";
					return _len.ToString() + "  ";
				}
			}

			public String Value {
				get { return String.Empty; }
			}
		}

		private Dictionary<String, Image> _cached_icons = new Dictionary<string, Image>();
		private Font _cached_font;

		private Image LoadTreeViewAdvResourceImage(String name, String type, Color color) {
			String key = String.Format("{0};{1};{2}", name, type, color.ToString());
			if (_cached_icons.ContainsKey(key))
				return _cached_icons[key];

			Bitmap bm = null;
			if (name == "FolderClosed")
				bm = new Bitmap(DicomDumpResources.FolderClosed);
			else if (name == "Folder")
				bm = new Bitmap(DicomDumpResources.Folder);
			else
				bm = new Bitmap(DicomDumpResources.Leaf);

			bm.MakeTransparent();
			Graphics g = Graphics.FromImage(bm);
			Brush b = new SolidBrush(color);
			if (_cached_font == null) {
				_cached_font = new Font(FontFamily.GenericSansSerif, 7, FontStyle.Regular);
			}
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Center;
			sf.Trimming = StringTrimming.None;
			g.DrawString(type, _cached_font, b, new RectangleF(-2, 0, bm.Width + 5, bm.Height), sf);
			_cached_icons.Add(key, bm);
			return bm;
		}

		private TreeModel LoadFileFormat(DicomFileFormat ff) {
			TreeModel model = new TreeModel();
			LoadDataset(ff.FileMetaInfo, model.Nodes);
			LoadDataset(ff.Dataset, model.Nodes);
			return model;
		}

		private TreeModel LoadDataset(DcmDataset ds) {
			TreeModel model = new TreeModel();
			LoadDataset(ds, model.Nodes);
			return model;
		}

		private void LoadDataset(DcmDataset ds, Collection<Node> parent) {
			if (ds == null)
				return;

			foreach (DcmItem di in ds.Elements) {
				Image icon = LoadTreeViewAdvResourceImage("Leaf", di.VR.VR, Color.Blue);

				DicomNode dn = new DicomNode(icon, di);
				parent.Add(dn);

				if (di is DcmItemSequence) {
					dn.Icon = LoadTreeViewAdvResourceImage("FolderClosed", "SQ", Color.Blue);

					DcmItemSequence sq = di as DcmItemSequence;
					foreach (DcmItemSequenceItem item in sq.SequenceItems) {
						icon = LoadTreeViewAdvResourceImage("Folder", "", Color.Black);
						DicomTagNode din = new DicomTagNode(icon, DicomTags.Item, item.StreamLength);
						dn.Nodes.Add(din);
						LoadDataset(item.Dataset, din.Nodes);
						if (item.StreamLength == 0xffffffff) {
							icon = LoadTreeViewAdvResourceImage("FolderClosed", "", Color.Black);
							din.Nodes.Add(new DicomTagNode(icon, DicomTags.ItemDelimitationItem, 0));
						}
					}
					if (sq.StreamLength == 0xffffffff) {
						icon = LoadTreeViewAdvResourceImage("FolderClosed", "", Color.Black);
						dn.Nodes.Add(new DicomTagNode(icon, DicomTags.SequenceDelimitationItem, 0));
					}
				}
			}
		}

		private void treeDump_SizeChanged(object sender, EventArgs e) {
			int width = treeDump.Size.Width - 30 - ElementColumn.Width - VrColumn.Width - LengthColumn.Width;
			if (width > 0)
				ValueColumn.Width = width;
		}

		private void treeDump_NodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e) {
			if (e.Node.Tag is DicomNode) {
				DicomNode dn = e.Node.Tag as DicomNode;

				if (dn.VR != "SQ" || e.Button == MouseButtons.Right) {
					DicomDictionaryForm ddf = new DicomDictionaryForm(dn.ElementTag.Substring(1, 9));
					ddf.ShowDialog(this);
				}
			}
		}

		private void tsbOpenFile_Click(object sender, EventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Multiselect = true;
			if (ofd.ShowDialog() == DialogResult.OK) {
				bool first = true;
				foreach (string file in ofd.FileNames) {
					AddFile(file);
					if (first && _files.Count > 1)
						SelectFile(_files.Count - 1);
					first = false;
				}
			}
		}

		private void tsbDictionary_Click(object sender, EventArgs e) {
			DicomDictionaryForm ddf = new DicomDictionaryForm();
			ddf.ShowDialog(this);
		}

		private void OnViewImage(object sender, EventArgs e) {
			if (_selected == -1)
				return;

			try {
				DicomQuickDisplayForm qdf = new DicomQuickDisplayForm(_files[_selected]);
				qdf.ShowDialog(this);
			} catch {
			}
		}

		private void OnClickPrev(object sender, EventArgs e) {
			if (_selected != -1 && _selected > 0)
				SelectFile(_selected - 1);
		}

		private void OnClickNext(object sender, EventArgs e) {
			if (_selected != -1 && _selected < (_files.Count - 1))
				SelectFile(_selected + 1);
		}

		private void OnFormShown(object sender, EventArgs e) {
			if (_files.Count > 0)
				SelectFile(0);
		}

		private void OnClickExtractPixels(object sender, EventArgs e) {
			if (_selected == -1)
				return;

			try {
				DicomFileFormat ff = new DicomFileFormat();
				ff.Load(_files[_selected], DicomReadOptions.Default |
						DicomReadOptions.KeepGroupLengths |
						DicomReadOptions.DeferLoadingLargeElements |
						DicomReadOptions.DeferLoadingPixelData);

				DcmPixelData pixels = new DcmPixelData(ff.Dataset);

				if (pixels.NumberOfFrames == 0)
					return;
				else if (pixels.NumberOfFrames >= 1) {
					SaveFileDialog sfd = new SaveFileDialog();
					sfd.RestoreDirectory = true;
					if (sfd.ShowDialog(this) == DialogResult.OK) {
						byte[] data = pixels.GetFrameDataU8(0);
						File.WriteAllBytes(sfd.FileName, data);
					}
				}

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
			catch {
			}
		}

		private void OnClickPixelDataMD5(object sender, EventArgs e) {
			if (_selected == -1)
				return;

			try {
				DicomFileFormat ff = new DicomFileFormat();
				ff.Load(_files[_selected], DicomReadOptions.Default |
						DicomReadOptions.KeepGroupLengths |
						DicomReadOptions.DeferLoadingLargeElements |
						DicomReadOptions.DeferLoadingPixelData);

				DcmPixelData pixels = new DcmPixelData(ff.Dataset);

				if (pixels.NumberOfFrames == 0) {
					MessageBox.Show(this, "No pixel data", "Pixel Data MD5");
				} else if (pixels.NumberOfFrames >= 1) {
					MessageBox.Show(this, pixels.ComputeMD5(), String.Format("Pixel Data MD5 [{0} frames]", pixels.NumberOfFrames));
				}

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			} catch {
			}
		}

		private void OnClickClose(object sender, EventArgs e) {
			if (_files.Count == 0) {
				Close();
				return;
			}

			try {
				_files.RemoveAt(_selected);

				if (_files.Count == 0) {
					_selected = -1;
					tsbNext.Enabled = false;
					tsbPrev.Enabled = false;
					lblCount.Text = "0/0";
					treeDump.Model = new TreeModel();
				} else {
					if (_selected >= _files.Count)
						_selected = _files.Count - 1;
					SelectFile(_selected);
				}
			}
			catch {
				Close();
			}
		}

		private void OnClickSaveWithTransferSyntax(object sender, EventArgs e) {
			if (_selected == -1)
				return;

			TransferSyntaxForm tsForm = new TransferSyntaxForm();
			if (tsForm.ShowDialog(this) == DialogResult.OK) {
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.RestoreDirectory = true;
				if (sfd.ShowDialog(this) == DialogResult.OK) {
					DicomFileFormat ff = new DicomFileFormat();
					ff.Load(_files[_selected], DicomReadOptions.Default);
					if (tsForm.SelectedTransferSyntax != null)
						ff.ChangeTransferSyntax(tsForm.SelectedTransferSyntax, null);
					ff.Save(sfd.FileName, DicomWriteOptions.Default);
				}
			}
		}
	}
}