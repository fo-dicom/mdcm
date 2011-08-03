// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Dicom.IO;
using Dicom.Utility;

namespace Dicom.Data {
	public static class XDicom {
		public static XDocument ToXML(DcmDataset dataset, XDicomOptions options) {
			XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
			XElement root = new XElement("dicom");
			LoadSequence(root, dataset.Elements, options);
			document.Add(root);
			return document;
		}

		private static void LoadSequence(XElement parent, IList<DcmItem> items, XDicomOptions options) {
			foreach (DcmItem item in items) {
				if (!Flags.IsSet(options, XDicomOptions.IncludePixelData) && item.Tag == DicomTags.PixelData)
					continue;

				XElement attr = new XElement("attr");

				attr.SetAttributeValue("tag", item.Tag.Card.ToString("x8"));
				attr.SetAttributeValue("vr", item.VR.VR);

				if (item is DcmItemSequence) {
					DcmItemSequence seq = (DcmItemSequence)item;
					attr.SetAttributeValue("len", -1);

					foreach (DcmItemSequenceItem si in seq.SequenceItems) {
						XElement itm = new XElement("item");
						LoadSequence(itm, si.Dataset.Elements, options);
						attr.Add(itm);
					}
				}
				else if (item is DcmFragmentSequence) {
					DcmFragmentSequence seq = (DcmFragmentSequence)item;
					attr.SetAttributeValue("len", -1);

					LoadFragmentOffsetTable(attr, seq);
					foreach (ByteBuffer fi in seq.Fragments) {
						LoadFragmentItem(attr, seq.VR, fi);
					}
				}
				else {
					DcmElement element = (DcmElement)item;
					attr.SetAttributeValue("len", element.Length);
					attr.Add(element.GetValueString());
				}

				if (Flags.IsSet(options, XDicomOptions.Comments))
					parent.Add(new XComment(item.Tag.Entry.Name));
				parent.Add(attr);
			}
		}

		private static void LoadFragmentOffsetTable(XElement parent, DcmFragmentSequence seq) {
			if (seq.HasOffsetTable) {
				XElement item = new XElement("item");
				item.SetAttributeValue("len", seq.OffsetTable.Count * 4);
				StringBuilder sb = new StringBuilder();
				foreach (uint offset in seq.OffsetTable) {
					sb.AppendFormat("{0:X8}\\", offset);
				}
				item.Add(sb.ToString().TrimEnd('\\'));
				parent.Add(item);
			}
			else {
				XElement item = new XElement("item");
				item.SetAttributeValue("len", 0);
				parent.Add(item);
			}
		}

		private static void LoadFragmentItem(XElement parent, DicomVR vr, ByteBuffer fragment) {
			XElement item = new XElement("item");
			item.SetAttributeValue("len", fragment.Length);
			parent.Add(item);

			if (vr == DicomVR.OW) {
				ushort[] data = fragment.ToUInt16s();
				StringBuilder sb = new StringBuilder();
				foreach (ushort d in data) {
					sb.AppendFormat("{0:X4}\\", d);
				}
				item.Add(sb.ToString().TrimEnd('\\'));
			}
			else {
				byte[] data = fragment.ToBytes();
				StringBuilder sb = new StringBuilder();
				foreach (byte d in data) {
					sb.AppendFormat("{0:X2}\\", d);
				}
				item.Add(sb.ToString().TrimEnd('\\'));
			}
		}

		public static DcmDataset ToDICOM(XDocument document) {
			DcmDataset dataset = new DcmDataset(DicomTransferSyntax.ExplicitVRLittleEndian);
			Save(document.Root, dataset);
			return dataset;
		}

		private static void Save(XElement parent, DcmDataset dataset) {
			foreach (XElement attr in parent.Elements("attr")) {
				DicomTag tag = DicomTag.Parse(attr.Attribute("tag").Value);
				DicomVR vr = DicomVR.Lookup(attr.Attribute("vr").Value);
				int len = int.Parse(attr.Attribute("len").Value, CultureInfo.InvariantCulture);

				if (vr == DicomVR.SQ) {
					DcmItemSequence seq = new DcmItemSequence(tag);
					foreach (XElement itm in attr.Elements("item")) {
						DcmItemSequenceItem item = new DcmItemSequenceItem();
						Save(itm, item.Dataset);
						seq.AddSequenceItem(item);
					}
					dataset.AddItem(seq);
				}
				else if (len == -1) {
					DcmFragmentSequence seq = new DcmFragmentSequence(tag, vr);
					bool first = true;
					foreach (XElement itm in attr.Elements("item")) {
						if (first) {
							SaveFragmentOffsetTable(itm, seq);
							first = false;
						}
						else {
							SaveFragmentItem(itm, seq);
						}
					}
					dataset.AddItem(seq);
				}
				else {
					DcmElement element = DcmElement.Create(tag, vr);
					element.SetValueString(attr.FirstText());
					dataset.AddItem(element);
				}
			}
		}

		private static void SaveFragmentOffsetTable(XElement item, DcmFragmentSequence seq) {
			if (item.Value == String.Empty)
				return;

			string[] strs = item.FirstText().Split('\\');
			foreach (string s in strs) {
				seq.OffsetTable.Add(uint.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
			}
		}

		private static void SaveFragmentItem(XElement item, DcmFragmentSequence seq) {
			ByteBuffer bb = new ByteBuffer();
			string[] strs = item.FirstText().Split('\\');
			if (seq.VR == DicomVR.OW) {
				foreach (string s in strs) {
					bb.Writer.Write(ushort.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
				}
			}
			else {
				foreach (string s in strs) {
					bb.Writer.Write(byte.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
				}
			}
			seq.AddFragment(bb);
		}
	}
}
