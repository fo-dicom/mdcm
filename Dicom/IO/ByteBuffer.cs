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
using System.IO;
using System.Text;

using Dicom.Data;

namespace Dicom.IO {
	public class ByteBuffer {
		#region Private Members
		private MemoryStream _ms;
		private byte[] _data;
		private BinaryReader _br;
		private BinaryWriter _bw;
		private Endian _endian;
		private Encoding _encoding;
		private FileSegment _segment;
		#endregion

		#region Public Constructors
		public ByteBuffer() : this(Endian.LocalMachine) {
		}

		public ByteBuffer(Endian endian) {
			_endian = endian;
			_encoding = DcmEncoding.Default;
		}

		public ByteBuffer(byte[] data) : this(data, Endian.LocalMachine) {
		}

		public ByteBuffer(byte[] data, Endian endian) {
			_data = data;
			_endian = endian;
			_encoding = DcmEncoding.Default;
		}

		public ByteBuffer(FileSegment segment) : this(segment, Endian.LocalMachine) {
		}

		public ByteBuffer(FileSegment segment, Endian endian) {
			_segment = segment;
			_endian = endian;
			_encoding = DcmEncoding.Default;
		}
		#endregion

		#region Public Properties
		public MemoryStream Stream {
			get {
				if (_ms == null) {
					if (_data != null) {
						_ms = new MemoryStream(_data);
						_data = null;
					} else if (_segment != null) {
						_ms = new MemoryStream(_segment.GetData());
					} else {
						_ms = new MemoryStream();
					}
				}
				return _ms;
			}
		}

		public BinaryReader Reader {
			get {
				if (_br == null) {
					_br = EndianBinaryReader.Create(Stream, Encoding, Endian);
				}
				return _br;
			}
		}

		public BinaryWriter Writer {
			get {
				if (_bw == null) {
					_bw = EndianBinaryWriter.Create(Stream, Encoding, Endian);
					_segment = null;
				}
				return _bw;
			}
		}

		public Endian Endian {
			get { return _endian; }
			set {
				_endian = value;
				_br = null;
				_bw = null;
			}
		}

		public Encoding Encoding {
			get { return _encoding; }
			set { _encoding = value; }
		}

		public int Length {
			get {
				if (_ms != null)
					return (int)_ms.Length;
				if (_data != null)
					return _data.Length;
				if (_segment != null)
					return (int)_segment.Length;
				return 0;
			}
		}

		public bool CanUnload {
			get { return _segment != null; }
		}

		public bool IsDeferred {
			get { return CanUnload && (_ms != null || _data != null); }
		}
		#endregion

		#region Public Functions
		public void Preload() {
			if (_segment != null && _data == null && _ms == null) {
				ToBytes();
			}
		}

		public void Unload() {
			if (_segment != null) {
				_ms = null;
				_br = null;
				_bw = null;
				_data = null;
			}
		}

		public ByteBuffer Clone() {
			ByteBuffer clone = null;
			if (_data != null)
				clone = new ByteBuffer((byte[])_data.Clone(), Endian);
			else if (_segment != null)
				clone = new ByteBuffer(_segment, Endian);
			else
				clone = new ByteBuffer((byte[])ToBytes().Clone(), Endian);
			clone.Encoding = Encoding;
			return clone;
		}

		public void Clear() {
			_ms = null;
			_br = null;
			_bw = null;
			_data = null;
			_segment = null;
		}

		public void Chop(int count) {
			_segment = null;
			int len = (int)Stream.Length;
			if (len <= count) {
				Stream.SetLength(0);
				return;
			}
			byte[] bytes = GetChunk(count, len - count);
			Stream.SetLength(0);
			Stream.Position = 0;
			Stream.Write(bytes, 0, bytes.Length);
		}

		public void Append(byte[] buffer, int offset, int count) {
			long pos = Stream.Position;
			Stream.Seek(0, SeekOrigin.End);
			Stream.Write(buffer, offset, count);
			Stream.Position = pos;
			_segment = null;
		}

