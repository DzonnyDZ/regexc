using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Dzonny.RegexCompiler
{
    class Program
    {
        private enum ParamStates
        {
            Files,
            AssemblyName,
            Version
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Argument can be filenames containing regexes to compile or special arguments:");
                Console.WriteLine("/assembly {name} - Name of assembly");
                Console.WriteLine("/ver {version} - Assembly version");
                Console.WriteLine("/nop - Just compile the regexes, don't add properties for named groups");
            }

            string assembly;
            bool postProcess;
            List<string> files;
            Version version;
            ParseCommandLine(args, out files, out assembly, out version, out postProcess);

            new RegexCompiler().Compile(files, assembly, version, postProcess);
        }
        private static void ParseCommandLine(string[] args, out List<string> files, out string assembly, out Version version, out bool postProcess)
        {
            ParamStates state = ParamStates.Files;
            assembly = null;
            postProcess = true;
            files = new List<string>();
            version = null;
            foreach (string arg in args)
            {
                switch (state)
                {
                    case ParamStates.Files:
                        switch (arg)
                        {
                            case "/assembly": state = ParamStates.AssemblyName; break;
                            case "/nop": postProcess = false; break;
                            case "/ver": state = ParamStates.Version; break;
                            default: files.Add(arg); break;
                        }
                        break;
                    case ParamStates.AssemblyName:
                        if (assembly != null) throw new ArgumentException("Assembly name specified twice", "/assembly");
                        assembly = arg;
                        state = ParamStates.Files;
                        break;
                    case ParamStates.Version:
                        if (version != null) throw new ArgumentException("Version specified twice", "/ver");
                        version = Version.Parse(arg);
                        state = ParamStates.Files;
                        break;
                }
            }
        }
    }
}
