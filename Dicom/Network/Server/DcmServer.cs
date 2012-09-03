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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Dicom.Network.Server {
	public class DcmServer<T> where T: DcmServiceBase {
		public delegate void DicomClientCreatedDelegate(DcmServer<T> server, T client, DcmSocketType socketType);
		public delegate void DicomClientClosedDelegate(DcmServer<T> server, T client);

		#region Private Members
		private List<int> _ports = new List<int>();
		private List<DcmSocketType> _types = new List<DcmSocketType>();

		private bool _stop;
		private List<Thread> _threads = new List<Thread>();
		private int _clientCount = 0;
		#endregion

		#region Public Members
		public DicomClientCreatedDelegate OnDicomClientCreated;
		public DicomClientClosedDelegate OnDicomClientClosed;

		public int ClientCount {
			get { return _clientCount; }
		}

		public void AddPort(int port, DcmSocketType type) {
			_ports.Add(port);
			_types.Add(type);
		}

		public void ClearPorts() {
			_ports.Clear();
			_types.Clear();
		}

		public void Start() {
			if (_threads.Count == 0) {
				_stop = false;

				for (int i = 0; i < _ports.Count; i++) {
					try {
						DcmSocket socket = DcmSocket.Create(_types[i]);
						socket.Bind(new IPEndPoint(IPAddress.Any, _ports[i]));
						socket.Listen(5);

						Thread thread = new Thread(ServerProc);
						thread.IsBackground = true;
						thread.Start(socket);

						_threads.Add(thread);

						Dicom.Debug.Log.Info("DICOM {0} server listening on port {1}", _types[i].ToString(), _ports[i]);
					}
					catch {
						Dicom.Debug.Log.Error("Unable to start DICOM {0} server on port {1}; Check to see that no other network services are using this port and try again.", _types[i].ToString(), _ports[i]); 
					}
				}
			}
		}

		public void Stop() {
			_stop = true;
			foreach (Thread thread in _threads) {
				thread.Join();
			}
			_threads.Clear();
			_clientCount = 0;
			Dicom.Debug.Log.Info("DICOM services stopped");
		}
		#endregion

		#region Private Members
		private void ServerProc(object state) {
			DcmSocket socket = (DcmSocket)state;
			try {
				List<DcmSocket> sockets = new List<DcmSocket>();
				List<T> clients = new List<T>();

				while (!_stop) {
					if (socket.Poll(250000, SelectMode.SelectRead)) {
						try {
							DcmSocket client = socket.Accept();

							Debug.Log.Info("Client connecting from {0}", client.RemoteEndPoint);

							if (client.Type == DcmSocketType.TLS)
								Debug.Log.Info("Authenticating SSL/TLS for client: {0}", client.RemoteEndPoint);

							T handler = Activator.CreateInstance<T>();

							if (OnDicomClientCreated != null)
								OnDicomClientCreated(this, handler, client.Type);

							handler.InitializeService(client);
							clients.Add(handler);

							Interlocked.Increment(ref _clientCount);
						}
						catch (Exception e) {
							Debug.Log.Error(e.Message);
						}
					}

					for (int i = 0; i < clients.Count; i++) {
						if (clients[i].IsClosed) {
							clients[i].Close();
							if (OnDicomClientClosed != null)
								OnDicomClientClosed(this, clients[i]);
							clients.RemoveAt(i--);
							Interlocked.Decrement(ref _clientCount);
						}
					}
				}

				for (int i = 0; i < clients.Count; i++) {
					clients[i].Close();
					if (OnDicomClientClosed != null)
						OnDicomClientClosed(this, clients[i]);
					Interlocked.Decrement(ref _clientCount);
				}

				foreach (DcmSocket s in sockets) {
					s.Close();
				}
			}
			finally {
				socket.Close();
			}
		}
		#endregion
	}
}
