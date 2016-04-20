using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dzonny.RegexCompiler.Compilation
{
    /// <summary>Regex compiler error and warning codes</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegexCompilerErrorCodes
    {
        /// <summary>1st line of regex block must start with 'Name:'</summary>
        RXC001 = 1,
        /// <summary>Invalid regex options</summary>
        RXC002 = 2,
        /// <summary>Invalid timeout specified</summary>
        RXC003 = 3,
        /// <summary>Exception while preparing <see cref="RegexCompilationInfo"/></summary>
        RXC004 = 4,
        /// <summary>Unexpected end of file</summary>
        RXC005 = 5 
    }
} 