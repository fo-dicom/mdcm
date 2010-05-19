using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

using Dicom.Codec;
using Dicom.Data;
using Dicom.Forms;
using Dicom.Network;
using Dicom.Network.Client;

using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Win32.Targets;

namespace Dicom.Scu {
	public delegate void BoolDelegate(bool state);
	public delegate void MessageBoxDelegate(string message, string caption, bool isError);

	public partial class MainForm : Form {
		private ScuConfig Config = null;
		private string ConfigPath = null;

		private string[] TransferSyntaxDescriptions;
		private DicomTransferSyntax[] TransferSyntaxes;

		public MainForm() {
			InitializeComponent();

			ConfigPath = Path.Combine(Dicom.Debug.GetStartDirectory(), "dicomscu.xml");

			int i = 0;
			TransferSyntaxDescriptions = new string[13];
			TransferSyntaxDescriptions[i++] = "Automatic";
			TransferSyntaxDescriptions[i++] = "JPEG 2000 Lossy";
			TransferSyntaxDescriptions[i++] = "JPEG 2000 Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG-LS Near Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG-LS Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG Baseline P1 (8-bit) [.50]";
			TransferSyntaxDescriptions[i++] = "JPEG Extended P4 (12-bit) [.51]";
			TransferSyntaxDescriptions[i++] = "JPEG Lossless P14 [.57]";
			TransferSyntaxDescriptions[i++] = "JPEG Lossless P14 SV1 [.70]";
			TransferSyntaxDescriptions[i++] = "RLE Lossless";
			TransferSyntaxDescriptions[i++] = "Explicit VR Little Endian";
			TransferSyntaxDescriptions[i++] = "Implicit VR Little Endian";
			TransferSyntaxDescriptions[i++] = "Explicit VR Big Endian";

			i = 0;
			TransferSyntaxes = new DicomTransferSyntax[13];
			TransferSyntaxes[i++] = null;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEG2000Lossy;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEG2000Lossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGLSNearLossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGLSLossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess1;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess2_4;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess14;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess14SV1;
			TransferSyntaxes[i++] = DicomTransferSyntax.RLELossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.ExplicitVRLittleEndian;
			TransferSyntaxes[i++] = DicomTransferSyntax.ImplicitVRLittleEndian;
			TransferSyntaxes[i++] = DicomTransferSyntax.ExplicitVRBigEndian;

			foreach (string tx in TransferSyntaxDescriptions) {
				cbTransferSyntax.Items.Add(tx);
			}
			cbTransferSyntax.SelectedIndex = 0;
			
			LoadConfig();
		}

		public void ShowMessageBox(string message, string caption, bool isError) {
			MessageBox.Show(this, message, caption, MessageBoxButtons.OK, isError ? MessageBoxIcon.Error : MessageBoxIcon.Information);
		}

		public void OnLoad(object sender, EventArgs e) {
			InitializeLog();

			DicomCodec.RegisterCodecs();
			DicomCodec.RegisterExternalCodecs(".", "Dicom.Codec.*.dll");

			if (File.Exists("dicom.dic"))
				DcmDictionary.ImportDictionary("dicom.dic");
			else
				DcmDictionary.LoadInternalDictionary();

			if (File.Exists("private.dic"))
				DcmDictionary.ImportDictionary("private.dic");
		}

		public void InitializeLog() {
			LoggingConfiguration config = new LoggingConfiguration();

			DicomRichTextBoxTarget target = new DicomRichTextBoxTarget();
			target.UseDefaultRowColoringRules = true;
			target.Layout = "${message}";
			target.Control = rtbLog;

			config.AddTarget("DicomRichTextBox", target);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));

