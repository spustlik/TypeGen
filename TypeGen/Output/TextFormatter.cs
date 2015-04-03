using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public class TextFormatter
    {
        private bool _lineWritten = false;
        public StringBuilder Output { get; private set; }
        public string DebuggerOutput { get { return Output.ToString(); } }
        public int IndentationSize { get; set; }
        public int CurrentIndentation { get; private set; }
        public TextFormatter(StringBuilder output)
        {
            Output = output;
            IndentationSize = 4;
        }
        public TextFormatter() : this(new StringBuilder())
        {
        }
        public void PushIndent()
        {
            CurrentIndentation++;
        }
        public void PopIndent()
        {
            CurrentIndentation--;
        }
        public void Write(string s)
        {
            var lines = s.Split(new[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                WriteIndentIfNeeded();
                Output.Append(line);
                if (i != lines.Length - 1)
                {
                    WriteLine();
                }
            }
        }

        public void WriteLine()
        {
            Output.AppendLine();
            _lineWritten = true;
        }

        public void WriteEndOfLine()
        {
            if (!_lineWritten)
            {
                WriteLine();
            }
        }

        private void WriteIndentIfNeeded()
        {
            if (_lineWritten)
            {
                WriteIndent();
                _lineWritten = false;
            }
        }

        public void WriteIndent()
        {
            Output.Append(new String(' ', CurrentIndentation * IndentationSize));
        }

        public void WriteSeparated<T>(string separator, IEnumerable<T> items, Action<T> generate)
        {
            bool needSeparator = false;
            foreach (var item in items)
            {
                if (needSeparator)
                {
                    Write(separator);
                }
                generate(item);
                needSeparator = true;
            }
        }

    }

}
