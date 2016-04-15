using System;

namespace Dzonny.RegexCompiler.Compilation
{
    /// <summary>Regex compiler message severity</summary>
    public enum RegexCompilerMessageSeverity
    {
        /// <summary>Just an information</summary>
        Info,
        /// <summary>Warning - compilation continues</summary>
        Warning,
        /// <summary>Error - compilation interrupted</summary>
        Error
    }

    /// <summary>Interface of an object that receives and processes compiler messages</summary>
    public interface IRegexCompilerMessageSink
    {
        /// <summary>Receives and processes the compiler message</summary>
        /// <param name="severity">Message severity level</param>
        /// <param name="code">Identifies the error, warning or info by code</param>
        /// <param name="text">Message text</param>
        /// <param name="fileName">Optional: Name of path of file where the error happened (null when unknown)</param>
        /// <param name="line">Optional: 1-based line number where the error happened (0 when unknown)</param>
        /// <param name="column">Optional: 1-based column number where the error happened (0 when unknown)</param>
        void Report(RegexCompilerMessageSeverity severity, RegexCompilerErrorCodes code, string text, string fileName, int line, int column);

        /// <summary>Gets total number of errors (<see cref="RegexCompilerMessageSeverity.Error"/>) passed to <see cref="Report"/></summary>
        int ErrorCount { get; }
        /// <summary>Gets total number of warnings (<see cref="RegexCompilerMessageSeverity.Warning"/>) passed to <see cref="Report"/></summary>
        int WarningCount { get; }
    }

    /// <summary>Implements <see cref="IRegexCompilerMessageSink"/> using a <see cref="IRegexCompilerMessageSink.Report"/>-compatible delegate</summary>
    public class DelegateCompilerMessageSink : IRegexCompilerMessageSink
    {
        /// <summary>The implementing delegate</summary>
        private readonly Action<RegexCompilerMessageSeverity, string, string, int, int> @delegate;

        /// <summary>CTor - creates a new instance of the <see cref="DelegateCompilerMessageSink"/> class</summary>
        /// <param name="delegate">A delegate to implement <see cref="Report"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="delegate"/> is null</exception>
        public DelegateCompilerMessageSink(Action<RegexCompilerMessageSeverity, string, string, int, int> @delegate)
        {
            if (@delegate == null) throw new ArgumentNullException(nameof(@delegate));
            this.@delegate = @delegate;
        }

        /// <summary>Receives and processes the compiler message</summary>
        /// <param name="severity">Message severity level</param>
        /// <param name="code">Identifies the error, warning or info by code</param>
        /// <param name="text">Message text</param>
        /// <param name="fileName">Optional: Name of path of file where the error happened (null when unknown)</param>
        /// <param name="line">Optional: 1-based line number where the error happened (0 when unknown)</param>
        /// <param name="column">Optional: 1-based column number where the error happened (0 when unknown)</param>
        public void Report(RegexCompilerMessageSeverity severity, RegexCompilerErrorCodes code , string text, string fileName, int line, int column)
        {
            switch (severity)
            {
                case RegexCompilerMessageSeverity.Error: ErrorCount++; break;
                case RegexCompilerMessageSeverity.Warning: WarningCount++; break;
            }
            @delegate(severity, text, fileName, line, column);
        }

        /// <summary>Gets total number of errors (<see cref="RegexCompilerMessageSeverity.Error"/>) passed to <see cref="IRegexCompilerMessageSink.Report"/></summary>
        public int ErrorCount { get; private set; }

        /// <summary>Gets total number of warnings (<see cref="RegexCompilerMessageSeverity.Warning"/>) passed to <see cref="IRegexCompilerMessageSink.Report"/></summary>
        public int WarningCount { get; private set; }
    }    
}