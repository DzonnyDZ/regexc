using System;
using System.Linq;

namespace RegexTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var r1 = new Regex1();
            var m1 = r1.Match("7");
            var m2 = r1.Match("1");
            var m3 = r1.Match("sfjslkfjsklf", 3);
            var m4 = r1.Match("áíščřáí ěšrui564564564šš", 4, 8);
            var ms1 = r1.Matches("aaaa");
            var ms2 = r1.Matches("aaaa", 0);

            var r2 = new R2D2();
            var rm1 = r2.Match("01/1/1990");
            Console.WriteLine($"{rm1.Year}-{rm1.Month}-{rm1.Day}");
            var rm2 = r2.Match("a01/1/1990", 1);
            var rm3 = r2.Match("a01/1/1990b", 1, 9);
            var rms1 = r2.Matches("01/1/199006/06/8741");
            var rms2 = r2.Matches("a01/1/1990ř06/06/8741Ň", 1);
            Console.WriteLine(rms2.Skip(1).First().Year);
        }
    }
}
