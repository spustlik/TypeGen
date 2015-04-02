using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class TypescriptTypeBase : TsBase
    {
    }


    //TODO: generic instance
    public class TypescriptTypeReference : TsBase
    {
        public TypescriptTypeBase ReferencedType { get; private set; }
        public string TypeName { get; private set; }
        public TypescriptTypeReference(TypescriptTypeBase type)
        {
            ReferencedType = type;
        }
        public TypescriptTypeReference(string typeName)
        {
            TypeName = typeName;
        }

        public override string ToString()
        {
            return TypeName ?? ReferencedType.ToString();
        }
        public static implicit operator TypescriptTypeReference(TypescriptTypeBase type)
        {
            return new TypescriptTypeReference(type);
        }
    }
}
