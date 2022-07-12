using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class DeclarationBase : TypescriptTypeBase
    {
        public string Name { get; set; }

        public List<GenericParameter> GenericParameters { get; } = new List<GenericParameter>();
        public bool IsGeneric { get { return GenericParameters.Count > 0; } }

        public List<DeclarationMember> Members { get; } = new List<DeclarationMember>();

        public DeclarationBase(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}{(IsGeneric ? "<" + String.Join(",", GenericParameters) + ">" : "")} ({Members.Count} members)";
        }
    }

    public class AnonymousDeclaration : TypescriptTypeBase
    {
        public List<DeclarationMember> Members { get; } = new List<DeclarationMember>();
        public override string ToString()
        {
            return $"(anonymous {Members.Count} members)";
        }
    }

    public sealed class GenericParameter : TypeDomBase
    {
        public string Comment { get; set; }
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
        public List<TypescriptTypeReference> ExtendsTypes { get; } = new List<TypescriptTypeReference>();
        public bool IsExtending { get { return ExtendsTypes.Count > 0; } }
        public InterfaceType(string name)
            : base(name)
        {
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
        public List<TypescriptTypeReference> Implementations { get; } = new List<TypescriptTypeReference>();
        public bool IsImplementing { get { return Implementations.Count > 0; } }

        public TypescriptTypeReference Extends { get; set; }
        public ClassType(string name) :base(name)
        {
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
            if (decl is InterfaceType intf)
            {
                return intf.ExtendsTypes;
            }
            else if (decl is ClassType cls)
            {
                return (new[] { cls.Extends }).Where(x => x != null);
            }
            else
            {
                throw new NotImplementedException("Cannot get extends from " + decl);
            }
        }
    }

}
