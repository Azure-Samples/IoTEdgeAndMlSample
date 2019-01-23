//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

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