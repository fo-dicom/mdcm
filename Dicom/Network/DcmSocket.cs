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
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Dicom.Network {
	//default ports:
	// 104, 11112 dicom
	// 2762 dicom-tls
	// 2761 dicom-iscl

	public enum DcmSocketType {
		Unknown,
		TCP,
		TLS,
		ISCL
	}

	public abstract class DcmSocket : IDisposable {
		#region Private Members
		private ConnectionStats _localStats = new ConnectionStats();
		private Stream _stream = null;
		private HookStream _hookStream = null;
		private int _throttleSpeed;
		#endregion

		#region Static
		private static List<DcmSocket> _sockets = new List<DcmSocket>();
		private static int _connections = 0;
		private static ConnectionStats _globalStats = new ConnectionStats();

		public static DcmSocket Create(DcmSocketType type) {
#if SILVERLIGHT
			if (type == DcmSocketType.TCP)
				return new DcmTcpSocket();
            else
				return null;
#else
			if (type == DcmSocketType.TLS)
				return new DcmTlsSocket();
			else if (type == DcmSocketType.TCP)
				return new DcmTcpSocket();
			else if (type == DcmSocketType.ISCL)
				return null;
			else
				return null;
#endif
        }

		protected static void RegisterSocket(DcmSocket socket) {
			lock (_sockets) {
				if (!_sockets.Contains(socket)) {
					_sockets.Add(socket);
				}
				_connections = _sockets.Count;
			}
		}

		protected static void UnregisterSocket(DcmSocket socket) {
			lock (_sockets) {
				_sockets.Remove(socket);
				_connections = _sockets.Count;
			}
		}

		public static int Connections {
			get { return _connections; }
		}

		public static int IncomingConnections {
			get {
				lock (_sockets) {
					int con = 0;
					foreach (DcmSocket socket in _sockets) {
						if (socket.IsIncomingConnection)
							con++;
					}
					return con;
				}
			}
		}

		public static int OutgoingConnections {
			get {
				lock (_sockets) {
					int con = 0;
					foreach (DcmSocket socket in _sockets) {
						if (!socket.IsIncomingConnection)
							con++;
					}
					return con;
				}
			}
		}

		public static IEnumerable<DcmSocket> ConnectedSockets {
			get { return _sockets; }
		}

		public static ConnectionStats GlobalStats {
			get { return _globalStats; }
		}
		#endregion

		#region Abstracts
		public abstract DcmSocketType Type { get; }
		public abstract bool Blocking { get; set; }
		public abstract bool NoDelay { get; set; }
		public abstract bool Connected { get; }
		public abstract int ConnectTimeout { get; set; }
		public abstract int SendTimeout { get; set; }
		public abstract int ReceiveTimeout { get; set; }
		public abstract EndPoint LocalEndPoint { get; }
		public abstract EndPoint RemoteEndPoint { get; }
		public abstract int Available { get; }
		public abstract DcmSocket Accept();
		public abstract void Bind(EndPoint localEP);
		public abstract void Close();
		public abstract void Connect(EndPoint remoteEP);
		public abstract void Reconnect();
		public abstract void Listen(int backlog);
		public abstract bool Poll(int microSeconds, SelectMode mode);
		public abstract Stream GetInternalStream();
		protected abstract bool IsIncomingConnection { get; }
		#endregion

		#region Properties
		public ConnectionStats LocalStats {
			get { return _localStats; }
		}

		public int ThrottleSpeed {
			get { return _throttleSpeed; }
			set {
				_throttleSpeed = value;
				if (_stream != null && _stream is ThrottleStream) {
					ThrottleStream ts = (ThrottleStream)_stream;
					ts.MaximumBytesPerSecond = _throttleSpeed;
				}
			}
		}
		#endregion

		#region Methods
		public void Hook(HookStream stream) {
			_hookStream = stream;
			if (_stream != null)
				_hookStream.Hook(stream);
		}

		public Stream GetStream() {
			if (_stream == null) {
				Stream stream = GetInternalStream();
				ConnectionMonitorStream mstream = new ConnectionMonitorStream(stream);
				mstream.AttachStats(GlobalStats);
				mstream.AttachStats(LocalStats);
				_stream = new ThrottleStream(mstream, _throttleSpeed);
				if (_hookStream != null)
					_hookStream.Hook(_stream);
			}
			if (_hookStream != null)
				return _hookStream;
			return _stream;
		}

		public void Connect(string host, int port) {
#if SILVERLIGHT
		    IPAddress[] addresses = new[] { IPAddress.Parse(host) };
#else
			IPAddress[] addresses = Dns.GetHostAddresses(host);
#endif
			for (int i = 0; i < addresses.Length; i++) {
				if (addresses[i].AddressFamily == AddressFamily.InterNetwork) {
					Connect(new IPEndPoint(addresses[i], port));
					return;
				}
			}
			throw new Exception("Unable to resolve host!");
		}
		#endregion

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			Close();
		}

		#endregion
	}

	#region TCP
	public class DcmTcpSocket : DcmSocket {
		private EndPoint _remoteEP;
		private Socket _socket;
		private bool _incoming;
		private int _connectTimeout;

		public DcmTcpSocket() {
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.NoDelay = true;
		}

		private DcmTcpSocket(Socket socket) {
			_socket = socket;
			_socket.NoDelay = true;
			_incoming = true;
			RegisterSocket(this);
		}

		public override DcmSocketType Type {
			get { return DcmSocketType.TCP; }
		}

		public override bool Blocking {
			get { return _socket.Blocking; }
			set { _socket.Blocking = value; }
		}

        public override bool NoDelay {
			get { return _socket.NoDelay; }
			set { _socket.NoDelay = value; }
		}

		public override bool Connected {
			get {
				if (!_socket.Connected)
					return false;
				try {
					_socket.Send(new byte[1], 0, 0);
					return true;
				}
				catch (SocketException ex) {
					if (ex.SocketErrorCode == SocketError.WouldBlock)
						return true;
				}
				return false;
			}
		}

		public override int ConnectTimeout {
			get { return _connectTimeout; }
			set { _connectTimeout = value; }
		}

		public override int SendTimeout {
			get { return _socket.SendTimeout; }
			set { _socket.SendTimeout = value; }
		}

		public override int ReceiveTimeout {
			get { return _socket.ReceiveTimeout; }
			set { _socket.ReceiveTimeout = value; }
		}

		public override EndPoint LocalEndPoint {
			get { return _socket.LocalEndPoint; }
		}

		public override EndPoint RemoteEndPoint {
			get { return _socket.RemoteEndPoint; }
		}

		public override int Available {
			get { return _socket.Available; }
		}

		public override DcmSocket Accept() {
			Socket socket = _socket.Accept();
			return new DcmTcpSocket(socket);
		}

		public override void Bind(EndPoint localEP) {
			_socket.Bind(localEP);
		}

        public override void Close() {
			if (_socket != null) {
				UnregisterSocket(this);
				_socket.Close();
				_socket = null;
			}
		}

		public override void Connect(EndPoint remoteEP) {
			if (_connectTimeout == 0 || true) {
				_socket.Connect(remoteEP);
			} else {
				IAsyncResult result = _socket.BeginConnect(remoteEP, null, null);
				if (!result.AsyncWaitHandle.WaitOne(_connectTimeout, true))
					throw new SocketException((int)SocketError.TimedOut);
			}
			_remoteEP = remoteEP;
			RegisterSocket(this);
		}

		public override void Reconnect() {
			Close();
			Connect(_remoteEP);
		}

		public override void Listen(int backlog) {
			_socket.Listen(backlog);
		}

		public override bool Poll(int microSeconds, SelectMode mode) {
			return _socket.Poll(microSeconds, mode);
		}

		public override Stream GetInternalStream() {
			return new NetworkStream(_socket);
		}

		protected override bool IsIncomingConnection {
			get { return _incoming; }
		}
	}
	#endregion

