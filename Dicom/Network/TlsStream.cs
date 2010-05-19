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
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Dicom.Network {
	public class TlsClientStream : Stream {
		public static bool UseOpenSSL = false;

		private Socket _socket;
		private Stream _stream;

		public TlsClientStream(Socket socket) {
			_socket = socket;
			if (UseOpenSSL) {
#if OPENSSL
				_stream = new OpenSslStream(_socket, OpenSslProtocol.AutoDetect);
#else
				throw new NotImplementedException();
#endif
			}
			else {
				NetworkStream net = new NetworkStream(_socket);
				SslStream ssl = new SslStream(net, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
				ssl.AuthenticateAsClient("", null, SslProtocols.Tls, false);
				_stream = ssl;
			}
		}

		public override bool CanRead {
			get { return true; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override void Flush() {
			_stream.Flush();
		}

		public override long Length {
			get { throw new NotSupportedException(); }
		}

		public override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return _stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			_stream.Write(buffer, offset, count);
		}

		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			return true;
		}
	}

	public class TlsServerStream : Stream {
		public static bool UseOpenSSL = true;

		private Socket _socket;
		private Stream _stream;

#if OPENSSL
		public static string PemFileName = String.Empty;
		private static OpenSslContext _OpenSslContext;
#endif

		public TlsServerStream(Socket socket) {
			_socket = socket;

			if (UseOpenSSL) {
#if OPENSSL				
				if (_OpenSslContext == null) {
					if (PemFileName == String.Empty) {
						string[] files = Directory.GetFiles(Dicom.Debug.GetStartDirectory(), "*.pem", SearchOption.TopDirectoryOnly);
						if (files.Length > 0)
							PemFileName = files[0];
						else
							throw new Exception("Unable to find SSL/TLS server certificate.");
					}
					_OpenSslContext = new OpenSslContext(OpenSslProtocol.AutoDetect, true);
					_OpenSslContext.Certificate = OpenSslCertificate.FromPemFile(PemFileName);
					_OpenSslContext.PrivateKey = OpenSslPrivateKey.FromPemFile(PemFileName);
				}
				_stream = new OpenSslStream(_socket, _OpenSslContext);
#else
				throw new NotImplementedException();
#endif			
			}
			else {
				throw new NotImplementedException();
			}
		}

		public override bool CanRead {
			get { return true; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override void Flush() {
			_stream.Flush();
		}

		public override long Length {
			get { throw new NotSupportedException(); }
		}

		public override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return _stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			_stream.Write(buffer, offset, count);
		}

		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			return true;
		}
	}
}
