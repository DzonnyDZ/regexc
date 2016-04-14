using System.Text.RegularExpressions;

namespace Dzonny.RegexCompiler
{
    /// <summary>Common base class for compiled regular expressions</summary>
    public abstract class RegexBase:Regex
    {
        /// <summary>CTor - creates a new instance of the <see cref="RegexBase"/> class</summary>
        protected RegexBase() { }
    }
}