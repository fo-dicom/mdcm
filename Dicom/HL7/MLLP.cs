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

namespace Dicom.HL7 {
	public class MLLP {
		private static byte[] StartBlock = new byte[] { 0x0B };
		private static byte[] EndBlock = new byte[] { 0x1C, 0x0D };
		private static byte[] ACK = new byte[] { 0x0B, 0x06, 0x1C, 0x0D };
		private static byte[] NAK = new byte[] { 0x0B, 0x15, 0x1C, 0x0D };

		private Stream _stream;
		private bool _version3;

		public MLLP(Stream stream, bool version3) {
			_stream = stream;
			_version3 = version3;
		}

		public bool Send(string message) {
#if SILVERLIGHT
            byte[] bytes = Encoding.UTF8.GetBytes(message);
#else
			byte[] bytes = Encoding.ASCII.GetBytes(message);
#endif
			_stream.Write(StartBlock, 0, StartBlock.Length);
			_stream.Write(bytes, 0, bytes.Length);
			_stream.Write(EndBlock, 0, EndBlock.Length);
			_stream.Flush();
			if (_version3) {
				byte[] rsp = new byte[4];
				if (_stream.Read(rsp, 0, 4) != 4)
					return false;
				return rsp[1] == 0x06;
			}
			return true;
		}

		public string Receive() {
			int ib = 0x00;
			MemoryStream ms = new MemoryStream();
			for (; _stream.ReadByte() != 0x0B; ) ;
			while (true) {
				if (ib == 0x1C) {
					ib = _stream.ReadByte();
					if (ib == 0x0D)
						break;
					ms.WriteByte(0x1C);
					ms.WriteByte((byte)ib);
				}
				else {
					ib = _stream.ReadByte();
					if (ib != 0x1C)
						ms.WriteByte((byte)ib);
				}
			}
			if (_version3) {
				_stream.Write(ACK, 0, ACK.Length);
				_stream.Flush();
			}
#if SILVERLIGHT
            return Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
#else
			return Encoding.ASCII.GetString(ms.ToArray());
#endif
		}
	}
}
