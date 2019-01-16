using System;

namespace turbofanRouter
{
    /// <summary>
    /// This class is a stand in for a more robust logging system
    /// </summary>
    public class Logger
    {
        public static LogSeverity LoggingLevel = LogSeverity.Verbose;

        public static void Log(string message, LogSeverity severity = LogSeverity.Verbose)
        {
            if (severity <= Logger.LoggingLevel)
            {
                Console.WriteLine(message);
            }
        }
    }

    public enum LogSeverity { Error, Warning, Verbose }
}