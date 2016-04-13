using System.IO;
using System.Text;

namespace Dzonny.RegexCompiler
{
    public class UnicodeTextReader : TextReader
    {
        private readonly TextReader inner;

        public UnicodeTextReader(TextReader reader)
        {
            inner = reader;
        }

        public override int Read() => inner.Read();
        public override int Peek() => inner.Peek();

        private string lastNewLine = null;

        public string LastNewLine => lastNewLine;

        public override string ReadLine()
        {
            lastNewLine = null;
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                int num = this.Read();
                switch (num)
                {
                    case -1:
                        if (builder.Length > 0) return builder.ToString();
                        return null;
                    case 0x0d: //CR
                        if (Peek() == 10)
                            lastNewLine = ((char)num).ToString() + ((char)Read()).ToString();
                        else
                            lastNewLine = ((char)num).ToString();
                        return builder.ToString();
                    case 0x0a://LF
                    case 0x0b://VT
                    case 0x0c://FF
                    case 0x85://NEL
                    case 0x2028://LS
                    case 0x2029://PS
                        lastNewLine = ((char)num).ToString();
                        return builder.ToString();
                }
                builder.Append((char)num);
            }

        }
    }
}
