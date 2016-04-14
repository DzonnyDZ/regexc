using System;

namespace Dzonny.RegexCompiler.Compilation
{
    /// <summary>Logs compiler messages to console</summary>
    internal class ConsoleMessageSink : IRegexCompilerMessageSink
    {
        /// <summary>Gets total number of errors (<see cref="RegexCompilerMessageSeverity.Error"/>) passed to <see cref="IRegexCompilerMessageSink.Report"/></summary>
        public int ErrorCount { get; private set; }

        /// <summary>Gets total number of warnings (<see cref="RegexCompilerMessageSeverity.Warning"/>) passed to <see cref="IRegexCompilerMessageSink.Report"/></summary>
        public int WarningCount { get; private set; }


        /// <summary>Receives and processes the compiler message</summary>
        /// <param name="severity">Message severity level</param>
        /// <param name="text">Message text</param>
        /// <param name="fileName">Optional: Name of path of file where the error happened (null when unknown)</param>
        /// <param name="line">Optional: 1-based line number where the error happened (0 when unknown)</param>
        /// <param name="column">Optional: 1-based column number where the error happened (0 when unknown)</param>
        public void Report(RegexCompilerMessageSeverity severity, string text, string fileName, int line, int column)
        {
            Action<string> log;
            if (severity == RegexCompilerMessageSeverity.Error)
                log = Console.Error.WriteLine;
            else
                log = Console.WriteLine;
            ConsoleColor oldc = Console.ForegroundColor;
            switch (severity)
            {
                case RegexCompilerMessageSeverity.Warning:
                    WarningCount++;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case RegexCompilerMessageSeverity.Error:
                    ErrorCount++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            try
            {
                log(
                    (fileName == null ? null : (fileName + ":")) +
                    (line > 0 ? line.ToString() : null) +
                    (column > 0 ? (line > 0 ? "," : null) + column.ToString() : null) +
                    ((column > 0 || line > 0) ? ": " : (fileName == null ? null : " ")) +
                    severity.ToString() + ": " + text
                    );
            }
            finally
            {
                if (Console.ForegroundColor != oldc)
                    Console.ForegroundColor = oldc;
            }
        }
    }
}