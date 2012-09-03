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
using Dicom.Utility;

namespace Dicom.Network {
	public class ConnectionStats {
		#region Member Variables
		private long bytesDownloaded;
		private long bytesUploaded;
		private double downloadSpeed;
		private double downloadSpeedPrevious;
		private int lastUploadUpdateTime;
		private int lastDownloadUpdateTime;
		private int tempUploadCount;
		private int tempDownloadCount;
		private double uploadSpeed;
		private double uploadSpeedPrevious;
		private object downloadLock = new object();
		private object uploadLock = new object();
		#endregion

		#region Public Properties
		/// <summary>
		/// Returns the total bytes downloaded from this peer
		/// </summary>
		public long BytesDownloaded {
			get { return bytesDownloaded; }
		}


		/// <summary>
		/// Returns the total bytes uploaded to this peer
		/// </summary>
		public long BytesUploaded {
			get { return bytesUploaded; }
		}


		/// <summary>
		/// The current average download speed in bytes per second
		/// </summary>
		/// <returns></returns>
		public double DownloadSpeed {
			get {
				UpdateDownloadSpeed();
				return downloadSpeed;
			}
		}


		/// <summary>
		/// The current average upload speed in byte/second
		/// </summary>
		/// <returns></returns>
		public double UploadSpeed {
			get {
				UpdateUploadSpeed();
				return uploadSpeed;
			}
		}
		#endregion

		#region Constructors
		internal ConnectionStats() {
			lastUploadUpdateTime = Environment.TickCount;
			lastDownloadUpdateTime = Environment.TickCount;
		}
		#endregion

		#region Methods
		internal void NotifyBytesDownloaded(int bytes) {
			lock (downloadLock) {
				bytesDownloaded += bytes;
				tempDownloadCount += bytes;
			}
		}

		private void UpdateDownloadSpeed() {
			lock (downloadLock) {
				int currentTime = Environment.TickCount;

				int difference = currentTime - lastDownloadUpdateTime;

				if (difference <= 0)
					difference = 1000;

				if (difference < 500)
					return;

				downloadSpeed = tempDownloadCount / (difference / 1000.0);
				
				if (downloadSpeedPrevious != 0.0)
					downloadSpeed = (downloadSpeed + downloadSpeedPrevious) / 2;
				
				downloadSpeedPrevious = downloadSpeed;

				tempDownloadCount = 0;
				lastDownloadUpdateTime = currentTime;
			}
		}

		internal void NotifyBytesUploaded(int bytes) {
			lock (uploadLock) {
				bytesUploaded += bytes;
				tempUploadCount += bytes;
			}
		}

		private void UpdateUploadSpeed() {
			lock (uploadLock) {
				int currentTime = Environment.TickCount;

				int difference = currentTime - lastUploadUpdateTime;

				if (difference <= 0)
					difference = 1000;

				if (difference < 500)
					return;

				uploadSpeed = tempUploadCount / (difference / 1000.0);

				if (uploadSpeedPrevious != 0.0)
					uploadSpeed = (uploadSpeed + uploadSpeedPrevious) / 2;

				uploadSpeedPrevious = uploadSpeed;

				tempUploadCount = 0;
				lastUploadUpdateTime = currentTime;
			}
		}

		public override string ToString() {
			return String.Format("Sent: {0} @ {1}/s; Recv: {2} @ {3}/s;",
				Format.ByteCount(BytesUploaded),
				Format.ByteCount(UploadSpeed),
				Format.ByteCount(BytesDownloaded),
				Format.ByteCount(DownloadSpeed));
		}
		#endregion
	}

	public class ConnectionMonitorStream : Stream {
		#region Member Variables
		private Stream stream;
		private List<ConnectionStats> stats;
		#endregion Member Variables

		#region Constructors
		/// <summary>
		/// Creates a new ConnectionMonitorStream
		/// </summary>
		internal ConnectionMonitorStream(Stream stream)
		{
			this.stream = stream;
			this.stats = new List<ConnectionStats>();
		}
		#endregion

		#region Methods
		public void AttachStats(ConnectionStats stat) {
			if (!stats.Contains(stat))
				stats.Add(stat);
		}
		#endregion

		#region Stream Members
		public override bool CanRead {
			get { return stream.CanRead; }
		}

		public override bool CanSeek {
			get { return stream.CanSeek; }
		}

		public override bool CanWrite {
			get { return stream.CanWrite; }
		}

		public override bool CanTimeout {
			get { return stream.CanTimeout; }
		}

		public override void Flush() {
			stream.Flush();
		}

		public override long Length {
			get { return stream.Length; }
		}

		public override long Position {
			get { return stream.Position; }
			set { stream.Position = value; }
		}

		public override int Read(byte[] buffer, int offset, int count) {
			int ret = stream.Read(buffer, offset, count);
			foreach (ConnectionStats stat in stats) {
				stat.NotifyBytesDownloaded(ret);
			}
			return ret;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return stream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			stream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			stream.Write(buffer, offset, count);
			foreach (ConnectionStats stat in stats) {
				stat.NotifyBytesUploaded(count);
			}
		}
		#endregion
	}
}
