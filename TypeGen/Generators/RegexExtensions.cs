using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public static class RegexExtensions
    {
        public static IEnumerable<string> SplitToParts(this Regex regex, string s)
        {
            int i = 0;
            foreach (Match m in regex.Matches(s))
            {
                if (m.Index != i)
                    yield return s.Substring(i, m.Index - i);
                yield return m.Value;
                i = m.Index + m.Value.Length;
            }
            if (i < s.Length)
                yield return s.Substring(i);
            /*
            var result = new List<string>();
            int i = 0;
            foreach (Match m in regex.Matches(s))
            {
                if (m.Index != i)
                    result.Add(s.Substring(i, m.Index - i));
                result.Add(m.Value);
                i = m.Index + m.Value.Length;
            }
            if (i < s.Length)
                result.Add(s.Substring(i));
            return result;
            */
        }
    }
}
