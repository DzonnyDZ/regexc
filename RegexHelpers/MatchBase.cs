using System;
using System.Text.RegularExpressions;

namespace Dzonny.RegexCompiler
{
    /// <summary>Base class for matches with properties for named capture groups</summary>
    public abstract class MatchBase
    {
        /// <summary>Gets the <see cref="System.Text.RegularExpressions"/>'s <see cref="Match"/></summary>
        public Match RegexMatch { get; }

        /// <summary>CTor - creates a new instance of the <see cref="MatchBase"/> class</summary>
        /// <param name="regexMatch">The <see cref="System.Text.RegularExpressions"/>'s <see cref="Match"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="regexMatch"/> is null</exception>
        protected MatchBase(Match regexMatch)
        {
            if (regexMatch == null) throw new ArgumentNullException(nameof(regexMatch));
            RegexMatch = regexMatch;
        }

        #region Match
        /// <summary>Returns a new <see cref="Match"/> object with the results for the next match, starting at the position at which the last match ended (at the character after the last matched character).</summary>
        /// <returns>The next regular expression match.</returns>
        /// <seealso cref="Match.NextMatch"/>
        public Match NextMatch() => RegexMatch.NextMatch();

        /// <summary>Returns the expansion of the specified replacement pattern. </summary>
        /// <param name="replacement">The replacement pattern to use. </param>
        /// <returns>The expanded version of the <paramref name="replacement"/> parameter.</returns>
        /// <seealso cref="Match.Result"/>
        public string Result(string replacement) => RegexMatch.Result(replacement);

        /// <summary>Gets a collection of groups matched by the regular expression.</summary>
        /// <value>The character groups matched by the pattern.</value>
        /// <seealso cref="Match.Groups"/>
        public GroupCollection Groups => RegexMatch.Groups;
        #endregion

        #region Group

        /// <summary>Gets a collection of all the captures matched by the capturing group, in innermost-leftmost-first order (or innermost-rightmost-first order if the regular expression is modified with the RegexOptions.RightToLeft option). The collection may have zero or more items.</summary>
        /// <value>The collection of substrings matched by the group.</value>
        /// <seealso cref="Group.Captures"/>
        public CaptureCollection Captures => RegexMatch.Captures;

        /// <summary>Gets a value indicating whether the match is successful.</summary>
        /// <value>True if the match is successful; otherwise, false.</value>
        /// <seealso cref="Group.Captures"/>
        public bool Success => RegexMatch.Success;

        #endregion

        #region Capture

        /// <summary>Retrieves the captured substring from the input string by calling the Value property. </summary>
        /// <returns>The substring that was captured by the match.</returns>
        /// <remarks><see cref="ToString"/> is actually an internal call to the <see cref="Capture.Value"/> property.</remarks>
        /// <seealso cref="Capture.ToString"/>
        public override string ToString() => RegexMatch.ToString();

        /// <summary>The position in the original string where the first character of the captured substring is found.</summary>
        /// <value>The zero-based starting position in the original string where the captured substring is found.</value>
        /// <seealso cref="Capture.Index"/>
        public int Index => RegexMatch.Index;

        /// <summary>Gets the length of the captured substring.</summary>
        /// <value>The length of the captured substring.</value>
        /// <seealso cref="Capture.Length"/>
        public int Length => RegexMatch.Length;

        /// <summary>Gets the captured substring from the input string.</summary>
        /// <value>The substring that is captured by the match.</value>
        /// <see cref="Capture.Value"/>
        public string Value => RegexMatch.Value;

        #endregion

        /// <summary>Converts <see cref="Match"/> to <see cref="MatchBase"/></summary>
        /// <param name="match">A <see cref="Match"/> to convert</param>
        /// <returns>A new instance of <see cref="NonNamedMatch"/> initialized by <paramref name="match"/>. Null if <paramref name="match"/> is null.</returns>
        public static implicit operator MatchBase(Match match)
        {
            if (match == null) return null;
            return new NonNamedMatch(match);
        }

        /// <summary>Converts <see cref="MatchBase"/> to <see cref="Match"/></summary>
        /// <param name="match">A <see cref="MatchBase"/> to convert</param>
        /// <returns><paramref name="match"/>.<see cref="MatchBase.RegexMatch">RegexMatch</see>. Null if <paramref name="match"/> is null.</returns>
        public static implicit operator Match(MatchBase match) => match?.RegexMatch;
    }
}
