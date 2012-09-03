using System;
using System.IO;

namespace Dicom.Network {
	public abstract class HookStream : Stream {
		#region Private Members
		private Stream _internal;
		#endregion

		#region Public Constructors
		public HookStream() : base() {
		}

		public HookStream(Stream stream) : base() {
			_internal = stream;
		}
		#endregion

		#region Protected Properties
		protected Stream InternalStream {
			get {
				if (_internal == null)
					throw new InvalidOperationException("Stream not hooked!");
				return _internal;
			}
		}
		#endregion

		#region Public Methods
		public void Hook(Stream stream) {
			_internal = stream;
		}
		#endregion

		#region Stream Methods
		public override bool CanRead {
			get { return InternalStream.CanRead; }
		}

		public override bool CanSeek {
			get { return InternalStream.CanSeek; }
		}

		public override bool CanTimeout {
			get { return InternalStream.CanTimeout; }
		}

		public override bool CanWrite {
			get { return InternalStream.CanWrite; }
		}

		public override int ReadTimeout {
			get { return InternalStream.ReadTimeout; }
			set { InternalStream.ReadTimeout = value; }
		}

		public override int WriteTimeout {
			get { return InternalStream.WriteTimeout; }
			set { InternalStream.WriteTimeout = value; }
		}

		public override void Close() {
			InternalStream.Close();
		}

		public override void Flush() {
			InternalStream.Flush();
		}

		public override long Length {
			get { return InternalStream.Length; }
		}

		public override long Position {
			get { return InternalStream.Position;  }
			set { InternalStream.Position = value; }
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return InternalStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return InternalStream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			InternalStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			InternalStream.Write(buffer, offset, count);
		}
		#endregion
	}
}
