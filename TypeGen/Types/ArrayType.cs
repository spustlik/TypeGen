using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class ArrayType: TypescriptTypeBase
    {
        public TypescriptTypeReference ElementType { get; set; }

        public override string ToString()
        {
            return ElementType.ToString() + "[]";
        }

        public ArrayType(TypescriptTypeReference elementType)
        {
            ElementType = elementType;
        }
    }
}
