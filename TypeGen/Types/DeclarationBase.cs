using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public class DeclarationBase : TypescriptTypeBase
    {
        public string Name { get; set; }
        public List<TypescriptTypeReference> ExtendsTypes { get; private set; }
        public bool IsExtending { get { return ExtendsTypes.Count > 0; } }

        public List<GenericParameter> GenericParameters { get; private set; }
        public bool IsGeneric { get { return GenericParameters.Count > 0; } }

        public List<DeclarationMember> Members { get; private set; }

        public DeclarationBase(string name)
        {
            Name = name;
            ExtendsTypes = new List<TypescriptTypeReference>();
            GenericParameters = new List<GenericParameter>();
            Members = new List<DeclarationMember>();
        }

    }

    public sealed class GenericParameter : TypeDomBase
    {
        public string Name { get; set; }
        public GenericParameter(string name)
        {
            Name = name;
        }
        //extends constraint
        public TypescriptTypeReference Constraint { get; set; }
    }

    public sealed class InterfaceType : DeclarationBase
    {
        public InterfaceType(string name) :base(name)
        {
        }
        // interface xxx<T1,T2> extends ... { } 
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("interface ");
            sb.Append(Name);
            if (IsGeneric)
            {
                sb.Append("<");
                sb.Append(String.Join(", ", GenericParameters.Select(p => p.Name)));
                sb.Append(">");
            }
            if (IsExtending)
            {
                sb.Append(" extends ");
                sb.Append(String.Join(", ", ExtendsTypes.Select(t => t.ToString())));
            }
            return sb.ToString();

        }
    }

    public sealed class ClassType : DeclarationBase
    {
        // class xxx<T1,T2> extends ... implements yyy
        public List<TypescriptTypeReference> Implementations { get; private set; }
        public bool IsImplementing { get { return Implementations.Count > 0; } }
        public ClassType(string name) :base(name)
        {
            Implementations = new List<TypescriptTypeReference>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ");
            sb.Append(Name);
            if (IsGeneric)
            {
                sb.Append("<");
                sb.Append(String.Join(", ", GenericParameters.Select(p => p.Name)));
                sb.Append(">");
            }
            if (IsExtending)
            {
                sb.Append(" extends ");
                sb.Append(String.Join(", ", ExtendsTypes.Select(t => t.ToString())));
            }
            if (IsImplementing)
            {
                sb.Append(" implements");
                sb.Append(String.Join(", ", Implementations.Select(t => t.ToString())));
            }
            return sb.ToString();
        }
    }

}
