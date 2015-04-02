using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class DeclarationMember : TypeDomBase
    {
        //according to spec: properties are public by default
        public AccessibilityEnum? Accessibility { get; set; }

    }

    public sealed class PropertyMember : DeclarationMember
    {
        public string Name { get; set; }

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


    //indexer?
    //accessor? (get method, set method) ES5
    //constructor, index, call
}
