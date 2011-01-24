using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Dicom.HL7 {
	/// <summary>
	/// Callback for received HL7v2 message.
	/// </summary>
	/// <param name="client">Connection to remote client</param>
	/// <param name="hl7">Received HL7v2 message</param>
	/// <returns>HL7 message to respond with or null if no response required</returns>
	public delegate HL7v2 HL7v2MessageCallback(MLLP client, HL7v2 hl7);

	public class HL7v2Server {
		#region Private Members
		private int _port;
		private TcpListener _listener;
		private int _clients = 0;
		private bool _stop = false;
		#endregion

		#region Public Constructor
		public HL7v2Server(int port) {
			_port = port;
		}
		#endregion

		#region Public Properties
		public bool IsRunning {
			get { return _listener != null; }
		}

		public int Clients {
			get { return _clients; }
		}

		public HL7v2MessageCallback OnReceiveMessage;
		#endregion

		#region Public Methods
		public void Start() {
			_stop = false;
			_listener = new TcpListener(IPAddress.Any, _port);
			_listener.Start();
			_listener.BeginAcceptSocket(OnAcceptSocket, null);
		}

		public void Stop() {
			_stop = true;
			if (_listener != null) {
				_listener.Stop();
				_listener = null;
			}
		}
		#endregion

		#region Private Methods
		private void OnAcceptSocket(IAsyncResult result) {
			try {
				Socket s = _listener.EndAcceptSocket(result);

				if (_stop) {
					s.Close();
					return;
				}

				new Thread(HL7ClientProc).Start(s);
			} catch {
			} finally {
				if (!_stop)
					_listener.BeginAcceptSocket(OnAcceptSocket, null);
			}
		}

		private void HL7ClientProc(object state) {
			Socket socket = (Socket)state;

			try {
				Interlocked.Increment(ref _clients);
				Debug.Log.Info("HL7 client connected: " + socket.RemoteEndPoint);

				NetworkStream stream = new NetworkStream(socket);
				MLLP mllp = new MLLP(stream, false);

				while (socket.Connected && !_stop) {
					if (!socket.Poll(100000, SelectMode.SelectRead))
						continue;

					try {
						if (!stream.DataAvailable)
							break;
					} catch {
						// http://msdn.microsoft.com/en-us/library/system.net.sockets.networkstream.dataavailable.aspx
						// "If the remote host shuts down or closes the connection, DataAvailable may throw a SocketException."
						break;
					}

					string hl7 = mllp.Receive();

					if (OnReceiveMessage != null) {
						try {
							HL7v2 req = HL7v2.Parse(hl7);
							HL7v2 rsp = OnReceiveMessage(mllp, req);
							if (rsp != null)
								mllp.Send(rsp.ToString());
						} catch (Exception ex) {
							Debug.Log.Error("Error processing HL7 message: " + ex.ToString());
						}
					}
				}

				try {
					socket.Close();
				} catch {
				}

				Debug.Log.Info("HL7 client closed: " + socket.RemoteEndPoint);
			} catch {
				Debug.Log.Info("HL7 client closed on error: " + socket.RemoteEndPoint);
			} finally {
				Interlocked.Decrement(ref _clients);
			}
		}
		#endregion
	}
}
