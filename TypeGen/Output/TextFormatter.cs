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
        public TextFormatter PushIndent()
        {
            CurrentIndentation++;
            return this;
        }
        public TextFormatter PopIndent()
        {
            CurrentIndentation--;
            return this;
        }
        public TextFormatter Write(string s)
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
            return this;
        }

        public TextFormatter WriteLine()
        {
            Output.AppendLine();
            _lineWritten = true;
            return this;
        }

        public TextFormatter WriteEndOfLine()
        {
            if (!_lineWritten)
            {
                WriteLine();
            }
            return this;
        }

        private TextFormatter WriteIndentIfNeeded()
        {
            if (_lineWritten)
            {
                WriteIndent();
                _lineWritten = false;
            }
            return this;
        }

        public TextFormatter WriteIndent()
        {
            Output.Append(new String(' ', CurrentIndentation * IndentationSize));
            return this;
        }

        public TextFormatter WriteSeparated<T>(string separator, IEnumerable<T> items, Action<T> generate)
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
            return this;
        }

    }

}
