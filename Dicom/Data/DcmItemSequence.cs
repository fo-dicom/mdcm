// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2009  Colby Dillion
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
using System.Text;

using Dicom.IO;

namespace Dicom.Data {
	public class DcmItemSequenceItem : DcmItem {
		#region Private Members
		private DcmDataset _dataset;
		#endregion

		#region Public Constructors
		public DcmItemSequenceItem()
			: base(DicomTags.Item, DicomVR.NONE) {
		}

		public DcmItemSequenceItem(long pos, uint length)
			: base(DicomTags.Item, DicomVR.NONE, pos, Endian.LocalMachine) {
		}
		#endregion

		#region Public Properties
		public DcmDataset Dataset {
			get {
				if (_dataset == null)
					_dataset = new DcmDataset(DicomTransferSyntax.ExplicitVRLittleEndian);
				return _dataset;
			}
			set {
				_dataset = value;
				Endian = _dataset.InternalTransferSyntax.Endian;
			}
		}

		public uint StreamLength {
			get { return _dataset.StreamLength; }
		}
		#endregion

		#region DcmItem Overrides
		internal override uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 4 + 4;
			length += Dataset.CalculateWriteLength(syntax, options & ~DicomWriteOptions.CalculateGroupLengths);
			if (!Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequenceItem))
				length += 4 + 4; // Sequence Item Delimitation Item
			return length;
		}

		protected override void ChangeEndianInternal() {
			Dataset.SelectByteOrder(Endian);
		}

		internal override void Preload() {
			if (_dataset != null)
				_dataset.PreloadDeferredBuffers();
		}

		internal override void Unload() {
			if (_dataset != null)
				_dataset.UnloadDeferredBuffers();
		}

		public override DcmItem Clone() {
			DcmItemSequenceItem si = new DcmItemSequenceItem(StreamPosition, StreamLength);
			si.Dataset = Dataset.Clone();
			return si;
		}

		public override void Dump(StringBuilder sb, string prefix, DicomDumpOptions options) {
			sb.AppendLine().Append(prefix).Append(" Item:").AppendLine();
			Dataset.Dump(sb, prefix + "  > ", options);
			sb.Length = sb.Length - 1;
		}
		#endregion
	}

	public class DcmItemSequence : DcmItem {
		private List<DcmItemSequenceItem> _items = new List<DcmItemSequenceItem>();
		private uint _streamLength = 0xffffffff;

		public DcmItemSequence(DicomTag tag) 
			: base(tag, DicomVR.SQ) {
		}

		public DcmItemSequence(DicomTag tag, long pos, uint length, Endian endian)
			: base(tag, DicomVR.SQ, pos, endian) {
			_streamLength = length;
		}

		public uint StreamLength {
			get { return _streamLength; }
		}

		public IList<DcmItemSequenceItem> SequenceItems {
			get { return _items; }
		}

		public void AddSequenceItem(DcmDataset itemDataset) {
			DcmItemSequenceItem item = new DcmItemSequenceItem();
			item.Dataset = itemDataset;
			AddSequenceItem(item);
		}

		public void AddSequenceItem(DcmItemSequenceItem item) {
			_items.Add(item);
		}

		internal override uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 0;
			length += 4; // element tag
			if (syntax.IsExplicitVR) {
				length += 2; // vr
				length += 6; // length
			} else {
				length += 4; // length
			}
			foreach (DcmItemSequenceItem item in SequenceItems) {
				length += item.CalculateWriteLength(syntax, options);
			}
			if (!Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequence))
				length += 4 + 4; // Sequence Delimitation Item
			return length;
		}

		protected override void ChangeEndianInternal() {
			foreach (DcmItemSequenceItem item in SequenceItems) {
				item.Endian = Endian;
			}
		}

		internal override void Preload() {
			foreach (DcmItemSequenceItem item in SequenceItems) {
				item.Preload();
			}
		}

		internal override void Unload() {
			foreach (DcmItemSequenceItem item in SequenceItems) {
				item.Unload();
			}
		}

		public override DcmItem Clone() {
			DcmItemSequence sq = new DcmItemSequence(Tag, StreamPosition, StreamLength, Endian);
			foreach (DcmItemSequenceItem si in SequenceItems) {
				sq.AddSequenceItem((DcmItemSequenceItem)si.Clone());
			}
			return sq;
		}

		public override void Dump(StringBuilder sb, string prefix, DicomDumpOptions options) {
			sb.Append(prefix);
			sb.AppendFormat("{0} {1} {2}", Tag.ToString(), VR.VR, Tag.Entry.Name);
			foreach (DcmItemSequenceItem item in SequenceItems) {
				item.Dump(sb, prefix, options);
			}
		}
	}
}
