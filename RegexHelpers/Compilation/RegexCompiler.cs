using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using IO = System.IO;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

// ReSharper disable InconsistentNaming

namespace Dzonny.RegexCompiler.Compilation
{
    /// <summary>Regular expression compiler</summary>
    public class RegexCompiler
    {
        /// <summary>Gets copy of <see cref="RegexCompilationSettings"/> passed to <see cref=".ctor(RegexCompilationSettings)">constructor</see></summary>
        protected RegexCompilationSettings Settings { get; }
        /// <summary>CTor - creates a new instance of the <see cref="RegexCompiler"/> class</summary>
        /// <param name="settings">Indicates which files to compile and compilation options</param>
        /// <remarks><paramref name="settings"/>.<see cref="RegexCompilationSettings.Files">Files</see> can be empty, but then you cannot use <see cref="Compile()"/> method.</remarks>
        public RegexCompiler(RegexCompilationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            this.Settings = settings.Clone();
        }

        /// <summary>Compiles regular expressions from files provided in <see cref=".ctor(RegexCompilationSettings)">constructor</see></summary>
        /// <exception cref="InvalidOperationException"><see cref="RegexCompilationSettings.Files"/> passed to <see cref=".ctor(RegexCompilationSettings)">constructor</see> don't contain any files</exception>
        public void Compile()
        {
            if (Settings.Files.Count == 0) throw new InvalidOperationException("No files specified in compilation settings");
            Compile(new Tuple<IO.TextReader, string>[] { }, false);
        }

        /// <summary>Creates <see cref="AssemblyName"/> from <see cref="Settings"/></summary>
        private AssemblyName GetAssemblyName() => new AssemblyName(Settings.AssemblyName) { Version = Settings.Version };

        #region Compile
        /// <summary>Compiles regular expressions from files</summary>
        /// <param name="files">Files containing regular expression definitions. Each file can contain multiple regular expressions. Files must be in defined format.</param>
        /// <exception cref="ArgumentNullException"><paramref name="files"/> is null</exception>
        /// <remarks><see cref="RegexCompilationSettings.Files"/> and <paramref name="files"/> are concatenated</remarks>
        public void Compile(params string[] files) => Compile((IEnumerable<string>)files);

        /// <summary>Compiles regular expressions from files</summary>
        /// <param name="files">Files containing regular expression definitions. Each file can contain multiple regular expressions. Files must be in defined format.</param>
        /// <exception cref="ArgumentNullException"><paramref name="files"/> is null</exception>
        /// <remarks><see cref="RegexCompilationSettings.Files"/> and <paramref name="files"/> are concatenated</remarks>
        public void Compile(IEnumerable<string> files)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
#pragma warning disable CC0022 // Should dispose object
            Compile(
                from f in files
                select Tuple.Create((IO.TextReader)new IO.StreamReader(IO.File.Open(f, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)), f),
                true
            );
#pragma warning restore CC0022 // Should dispose object
        }

        /// <summary>Compiles regular expressions from text readers</summary>
        /// <param name="readers">Readers reading content of files containing regular expression definitions. Each reader can read multiple regular expressions. Text must be in defined format.</param>
        /// <remarks><see cref="RegexCompilationSettings.Files"/> and <paramref name="readers"/> are concatenated</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="readers"/> is null</exception>
        public void Compile(params IO.TextReader[] readers) => Compile((IEnumerable<IO.TextReader>)readers);

        /// <summary>Compiles regular expressions from text readers</summary>
        /// <param name="readers">Readers reading content of files containing regular expression definitions. Each reader can read multiple regular expressions. Text must be in defined format.</param>
        /// <remarks>
        /// <see cref="RegexCompilationSettings.Files"/> and <paramref name="readers"/> are concatenated.
        /// <para>Readers from <paramref name="readers"/> won't be disposed. It's caller's responsibility to dispose them.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="readers"/> is null</exception>
        public void Compile(IEnumerable<IO.TextReader> readers)
        {
            if (readers == null) throw new ArgumentNullException(nameof(readers));
            Compile(from r in readers select Tuple.Create(r, default(string)), false);
        }

        /// <summary>Compiles regular expressions from text readers with provided file names</summary>
        /// <param name="sources">Contains text readers and file names the readers are reading. File names can be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sources"/> is null</exception>
        public void Compile(IEnumerable<Tuple<IO.TextReader, string>> sources) => Compile(sources, false);

