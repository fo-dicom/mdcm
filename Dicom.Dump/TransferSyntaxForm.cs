using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Dicom;
using Dicom.Data;

namespace Dicom.Forms {
	public partial class TransferSyntaxForm : Form {
		private string[] TransferSyntaxDescriptions;
		private DicomTransferSyntax[] TransferSyntaxes;

		private DicomTransferSyntax _selectedSyntax = null;
		private int _selectedQualityRate = 0;

		public TransferSyntaxForm() {
			InitializeComponent();

			int i = 0;
			TransferSyntaxDescriptions = new string[11];
			TransferSyntaxDescriptions[i++] = "Automatic";
			TransferSyntaxDescriptions[i++] = "JPEG 2000 Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG-LS Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG-LS Near Lossless";
			TransferSyntaxDescriptions[i++] = "JPEG Lossless P14 SV1";
			TransferSyntaxDescriptions[i++] = "JPEG Lossless P14";
			TransferSyntaxDescriptions[i++] = "RLE Lossless";
			TransferSyntaxDescriptions[i++] = "Deflated Explicit VR Little Endian";
			TransferSyntaxDescriptions[i++] = "Explicit VR Little Endian";
			TransferSyntaxDescriptions[i++] = "Implicit VR Little Endian";
			TransferSyntaxDescriptions[i++] = "Explicit VR Big Endian";

			i = 0;
			TransferSyntaxes = new DicomTransferSyntax[11];
			TransferSyntaxes[i++] = null;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEG2000Lossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGLSLossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGLSNearLossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess14SV1;
			TransferSyntaxes[i++] = DicomTransferSyntax.JPEGProcess14;
			TransferSyntaxes[i++] = DicomTransferSyntax.RLELossless;
			TransferSyntaxes[i++] = DicomTransferSyntax.DeflatedExplicitVRLittleEndian;
			TransferSyntaxes[i++] = DicomTransferSyntax.ExplicitVRLittleEndian;
			TransferSyntaxes[i++] = DicomTransferSyntax.ImplicitVRLittleEndian;
			TransferSyntaxes[i++] = DicomTransferSyntax.ExplicitVRBigEndian;

			foreach (string tx in TransferSyntaxDescriptions) {
				cbTransferSyntax.Items.Add(tx);
			}
			cbTransferSyntax.SelectedIndex = 0;
		}

		public DicomTransferSyntax SelectedTransferSyntax {
			get { return _selectedSyntax; }
		}

		private void OnSelectTransferSyntax(object sender, EventArgs e) {
			_selectedSyntax = TransferSyntaxes[cbTransferSyntax.SelectedIndex];
		}

		private void OnChangeQualityRate(object sender, EventArgs e) {
			_selectedQualityRate = (int)nuQualityRate.Value;
		}
	}
}
