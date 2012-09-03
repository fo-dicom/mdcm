using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Dicom.Data;
using Dicom.Utility;

namespace Dicom.Forms {
	public partial class DicomDictionaryForm : Form {
		public DicomDictionaryForm() {
			InitializeComponent();
			PerformQuery();
		}

		public DicomDictionaryForm(String query) {
			InitializeComponent();
			tbSearch.Text = query;
			PerformQuery();
		}

		private void DisplayEntry(DcmDictionaryEntry entry) {
			if (entry == null) {
				lblElementTag.Text = "";
				lblElementTagMask.Text = "";
				lblElementName.Text = "";
				lbValueRepresentations.Items.Clear();
				lblValueMultiplicity.Text = "";
				lblPrivateCreator.Text = "";
			} else {
				lblElementTag.Text = entry.DisplayTag.ToUpper();
				lblElementTagMask.Text = String.Format("{0:X8}", entry.Mask);
				if (entry.Retired)
					lblElementName.Text = entry.Name + " (Retired)";
				else
					lblElementName.Text = entry.Name;
				lbValueRepresentations.Items.Clear();
				foreach (DicomVR vr in entry.AllowedVRs) {
					lbValueRepresentations.Items.Add(vr);
				}
				lblValueMultiplicity.Text = entry.VM;
				lblPrivateCreator.Text = entry.PrivateCreator;
			}
		}

		private void PerformQuery() {
			lbElements.SuspendLayout();
			lbElements.Items.Clear();

			string NameQuery = tbSearch.Text + "*";
			string TagQuery = tbSearch.Text.Replace(",", "").ToLower().PadRight(8, 'x');

			uint TagCard = 0, TagMask = 0;
			bool CheckTag = uint.TryParse(TagQuery.Replace('x', '0'), System.Globalization.NumberStyles.HexNumber, null, out TagCard);
			if (CheckTag) {
				StringBuilder msb = new StringBuilder();
				msb.Append(TagQuery.ToLower());
				msb .Replace('0', 'F').Replace('1', 'F').Replace('2', 'F')
					.Replace('3', 'F').Replace('4', 'F').Replace('5', 'F')
					.Replace('6', 'F').Replace('7', 'F').Replace('8', 'F')
					.Replace('9', 'F').Replace('a', 'F').Replace('b', 'F')
					.Replace('c', 'F').Replace('d', 'F').Replace('e', 'F')
					.Replace('f', 'F').Replace('x', '0');
				CheckTag = uint.TryParse(msb.ToString(), System.Globalization.NumberStyles.HexNumber, null, out TagMask);
			}

			foreach (DcmDictionaryEntry entry in DcmDictionary.Entries) {
				if (CheckTag) {
					if ((entry.Tag & TagMask) == (TagCard & entry.Mask))
						lbElements.Items.Add(entry);
				} else if (Wildcard.Match(NameQuery, entry.Name, false)) {
					lbElements.Items.Add(entry);
				}
			}
			if (lbElements.Items.Count > 0)
				lbElements.SelectedIndex = 0;
			lbElements.ResumeLayout();
		}

		private void lbElements_SelectedIndexChanged(object sender, EventArgs e) {
			DcmDictionaryEntry entry = (DcmDictionaryEntry)lbElements.SelectedItem;
			if (entry != null)
				DisplayEntry(entry);
		}

		private void tbSearch_TextChanged(object sender, EventArgs e) {
			PerformQuery();
		}
	}
}