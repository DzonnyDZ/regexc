using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class R1:Regex1
    {
        public new M1 Match(string input)
        {
            return new M1( base.Match(input));
        }
        public new M1 Match(string input, int startat) { return new M1( base.Match(input, startat )); }

        public new M1 Match(string input, int beginning, int length)
        {
            return new M1( base.Match(input, beginning, length ));
        }

        public new IReadOnlyCollection<M1> Matches(string input)
        {
            return from m in base.Matches(input) select new M1(m);
        }
        public new IReadOnlyCollection<M1> Matches(string input, int startat) { return from m in  base.Matches(input, startat) select new M1(m); }
    }

    public class M1 : Match
    {
        public M1(Math m):base ()
        {
            
        } 
    }
}
