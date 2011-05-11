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
using System.Diagnostics;
using NLog;
using NLog.Config;

namespace Dicom
{
	public static class Debug
	{
		private static Logger _log;

		public static Logger Log {
			get { return _log ?? (_log = LogManager.GetLogger("DICOM")); }
			set { _log = value; }
		}

		public static void InitializeIsolatedStorageDebugLogger()
		{
			LoggingConfiguration config = new LoggingConfiguration();

			using (IsolatedStorageTarget target = new IsolatedStorageTarget { Layout = "${date} ${level:uppercase=true} >>> ${message}" })
			{
				config.AddTarget("IsolatedStorage", target);
				config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
			}

			LogManager.Configuration = config;
		}

		public static string GetCallingFunction()
		{
			try
			{
				StackTrace trace = new StackTrace(true);
				StackFrame frame = trace.GetFrame(2);
				return String.Format("{0}() at {1}:{2}", frame.GetMethod().Name, frame.GetFileName(),
									 frame.GetFileLineNumber());
			}
			catch
			{
				return "[unable to trace stack]";
			}
		}
	}
}
