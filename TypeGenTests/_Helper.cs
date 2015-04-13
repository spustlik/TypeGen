using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGenTests
{
    class Helper
    {
        class LinesComparer
        {
            public bool CompareWhitespaces { get; set; }
            public bool AppendLineNumbers = true;
            public StringComparison Comparison { get; set; }
            private List<string> _expectedLines;
            private List<string> _actualLines;
            private List<string> _result;
            private int _expectedIndex;
            private int _actualIndex;
            private string _expectedLine;
            private string _actualLine;


            public LinesComparer(string expectedString, string actualString)
            {
                _expectedLines = NormalizeLines(expectedString).ToList();
                _actualLines = NormalizeLines(actualString).ToList();
            }

            #region Normalization & comparison
            private string[] NormalizeLines(string s)
            {
                if (!CompareWhitespaces)
                {
                    s = s.Trim();
                }
                s = s.Replace('\x0d', '\x0a').Replace("\x0a\x0a", "\x0a");
                var lines = s.Split('\x0a');
                return lines;
            }

            private string NormalizeString(string s)
            {
                if (!CompareWhitespaces)
                {
                    s = s.Trim();
                }
                return s;
            }

            private bool CompareLines(int expectedIndex, int actualIndex)
            {
                string expectedLine = this._expectedLine;
                if (_expectedIndex != expectedIndex)
                    expectedLine = NormalizeString(_expectedLines[expectedIndex]);
                string actualLine = this._actualLine;
                if (_actualIndex != actualIndex)
                    actualLine = NormalizeString(_actualLines[actualIndex]);
                return String.Compare(expectedLine, actualLine, Comparison) == 0;
            }

            #endregion

            internal bool Compare()
            {
                _result = new List<string>();
                _expectedIndex = 0;
                _actualIndex = 0;
                _expectedLine = null;
                while (_expectedIndex < _expectedLines.Count)
                {
                    var line = " [" + (_actualIndex + 1) + " : " + (_expectedIndex + 1) + "]";
                    if (!AppendLineNumbers)
                        line = null;
                    _expectedLine = NormalizeString(_expectedLines[_expectedIndex]);
                    if (_actualIndex >= _actualLines.Count)
                        break;
                    _actualLine = NormalizeString(_actualLines[_actualIndex]);
                    if (CompareLines(_expectedIndex, _actualIndex))
                    {
                        _expectedIndex++;
                        _actualIndex++;
                    }
                    else
                    {
                        if (LookupForwardActualLines())
                        {
                            //expectedline in next few lines, so skip actualLine
                            _result.Add("[EXTR]:" + _actualLine + line);
                            _actualIndex++;

                        }
                        else if (LookupForwardExpectedLines())
                        {
                            _result.Add("[MISS]:" + _expectedLine + line);
                            _expectedIndex++;
                        }
                        else
                        {
                            _result.Add("[DIFF]:" + _expectedLine + " ==> " + _actualLine + line);
                            _expectedIndex++;
                            _actualIndex++;
                        }
                    }
                }
                //TODO: more actual lines
                return _result.Count == 0;
            }

            private bool LookupForwardActualLines()
            {
                // try forward lookup for expectedline
                for (int i = 0; i < 10; i++)
                {
                    if (i + _actualIndex < _actualLines.Count)
                    {
                        if (CompareLines(_expectedIndex, i + _actualIndex))
                        {
                            //OK, we found same line
                            return true;
                        }
                    }
                }
                return false;
            }

            private bool LookupForwardExpectedLines()
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i + _expectedIndex < _expectedLines.Count)
                    {
                        if (CompareLines(i + _expectedIndex, _actualIndex))
                        {
                            //OK, we found same line
                            return true;
                        }
                    }
                }
                return false;
            }

            public string[] Result
            {
                get { return _result.ToArray(); }
            }

            public string ResultAsText
            {
                get
                {
                    return String.Join("\x0D", _result);
                }
            }
        }

        internal static string StringCompare(string expectedString, string actualString, bool whitespaces = false, StringComparison comparison = StringComparison.InvariantCulture)
        {
            var comparer = new LinesComparer(expectedString, actualString) { CompareWhitespaces = whitespaces, Comparison = comparison };
            if (comparer.Compare())
                return null;
            return comparer.ResultAsText;
        }

        internal static string[] GetStringCompare(string expectedString, string actualString, bool whitespaces = false, StringComparison comparison = StringComparison.InvariantCulture)
        {
            var comparer = new LinesComparer(expectedString, actualString) { CompareWhitespaces = whitespaces, Comparison = comparison };
            if (comparer.Compare())
                return new string[0];
            return comparer.Result;
        }

    }
}
