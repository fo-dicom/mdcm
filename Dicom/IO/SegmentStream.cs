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

namespace Dicom.IO {
	public class SegmentStream : Stream {
		private Stream _internalStream;
		private long _position;
		private long _length;

		public SegmentStream(Stream stream, long position, long length) : base() {
			_internalStream = stream;
			_position = position;
			_length = length;
		}

		public override bool CanRead {
			get { return _internalStream.CanRead; }
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
			get {
				return _internalStream.Position - _position;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException("Attempted to set the position to a negative value.");
				if (value >= _length)
					throw new EndOfStreamException("Attempted seeking past the end of the stream or segment.");
				_internalStream.Position = _position + value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (_internalStream.Position < _position ||
				_internalStream.Position > (_position + _length))
				throw new IOException("Internal stream position not in segment range.");
			if ((Position + count) > Length)
				count -= (int)((Position + count) - Length);
			return _internalStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if (origin == SeekOrigin.Begin)
				Position = offset;
			else if (origin == SeekOrigin.End)
				Position = Length - offset;
			else
				Position = Position + offset;
			return Position;
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}
	}
}
