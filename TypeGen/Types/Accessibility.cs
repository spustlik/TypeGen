using System;

namespace TypeGen
{

    public enum AccessibilityEnum
    {
        Public,
        Private,
        Protected,
    }

    public static class AccessibilityConverter
    {
        public static string ToStr(this AccessibilityEnum a)
        {
            switch (a)
            {
                case AccessibilityEnum.Public:
                    return "public";
                case AccessibilityEnum.Private:
                    return "private";
                case AccessibilityEnum.Protected:
                    return "protected";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}