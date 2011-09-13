using System;

namespace NLog
{
	public class Logger
	{
		#region  CONSTRUCTORS
		
		internal Logger ()
		{
		}
		
		#endregion
		
		#region METHODS
		
		public void Log(LogLevel level, string message)
		{
			switch (level)
			{
			case LogLevel.Info:
				Info (message);
				break;
			case LogLevel.Error:
				Error (message);
				break;
			default:
				break;
			}
		}
		
		public void Info(string format, params object[] args)
		{
			Console.WriteLine("INFO: " + format, args);
		}
		
		public void Warn(string format, params object[] args)
		{
			Console.WriteLine("WARN: " + format, args);
		}
		
		public void Error(string format, params object[] args)
		{
			Console.WriteLine("ERROR: " + format, args);
		}
		
		public void Debug(string format, params object[] args)
		{
			Console.WriteLine("DEBUG: " + format, args);
		}
		
		#endregion
	}
}

