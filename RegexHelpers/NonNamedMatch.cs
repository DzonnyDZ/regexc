using System;
using System.Text.RegularExpressions;

namespace Dzonny.RegexCompiler
{
    /// <summary>Implements <see cref="MatchBase"/> without properties for named capture groups</summary>
    /// <remarks>Used by Regex Compiler for regular expressions which don't specify named capture groups.</remarks>
    public class NonNamedMatch : MatchBase
    {
        /// <summary>CTor - creates a new instance of the <see cref="NonNamedMatch"/> class</summary>
        /// <param name="regexMatch">The <see cref="System.Text.RegularExpressions"/>'s <see cref="Match"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="regexMatch"/> is null</exception>
        public NonNamedMatch(Match regexMatch) : base(regexMatch) { }
    }
}