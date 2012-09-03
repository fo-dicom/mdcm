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

namespace Dicom.IO {
	internal class ChunkStream : Stream {
		#region Private Members
		private long _position;
		private long _length;
		private int _current;
		private int _offset;
		private List<byte[]> _chunks;
		#endregion

		#region Public Constructors
		public ChunkStream() : base() {
			_current = 0;
			_offset = 0;
			_position = 0;
			_length = 0;
			_chunks = new List<byte[]>();
		}
		#endregion

		#region Public Members
		public void AddChunk(byte[] chunk) {
			_chunks.Add(chunk);
			_length += chunk.Length;
		}

		public void Clear() {
			_current = 0;
			_offset = 0;
			_position = 0;
			_length = 0;
			_chunks.Clear();
		}
		#endregion

		#region Stream Members
		public override bool CanRead {
			get { return true; }
		}

		public override bool CanSeek {
			get { return true; }
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override void Flush() {
		}

		public override long Length {
			get { return _length; }
		}

		public override long Position {
			get { return _position; }
			set { Seek(value, SeekOrigin.Begin); }
		}

		public override int Read(byte[] buffer, int offset, int count) {
			int read = 0, dstOffset = 0;
			for (int i = _current; i < _chunks.Count; i++) {
				byte[] chunk = _chunks[i];
				int bytesInChunk = chunk.Length - _offset;

				if (bytesInChunk > count) {
					Buffer.BlockCopy(chunk, _offset, buffer, dstOffset, count);
					read += count;
					_offset += count;
					_position += count;
					return read;
				}

				Buffer.BlockCopy(chunk, _offset, buffer, dstOffset, bytesInChunk);

				read += bytesInChunk;
				count -= bytesInChunk;
				dstOffset += bytesInChunk;
				_position += bytesInChunk;
				_current++;
				_offset = 0;
			}
			return read;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if (origin == SeekOrigin.Begin) {
				_current = 0;
				_offset = 0;
				_position = 0;
			}
			if (origin == SeekOrigin.End) {
				_current = _chunks.Count - 1;
				_offset = _chunks[_current].Length - 1;
				_position = _length - 1;
			}

			_position += offset;
			if (_position < 0)
				_position = 0;
			else if (_position >= _length)
				_position = _length - 1;

			_current = 0;
			_offset = 0;
			long remain = _position;

			for (int i = 0; i < _chunks.Count; i++) {
				byte[] chunk = _chunks[i];
				if (remain > chunk.Length) {
					remain -= chunk.Length;
				} else {
					_offset = (int)remain;
					return _position;
				}
				_current++;
			}

			_position -= remain;
			return _position;
		}

		public override void SetLength(long value) {
			throw new Exception("Unable to set length of unwritable stream!");
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new Exception("Stream not writable!");
		}
		#endregion
	}
}
