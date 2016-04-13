using System.Text.RegularExpressions;

namespace Dzonny.RegexCompiler
{
    public abstract class MatchBase
    {
        public Match RegexMatch { get; }

        protected MatchBase(Match regexMatch)
        {
            RegexMatch = regexMatch;
        }

        #region Match

        public Match NextMatch() => RegexMatch.NextMatch();

        public string Result(string replacement) => RegexMatch.Result(replacement);

        public GroupCollection Groups => RegexMatch.Groups;
        #endregion

        #region Group

        public CaptureCollection Captures => RegexMatch.Captures;

        public bool Success => RegexMatch.Success;

        #endregion

        #region Capture

        public override string ToString() => RegexMatch.ToString();

        public int Index => RegexMatch.Index;

        public int Length => RegexMatch.Length;

        public string Value => RegexMatch.Value;

        #endregion
    }
}
