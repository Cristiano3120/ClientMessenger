﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ClientMessenger.Logging
{
    internal static partial class Logger
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [GeneratedRegex("(\"ProfilePicture\": \")[^\"]*(\")")]
        private static partial Regex FilterProfilPicRegex();

        static Logger()
        {
            AllocConsole();
        }

        #region LogInformation

        public static void LogInformation(ConsoleColor color, params string[] logs)
        {
            Console.ForegroundColor = color;
            LogInformationHelper(logs);
        }

        public static void LogInformation(params string[] logs)
        {
            Console.ForegroundColor = ConsoleColor.White;
            LogInformationHelper(logs);
        }

        private static void LogInformationHelper(params string[] logs)
        {
            for (int i = 0; i < logs.Length; i++)
            {
                string message = FilterProfilPicRegex().Replace(logs[i], "$1[Image]$2");
                Console.WriteLine($"[{DateTime.Now}]: {message}");
            }
        }

        #endregion

        /// <summary>
        /// Outputs the wanted logs as yellow text in the console
        /// </summary>
        public static void LogWarning(params string[] logs)
        {
            Log(ConsoleColor.Yellow, logs);
        }

        /// <summary>
        /// Logs the error in red in the console with the error message and the file, method, line and column where the error occured
        /// </summary>
        /// <typeparam name="T">Has to be of type <c>EXCEPTION</c>, <c>UnobservedTaskExceptionEventArgs</c>, <c>NpgsqlException</c> <c>string</c> </typeparam>
        /// <exception cref="ArgumentException"></exception>
        public static void LogError<T>(T exception)
        {
            if (exception is string str)
            {
                Log(ConsoleColor.Red, str);
                return;
            }

            if (exception is UnobservedTaskExceptionEventArgs unobservedEx)
            {
                foreach (Exception innerEx in unobservedEx.Exception.Flatten().InnerExceptions)
                {
                    LogError(innerEx);
                }
                return;
            }

            var ex = exception as Exception
                ?? throw new ArgumentException($"Type {typeof(T).Name} must be of type EXCEPTION, UnobservedTaskExceptionEventArgs, NpgsqlExceptin or string.");

            StackTrace stackTrace = new(ex, true);
            StackFrame? stackFrame = null;
            foreach (StackFrame item in stackTrace.GetFrames())
            {
                //Looking for the frame contains the infos about the error
                if (item.GetMethod()?.Name != null && item.GetFileName() != null)
                {
                    stackFrame = item;
                    break;
                }
            }

            if (stackFrame != null)
            {
                var methodName = stackFrame?.GetMethod()?.Name + "()";
                var filename = stackFrame?.GetFileName() ?? "missing filename";
                var lineNum = stackFrame?.GetFileLineNumber();
                var columnNum = stackFrame?.GetFileColumnNumber();

                var index = filename.LastIndexOf(@"\") + 1;
                filename = filename.Remove(0, index);

                var errorInfos = $"ERROR in file {filename}, in {methodName}, at line: {lineNum}, at column: {columnNum}";
                Log(ConsoleColor.Red, errorInfos);
            }

            Log(ConsoleColor.Red, $"ERROR: {ex.Message}");
        }

        private static void Log(ConsoleColor color, params string[] logs)
        {
            Console.ForegroundColor = color;
            foreach (var log in logs)
            {
                Console.WriteLine($"{DateTime.Now:HH: dd: ss} {log}");
            }
            Console.WriteLine("");
        }
    }
}
