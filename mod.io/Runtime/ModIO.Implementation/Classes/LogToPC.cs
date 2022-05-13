using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ModIO.Implementation
{
    internal class LogToPC
    {
        private string filenameHandle;
        private const int MaxLogs = 100;
        public const string SessionIdentifier = "Session_Log_";
        public const string fileEnding = ".txt";
        public const string dateTimeFormat = "yyyy'-'MM'-'dd'T'HH'-'mm'-'ss";
        public bool halt = false;

        public LogToPC()
        {
            Setup(DateTime.Now);
        }

        public LogToPC(DateTime date)
        {
            Setup(date);
        }

        private void Setup(DateTime date)
        {
            string folderPath = GetFolderPath();
            AttemptCreateDirectory(folderPath);

            IEnumerable<string> oldLogs = GetOldLogs(MaxLogs, Directory.GetFiles(folderPath));
            ClearFiles(oldLogs);

            filenameHandle = ConstructFilePath(folderPath, date);

            Log(LogLevel.Message, $"\n\n\n------ New Log for [{DateTime.Now.ToString(dateTimeFormat)}] ------\n\n");
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
        {            
            return folderPath
                + @"/" + $"{SessionIdentifier}{time.ToString(dateTimeFormat)}{fileEnding}";
        }

        private static void AttemptCreateDirectory(string path)
        {             
            Directory.CreateDirectory(path);
        }

        public static string GetFolderPath()
        {
            return Application.persistentDataPath + @"/ModIoLogs";
        }

        public void Log(LogLevel level, string logMessage)
        {
            if(halt)
            {
                return;
            }

            try
            {
                using(StreamWriter w = File.AppendText(filenameHandle))
                {
                    string log = $"{level} - {DateTime.Now.ToString("HH:mm:ss")}: {logMessage}";
                    w.WriteLine(log);
                }

            }
            catch(Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Exception writing log to PC. Halting log to pc functionality for this session. Exception: {ex}", false);
                halt = true;
            }
        }
    }
}
