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
	/// <summary>
	/// File Segment
	/// </summary>
	public class FileSegment {
		#region Private Members
		private string _fileName;
		private long _position;
		private long _length;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="FileSegment"/> class.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="position">The position.</param>
		/// <param name="length">The length.</param>
		public FileSegment(string fileName, long position, long length) {
			FileName = fileName;
			Position = position;
			Length = length;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <value>The name of the file.</value>
		public string FileName {
			get { return _fileName; }
			private set { _fileName = value; }
		}

		/// <summary>
		/// Gets or sets the segment position.
		/// </summary>
		/// <value>The segment position.</value>
		public long Position {
			get { return _position; }
			private set { _position = value; }
		}


		/// <summary>
		/// Gets or sets the segment length.
		/// </summary>
		/// <value>The segment length.</value>
		public long Length {
			get { return _length; }
			private set { _length = value; }
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Opens a readonly stream for this segment.
		/// </summary>
		/// <returns>FileStream at segment position.</returns>
		public FileStream OpenStream() {
			FileStream fs = File.OpenRead(FileName);
			fs.Seek(Position, SeekOrigin.Begin);
			return fs;
		}

		/// <summary>
		/// Gets the data for this segment.
		/// </summary>
		/// <returns>Byte array of segment data.</returns>
		public byte[] GetData() {
			byte[] data = new byte[Length];
			using (FileStream fs = OpenStream()) {
				fs.Read(data, 0, (int)Length);
				fs.Close();
			}
			return data;
		}

		/// <summary>
		/// Copies this segment to a stream.
		/// </summary>
		/// <param name="s">Target stream.</param>
		public void WriteTo(Stream s) {
			using (FileStream fs = OpenStream()) {
				byte[] buffer = new byte[65536];
				int count = (int)Length;
				while (count > 0) {
					int size = Math.Min(count, buffer.Length);
					size = fs.Read(buffer, 0, size);
					s.Write(buffer, 0, size);
					count -= size;
				}
				fs.Close();
			}
		}

		/// <summary>
		/// Gets a ByteBuffer containing this segment's data.
		/// </summary>
		/// <returns>ByteBuffer of segment data.</returns>
		public ByteBuffer GetBuffer() {
			return new ByteBuffer(GetData());
		}
		#endregion
	}
}