        /// <summary>Compiles regular expressions from text readers with provided file names</summary>
        /// <param name="sources">Contains text readers and file names the readers are reading. File names can be null.</param>
        /// <param name="disposeReaders">
        /// True to dispose readers after they are read. False to leave them open.
        /// When true only create the readers in <see cref="IEnumerator.MoveNext"/> (i.e. in <see cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>).
        /// Readers that were not encountered won't be disposed!
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="sources"/> is null</exception>
        protected virtual void Compile(IEnumerable<Tuple<IO.TextReader, string>> sources, bool disposeReaders)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            var regexes = new List<RegexCompilationInfo>();
#pragma warning disable CC0022 // Should dispose object
            var srcs = (from f in Settings.Files select Tuple.Create((IO.TextReader)new IO.StreamReader(IO.File.Open(f, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)), f, true))
                       .Concat(
                        from x in sources select Tuple.Create(x.Item1, x.Item2, disposeReaders)
                       );
#pragma warning restore CC0022 // Should dispose object

            foreach (var source in srcs)
            {
                try
                {
                    regexes.AddRange(Read(source.Item1, source.Item2));
                }
                finally
                {
                    if (source.Item3) source.Item1.Dispose();
                }
            }
            if (Settings.MessageSink.ErrorCount == 0)
                Compile(regexes);
        }

        /// <summary>Compiles regular expressions from <see cref="RegexCompilationInfo"/>s</summary>
        /// <param name="regexes"><see cref="RegexCompilationInfo"/>s specifying the regular expressions to compile.</param>
        /// <exception cref="ArgumentNullException"><paramref name="regexes"/>is null</exception>
        public virtual void Compile(IEnumerable<RegexCompilationInfo> regexes)
        {
            if (regexes == null) throw new ArgumentNullException(nameof(regexes));
            var an = GetAssemblyName();
            var arr = regexes.ToArray();
            var objDir = IO.Directory.CreateDirectory(Settings.ObjDir ?? IO.Path.Combine(IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
            try
            {
                var oldcd = Environment.CurrentDirectory;
                string assemblyPath;
                try
                {
                    Environment.CurrentDirectory = objDir.FullName;
                    Regex.CompileToAssembly(arr, an); //IMPORTANT line
                    assemblyPath = IO.Path.Combine(objDir.FullName, an.Name + ".dll");
                }
                finally
                {
                    Environment.CurrentDirectory = oldcd;
                }
                if (Settings.PostProcess)
                    PostProcess(assemblyPath, arr);
                else if (Settings.Snk != null)
                    SignAssembly(assemblyPath);
                if (Settings.Output == null)
                    IO.File.Copy(assemblyPath, IO.Path.GetFileName(assemblyPath), true);
                else
                    IO.File.Copy(assemblyPath, Settings.Output, true);
                IO.File.Delete(assemblyPath);
            }
            finally
            {
                if (Settings.ObjDir == null)
                    objDir.Delete();
            }
        }
        #endregion

        /// <summary>Reads regular expression definitions from text reader</summary>
        /// <param name="reader">Reader to read the definitions from</param>
        /// <param name="path">Path of file reader was opened from (optional)</param>
        /// <returns>Regular expression definitions read from <paramref name="reader"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null</exception>
        protected virtual IEnumerable<RegexCompilationInfo> Read(IO.TextReader reader, string path = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var regexes = new List<RegexCompilationInfo>();
            int fullLineNumber = 0;
            int blockLineNumber = 0;
            bool keepWhite = false;
            string name = null;
            RegexOptions options = RegexOptions.None;
            StringBuilder regex = new StringBuilder();
            TimeSpan? timeout = null;
            bool @public = true;

            using (var r = new UnicodeNewlineTextReader(reader))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    string trimmedLine = line.Trim();
                    try
                    {
                        if (trimmedLine.StartsWith("#") || trimmedLine == string.Empty) continue;
                        switch (blockLineNumber)
                        {
                            case 0:
                                if (!trimmedLine.StartsWith("Name:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    Settings.MessageSink.Report(RegexCompilerMessageSeverity.Error, RegexCompilerErrorCodes.RXC001, "1st line of regex block must start with 'Name:'", path, fullLineNumber + 1, 1);
                                    return regexes;
                                }
                                name = line.Substring(5).Trim();
                                break;
                            case 1:
                            case 2:
                                if (trimmedLine.StartsWith("Options:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] parts = line.Substring(8).Trim().Split(',');
                                    var optionsParts = (from p in parts
                                                        where !StringComparer.InvariantCultureIgnoreCase.Equals(p.Trim(), "Public") &&
                                                              !StringComparer.InvariantCultureIgnoreCase.Equals(p.Trim(), "Private") &&
                                                              !StringComparer.InvariantCultureIgnoreCase.Equals(p.Trim(), "KeepWhite")
                                                        select p
                                        ).ToArray();
                                    if (optionsParts.Length > 0)
                                    {
                                        if (!Enum.TryParse(string.Join(",", optionsParts), true, out options))
                                        {
                                            Settings.MessageSink.Report(RegexCompilerMessageSeverity.Error, RegexCompilerErrorCodes.RXC002, $"Invalid regex options {line.Substring(8).Trim()}", path, fullLineNumber + 1, 1);
                                            options = RegexOptions.None; //It's error, but we can actually continue
                                        }
                                    }
                                    else
                                    {
                                        options = RegexOptions.None;
                                    }
                                    if (parts.Contains("Private", StringComparer.InvariantCultureIgnoreCase)) @public = false;
                                    if (parts.Contains("KeepWhite", StringComparer.InvariantCultureIgnoreCase)) keepWhite = true;
                                }
                                else if (trimmedLine.StartsWith("Timeout:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    TimeSpan to;
                                    if (TimeSpan.TryParse(line.Substring(8).Trim(), CultureInfo.InvariantCulture, out to))
                                    {
                                        timeout = to;
                                    }
                                    else
                                    {
                                        Settings.MessageSink.Report(RegexCompilerMessageSeverity.Error, RegexCompilerErrorCodes.RXC003, $"Invalid timeout specified {line.Substring(8).Trim()}", path, fullLineNumber + 1, 1);
                                        timeout = null; //It's error, but we can actually continue
                                    }
                                }
                                else
                                    goto default;
                                break;
                            default:
                                if (trimmedLine.StartsWith("------")) //6×
                                {
                                    try
                                    {
                                        regexes.Add(BuildRegex(regex.ToString(), name, options, timeout, @public));
                                    }
                                    catch (Exception ex)
                                    {
                                        Settings.MessageSink.Report(RegexCompilerMessageSeverity.Error, RegexCompilerErrorCodes.RXC004, ex.Message, path, fullLineNumber + 1, 1);
                                    }
                                    blockLineNumber = 0;
                                    regex.Clear();
                                    options = RegexOptions.None;
                                    timeout = null;
                                    @public = true;
                                    name = null;
                                    continue;
                                }
                                if (keepWhite)
                                    regex.Append(line + r.LastNewLine);
                                else
                                    regex.Append(trimmedLine);
                                break;
                        }
                    }
                    finally
                    {
                        fullLineNumber++;
                    }
                    blockLineNumber++;
                }
            }

            if (blockLineNumber == 1 || (blockLineNumber > 1 && regex.Length == 0))
            {
                Settings.MessageSink.Report(RegexCompilerMessageSeverity.Error, RegexCompilerErrorCodes.RXC005, "Unexpected end of file", path, fullLineNumber + 1, 0);
                return regexes;
            }
            if (blockLineNumber > 1)
                regexes.Add(BuildRegex(regex.ToString(), name, options, timeout, @public));
            return regexes;
        }

        /// <summary>Creates <see cref="RegexCompilationInfo"/></summary>
        /// <param name="regexText">Regular expression pattern</param>
        /// <param name="name">Name of the regular expression</param>
        /// <param name="options">Regex options</param>
        /// <param name="timeout">Execution timeout</param>
        /// <param name="public">Is the regex public (internal/assembly when false)</param>
        /// <returns><see cref="RegexCompilationInfo"/> created from given parameters</returns>
        protected virtual RegexCompilationInfo BuildRegex(string regexText, string name, RegexOptions options, TimeSpan? timeout, bool @public)
        {
            string @namespace = null;
            string simpleName = name.Contains('.') ? name.Substring(name.LastIndexOf('.') + 1) : name;
            if (name.Contains('.')) @namespace = name.Substring(0, name.LastIndexOf('.'));

            var i = new RegexCompilationInfo(regexText, options, simpleName, @namespace ?? "", @public);
            if (timeout.HasValue) i.MatchTimeout = timeout.Value;

            return i;
        }

        private TypeReference trGroup;
        private TypeReference trRegex;
        private TypeReference trMatchBase;
        private TypeReference trRegexBase;
        private TypeReference trMatch;
        private TypeReference trString;
        private TypeReference trVoid;
        private TypeReference trInt32;
        private TypeReference trNonNamedMatch;
        private TypeReference trIEnumerable;
        private TypeReference trIEnumerator;
        private TypeReference trList1;
        private TypeReference trList_NonNamedMatch;
        private TypeReference trIReadOnlyCollection1;
        private TypeReference trIReadOnlyCollection_NonNamedMatch;
        private TypeReference trSecurityRuleSet;

        private MethodReference getMatch_Groups;
        MethodReference getMatchBase_Groups;
        MethodReference getMatchBase_RegexMatch;
        MethodReference getGroupCollection_Item;
        MethodReference getIEnumerator_Current;
        MethodReference ctorMatchBase;
        MethodReference ctorNonNamedMatch;
        MethodReference ctorList_NonNamedMatch;
        MethodReference ctorSecurityRulesAttributes;
        MethodReference ctorRegex;
        MethodReference ctorRegexBase;
        MethodReference mtdRegex_Match_string;
        MethodReference mtdRegex_Match_string_int;
        MethodReference mtdRegex_Match_string_int_int;
        MethodReference mtdRegex_Matches_string;
        MethodReference mtdRegex_Matches_string_int;
        MethodReference mtdIEnumerable_GetEnumerator;
        MethodReference mtdIEnumerator_MoveNext;
        MethodReference mtdList_NonNamedMatch_Add;
        private MethodReference ctorRegexResolved;

        //Changed in foreach
        TypeReference trIReadOnlyCollection_MatchClass;
        TypeReference trList_MatchClass;
        TypeReference trMatchClass;
        MethodReference ctorList_MatchClass;
        MethodReference ctorMatchClass;
        MethodReference mtdList_MatchClass_Add;

        private void InitializeMetadataReferences(AssemblyDefinition asm)
        {
            trGroup = asm.MainModule.Import(typeof(Group));
            trRegex = asm.MainModule.Import(typeof(Regex));
            trMatchBase = asm.MainModule.Import(typeof(MatchBase));
            trRegexBase = asm.MainModule.Import(typeof(RegexBase));
            trMatch = asm.MainModule.Import(typeof(Match));
            trString = asm.MainModule.Import(typeof(String));
            trVoid = asm.MainModule.Import(typeof(void));
            trInt32 = asm.MainModule.Import(typeof(Int32));
            trNonNamedMatch = asm.MainModule.Import(typeof(NonNamedMatch));
            trIEnumerable = asm.MainModule.Import(typeof(IEnumerable));
            trIEnumerator = asm.MainModule.Import(typeof(IEnumerator));
            trList1 = asm.MainModule.Import(typeof(List<>));
            trList_NonNamedMatch = trList1.MakeGenericInstanceType(trNonNamedMatch);
            trIReadOnlyCollection1 = asm.MainModule.Import(typeof(IReadOnlyCollection<>));
            trIReadOnlyCollection_NonNamedMatch = trIReadOnlyCollection1.MakeGenericInstanceType(trNonNamedMatch);
            trSecurityRuleSet = asm.MainModule.Import(typeof(SecurityRuleSet));

            getMatch_Groups = asm.MainModule.Import(typeof(Match).GetProperty(nameof(Match.Groups)).GetMethod);
            getMatchBase_Groups = asm.MainModule.Import(typeof(MatchBase).GetProperty(nameof(MatchBase.Groups)).GetMethod);
            getMatchBase_RegexMatch = asm.MainModule.Import(typeof(MatchBase).GetProperty(nameof(MatchBase.RegexMatch)).GetMethod);
            getGroupCollection_Item = asm.MainModule.Import(typeof(GroupCollection).GetProperty("Item", new[] { typeof(string) }).GetMethod);
            getIEnumerator_Current = asm.MainModule.Import(typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current)).GetMethod);
            ctorMatchBase = asm.MainModule.Import(typeof(MatchBase).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Match) }, null));
            ctorNonNamedMatch = asm.MainModule.Import(typeof(NonNamedMatch).GetConstructor(new[] { typeof(Match) }));
            ctorList_NonNamedMatch = asm.MainModule.Import(typeof(List<NonNamedMatch>).GetConstructor(Type.EmptyTypes));
            ctorSecurityRulesAttributes = asm.MainModule.Import(typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) }));
            ctorRegex = asm.MainModule.Import(typeof(Regex).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            ctorRegexBase = asm.MainModule.Import(typeof(RegexBase).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            mtdRegex_Match_string = asm.MainModule.Import(typeof(Regex).GetMethod(nameof(Regex.Match), new[] { typeof(string) }));
            mtdRegex_Match_string_int = asm.MainModule.Import(typeof(Regex).GetMethod(nameof(Regex.Match), new[] { typeof(string), typeof(int) }));
            mtdRegex_Match_string_int_int = asm.MainModule.Import(typeof(Regex).GetMethod(nameof(Regex.Match), new[] { typeof(string), typeof(int), typeof(int) }));
            mtdRegex_Matches_string = asm.MainModule.Import(typeof(Regex).GetMethod(nameof(Regex.Matches), new[] { typeof(string) }));
            mtdRegex_Matches_string_int = asm.MainModule.Import(typeof(Regex).GetMethod(nameof(Regex.Matches), new[] { typeof(string), typeof(int) }));
            mtdIEnumerable_GetEnumerator = asm.MainModule.Import(typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator)));
            mtdIEnumerator_MoveNext = asm.MainModule.Import(typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            mtdList_NonNamedMatch_Add = asm.MainModule.Import(typeof(List<NonNamedMatch>).GetMethod(nameof(List<NonNamedMatch>.Add)));
            ctorRegexResolved = ctorRegex.Resolve();
        }

        /// <summary>Post-processes and assembly generated by <see cref="Regex.CompileToAssembly(RegexCompilationInfo[], AssemblyName)"/></summary>
        /// <param name="assemblyPath">Path of the assembly (DLL)</param>
        /// <param name="regexCompilationInfos">Information about regexes compiled to the assembly</param>
        protected virtual void PostProcess(string assemblyPath, IEnumerable<RegexCompilationInfo> regexCompilationInfos)
        {
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
            InitializeMetadataReferences(asm);
 var regexes = regexCompilationInfos.ToDictionary(i => (string.IsNullOrEmpty(i.Namespace) ? string.Empty : (i.Namespace + ".")) + i.Name, i => new Regex(i.Pattern, i.Options));
            //TODO: Remove next 2 lines
            asm.CustomAttributes.Remove(asm.CustomAttributes.Single(ca => ca.Constructor.DeclaringType.FullName == typeof(SecurityRulesAttribute).FullName));
            asm.CustomAttributes.Add(new CustomAttribute(ctorSecurityRulesAttributes) { ConstructorArguments = { new CustomAttributeArgument(trSecurityRuleSet, SecurityRuleSet.Level1) } });

            foreach (var regexClass in asm.MainModule.Types.Where(t => t.BaseType?.FullName == typeof(Regex).FullName).ToArray())
            {
                var regex = regexes[regexClass.FullName];
                ChangeRegexClassBaseType(regexClass);
                int ignore;
                var groups = regex.GetGroupNames().Where(gn => !int.TryParse(gn, NumberStyles.Any, CultureInfo.InvariantCulture, out ignore)).ToArray();

                if (groups.Length > 0)
                {
                    TypeDefinition defMatchClass = CreateMatchClass(regexClass.Name, groups);
                    regexClass.NestedTypes.Add(defMatchClass);

                    trMatchClass = defMatchClass;
                    trIReadOnlyCollection_MatchClass = trIReadOnlyCollection1.MakeGenericInstanceType(trMatchClass);
                    trList_MatchClass = trList1.MakeGenericInstanceType(trMatchClass);
                    ctorList_MatchClass = new MethodReference(".ctor", trVoid, trList_MatchClass) { HasThis = true };
                    ctorMatchClass = defMatchClass.Methods.Single(m => m.IsConstructor);
                    mtdList_MatchClass_Add = new MethodReference(nameof(List<int>.Add), trVoid, trList_MatchClass)
                    {
                        Parameters = { new ParameterDefinition(trList1.GenericParameters[0]) },
                        HasThis = true
                    };
                }
                else
                {
                    ctorList_MatchClass = ctorList_NonNamedMatch;
                    ctorMatchClass = ctorNonNamedMatch;
                    trIReadOnlyCollection_MatchClass = trIReadOnlyCollection_NonNamedMatch;
                    trList_MatchClass = trList_NonNamedMatch;
                    trMatchClass = trNonNamedMatch;
                    mtdList_MatchClass_Add = mtdList_NonNamedMatch_Add;
                }

                //Match(string);
                regexClass.Methods.Add(CreateRegexClassMatchFunction_String());
                //Match(string, int);
                regexClass.Methods.Add(CreateRegexClassMatchFunction_String_Int32());
                //Match(string, int, int);
                regexClass.Methods.Add(CreateRegexClassMatchFunction_String_Int32_Int32());

                //Matches(string);
                regexClass.Methods.Add(CreateRegexClassMatchesFunction_String());

                //Matches(string, int);
                regexClass.Methods.Add(CreateRegexClassMatchesFunction_String_Int32());
            }
            if (Settings.Snk != null)
                using (var fs = IO.File.Open(Settings.Snk, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                    asm.Write(assemblyPath, new WriterParameters { StrongNameKeyPair = new StrongNameKeyPair(fs) });
            else
                asm.Write(assemblyPath);
        }

        /// <summary>Signs an assembly using Mono.CECIL</summary>
        /// <param name="assemblyPath">Path of an assembly to sign</param>
        private void SignAssembly(string assemblyPath)
        {
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
            using (var fs = IO.File.Open(Settings.Snk, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                asm.Write(assemblyPath, new WriterParameters { StrongNameKeyPair = new StrongNameKeyPair(fs) });
        }

        /// <summary>Changes base type of regular expression class from <see cref="Regex"/> to <see cref="RegexBase"/></summary>
        /// <param name="regexClass">Definition of the class to change base class off</param>
        private void ChangeRegexClassBaseType(TypeDefinition regexClass)
        {
            regexClass.BaseType = trRegexBase;
            foreach (var mtd in regexClass.Methods)
            {
                if (mtd.IsConstructor)
                {
                    foreach (var inst in mtd.Body.Instructions)
                    {
                        if (inst.OpCode == OpCodes.Call && inst.Operand is MethodReference && ((MethodReference)inst.Operand).Resolve() == ctorRegexResolved)
                        {
                            inst.Operand = ctorRegexBase;
                            break; //Only one call to base ctor
                        }
                    }
                }
            }
        }

        /// <summary>Creates a class for match with named capture groups</summary>
        /// <param name="regexName">Name of the regular expression</param>
        /// <param name="groups">Names of named capture groups</param>
        /// <returns>Definition of created class</returns>
        private TypeDefinition CreateMatchClass(string regexName, string[] groups)
        {
            var defMatchClass = new TypeDefinition(null, regexName + "Match", TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.Sealed)
            {
                BaseType = trMatchBase,
                Methods = { CreateMatchClassCTor() }
            };

            foreach (var gName in groups)
            {
                var prp = CreateMatchClassNamedGroupProperty(gName);
                defMatchClass.Methods.Add(prp.GetMethod);
                defMatchClass.Properties.Add(prp);
            }
            return defMatchClass;
        }

        /// <summary>Creates a constructor for match class</summary>
        /// <returns>Constructor definition</returns>
        private MethodDefinition CreateMatchClassCTor()
        {
            //MatchClass..ctor
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Assembly | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, trVoid)
            {
                Parameters = { new ParameterDefinition(trMatch) }
            };
            var il = ctor.Body.GetILProcessor();
            //base(match)                                                                                           <     |    >
            ctor.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this      // ldarg.0                          <this |    >
            ctor.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));             // ldard.1                          <match|this>
            ctor.Body.Instructions.Add(il.Create(OpCodes.Tail));
            ctor.Body.Instructions.Add(il.Create(OpCodes.Call, ctorMatchBase)); // tail.call MatchBase..ctor(Match) <     |    >
            ctor.Body.Instructions.Add(il.Create(OpCodes.Ret));                 // ret                              <     |    >
            ctor.Body.MaxStackSize = 2;
            return ctor;
        }

        /// <summary>Creates a property for accessing named capture group</summary>
        /// <param name="groupName">Name of the group</param>
        /// <returns>Property definition</returns>
        private PropertyDefinition CreateMatchClassNamedGroupProperty(string groupName)
        {
            //MatchClass.get_{GroupName}
            var getter = new MethodDefinition("get_" + groupName, MethodAttributes.Public | MethodAttributes.SpecialName, trGroup);
            var il = getter.Body.GetILProcessor();
            //return ((Match)this).Groups[g];                                                                                             <               |      >
            getter.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this                // ldarg.0                                    <this           |      >
            getter.Body.Instructions.Add(il.Create(OpCodes.Castclass, trMatchBase));        // castclass Match                            <(MatchBaae)this|      >
            getter.Body.Instructions.Add(il.Create(OpCodes.Callvirt, getMatchBase_Groups)); // callvirt MatchBase.get_Groups()            <groups         |      >
            getter.Body.Instructions.Add(il.Create(OpCodes.Ldstr, groupName));              // ldstr {g}                                  <{g}            |groups>
            getter.Body.Instructions.Add(il.Create(OpCodes.Tail));
            getter.Body.Instructions.Add(il.Create(OpCodes.Call, getGroupCollection_Item)); // tail.call GroupCollection.get_Item(String) <group          |      >
            getter.Body.Instructions.Add(il.Create(OpCodes.Ret));                           // ret                                        <               |      >
            getter.Body.MaxStackSize = 2;

            return new PropertyDefinition(groupName, PropertyAttributes.None, trGroup) { GetMethod = getter };
        }

        private MethodDefinition CreateRegexClassMatchFunction_String()
        {
            var m = new MethodDefinition("Match", MethodAttributes.Public, trMatchClass)
            { Parameters = { new ParameterDefinition("input", ParameterAttributes.In, trString) } };
            var il = m.Body.GetILProcessor();
            //return new matchClass(((Regex)this).Match(input))                                                          <           |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this                // ldarg.0                        <this       |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Castclass, trRegex));            // castclass MatchBase            <(Regex)this|     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));                       // ldarg.1                        <arg1       |match>
            m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdRegex_Match_string));   // call Regex.Match(String)       <match      |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Newobj, ctorMatchClass));        // newobj matchClass..ctor(Match) <matchClass |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ret));                           // ret                            <           |     >
            m.Body.MaxStackSize = 2;
            return m;
        }

        private MethodDefinition CreateRegexClassMatchFunction_String_Int32()
        {
            var m = new MethodDefinition("Match", MethodAttributes.Public, trMatchClass)
            { Parameters = { new ParameterDefinition("input", ParameterAttributes.In, trString), new ParameterDefinition("startAt", ParameterAttributes.In, trInt32) } };
            var il = m.Body.GetILProcessor();
            //return new matchClass(((Regex)this).Match(input, startAt))                                                   <           |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this                  // ldarg.0                        <this       |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Castclass, trRegex));              // castclass MatchBase            <(Regex)this|     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));                         // ldarg.1                        <arg1       |match|     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_2));                         // ldarg.2                        <arg2       |arg1 |match>
            m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdRegex_Match_string_int)); // call Regex.Match(String)       <match      |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Newobj, ctorMatchClass));          // newobj matchClass..ctor(Match) <matchClass |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ret));                             // ret                            <           |     |     >
            m.Body.MaxStackSize = 3;
            return m;
        }

        private MethodDefinition CreateRegexClassMatchFunction_String_Int32_Int32()
        {
            var m = new MethodDefinition("Match", MethodAttributes.Public, trMatchClass)
            {
                Parameters = {
                    new ParameterDefinition("input", ParameterAttributes.In, trString),
                    new ParameterDefinition ("startAt", ParameterAttributes.In, trInt32),
                    new ParameterDefinition ("length", ParameterAttributes.In, trInt32)
                }
            };
            var il = m.Body.GetILProcessor();
            //return new matchClass(((Regex)this).Match(input, startAt, lenght))                                               <           |     |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this                      // ldarg.0                        <this       |     |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Castclass, trRegex));                  // castclass MatchBase            <(Regex)this|     |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));                             // ldarg.1                        <arg1       |match|     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_2));                             // ldarg.2                        <arg2       |arg1 |match|     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_3));                             // ldarg.3                        <arg3       |arg2 |arg1 |match>
            m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdRegex_Match_string_int_int)); // call Regex.Match(String)       <match      |     |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Newobj, ctorMatchClass));              // newobj matchClass..ctor(Match) <matchClass |     |     |     >
            m.Body.Instructions.Add(il.Create(OpCodes.Ret));                                 // ret                            <           |     |     |     >
            m.Body.MaxStackSize = 3;
            return m;
        }

        private MethodDefinition CreateRegexClassMatchesFunction_String()
        {
            var m = new MethodDefinition("Matches", MethodAttributes.Public, trIReadOnlyCollection_MatchClass)
            { Parameters = { new ParameterDefinition("input", ParameterAttributes.In, trString), } };
            var il = m.Body.GetILProcessor();
            GenerateMatchesMethodBody(m, il, stackSize =>
               {
                   m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));                       // ldarg.1
                   m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdRegex_Matches_string)); // call Regex.Matches(String)
                   return stackSize + 1;
               }
            );
            return m;
        }

        private MethodDefinition CreateRegexClassMatchesFunction_String_Int32()
        {
            var m = new MethodDefinition("Matches", MethodAttributes.Public, trIReadOnlyCollection_MatchClass)
            {
                Parameters = {
                    new ParameterDefinition("input", ParameterAttributes.In, trString),
                    new ParameterDefinition ("startAt", ParameterAttributes.In, trInt32)
                }
            };
            var il = m.Body.GetILProcessor();
            GenerateMatchesMethodBody(
                m, il,
                stackSize =>
                {
                    m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_1));                           // ldarg.1
                    m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_2));                           // ldarg.2
                    m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdRegex_Matches_string_int)); // call regex.Matches(String, Int32)
                    return stackSize + 2;
                }
            );
            return m;
        }

        private void GenerateMatchesMethodBody(MethodDefinition m, ILProcessor il, Func<int, int> generateBaseCall)
        {
            int maxStack;
            m.Body.InitLocals = true;
            //IEnumerator @0;                                                                      .locals {                                     <          |    >
            m.Body.Variables.Add(new VariableDefinition(trIEnumerator));                        //     IEnumerable,                              <          |    >
            //List<MatchClass> @1;                                                                                                               <          |    >
            m.Body.Variables.Add(new VariableDefinition(trList_MatchClass));                    //     List`1[MatchClass]                        <          |    >
            //@0 = ((Match)this).Matches(input, ...).GetEnumerator();                              }                                             <          |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldarg_0)); //this                            ldarg.0                                       <this      |    >
            maxStack = generateBaseCall(1);                                                     // ldarg.x; call Regex.Matches(String, ...)      <matches   |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Callvirt, mtdIEnumerable_GetEnumerator)); // callvirt IEnumerable.GetEnumerator()          <enumerator|    >
            m.Body.Instructions.Add(il.Create(OpCodes.Stloc_0));                                // stloc.0                                       <          |    >

            //@1 = new List<matchClass>();                                                                                                       <          |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Newobj, ctorList_MatchClass));            // newobj List`1[matchClass]..ctor()             <list      |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Stloc_1));                                // stloc.1                                       <          |    >

            //if(!@0.MoveNext()) goto afterDo
            var afterDo = il.Create(OpCodes.Ldloc_1);                                           // ┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┐
            Instruction beforeDo;                                                               //                                            ┊
            m.Body.Instructions.Add(beforeDo = il.Create(OpCodes.Ldloc_0));                     // ldloc.0    ←───────────────────────────┐   ┊ <enumerator|    >
            m.Body.Instructions.Add(il.Create(OpCodes.Callvirt, mtdIEnumerator_MoveNext));      // callvirt IEnumerator.MoveNext()        │   ┊ <bool      |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Brfalse_S, afterDo));                     // brfalse.s ─────────────────────────────┼─┐ ┊ <          |    >
            //                                                                                                                            │ │ ┊
            //@1.Add(new matchClass(@0.Current));                                                                                         │ │ ┊
            m.Body.Instructions.Add(il.Create(OpCodes.Ldloc_1));                                // ldloc.1                                │ │ ┊ <list      |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Ldloc_0));                                // ldloc.0                                │ │ ┊ <enumerator|list>
            m.Body.Instructions.Add(il.Create(OpCodes.Callvirt, getIEnumerator_Current));       // callvirt IEnumertator.get_Current()    │ │ ┊ <obj(match)|list>
            m.Body.Instructions.Add(il.Create(OpCodes.Castclass, trMatch));                     // castclass Match                        │ │ ┊ <match     |list>
            m.Body.Instructions.Add(il.Create(OpCodes.Newobj, ctorMatchClass));                 // newobj matchClass..ctor(Match)         │ │ ┊ <matchClass|list>
            m.Body.Instructions.Add(il.Create(OpCodes.Call, mtdList_MatchClass_Add));           // call List`[matchClass].Add(matchClass) │ │ ┊ <          |    >
            //                                                                                                                            │ │ ┊
            //goto beforeDo                                                                     //                                        │ │ ┊ <          |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Br_S, beforeDo));                         // br.s  ─────────────────────────────────┘ │ ┊ <          |    >
            //                                                                                                                              │ ┊
            //return @1;                                                                                                                    │ ┊ <          |    >
            m.Body.Instructions.Add(afterDo);                                                   // ldloc.1  ←───────────────────────────────┴┉┘ <list      |    >
            m.Body.Instructions.Add(il.Create(OpCodes.Ret));                                    // ret                                           <          |    >
            m.Body.MaxStackSize = Math.Max(2, maxStack);
        }
    }
}
