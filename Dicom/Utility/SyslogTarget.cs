using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using NLog;
using NLog.Targets;

namespace Dicom.Utility {
	public enum SyslogLevel {
		Emergency = 0,
		Alert = 1,
		Critical = 2,
		Error = 3,
		Warning = 4,
		Notice = 5,
		Information = 6,
		Debug = 7
	}

	public enum SyslogFacility {
		Kernel = 0,
		User = 1,
		Mail = 2,
		Daemon = 3,
		Auth = 4,
		Syslog = 5,
		Lpr = 6,
		News = 7,
		UUCP = 8,
		Cron = 9,
		Local0 = 10,
		Local1 = 11,
		Local2 = 12,
		Local3 = 13,
		Local4 = 14,
		Local5 = 15,
		Local6 = 16,
		Local7 = 17
	}

	[Target("Syslog")]
	public class SyslogTarget : TargetWithLayout {
		private int _port;
		private string _host;
		private string _identity;
		private IPEndPoint _endpoint;
		private Socket _socket;
		private SyslogFacility _facility;

		public SyslogTarget() {
			Host = "127.0.0.1";
			Port = 514;
			Identity = Environment.MachineName.Replace(' ', '_');
			Facility = SyslogFacility.User;
			Layout = "${message}";
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		public string Host {
			get { return _host; }
			set { _host = value; }
		}

		public int Port {
			get { return _port; }
			set { _port = value; }
		}

		public string Identity {
			get { return _identity; }
			set { _identity = value; }
		}

		public SyslogFacility Facility {
			get { return _facility; }
			set { _facility = value; }
		}

		protected override void Write(LogEventInfo logEvent) {
			if (_endpoint == null) {
				try {
					IPHostEntry entry = Dns.GetHostEntry(_host);
					for (int i = 0; i < entry.AddressList.Length; i++) {
						if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork) {
							_endpoint = new IPEndPoint(entry.AddressList[i], _port);
							break;
						}
					}
				}
				catch {
					_endpoint = null;
				}

				if (_endpoint == null) {
					_endpoint = new IPEndPoint(IPAddress.Loopback, _port);
				}
			}

			int facility = (int)_facility;

			int level = (int)SyslogLevel.Debug;
			if (logEvent.Level == LogLevel.Info)
				level = (int)SyslogLevel.Information;
			else if (logEvent.Level == LogLevel.Warn)
				level = (int)SyslogLevel.Warning;
			else if (logEvent.Level == LogLevel.Error)
				level = (int)SyslogLevel.Error;
			else if (logEvent.Level == LogLevel.Fatal)
				level = (int)SyslogLevel.Critical;

			int priority = (facility * 8) + level;

			string time = null;
			if (logEvent.TimeStamp.Day < 10)
				time = logEvent.TimeStamp.ToString("MMM  d HH:mm:ss");
			else
				time = logEvent.TimeStamp.ToString("MMM dd HH:mm:ss");

			string message = String.Format("<{0}>{1} {2} {3}", priority, time, _identity, CompiledLayout.GetFormattedMessage(logEvent));
			byte[] buffer = Encoding.ASCII.GetBytes(message);

			try {
				_socket.SendTo(buffer, _endpoint);
			}
			catch {
			}
		}
	}

	public class SyslogServer {
		private int _port;
		private bool _stop;
		private Thread _thread;
		private Logger _log;

		public SyslogServer(Logger logTarget) : this(514, logTarget) {
		}

		public SyslogServer(int listenPort, Logger logTarget) {
			_port = listenPort;
			_log = logTarget;
		}

		public void Start() {
			if (_thread == null) {
				_stop = false;
				_thread = new Thread(ServerProc);
				_thread.IsBackground = true;
				_thread.Start();
			}
		}

		public void Stop() {
			if (_thread != null) {
				_stop = true;
				//_thread.Join();
				_thread = null;
			}
		}

		private void ServerProc() {
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			EndPoint remoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
			char[] splitChars1 = new char[] { '>' };
			char[] splitChars2 = new char[] { ' ' };

			try {
				byte[] buffer = new byte[4096];

				socket.Bind(new IPEndPoint(IPAddress.Any, _port));

				while (!_stop) {
					if (!socket.Poll(50000, SelectMode.SelectRead))
						continue;

					try {
						int count = socket.ReceiveFrom(buffer, ref remoteEP);

						string message = Encoding.ASCII.GetString(buffer, 0, count);
						string[] parts = message.Split(splitChars1,  2);

						SyslogFacility facility = SyslogFacility.User;
						SyslogLevel level = SyslogLevel.Information;

						if (parts.Length == 2) {
							int code = int.Parse(parts[0].TrimStart('<'));
							facility = (SyslogFacility)(code / 8);
							level = (SyslogLevel)(code - ((int)facility * 8));
							message = parts[1];
						}

						message = message.Substring(16);
						parts = message.Split(splitChars2, 2);

						if (parts.Length == 2)
							message = parts[1];

						switch (level) {
							case SyslogLevel.Emergency:
							case SyslogLevel.Alert:
								_log.Log(LogLevel.Fatal, message);
								break;
							case SyslogLevel.Critical:
							case SyslogLevel.Error:
								_log.Log(LogLevel.Error, message);
								break;
							case SyslogLevel.Warning:
								_log.Log(LogLevel.Warn, message);
								break;
							case SyslogLevel.Debug:
								_log.Log(LogLevel.Debug, message);
								break;
							case SyslogLevel.Notice:
							case SyslogLevel.Information:
							default:
								_log.Log(LogLevel.Info, message);
								break;
						}
					}
					catch {
					}
				}
			}
			catch {
			}
			finally {
				try {
					socket.Close();
				}
				catch {
				}
			}
		}
	}
}
