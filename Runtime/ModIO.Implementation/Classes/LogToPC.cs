using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static ModIO.Utility;

namespace ModIO.Implementation
{

    internal class LogToPC
    {
        private class LogMessage
        {
            public LogLevel level;
            public string message;
        }

        public const string SessionIdentifier = "Session_Log_";
        public const string fileEnding = ".txt";
        public const string dateTimeFormat = "yyyy'-'MM'-'dd'T'HH'-'mm'-'ss";

        private const int MaxLogs = 100;

        private string filenameHandle;        
        private List<LogMessage> messageCache = new List<LogMessage>();
        
        public bool halt = false;

        public LogToPC() => Dispatcher.Instance.Run(() => Setup(DateTime.Now));
        public LogToPC(DateTime time) => Dispatcher.Instance.Run(() => Setup(time));

        private void Setup(DateTime date)
        {            
            string folderPath = GetFolderPath();
            AttemptCreateDirectory(folderPath);

            IEnumerable<string> oldLogs = GetOldLogs(MaxLogs, Directory.GetFiles(folderPath));
            ClearFiles(oldLogs);

            filenameHandle = ConstructFilePath(folderPath, date);

            //we add this, but we don't write it until we actually have to, to avoid unnecessary log files
            messageCache.Add(new LogMessage()
            {
                level = LogLevel.Message,
                message = $"\n\n\n------ New Log for [{DateTime.Now.ToString(dateTimeFormat)}] ------\n\n"
            });
        }

        public static IEnumerable<string> GetOldLogs(int maxLogs, params string[] files)
        {
            return files.Where(x => x.StartsWith(SessionIdentifier) && x.EndsWith(fileEnding)) 
                .OrderByDescending(x => x)
                .ToList()
                .Skip(maxLogs);
        }

        private static void ClearFiles(IEnumerable<string> files)
        {
            if(files == null || files.Count() == 0)
            {
                return;
            }

            foreach(string item in files)
            {
                try
                {
                    File.Delete(item);
                }
                catch(Exception) { } //if this can't happen it's because someone might be reading the log,
                                     //or in some other way holding its io ref - this is acceptable
                                     //log file would just be cleared out later
            }
        }

        public static string ConstructFilePath(string folderPath, DateTime time)
            => folderPath + @"/" + $"{SessionIdentifier}{time.ToString(dateTimeFormat)}{fileEnding}";

        private static void AttemptCreateDirectory(string path)
            => Directory.CreateDirectory(path);

        public static string GetFolderPath()
            => Application.persistentDataPath + @"/ModIoLogs";

        private void WriteMessagesToLog()
        {
            try
            {
                using(StreamWriter w = File.AppendText(filenameHandle))
                {
                    int index = 0;

                    while(index < messageCache.Count)
                    {
                        if(halt)
                            return;
            
                        string log = $"{messageCache[index].level} - {DateTime.Now.ToString("HH:mm:ss")}: {messageCache[index].message}";
                        w.WriteLine(log);
                        index++;
                    }
                    
                    messageCache.Clear();
                }
            }
            catch(Exception ex)
            {
                Debug.LogError("Unable to write message: " + ex);
            }
        }


        public void Log(LogLevel level, string logMessage)
        {
            if(halt)
                return;

            if(logMessage == null || logMessage == string.Empty)
                return;
            
            messageCache.Add(new LogMessage() { level = level, message = logMessage });
            Dispatcher.Instance.Run(WriteMessagesToLog);
        }
    }
}
