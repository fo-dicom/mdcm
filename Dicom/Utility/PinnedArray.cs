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
using System.Runtime.InteropServices;

namespace Dicom.Utility {
	public class PinnedArray<T> : IDisposable {
		#region Private Members
		private T[] _data;
		private int _count;
#if !SILVERLIGHT
		private int _size;
		private GCHandle _handle;
		private IntPtr _pointer;
#endif
		#endregion

		#region Public Properties
		public T[] Data {
			get { return _data; }
		}

		public int Count {
			get { return _count; }
		}

#if !SILVERLIGHT
		public int ByteSize {
			get { return _size; }
		}

		public IntPtr Pointer {
			get { return _pointer; }
		}
#endif
		public T this[int index] {
			get { return _data[index]; }
			set { _data[index] = value; }
		}
		#endregion

		#region Public Constructor
		public PinnedArray(int count) {
			_count = count;
			_data = new T[_count];
#if !SILVERLIGHT
			_size = Marshal.SizeOf(typeof(T)) * _count;
			_handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
			_pointer = _handle.AddrOfPinnedObject();
#endif
		}

		public PinnedArray(T[] data) {
			_count = data.Length;
			_data = data;
#if !SILVERLIGHT
			_size = Marshal.SizeOf(typeof(T)) * _count;
			_handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
			_pointer = _handle.AddrOfPinnedObject();
#endif
        }

		~PinnedArray() {
			Dispose(false);
		}
		#endregion

		#region Public Members
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region Private Members
		private void Dispose(bool disposing) {
            if (_data != null)
            {
#if !SILVERLIGHT
				_handle.Free();
				_pointer = IntPtr.Zero;
#endif
                _data = null;
            }
		}
		#endregion
	}

	public class PinnedByteArray : PinnedArray<byte> {
		public PinnedByteArray(int count)
			: base(count) {
		}
		public PinnedByteArray(byte[] data)
			: base(data) {
		}
	}

	public class PinnedIntArray : PinnedArray<int> {
		public PinnedIntArray(int count)
			: base(count) {
		}
		public PinnedIntArray(int[] data)
			: base(data) {
		}
	}
}
