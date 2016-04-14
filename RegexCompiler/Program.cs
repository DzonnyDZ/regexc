using System;
using Dzonny.RegexCompiler.Compilation;

namespace Dzonny.RegexCompiler
{
    /// <summary>Entry point of regexc application</summary>
    internal class RegexC
    {

        /// <summary>Entry point method</summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Argument can be filenames containing regexes to compile or special arguments:");
                Console.WriteLine("/assembly {name} - Name of assembly");
                Console.WriteLine("/ver {version} - Assembly version");
                Console.WriteLine("/nop - Just compile the regexes, don't add properties for named groups");
                Environment.Exit(1);
            }

            RegexCompilationSettings settings;
            try
            {
                settings = ParseCommandLine(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(2);
                return;
            }

            var compiler = new Compilation.RegexCompiler(settings);
            compiler.Compile();
            Console.WriteLine($"{settings.MessageSink.ErrorCount} errors, {settings.MessageSink.WarningCount} warnings");
            if (settings.MessageSink.ErrorCount > 0) Environment.Exit(3);
        }

        /// <summary>Reads command line arguments</summary>
        /// <summary>States of FSA reading command line parameters</summary>
        private enum ParamStates
        {
            /// <summary>Expects file or parameter</summary>
            Files,
            /// <summary>Value for /assembly parameter</summary>
            AssemblyName,
            /// <summary>Value for /ver parameter</summary>
            Version
        }

        /// <param name="args">Command line arguments</param>
        /// <returns>Compilation setup</returns>
        /// <exception cref="ArgumentException">
        /// A command line argument which cannot be repeated is specified more than once -or-
        /// No files are specified.
        /// </exception>
        private static RegexCompilationSettings ParseCommandLine(string[] args)
        {
            var ret = new RegexCompilationSettings();
            bool assemblyNameRead = false;
            bool versionRead = false;
            ParamStates state = ParamStates.Files;
            foreach (string arg in args)
            {
                switch (state)
                {
                    case ParamStates.Files:
                        switch (arg)
                        {
                            case "/assembly": state = ParamStates.AssemblyName; break;
                            case "/nop": ret.PostProcess = false; break;
                            case "/ver": state = ParamStates.Version; break;
                            default: ret.Files.Add(arg); break;
                        }
                        break;
                    case ParamStates.AssemblyName:
                        if (assemblyNameRead) throw new ArgumentException("Assembly name specified twice", "/assembly");
                        assemblyNameRead = true;
                        ret.AssemblyName = arg;
                        state = ParamStates.Files;
                        break;
                    case ParamStates.Version:
                        if (versionRead) throw new ArgumentException("Version specified twice", "/ver");
                        versionRead = true;
                        ret.Version = Version.Parse(arg);
                        state = ParamStates.Files;
                        break;
                }
            }
            if (ret.Files.Count == 0) throw new ArgumentException("No files specified");
            return ret;
        }
    }
}