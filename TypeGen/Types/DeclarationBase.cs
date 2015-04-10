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
        
        public List<GenericParameter> GenericParameters { get; private set; }
        public bool IsGeneric { get { return GenericParameters.Count > 0; } }

        public List<DeclarationMember> Members { get; private set; }

        public DeclarationBase(string name)
        {
            Name = name;
            GenericParameters = new List<GenericParameter>();
            Members = new List<DeclarationMember>();
        }

        public override string ToString()
        {
            return Name +
                    (IsGeneric ? "<" + String.Join(",", GenericParameters) + ">" : "") +
                    " (" + Members.Count + ")";
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
        public override string ToString()
        {
            return Name + (Constraint != null ? "extends " + Constraint : null);
        }
    }

    public sealed class InterfaceType : DeclarationBase
    {
        public List<TypescriptTypeReference> ExtendsTypes { get; private set; }
        public bool IsExtending { get { return ExtendsTypes.Count > 0; } }
        public InterfaceType(string name)
            : base(name)
        {
            ExtendsTypes = new List<TypescriptTypeReference>();
        }
        // interface xxx<T1,T2> extends ... { } 
        public override string ToString()
        {
            return "interface " + base.ToString() +
                    (IsExtending ? " extends " + String.Join(", ", ExtendsTypes.Select(x => x.ToString())) : "") 
                ;
        }
    }

    public sealed class ClassType : DeclarationBase
    {
        // class xxx<T1,T2> extends ... implements yyy
        public List<TypescriptTypeReference> Implementations { get; private set; }
        public bool IsImplementing { get { return Implementations.Count > 0; } }

        public TypescriptTypeReference Extends { get; set; }
        public ClassType(string name) :base(name)
        {
            Implementations = new List<TypescriptTypeReference>();
        }

        public override string ToString()
        {
            return "class " + base.ToString() +
                (Extends!=null ? " extends "+ Extends.ToString() : "") +
                (IsImplementing ? " implements " + String.Join(", ", Implementations.Select(x => x.ToString())) : "");
        }
    }

    public static class ExtendingExtension
    {
        public static IEnumerable<TypescriptTypeReference> GetExtends(this DeclarationBase decl)
        {
            if (decl is InterfaceType)
            {
                return ((InterfaceType)decl).ExtendsTypes;
            }
            else if (decl is ClassType)
            {
                return new[] { ((ClassType)decl).Extends };
            }
            else
            {
                throw new NotImplementedException("Cannot get extends from " + decl);
            }
        }
    }

}
