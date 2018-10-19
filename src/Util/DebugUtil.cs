using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace NodeProxy.Util
{
    public sealed class DebugUtil
    {
        public static bool debug = true;
        public static int outtype = 0;
        public const int INFO = 0x01;
        public const int ERROR = 0x02;
        public const int FILE = 0x04;
        public const string LOG_FILE_NAME = "./log_file.log";
        private static ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        public static void Log(object log)
        {
            if (!debug) return;
            if ((outtype & INFO) > 0) Console.WriteLine("[D][{0}] {1}", DateTime.Now, log.ToString());
            if ((outtype & FILE) > 0)
            {
                rwlock.EnterWriteLock();
                try
                {
                    File.AppendAllText(LOG_FILE_NAME, string.Format("[D][{0}] {1}\n", DateTime.Now, log.ToString()));
                }
                finally
                {
                    rwlock.ExitWriteLock();
                }
            }
        }

        public static void LogFormat(string format, params object[] args)
        {
            if (!debug) return;
            Log(string.Format(format, args));
        }

        public static void Error(object log)
        {
            if (!debug) return;
            if ((outtype & ERROR) > 0) Console.WriteLine("[E][{0}] {1}", DateTime.Now, log.ToString());
            if ((outtype & FILE) > 0)
            {
                rwlock.EnterWriteLock();
                try
                {
                    File.AppendAllText(LOG_FILE_NAME, string.Format("[D][{0}] {1}\n", DateTime.Now, log.ToString()));
                }
                finally
                {
                    rwlock.ExitWriteLock();
                }
            }
        }

        public static void ErrorFormat(string format, params object[] args)
        {
            if (!debug) return;
            Error(string.Format(format, args));
        }

        public static void ClearLogFile()
        {
            if (File.Exists(LOG_FILE_NAME)) File.Delete(LOG_FILE_NAME);
        }
    }
}
