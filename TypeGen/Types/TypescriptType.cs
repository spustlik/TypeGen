using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class TypescriptTypeBase : TypeDomBase
    {
    }

    public sealed class TypescriptTypeReference : TypeDomBase
    {
        public TypescriptTypeBase ReferencedType { get; }
        public string TypeName { get; }
        public RawStatements Raw { get;  }
        public List<TypescriptTypeReference> GenericParameters { get; } = new List<TypescriptTypeReference>();
        public AnonymousDeclaration Inline { get; }

        public TypescriptTypeReference(TypescriptTypeBase type) 
        {
            ReferencedType = type;
        }
        public TypescriptTypeReference(string typeName) 
        {
            TypeName = typeName;
        }
        public TypescriptTypeReference(RawStatements raw) 
        {
            Raw = raw;
        }
        public TypescriptTypeReference(AnonymousDeclaration inline) 
        {
            Inline = inline;
        }

        public override string ToString()
        {
            if (TypeName != null)
                return TypeName;
            if (ReferencedType != null)
                return "{" + ReferencedType.ToString() + "}";
            if (Raw != null)
                return Raw.ToString();
            return "<TypeRef>";
        }

        public static implicit operator TypescriptTypeReference(TypescriptTypeBase type)
        {
            return new TypescriptTypeReference(type);
        }

    }
}
