using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class DeclarationMember : TypeDomBase
    {
        public string Comment { get; set; }        

        public static implicit operator DeclarationMember(RawStatements from)
        {
            return new RawDeclarationMember(from);
        }
    }

    public sealed class PropertyMember : DeclarationMember
    {
        public AccessibilityEnum? Accessibility { get; set; }

        public string Name { get; set; }

        public TypescriptTypeReference MemberType { get; set; }

        public bool IsOptional { get; set; }

        public RawStatements Initialization { get; set; }

        public PropertyMember(string name)
        {
            Name = name;
        }
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
        public AccessibilityEnum? Accessibility { get; set; }
        public List<GenericParameter> GenericParameters { get; private set; }
        public bool IsGeneric { get { return GenericParameters.Count > 0; } }
        public List<FunctionParameter> Parameters { get; private set; }
        public TypescriptTypeReference ResultType { get; set; }        
        protected FunctionMemberBase(string name)
        {
            GenericParameters = new List<GenericParameter>();
            Parameters = new List<FunctionParameter>();
            Name = name;
        }
    }

    public sealed class FunctionParameter : TypeDomBase
    {
        public string Comment { get; set; }
        public string Name { get; set; }
        public FunctionParameter(string name)
        {
            Name = name;
        }
        public TypescriptTypeReference ParameterType { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRest { get; set; }
        public RawStatements DefaultValue { get; set; }
    }

    public sealed class FunctionDeclarationMember : FunctionMemberBase
    {
        public FunctionDeclarationMember(string name) : base(name)
        {
        }
    }

    public sealed class FunctionMember : FunctionMemberBase
    {
        public RawStatements Body { get; set; }
        public FunctionMember(string name, RawStatements body) : base(name)
        {
            Body = body;
        }
    }

    //indexer?
    //accessor? (get method, set method) ES5
    //constructor, index, call
    public sealed class RawDeclarationMember : DeclarationMember
    {
        public RawStatements Raw { get; set; }
        public RawDeclarationMember(RawStatements raw)
        {
            Raw = raw;
        }
    }

}
