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

using NLog;

namespace Dicom.Utility {
	public class LogWriter : TextWriter {
		private LogLevel _level;
		private Logger _log;

		public LogWriter(LogLevel level, Logger log) {
			_level = level;
			_log = log;
		}

		public override void Write(string value) {
			_log.Log(_level, value.TrimEnd('\n'));
		}

		public override void WriteLine(string value) {
			_log.Log(_level, value.TrimEnd('\n'));
		}

		public override Encoding Encoding {
			get { return Console.Out.Encoding; }
		}

#if !SILVERLIGHT
		public static void RedirectConsole(Logger log) {
			Console.SetOut(new LogWriter(LogLevel.Info, log));
			Console.SetError(new LogWriter(LogLevel.Error, log));
		}
#endif
	}
}
