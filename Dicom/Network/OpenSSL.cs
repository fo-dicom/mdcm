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
#if OPENSSL

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Dicom.Network {
	enum OpenSslProtocol {
		AutoDetect,
		SSLv2,
		SSLv3,
		TLSv1
	};

	class OpenSslCertificate : IDisposable {
		#region Private Members
		private IntPtr _x509;
		#endregion

		#region Initialization
		public OpenSslCertificate(IntPtr x509) {
			_x509 = x509;
		}

		public OpenSslCertificate(OpenSslBIO bio) {
			_x509 = OpenSslUtility.AssertNotNull(OpenSslNative.PEM_read_bio_X509(bio.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));
		}

		public static OpenSslCertificate FromPemFile(string file) {
			return new OpenSslCertificate(OpenSslBIO.FromFile(file));
		}
		#endregion

		#region Properties
		public IntPtr Handle {
			get { return _x509; }
		}
		#endregion

		#region IDisposable Members
		public void Dispose() {
			OpenSslNative.X509_free(_x509);
		}
		#endregion
	}

	class OpenSslPrivateKey : IDisposable {
		#region Private Members
		private IntPtr _pkey;
		#endregion

		#region Initialization
		public OpenSslPrivateKey(IntPtr pkey) {
			_pkey = pkey;
		}

		public OpenSslPrivateKey(OpenSslBIO bio) {
			_pkey = OpenSslUtility.AssertNotNull(OpenSslNative.PEM_read_bio_PrivateKey(bio.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));
		}

		public static OpenSslPrivateKey FromPemFile(string file) {
			return new OpenSslPrivateKey(OpenSslBIO.FromFile(file));
		}
		#endregion

		#region Properties
		public IntPtr Handle {
			get { return _pkey; }
		}
		#endregion

		#region IDisposable Members
		public void Dispose() {
			OpenSslNative.EVP_PKEY_free(_pkey);
		}
		#endregion
	}

	class OpenSslContext : IDisposable {
		#region Private Members
		private IntPtr _ctx;
		#endregion

		#region Initialization
		public OpenSslContext(IntPtr ctx) {
			_ctx = ctx;
		}

		public OpenSslContext(OpenSslProtocol protocol, bool server) {
			_ctx = OpenSslUtility.AssertNotNull(OpenSslNative.SSL_CTX_new(GetMethodFromProtocol(protocol, server)));
		}
		#endregion

		#region Properties
		public OpenSslCertificate Certificate {
			set {
				OpenSslUtility.AssertSuccess(OpenSslNative.SSL_CTX_use_certificate(_ctx, value.Handle));
			}
		}

		public OpenSslPrivateKey PrivateKey {
			set {
				OpenSslUtility.AssertSuccess(OpenSslNative.SSL_CTX_use_PrivateKey(_ctx, value.Handle));
			}
		}

		public IntPtr Handle {
			get { return _ctx; }
		}
		#endregion

		#region Methods
		private static IntPtr GetMethodFromProtocol(OpenSslProtocol protocol, bool server) {
			IntPtr method = IntPtr.Zero;

			switch (protocol) {
				case OpenSslProtocol.AutoDetect:
					if (server)
						method = OpenSslNative.SSLv23_server_method();
					else
						method = OpenSslNative.SSLv23_client_method();
					break;

				case OpenSslProtocol.SSLv2:
					if (server)
						method = OpenSslNative.SSLv2_server_method();
					else
						method = OpenSslNative.SSLv2_client_method();
					break;

				case OpenSslProtocol.SSLv3:
					if (server)
						method = OpenSslNative.SSLv3_server_method();
					else
						method = OpenSslNative.SSLv3_client_method();
					break;

				case OpenSslProtocol.TLSv1:
					if (server)
						method = OpenSslNative.TLSv1_server_method();
					else
						method = OpenSslNative.TLSv1_client_method();
					break;

				default:
					break;
			}

			return OpenSslUtility.AssertNotNull(method);
		}
		#endregion

		#region IDisposable Members

		public void Dispose() {
			OpenSslNative.SSL_CTX_free(_ctx);
		}

		#endregion
	}

	class OpenSslConnection : IDisposable {
		#region Private Members
		private IntPtr _ssl;
		private OpenSslContext _context;
		private Socket _socket;
		#endregion

		#region Initialization
		public OpenSslConnection(IntPtr ssl) {
			_ssl = ssl;
		}

		public OpenSslConnection(OpenSslContext ctx) {
			_context = ctx;
			_ssl = OpenSslUtility.AssertNotNull(OpenSslNative.SSL_new(_context.Handle));
		}

		public OpenSslConnection(OpenSslProtocol protocol, bool server)
			: this(new OpenSslContext(protocol, server)) {
		}
		#endregion

		#region Properties

		public OpenSslCertificate Certificate {
			set {
				OpenSslUtility.AssertSuccess(OpenSslNative.SSL_use_certificate(_ssl, value.Handle));
			}
		}

		public OpenSslPrivateKey PrivateKey {
			set {
				OpenSslUtility.AssertSuccess(OpenSslNative.SSL_use_PrivateKey(_ssl, value.Handle));
			}
		}

		public OpenSslCertificate PeerCertificate {
			get {
				return new OpenSslCertificate(OpenSslNative.SSL_get_peer_certificate(_ssl));
			}
		}

		public Socket InternalSocket {
			get {
				return _socket;
			}
		}

		#endregion

		#region Methods
		public void Accept(Socket socket) {
			InitializeSocket(socket);
			OpenSslUtility.AssertSuccess(OpenSslNative.SSL_accept(_ssl));
		}

		public void Connect(Socket socket) {
			InitializeSocket(socket);
			OpenSslUtility.AssertSuccess(OpenSslNative.SSL_connect(_ssl));
		}

		public void Close() {
			OpenSslNative.SSL_shutdown(_ssl);
			if (_socket.Connected)
				_socket.Close();
		}

		public int Read(byte[] buffer, int offset, int count) {
			if (buffer.Length < (count + offset)) {
				count = buffer.Length - offset;
			}
			if (offset == 0) {
				return OpenSslNative.SSL_read(_ssl, buffer, count);
			} else {
				byte[] temp = new byte[count];
				int res = OpenSslNative.SSL_read(_ssl, temp, count);
				if (res > 0) {
					Buffer.BlockCopy(temp, 0, buffer, offset, res);
				}
				return res;
			}
		}

		public void Write(byte[] buffer, int offset, int count) {
			if (buffer.Length < (count + offset)) {
				count = buffer.Length - offset;
			}
			if (offset == 0) {
				OpenSslNative.SSL_write(_ssl, buffer, count);
			} else if (count > 0) {
				byte[] temp = new byte[count];
				Buffer.BlockCopy(buffer, offset, temp, 0, count);
				OpenSslNative.SSL_write(_ssl, temp, (int)count);
			}
		}

		private void InitializeSocket(Socket socket) {
			OpenSslUtility.AssertSuccess(OpenSslNative.SSL_set_fd(_ssl, socket.Handle.ToInt32()));
			_socket = socket;
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			OpenSslNative.SSL_free(_ssl);
		}

		#endregion
	}

	class OpenSslStream : Stream {
		#region Private Members
		private OpenSslContext context;
		private OpenSslConnection connection;
		#endregion

		#region Initialization
		/// <summary>
		/// Initializes server connection
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="protocol"></param>
		/// <param name="certificate"></param>
		/// <param name="privatekey"></param>
		public OpenSslStream(Socket socket, OpenSslProtocol protocol, OpenSslCertificate certificate, OpenSslPrivateKey privatekey) {
			context = new OpenSslContext(protocol, true);
			connection = new OpenSslConnection(context);
			connection.Certificate = certificate;
			connection.PrivateKey = privatekey;
			connection.Accept(socket);
		}

		/// <summary>
		/// Initializes server connection
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="ctx"></param>
		public OpenSslStream(Socket socket, OpenSslContext ctx) {
			context = ctx;
			connection = new OpenSslConnection(context);
			connection.Accept(socket);
		}

		/// <summary>
		/// Initializes client connection
		/// </summary>
		/// <param name="socket"></param>
		public OpenSslStream(Socket socket, OpenSslProtocol protocol) {
			context = new OpenSslContext(protocol, false);
			connection = new OpenSslConnection(context);
			connection.Connect(socket);
		}
		#endregion

		#region Properties
		public OpenSslCertificate PeerCertificate {
			get { return connection.PeerCertificate; }
		}
		#endregion

		#region Methods
		public override void Close() {
			connection.Close();
		}
		#endregion

		#region Stream Methods
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
		}

		public override long Length {
			get { throw new NotSupportedException(); }
		}

		public override long Position {
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return connection.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			connection.Write(buffer, offset, count);
		}
		#endregion
	}

	class OpenSslException : Exception {
		#region Initialization
		public OpenSslException()
			: base(GetErrorMessages()) {
		}

		public OpenSslException(uint err)
			: base(GetErrorMessage(err)) {
		}
		#endregion

		#region Methods
		public static string GetErrorMessages() {
			StringBuilder sb = new StringBuilder();
			for (; ; ) {
				uint err = OpenSslNative.ERR_get_error();
				if (err == 0)
					break;
				sb.AppendLine(GetErrorMessage(err));
			}
			return sb.ToString().TrimEnd();
		}

		public static string GetErrorMessage(uint err) {
			if (err == 0) return "Unknown OpenSSL Error";
			byte[] buf = new byte[1024];
			OpenSslNative.ERR_error_string_n(err, buf, buf.Length);
			return Encoding.ASCII.GetString(buf);
		}
		#endregion
	}

	class OpenSslBIO : IDisposable {
		#region Private Members
		private IntPtr _bio;
		#endregion

		#region Initialization
		public OpenSslBIO(IntPtr bio) {
			_bio = bio;
		}

		public static OpenSslBIO FromFile(string file) {
			return new OpenSslBIO(OpenSslUtility.AssertNotNull(OpenSslNative.BIO_new_file(file, "r")));
		}
		#endregion

		#region Properties
		public IntPtr Handle {
			get { return _bio; }
		}
		#endregion

		#region IDisposable Members

		public void Dispose() {
			OpenSslNative.BIO_free(_bio);
		}

		#endregion
	}

	static class OpenSslNative {
		const string DLL_SSL = "ssleay32";
		const string DLL_EAY = "libeay32";

		#region Initialization
		static OpenSslNative() {
			try {
				if (!File.Exists("ssleay32.dll") || !File.Exists("libeay32.dll")) {
					if (IntPtr.Size == 8) {
						File.Copy("ssleay32.x64.dll", "ssleay32.dll", true);
						File.Copy("libeay32.x64.dll", "libeay32.dll", true);
					} else {
						File.Copy("ssleay32.x86.dll", "ssleay32.dll", true);
						File.Copy("libeay32.x86.dll", "libeay32.dll", true);
					}
				}

				SSL_load_error_strings();
				SSL_library_init();
			} catch {
				throw new Exception("OpenSSL initialization failed!  Check that the OpenSSL DLL files are in your path.");
			}
		}
		#endregion

		#region InterOp

		#region Crypto

		#region ERR
		/// <summary>
		/// Returns the earliest error code from the thread's error queue and removes 
		/// the entry. This function can be called repeatedly until there are no more 
		/// error codes to return.
		/// </summary>
		/// <returns>error code</returns>
		[DllImport(DLL_EAY)]
		public extern static uint ERR_get_error();

		/// <summary>
		/// generates a human-readable string representing the error code e, and places 
		/// it at buf. buf must be at least 120 bytes long.
		/// </summary>
		/// <param name="e">error code</param>
		/// <param name="buf">message buffer</param>
		/// <param name="len">length of buffer</param>
		[DllImport(DLL_EAY)]
		public extern static void ERR_error_string_n(uint e, byte[] buf, int len);
		#endregion

		#region BIO
		/// <summary>
		/// Creates BIO* from file.
		/// </summary>
		/// <param name="file">file name</param>
		/// <param name="mode">fopen mode - "r"</param>
		/// <returns>BIO*</returns>
		[DllImport(DLL_EAY)]
		public extern static IntPtr BIO_new_file(string file, string mode);

		/// <summary>
		/// Frees BIO*.
		/// </summary>
		/// <param name="bio">BIO*</param>
		[DllImport(DLL_EAY)]
		public extern static void BIO_free(IntPtr bio);
		#endregion

		#region PEM
		/// <summary>
		/// Reads X509 certificate from BIO.
		/// </summary>
		/// <param name="bp">BIO*</param>
		/// <param name="x">X509*</param>
		/// <param name="cb">callback</param>
		/// <param name="u">user param</param>
		/// <returns>X509* - certificate</returns>
		[DllImport(DLL_EAY)]
		public extern static IntPtr PEM_read_bio_X509(IntPtr bp, IntPtr x, IntPtr cb, IntPtr u);

		/// <summary>
		/// Reads private key from BIO.
		/// </summary>
		/// <param name="bp">BIO*</param>
		/// <param name="x">X509*</param>
		/// <param name="cb">callback</param>
		/// <param name="u">user param</param>
		/// <returns>EVP_PKEY* - private key</returns>
		[DllImport(DLL_EAY)]
		public extern static IntPtr PEM_read_bio_PrivateKey(IntPtr bp, IntPtr x, IntPtr cb, IntPtr u);
		#endregion

		#region X509
		/// <summary>
		/// Frees X509 certificate
		/// </summary>
		/// <param name="x">X509* - certificate</param>
		[DllImport(DLL_EAY)]
		public extern static void X509_free(IntPtr x);
		#endregion

		#region EVP
		/// <summary>
		/// Frees private key
		/// </summary>
		/// <param name="pkey">EVP_PKEY* - private key</param>
		[DllImport(DLL_EAY)]
		public extern static void EVP_PKEY_free(IntPtr pkey);
		#endregion

		#endregion

		#region SSL
		/// <summary>
		/// Registers the available ciphers and digests.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_library_init.html
		/// </summary>
		/// <returns>always returns 1</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_library_init();

		/// <summary>
		/// Registers error strings for libcrypto and libssl.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_library_init.html
		/// http://www.openssl.org/docs/crypto/ERR_load_crypto_strings.html
		/// </summary>
		[DllImport(DLL_SSL)]
		public extern static void SSL_load_error_strings();

		/// <summary>
		/// Constructor for the SSLv2 SSL_METHOD structure for a dedicated client.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv2_client_method();

		/// <summary>
		/// Constructor for the SSLv2 SSL_METHOD structure for a dedicated server.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv2_server_method();

		/// <summary>
		/// Constructor for the SSLv3 SSL_METHOD structure for a dedicated client.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv3_client_method();

		/// <summary>
		/// Constructor for the SSLv3 SSL_METHOD structure for a dedicated server.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv3_server_method();

		/// <summary>
		/// Constructor for the TLSv1 SSL_METHOD structure for a dedicated client.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr TLSv1_client_method();

		/// <summary>
		/// Constructor for the TLSv1 SSL_METHOD structure for a dedicated server.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr TLSv1_server_method();

		/// <summary>
		/// Constructor for the SSLv2, SSLv3, and TLSv1 SSL_METHOD structure for 
		/// an auto-detecting client.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv23_client_method();

		/// <summary>
		/// Constructor for the SSLv2, SSLv3, and TLSv1 SSL_METHOD structure for 
		/// an auto-detecting server.
		/// 
		/// http://www.openssl.org/docs/ssl/ssl.html
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <returns>SSL_METHOD*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSLv23_server_method();

		/// <summary>
		/// Creates a new SSL_CTX object as framework to establish TLS/SSL 
		/// enabled connections.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_new.html
		/// </summary>
		/// <param name="method">SSL_METHOD*</param>
		/// <returns>SSL_CTX*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSL_CTX_new(IntPtr method);

		/// <summary>
		/// Frees SSL Context.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_free.html
		/// </summary>
		/// <param name="ctx">SSL_CTX*</param>
		[DllImport(DLL_SSL)]
		public extern static void SSL_CTX_free(IntPtr ctx);

		/// <summary>
		/// Adds the certificate x into ctx.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_use_certificate.html
		/// </summary>
		/// <param name="ctx">SSL_CTX*</param>
		/// <param name="x">X509*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_CTX_use_certificate(IntPtr ctx, IntPtr x);

		/// <summary>
		/// Adds the private key pkey into ctx.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_use_certificate.html
		/// </summary>
		/// <param name="ctx">SSL_CTX*</param>
		/// <param name="pkey">EVP_PKEY*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_CTX_use_PrivateKey(IntPtr ctx, IntPtr pkey);

		/// <summary>
		/// Creates new SSL session.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_new.html
		/// </summary>
		/// <param name="ctx">SSL_CTX*</param>
		/// <returns>SSL*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSL_new(IntPtr ctx);

		/// <summary>
		/// Frees SSL session.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_free.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		[DllImport(DLL_SSL)]
		public extern static void SSL_free(IntPtr ssl);

		/// <summary>
		/// Initiate the TLS/SSL handshake with an TLS/SSL server.
		/// http://www.openssl.org/docs/ssl/SSL_connect.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_connect(IntPtr ssl);

		/// <summary>
		/// Wait for a TLS/SSL client to initiate a TLS/SSL handshake.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_accept.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_accept(IntPtr ssl);

		/// <summary>
		/// Shut down a TLS/SSL connection.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_shutdown.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_shutdown(IntPtr ssl);

		/// <summary>
		/// Tries to read num bytes from the specified ssl into the buffer buf.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_read.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <param name="buf">buffer</param>
		/// <param name="num">buffer length</param>
		/// <returns>
		/// >0 - number of bytes read
		///  0 - connection closed?
		/// &lt;0 - error
		/// </returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_read(IntPtr ssl, byte[] buf, int num);

		/// <summary>
		/// Writes num bytes from the buffer buf into the specified ssl connection.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_write.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <param name="buf">buffer</param>
		/// <param name="num">number of bytes to write</param>
		/// <returns>
		/// >0 - number of bytes written
		///  0 - connection closed?
		/// &lt; - error
		/// </returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_write(IntPtr ssl, byte[] buf, int num);

		/// <summary>
		/// Connect the SSL object with a file descriptor.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_set_fd.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <param name="fd">Socket handle</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_set_fd(IntPtr ssl, int fd);

		/// <summary>
		/// Get the X509 certificate of the peer.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_get_peer_certificate.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <returns>X509*</returns>
		[DllImport(DLL_SSL)]
		public extern static IntPtr SSL_get_peer_certificate(IntPtr ssl);

		/// <summary>
		/// Adds the certificate x into ssl.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_use_certificate.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <param name="x">X509*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_use_certificate(IntPtr ssl, IntPtr x);

		/// <summary>
		/// Adds the private key pkey into ssl.
		/// 
		/// http://www.openssl.org/docs/ssl/SSL_CTX_use_certificate.html
		/// </summary>
		/// <param name="ssl">SSL*</param>
		/// <param name="pkey">EVP_PKEY*</param>
		/// <returns>error code</returns>
		[DllImport(DLL_SSL)]
		public extern static int SSL_use_PrivateKey(IntPtr ssl, IntPtr pkey);
		#endregion

		#endregion
	}

	static class OpenSslUtility {
		#region Methods
		public static IntPtr AssertNotNull(IntPtr ptr) {
			if (ptr == IntPtr.Zero)
				throw new OpenSslException();
			return ptr;
		}

		public static int AssertSuccess(int ret) {
			if (ret != 1)
				throw new OpenSslException();
			return ret;
		}
		#endregion
	}
}

#endif
