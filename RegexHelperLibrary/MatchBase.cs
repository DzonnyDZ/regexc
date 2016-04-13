using System.Text.RegularExpressions;

namespace RegexHelperLibrary
{
    public abstract class MatchBase
    {
        private readonly Match match;

        protected MatchBase(Match match)
        {
            this.match = match;
        }

        #region Match

        public Match NextMatch() => match.NextMatch();

        public string Result(string replacement) => match.Result(replacement);

        public GroupCollection Groups => match.Groups;
        #endregion
        #region Group

        public CaptureCollection Captures => match.Captures;

        public bool Success => match.Success;

        #endregion

        #region Capture

        public override string ToString() => match.ToString();

        // Properties

        public int Index => match.Index;

        public int Length => match.Length;

        public string Value => match.Value;

        #endregion
    }
}