			LogManager.Configuration = config;
		}

		public void SaveConfig() {
			if (Config == null)
				Config = new ScuConfig();
			Config.LocalAE = tbLocalAE.Text;
			Config.RemoteAE = tbRemoteAE.Text;
			Config.RemoteHost = tbRemoteHost.Text;
			Config.RemotePort = (int)nuRemotePort.Value;
			Config.MaxPdu = (uint)nuMaxPdu.Value;
			Config.Timeout = (int)nuTimeout.Value;
			Config.TransferSyntax = cbTransferSyntax.SelectedIndex;
			Config.Quality = (int)nuQuality.Value;
			Config.UseTls = cbUseTls.Checked;
			XmlSerializer serializer = new XmlSerializer(Config.GetType());
			using (FileStream fs = new FileStream(ConfigPath, FileMode.Create)) {
				try {
					serializer.Serialize(fs, Config);
					fs.Flush();
				}
				catch {
				}
			}
		}

		public void LoadConfig() {
			if (!File.Exists(ConfigPath)) {
				Config = new ScuConfig();
			}
			else {
				XmlSerializer serializer = new XmlSerializer(typeof(ScuConfig));
				using (FileStream fs = new FileStream(ConfigPath, FileMode.Open)) {
					try {
						Config = (ScuConfig)serializer.Deserialize(fs);
					}
					catch {
						Config = new ScuConfig();
					}
				}
			}
			tbLocalAE.Text = Config.LocalAE;
			tbRemoteAE.Text = Config.RemoteAE;
			tbRemoteHost.Text = Config.RemoteHost;
			nuRemotePort.Value = Config.RemotePort;
			nuMaxPdu.Value = Config.MaxPdu;
			nuTimeout.Value = Config.Timeout;
			cbTransferSyntax.SelectedIndex = Config.TransferSyntax;
			nuQuality.Value = Config.Quality;
			cbUseTls.Checked = Config.UseTls;
		}

		private void OnClickTest(object sender, EventArgs e) {
			ToggleEchoButtons(false);

			SaveConfig();

			ThreadPool.QueueUserWorkItem(new WaitCallback(RunDicomEcho));
		}

		private void ToggleEchoButtons(bool state) {
			bttnTest.Enabled = state;
		}

		private void RunDicomEcho(object state) {
			bool success = false;
			string msg = "Unknown failure!";
			try {
				CEchoClient scu = new CEchoClient();
				scu.CallingAE = Config.LocalAE;
				scu.CalledAE = Config.RemoteAE;
				scu.MaxPduSize = Config.MaxPdu;
				scu.ConnectTimeout = 0;
				scu.SocketTimeout = 5;
				scu.DimseTimeout = 5;
				scu.OnCEchoResponse += delegate(byte presentationId, ushort messageId, DcmStatus status) {
					msg = status.ToString();
				};
				scu.Connect(Config.RemoteHost, Config.RemotePort, Config.UseTls ? DcmSocketType.TLS : DcmSocketType.TCP);
				success = scu.Wait();

				if (!success)
					msg = scu.ErrorMessage;
			}
			catch (Exception ex) {
				msg = ex.Message;
			}

			Invoke(new MessageBoxDelegate(ShowMessageBox), msg, "DICOM C-Echo Result", !success);
			Invoke(new BoolDelegate(ToggleEchoButtons), true);
		}

		private void OnSendAddImage(object sender, EventArgs e) {
			OpenFileDialog fd = new OpenFileDialog();
			fd.Multiselect = true;
			if (fd.ShowDialog(this) == DialogResult.OK) {
				foreach (string filename in fd.FileNames) {
					try {
						CStoreRequestInfo info = new CStoreRequestInfo(filename);

						ListViewItem item = new ListViewItem(filename, 0);
						item.SubItems.Add(info.SOPClassUID.Description);
						item.SubItems.Add(info.TransferSyntax.UID.Description);
						item.SubItems.Add(info.Status.Description);

						item.Tag = info;
						info.UserState = item;

						lvSendImages.Items.Add(item);
					}
					catch { }
				}
			}
		}

		private void OnSendRemoveImage(object sender, EventArgs e) {
			lvSendImages.BeginUpdate();
			foreach (ListViewItem lvi in lvSendImages.SelectedItems) {
				lvSendImages.Items.Remove(lvi);
			}
			lvSendImages.EndUpdate();
		}
		
		private void OnSendClear(object sender, EventArgs e) {
			lvSendImages.Items.Clear();
		}

		private void ToggleSendButtons(bool state) {
			bttnSend.Enabled = state;
			bttnSendAddImage.Enabled = state;
			bttnSendRemoveImage.Enabled = state;
			bttnSendClear.Enabled = state;
		}

		private void UpdateSendInfo(CStoreClient client, CStoreRequestInfo info) {
			ListViewItem lvi = (ListViewItem)info.UserState;
			lvi.ImageIndex = (info.Status == DcmStatus.Success) ? 1 : 2;
			lvi.SubItems[3].Text = info.Status.Description;
			lvSendImages.EnsureVisible(lvi.Index);
		}

		private void OnSend(object sender, EventArgs e) {
			ToggleSendButtons(false);

			SaveConfig();

			CStoreClient scu = new CStoreClient();
			scu.DisableFileStreaming = true;
			scu.CallingAE = Config.LocalAE;
			scu.CalledAE = Config.RemoteAE;
			scu.MaxPduSize = Config.MaxPdu;
			scu.ConnectTimeout = 0;
			scu.SocketTimeout = Config.Timeout;
			scu.DimseTimeout = Config.Timeout;
			scu.SerializedPresentationContexts = true;
			scu.PreferredTransferSyntax = TransferSyntaxes[Config.TransferSyntax];

			//if (scu.PreferredTransferSyntax == DicomTransferSyntax.JPEGProcess1 ||
			//    scu.PreferredTransferSyntax == DicomTransferSyntax.JPEGProcess2_4) {
			//    DcmJpegParameters param = new DcmJpegParameters();
			//    param.Quality = Config.Quality;
			//    scu.PreferredTransferSyntaxParams = param;
			//}
			//else if (scu.PreferredTransferSyntax == DicomTransferSyntax.JPEG2000Lossy) {
			//    DcmJpeg2000Parameters param = new DcmJpeg2000Parameters();
			//    param.Rate = Config.Quality;
			//    scu.PreferredTransferSyntaxParams = param;
			//}

			scu.OnCStoreResponseReceived = delegate(CStoreClient client, CStoreRequestInfo info) {
				Invoke(new CStoreRequestCallback(UpdateSendInfo), client, info);
			};

			foreach (ListViewItem lvi in lvSendImages.Items) {
				lvi.ImageIndex = 0;
				lvi.SubItems[3].Text = "Pending";

				CStoreRequestInfo info = (CStoreRequestInfo)lvi.Tag;
				scu.AddFile(info);
			}

			ThreadPool.QueueUserWorkItem(new WaitCallback(RunDicomSend), scu);
		}

		private void RunDicomSend(object state) {
			CStoreClient scu = (CStoreClient)state;
			scu.Connect(Config.RemoteHost, Config.RemotePort, Config.UseTls ? DcmSocketType.TLS : DcmSocketType.TCP);
			if (!scu.Wait())
				Invoke(new MessageBoxDelegate(ShowMessageBox), scu.ErrorMessage, "DICOM C-Store Error", true);

			Invoke(new BoolDelegate(ToggleSendButtons), true);
		}
	}

	[Serializable]
	public class ScuConfig {
		public string LocalAE = "TEST_SCU";
		public string RemoteAE = "ANY-SCP";
		public string RemoteHost = "localhost";
		public int RemotePort = 104;
		public uint MaxPdu = 16384;
		public int Timeout = 30;
		public int TransferSyntax = 0;
		public int Quality = 90;
		public bool UseTls = false;
	}
}
