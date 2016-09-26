using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public enum LetterCasing
    {
        None,
        Lower,
        Upper
    }

    public static class NamingHelper
    {
        public static string FirstLetter(LetterCasing casing, string name)
        {
            switch (casing)
            {
                case LetterCasing.Lower:
                    return name.Substring(0, 1).ToLower() + name.Substring(1);
                case LetterCasing.Upper:
                    return name.Substring(0, 1).ToUpper() + name.Substring(1);
                case LetterCasing.None:
                default:
                    return name;
            }
        }

        public static string GetNonGenericTypeName(Type type)
        {
            if (type.IsGenericType)
                return type.Name.Substring(0, type.Name.IndexOf('`'));
            return type.Name;
        }

        public static string RemovePrefix(string prefix, string s, LetterCasing nextLetterCasing = LetterCasing.Upper)
        {
            if (!s.StartsWith(prefix, StringComparison.Ordinal))
                return s;
            var x = s.Substring(prefix.Length);
            if (x != FirstLetter(nextLetterCasing, x))
                return s;
            return x;
        }

        public static string CamelCaseFromString(string s)
        {
            s = s.Trim();
            var i = 0;
            bool wasLower = false;
            bool needsUpper = true;
            var result = new StringBuilder();
            while(i<s.Length)
            {
                var c = s[i];
                if (Char.IsLetter(c))
                {
                    if (Char.IsLower(c))
                    {
                        //if (wasLower)
                        //    needsUpper = true;
                        wasLower = true;
                    }
                    else
                    {
                        wasLower = false;
                    }
                    if (needsUpper)
                    {
                        c = Char.ToUpper(c);
                        needsUpper = false;
                    }
                    result.Append(c);
                }
                else
                {
                    needsUpper = true;
                }
                i++;
            }
            return result.ToString();
        }
    }
}
