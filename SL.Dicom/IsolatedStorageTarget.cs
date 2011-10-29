using System;
using System.IO;
using System.IO.IsolatedStorage;
using NLog;
using NLog.Targets;

namespace Dicom
{
    /// <summary>
    /// Simple NLog isolated storage target implementation, adapted from user wageoghe's solution on Stackoverflow:
    /// http://stackoverflow.com/questions/4025471/logging-with-nlog-into-an-isolated-storage/4028151#4028151
    /// </summary>
    [Target("IsolatedStorage")]
    public sealed class IsolatedStorageTarget : TargetWithLayout
    {
        private readonly string _logFileName;
        private readonly bool _deleteExistingLog;

        public IsolatedStorageTarget()
            : this("Solution.Silverlight.log", true)
        {
        }

        public IsolatedStorageTarget(string iLogFileName, bool iDeleteExistingLog)
        {
            _logFileName = iLogFileName;
            _deleteExistingLog = iDeleteExistingLog;
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (_deleteExistingLog)
                    {
                        if (store.FileExists(_logFileName)) store.DeleteFile(_logFileName);
                        store.CreateFile(_logFileName);
                    }
                    else
                    {
                        if (!store.FileExists(_logFileName)) store.CreateFile(_logFileName);
                    }
                }
            }
            catch
            {
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                using (IsolatedStorageFile store =
                    IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (Stream stream = new IsolatedStorageFileStream
                        (_logFileName, FileMode.Append, FileAccess.Write, store))
                    {
                        var writer = new StreamWriter(stream);
                        writer.WriteLine(Layout.Render(logEvent));
                        writer.Close();
                    }
                }
            }
            catch
            {
            }
        }
    }
}