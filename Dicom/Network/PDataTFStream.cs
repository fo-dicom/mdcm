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
using System.IO;
using System.Text;

using Dicom.IO;

namespace Dicom.Network {
	internal class PDataTFStream : Stream {
		internal delegate void PduSentDelegate();

		#region Private Members
		private Stream _network;
		private bool _command;
		private int _max;
		private byte _pcid;
		private PDataTF _pdu;
		private byte[] _bytes;
		private int _sent;
		private MemoryStream _buffer;
		#endregion

		#region Public Constructors
		public PDataTFStream(Stream network, byte pcid, int max) {
			_network = network;
			_command = true;
			_pcid = pcid;
			_max = (max == 0) ? MaxPduSizeLimit : Math.Min(max, MaxPduSizeLimit);
			_pdu = new PDataTF();
			_buffer = new MemoryStream(_max * 2);
		}
		#endregion

		#region Public Properties
		public static int MaxPduSizeLimit = 4 * 1024 * 1024;

		public PduSentDelegate OnPduSent;

		public bool IsCommand {
			get { return _command; }
			set {
				CreatePDV();
				_command = value;
				WritePDU(true);
			}
		}

		public int BytesSent {
			get { return _sent; }
		}
		#endregion

		#region Public Members
		public void Flush(bool last) {
			WritePDU(last);
			_network.Flush();
		}
		#endregion

		#region Private Members
		private int CurrentPduSize() {
			return 6 + (int)_pdu.GetLengthOfPDVs();
		}

		private bool CreatePDV() {
			int len = Math.Min(GetBufferLength(), _max - CurrentPduSize() - 6);

			if (_bytes == null || _bytes.Length != len || _pdu.PDVs.Count > 0) {
				_bytes = new byte[len];
			}
			_sent = _buffer.Read(_bytes, 0, len);

			PDV pdv = new PDV(_pcid, _bytes, _command, false);
			_pdu.PDVs.Add(pdv);

			return pdv.IsLastFragment;
		}

		private void WritePDU(bool last) {
			if (_pdu.PDVs.Count == 0 || ((CurrentPduSize() + 6) < _max && GetBufferLength() > 0)) {
				CreatePDV();
			}
			if (_pdu.PDVs.Count > 0) {
				if (last) {
					_pdu.PDVs[_pdu.PDVs.Count - 1].IsLastFragment = true;
				}
				RawPDU raw = _pdu.Write();
				raw.WritePDU(_network);
				if (OnPduSent != null)
					OnPduSent();
				_pdu = new PDataTF();
			}
		}

		private void AppendBuffer(byte[] buffer, int offset, int count) {
			long pos = _buffer.Position;
			_buffer.Seek(0, SeekOrigin.End);
			_buffer.Write(buffer, offset, count);
			_buffer.Position = pos;
		}

		private int GetBufferLength() {
			return (int)(_buffer.Length - _buffer.Position);
		}
		#endregion

		#region Stream Members
		public override bool CanRead {
			get { return false; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override void Flush() {
			//_network.Flush();
		}

		public override long Length {
			get { throw new NotImplementedException(); }
		}

		public override long Position {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			AppendBuffer(buffer, offset, count);
			while ((CurrentPduSize() + 6 + GetBufferLength()) > _max) {
				WritePDU(false);
			}
		}

		public void Write(Stream stream) {
			int max = _max - 12;
			int length = (int)stream.Length;
			int position = (int)stream.Position;
			byte[] buffer = new byte[max];
			while (position < length) {
				int count = Math.Min(max, length - position);
				count = stream.Read(buffer, 0, count);
				AppendBuffer(buffer, 0, count);
				position += count;
				WritePDU(position == length);
			}
			_network.Flush();
		}
		#endregion
	}
}
