using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ClientMessenger
{
    internal static partial class Logger
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AllocConsole();

        [GeneratedRegex("(\"(?:profilePicture|newProfilePicture)\": \")[^\"]*(\")")]
        private static partial Regex FilterProfilPicRegex();

        [GeneratedRegex(@"\[[^\]]*\]")]
        private static partial Regex FilterKeywords();

        private static readonly string _pathToLogFile;
        private static readonly Lock _lock = new();

        static Logger()
        {
            AllocConsole();
            _pathToLogFile = MaintainLoggingSystem(maxAmmountLoggingFiles: 5);
        }

        private static string MaintainLoggingSystem(int maxAmmountLoggingFiles)
        {
            string pathToLoggingDic = Client.GetDynamicPath(@"Logging/");
            if (!Directory.Exists(pathToLoggingDic))
            {
                Directory.CreateDirectory(pathToLoggingDic);
            }
            else
            {
                string[] files = Directory.GetFiles(pathToLoggingDic, "*.md");

                if (files.Length >= maxAmmountLoggingFiles)
                {
                    files = [.. files.OrderBy(File.GetCreationTime)];
                    // +1 to make room for a new File
                    int filesToRemove = files.Length - maxAmmountLoggingFiles + 1;

                    for (int i = 0; i < filesToRemove; i++)
                    {
                        File.Delete(files[i]);
                    }
                }
            }

            string timestamp = DateTime.Now.ToString("dd-MM-yyyy/HH-mm-ss");
            string pathToNewFile = Client.GetDynamicPath($"Logging/{timestamp}.md");
            File.Create(pathToNewFile).Close();

            return pathToNewFile;
        }

        #region LogInformation

        public static void LogInformation(ConsoleColor color, params string[] logs)
        {
            Console.ForegroundColor = color;
            Log(color, true, logs);
        }

        public static void LogInformation(params string[] logs)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Log(ConsoleColor.White, true, logs);
        }

        public static void LogInformation(ConsoleColor color, string log, bool makeLineAfter = true)
        {
            Log(color, makeLineAfter, log);
        }

        #endregion

        /// <summary>
        /// Outputs the wanted logs as yellow text in the console
        /// </summary>
        public static void LogWarning(params string[] logs)
        {
            Log(ConsoleColor.Yellow, true, logs);
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
                Log(ConsoleColor.Red, true, str);
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

            Exception ex = exception as Exception
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
                string methodName = stackFrame?.GetMethod()?.Name + "()";
                string filename = stackFrame?.GetFileName() ?? "missing filename";
                int? lineNum = stackFrame?.GetFileLineNumber();
                int? columnNum = stackFrame?.GetFileColumnNumber();

                int index = filename.LastIndexOf('\\') + 1;
                filename = filename[index..];

                string errorInfos = $"ERROR in file {filename}, in {methodName}, at line: {lineNum}, at column: {columnNum}";
                Log(ConsoleColor.Red, false, errorInfos);
            }

            Log(ConsoleColor.Red, ex.InnerException == null, $"ERROR: {ex.Message}");

            if (ex.InnerException != null)
                LogError(ex.InnerException);
        }

        public static void LogPayload(ConsoleColor color, string payload, string prefix)
        {
            lock (_lock)
            {
                using StreamWriter streamWriter = new(_pathToLogFile, true);
                Console.ForegroundColor = color;


                string message = FilterProfilPicRegex().Replace(payload, "$1[Image]$2");
                JsonNode jsonNode = JsonNode.Parse(message)!;
                jsonNode["opCode"] = Enum.Parse<OpCode>(jsonNode["opCode"]!.ToString()).ToString();

                Console.WriteLine($"[{DateTime.Now:HH: mm: ss}]: {prefix} {jsonNode}");
                streamWriter.WriteLine($"{prefix} {FilterKeywords().Replace(jsonNode.ToString(), match => $"**{match.Value}**")}");

                streamWriter.WriteLine("");
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// The method that filters the logs and writes them into the Console
        /// </summary>
        private static void Log(ConsoleColor color, bool makeLineAfter, params string[] logs)
        {
            lock (_lock)
            {
                using StreamWriter streamWriter = new(_pathToLogFile, true);
                Console.ForegroundColor = color;

                for (int i = 0; i < logs.Length; i++)
                {
                    string message = FilterProfilPicRegex().Replace(logs[i], "$1[Image]$2");
                    Console.WriteLine($"[{DateTime.Now:HH: mm: ss}]: {message}");
                    streamWriter.WriteLine($"[{DateTime.Now:HH: mm: ss}]:");
                    streamWriter.WriteLine(FilterKeywords().Replace(message, match => $"**{match.Value}**"));
                }

                if (makeLineAfter)
                {
                    streamWriter.WriteLine("");
                    Console.WriteLine("");
                }
            }
        }
    }
}
