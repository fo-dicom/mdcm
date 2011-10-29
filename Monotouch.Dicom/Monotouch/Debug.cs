using NLog;

namespace Dicom
{
	public static class Debug
	{
		private static Logger _log;

		public static Logger Log {
			get { return _log ?? (_log = new Logger()); }
			set { _log = value; }
		}
	}
}