		public int CopyFrom(Stream s, int count) {
			_ms = null;
			_segment = null;
			_br = null;
			_bw = null;

			int read = 0;
			_data = new byte[count];

			while (read < count) {
				int rd = s.Read(_data, read, count - read);
				if (rd == 0)
					return read;
				read += rd;
			}

			return read;
		}

		public void CopyTo(Stream s) {
			if (_ms != null) {
				_ms.WriteTo(s);
				return;
			}
			if (_data != null) {
				s.Write(_data, 0, _data.Length);
				return;
			}
			if (_segment != null) {
				_segment.WriteTo(s);
				return;
			}
		}

		public void CopyTo(int srcOffset, Stream s, int dstOffset, int count) {
			byte[] bytes = new byte[count];
			Stream.Position = srcOffset;
			Stream.Read(bytes, 0, count);
			s.Write(bytes, 0, count);
		}

		public void CopyTo(int srcOffset, byte[] buffer, int dstOffset, int count) {
			if (_ms != null) {
				Stream.Position = srcOffset;
				Stream.Read(buffer, dstOffset, count);
				return;
			}
			if (_data != null) {
				Buffer.BlockCopy(_data, srcOffset, buffer, dstOffset, count);
				return;
			}
			if (_segment != null) {
				using (FileStream fs = _segment.OpenStream()) {
					fs.Seek(srcOffset, SeekOrigin.Current);
					fs.Read(buffer, dstOffset, count);
				}
				return;
			}
		}

		public byte[] GetChunk(int srcOffset, int count) {
			byte[] chunk = new byte[count];
			CopyTo(srcOffset, chunk, 0, count);
			return chunk;
		}

		public void FromBytes(byte[] bytes) {
			_data = bytes;
			_ms = null;
			_segment = null;
		}

		public byte[] ToBytes() {
			if (_data != null)
				return _data;
			if (_ms != null) {
				_data = _ms.ToArray();
				_ms = null;
				_br = null;
				_bw = null;
				return _data;
			}
			if (_segment != null) {
				_data = _segment.GetData();
				return _data;
			}
			return new byte[0];
		}

		public ushort[] ToUInt16s() {
			byte[] bytes = ToBytes();
			int count = bytes.Length / 2;
			ushort[] words = new ushort[count];
			for (int i = 0, p = 0; i < count; i++, p += 2) {
				words[i] = unchecked((ushort)((bytes[p] << 8) + bytes[p + 1]));
			}
			return words;
		}

		public short[] ToInt16s() {
			byte[] bytes = ToBytes();
			int count = bytes.Length / 2;
			short[] words = new short[count];
			for (int i = 0, p = 0; i < count; i++, p += 2) {
				words[i] = unchecked((short)((bytes[p] << 8) + bytes[p + 1]));
			}
			return words;
		}

		public uint[] ToUInt32s() {
			byte[] bytes = ToBytes();
			int count = bytes.Length / 4;
			uint[] dwords = new uint[count];
			for (int i = 0, p = 0; i < count; i++, p += 4) {
				dwords[i] = BitConverter.ToUInt32(bytes, p);
			}
			return dwords;
		}

		public string GetString()
		{
#if SILVERLIGHT
		    byte[] bytes = ToBytes();
			return _encoding.GetString(bytes, 0, bytes.Length);
#else
			return _encoding.GetString(ToBytes());
#endif
        }

		public void SetString(string val) {
			_data = _encoding.GetBytes(val);
			_ms = null;
			_segment = null;
		}

		public void SetString(string val, byte pad) {
			int count = _encoding.GetByteCount(val);
			if ((count & 1) == 1)
				count++;

			byte[] bytes = new byte[count];
			if (_encoding.GetBytes(val, 0, val.Length, bytes, 0) < count)
				bytes[count - 1] = pad;

			_data = bytes;
			_ms = null;
			_segment = null;
		}

		public void Swap(int bytesToSwap) {
			Endian.SwapBytes(bytesToSwap, ToBytes());
		}

		public void Swap2() {
			Endian.SwapBytes2(ToBytes());
		}
		public void Swap4() {
			Endian.SwapBytes4(ToBytes());
		}
		public void Swap8() {
			Endian.SwapBytes8(ToBytes());
		}
		#endregion
	}
}
