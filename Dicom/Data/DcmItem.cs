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
	public abstract class DcmItem {
		#region Private Members
		private DicomTag _tag;
		private DicomVR _vr;
		private long _streamPosition;
		private Endian _endian;
		#endregion

		#region Public Constructors
		public DcmItem(DicomTag tag, DicomVR vr) {
			_tag = tag;
			_vr = vr;
			_streamPosition = 0;
			_endian = Endian.LocalMachine;
		}

		public DcmItem(DicomTag tag, DicomVR vr, long pos, Endian endian) {
			_tag = tag;
			_vr = vr;
			_streamPosition = pos;
			_endian = endian;
		}
		#endregion

		#region Public Properties
		public DicomTag Tag {
			get { return _tag; }
		}

		public string Name {
			get { return Tag.Entry.Name; }
		}

		public DicomVR VR {
			get { return _vr; }
		}

		public long StreamPosition {
			get { return _streamPosition; }
			set { _streamPosition = value; }
		}

		public Endian Endian {
			get { return _endian; }
			internal set {
				if (_endian != value) {
					_endian = value;
					ChangeEndianInternal();
				}
			}
		}
		#endregion

		#region Public Methods
		public override string ToString() {
			return _tag.Entry.ToString();
		}
		#endregion

		#region Methods to Override
		internal abstract uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options);

		protected abstract void ChangeEndianInternal();

		internal abstract void Preload();

		internal abstract void Unload();

		public abstract DcmItem Clone();

		public virtual void Dump(StringBuilder sb, String prefix,  DicomDumpOptions options) {
			sb.Append(prefix).AppendLine(_tag.Entry.ToString());
		}
		#endregion
	}
}
