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
	public class DcmFragmentSequence : DcmItem {
		#region Private Members
		private List<uint> _table;
		private List<ByteBuffer> _fragments = new List<ByteBuffer>();
		#endregion

		#region Public Constructors
		public DcmFragmentSequence(DicomTag tag, DicomVR vr) : base(tag, vr) {
		}

		public DcmFragmentSequence(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}
		#endregion

		#region Public Properties
		public bool HasOffsetTable {
			get { return _table != null; }
		}

		public ByteBuffer OffsetTableBuffer {
			get {
				ByteBuffer offsets = new ByteBuffer();
				if (_table != null) {
					foreach (uint offset in _table) {
						offsets.Writer.Write(offset);
					}
				}
				return offsets;
			}
		}

		public List<uint> OffsetTable {
			get {
				if (_table == null)
					_table = new List<uint>();
				return _table;
			}
		}

		public IList<ByteBuffer> Fragments {
			get { return _fragments; }
		}
		#endregion

		#region Public Methods
		public void SetOffsetTable(ByteBuffer table) {
			_table = new List<uint>();
			_table.AddRange(table.ToUInt32s());
		}
		public void SetOffsetTable(List<uint> table) {
			_table = new List<uint>(table);
		}

		public void AddFragment(ByteBuffer fragment) {
			_fragments.Add(fragment);
		}
		#endregion

		#region DcmItem Methods
		internal override uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 0;
			length += 4; // element tag
			if (syntax.IsExplicitVR) {
				length += 2; // vr
				length += 6; // length
			} else {
				length += 4; // length
			}
			length += 4 + 4; // offset tag
			if (Flags.IsSet(options, DicomWriteOptions.WriteFragmentOffsetTable) && _table != null)
				length += (uint)(_table.Count * 4);
			foreach (ByteBuffer bb in _fragments) {
				length += 4; // item tag
				length += 4; // fragment length
				length += (uint)bb.Length;
			}
			return length;
		}

		protected override void ChangeEndianInternal() {
			foreach (ByteBuffer bb in Fragments) {
				if (bb.Endian != Endian) {
					bb.Endian = Endian;
					bb.Swap(VR.UnitSize);
				}
			}
		}

		internal override void Preload() {
			foreach (ByteBuffer bb in Fragments) {
				bb.Preload();
			}
		}
	
		internal override void Unload() {
			foreach (ByteBuffer bb in Fragments) {
				bb.Unload();
			}
		}

		public override DcmItem Clone() {
			DcmFragmentSequence sq = new DcmFragmentSequence(Tag, VR, StreamPosition, Endian);
			sq.SetOffsetTable(OffsetTable);
			foreach (ByteBuffer fragment in Fragments) {
				sq.AddFragment(fragment.Clone());
			}
			return sq;
		}

		public override void Dump(StringBuilder sb, string prefix, DicomDumpOptions options) {
			sb.Append(prefix);
			sb.AppendFormat("{0} {1} {2} {3}", Tag.ToString(), VR.VR, Tag.Entry.Name, HasOffsetTable ? "/w Offset Table" : "");
			for (int i = 0; i < _fragments.Count; i++) {
				sb.AppendLine().Append(prefix).AppendFormat(" Fragment {0}:  {1} bytes", i, _fragments[i].Length);
			}
		}
		#endregion
	}
}
