using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class DeclarationMember : TsBase
    {

    }

    public enum MemberAccessibility
    {
        Public,
        Private,
        Protected,
    }

    public static class AccessibilityConverter
    {
        public static string ToStr(this MemberAccessibility a)
        {
            switch (a)
            {
                case MemberAccessibility.Public:
                    return "public";
                case MemberAccessibility.Private:
                    return "private";
                case MemberAccessibility.Protected:
                    return "protected";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed class PropertyMember : DeclarationMember
    {
        public string Name { get; set; }

        //acc to spec: properties are public by default
        public MemberAccessibility? Accessibility { get; set; }

        public TypescriptTypeReference MemberType { get; set; }

        public bool IsOptional { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Accessibility != null)
            {
                sb.Append(Accessibility.Value.ToStr());
                sb.Append(" ");
            }
            sb.Append(Name);
            if (IsOptional)
                sb.Append("?");
            sb.Append(" : ");
            sb.Append(MemberType.ToString());
            return sb.ToString();
        }
    }

    public abstract class FunctionMemberBase : DeclarationMember
    {
        public string Name { get; set; }
        //TODO: parameters, generics, ...?
    }
    public sealed class FunctionDeclarationMember : FunctionMemberBase
    {
    }

    public sealed class FunctionMember : FunctionMemberBase
    {
        public RawStatements Body { get; set; }
    }


    //method
    //indexer?
    //accessor? (get method, set method)
    //construct, index, call
}
