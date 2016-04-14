using System;
using System.IO;
using System.Text;

namespace Dzonny.RegexCompiler.Compilation
{
    /// <summary>A wrapper of <see cref="TextReader"/> which supports all line terminators for <see cref="ReadLine"/></summary>
    internal class UnicodeNewlineTextReader : TextReader
    {
        /// <summary>The <see cref="TextReader"/> this instance wraps</summary>
        private readonly TextReader inner;

        /// <summary>CTor - creates a new instance of the <see cref="UnicodeNewlineTextReader"/> class</summary>
        /// <param name="reader">Inner <see cref="TextReader"/> to wrap</param>
        public UnicodeNewlineTextReader(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            inner = reader;
        }

        /// <summary>Reads the next character from the text reader and advances the character position by one character.</summary>
        /// <returns>The next character from the text reader, or -1 if no more characters are available. The default implementation returns -1.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed. </exception>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <filterpriority>1</filterpriority>
        public override int Read() => inner.Read();

        /// <summary>Reads the next character without changing the state of the reader or the character source. Returns the next available character without actually reading it from the reader.</summary>
        /// <returns>An integer representing the next character to be read, or -1 if no more characters are available or the reader does not support seeking.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed. </exception>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <filterpriority>1</filterpriority>
        public override int Peek() => inner.Peek();

        private string lastNewLine = null;

        /// <summary>Gets a character or series of characters that has been used a last read line terminator</summary>
        /// <returns>Last character or series of characters that composed last encountered line terminator. Series or characters can be only CrLf (\r\n), otherwise single character is returned.</returns>
        /// <value>Possible values are sequence CrLf and Unicode characters CR, LF, VT, FF, NEL, LS, PS</value>
        /// <remarks>
        /// This property is populated only when <see cref="ReadLine"/> is called. Any line terminators encountered when another methods such as <see cref="Read"/> are called are ignored.
        /// This property is null before first line terminator has been encountered or after last line of the stream has been read.
        /// </remarks>
        public string LastNewLine => lastNewLine;

        /// <summary>Reads a line of characters from the text reader and returns the data as a string.</summary>
        /// <returns>The next line from the reader, or null if all characters have been read.</returns>
        /// <exception cref="T:IOException">An I/O error occurs. </exception>
        /// <exception cref="OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string. </exception>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed. </exception>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="Int32.MaxValue"/></exception>
        /// <filterpriority>1</filterpriority>
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
