using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Dicom.Network {
	public class NetworkErrorStream : HookStream {
		#region Private Members
		private WeakReference _socket;
		private DateTime _errorTime;
		private bool _errorClose;
		private bool _errorTimeout;
		private bool _errorGarbage;
		private bool _readOnly;
		private static Random _random = new Random();

		private DcmSocket InternalSocket {
			get { return (DcmSocket)_socket.Target; }
		}
		#endregion

		#region Public Constructors
		public NetworkErrorStream(DcmSocket socket) {
			_socket = new WeakReference(socket);
			InternalSocket.Hook(this);
		}
		#endregion

		#region Public Methods
		public void SetErrorClose(int secondsTillError, bool readOnly) {
			if (secondsTillError < 0)
				secondsTillError = _random.Next(0, 60);
			_errorTime = DateTime.Now.AddSeconds(secondsTillError);
			_errorClose = true;
			_readOnly = readOnly;
		}

		public void SetErrorTimeout(int secondsTillError, bool readOnly) {
			if (secondsTillError < 0)
				secondsTillError = _random.Next(0, 60);
			_errorTime = DateTime.Now.AddSeconds(secondsTillError);
			_errorTimeout = true;
			_readOnly = readOnly;
		}

		public void SetErrorGarbage(int secondsTillError, bool readOnly) {
			if (secondsTillError < 0)
				secondsTillError = _random.Next(0, 60);
			_errorTime = DateTime.Now.AddSeconds(secondsTillError);
			_errorGarbage = true;
			_readOnly = readOnly;
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (_errorTimeout && DateTime.Now > _errorTime) {
				int readTimeout = InternalSocket.ReceiveTimeout / 1000;
				for (int i = 0;; i++) {
					if (readTimeout > 0 && i >= readTimeout)
						throw new SocketException((int)SocketError.TimedOut);
					if (!InternalSocket.Connected)
						break;
					Thread.Sleep(1000);
				}
			}
			else if (_errorClose && DateTime.Now > _errorTime) {
				InternalSocket.Close();
				throw new SocketException((int)SocketError.ConnectionReset);
			}
			int read = base.Read(buffer, offset, count);
			if (_errorGarbage && DateTime.Now > _errorTime && FlipCoin()) {
				if (buffer.Length == read)
					_random.NextBytes(buffer);
				else {
					byte[] temp = new byte[read];
					_random.NextBytes(temp);
					Buffer.BlockCopy(temp, 0, buffer, offset, read);
				}
			}
			return read;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (!_readOnly) {
				if (_errorTimeout && DateTime.Now > _errorTime) {
					int writeTimeout = InternalSocket.SendTimeout;
					if (writeTimeout > 0) {
						Thread.Sleep(writeTimeout);
						throw new SocketException((int)SocketError.TimedOut);
					}
				}
				else if (_errorClose && DateTime.Now > _errorTime) {
					InternalSocket.Close();
					throw new SocketException((int)SocketError.ConnectionReset);
				}
				else if (_errorGarbage && DateTime.Now > _errorTime && FlipCoin()) {
					if (buffer.Length == count)
						_random.NextBytes(buffer);
					else {
						byte[] temp = new byte[count];
						_random.NextBytes(temp);
						Buffer.BlockCopy(temp, 0, buffer, offset, count);
					}
				}
			}
			base.Write(buffer, offset, count);
		}
		#endregion

		#region Private Methods
		private static bool FlipCoin() {
			return _random.Next(0, 1) == 1;
		}
		#endregion
	}
}