#if !SILVERLIGHT
	#region TLS
	public class DcmTlsSocket : DcmSocket {
		private bool _server;
		private EndPoint _remoteEP;
		private Socket _socket;
		private int _connectTimeout;

		public DcmTlsSocket() {
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.NoDelay = true;
		}

		private DcmTlsSocket(Socket socket) {
			_server = true;
			_socket = socket;
			_socket.NoDelay = true;
			RegisterSocket(this);
		}

		public override DcmSocketType Type {
			get { return DcmSocketType.TLS; }
		}

		public override bool Blocking {
			get { return _socket.Blocking; }
			set { _socket.Blocking = value; }
		}

		public override bool NoDelay {
			get { return _socket.NoDelay; }
			set { _socket.NoDelay = value; }
		}

		public override bool Connected {
			get {
				if (!_socket.Connected)
					return false;
				try {
					_socket.Send(new byte[1], 0, 0);
					return true;
				}
				catch (SocketException ex) {
					if (ex.SocketErrorCode == SocketError.WouldBlock)
						return true;
				}
				return false;
			}
		}

		public override int ConnectTimeout {
			get { return _connectTimeout; }
			set { _connectTimeout = value; }
		}

		public override int SendTimeout {
			get { return _socket.SendTimeout; }
			set { _socket.SendTimeout = value; }
		}

		public override int ReceiveTimeout {
			get { return _socket.ReceiveTimeout; }
			set { _socket.ReceiveTimeout = value; }
		}

		public override EndPoint LocalEndPoint {
			get { return _socket.LocalEndPoint; }
		}

		public override EndPoint RemoteEndPoint {
			get { return _socket.RemoteEndPoint; }
		}

		public override int Available {
			get { return _socket.Available; }
		}

		public override DcmSocket Accept() {
			Socket socket = _socket.Accept();
			return new DcmTlsSocket(socket);
		}

		public override void Bind(EndPoint localEP) {
			_socket.Bind(localEP);
		}

		public override void Close() {
			if (_socket != null) {
				UnregisterSocket(this);
				_socket.Close();
				_socket = null;
			}
		}

		public override void Connect(EndPoint remoteEP) {
			_server = false;
			if (_connectTimeout == 0 || true) {
				_socket.Connect(remoteEP);
			} else {
				IAsyncResult result = _socket.BeginConnect(remoteEP, null, null);
				if (!result.AsyncWaitHandle.WaitOne(_connectTimeout, true))
					throw new SocketException((int)SocketError.TimedOut);
			}
			_remoteEP = remoteEP;
			RegisterSocket(this);
		}

		public override void Reconnect() {
			Close();
			Connect(_remoteEP);
		}

		public override void Listen(int backlog) {
			_server = true;
			_socket.Listen(backlog);
		}

		public override bool Poll(int microSeconds, SelectMode mode) {
			return _socket.Poll(microSeconds, mode);
		}

		public override Stream GetInternalStream() {
			if (_server)
				return new TlsServerStream(_socket);
			else
				return new TlsClientStream(_socket);
		}

		protected override bool IsIncomingConnection {
			get { return _server; }
		}
	}
	#endregion
#endif
}
