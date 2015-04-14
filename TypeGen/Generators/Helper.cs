using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public static class Helper
    {
        public static TypescriptTypeReference ExtractArrayElement(this TypescriptTypeReference item)
        {
            if (item.ReferencedType != null && item.ReferencedType is ArrayType)
                return ((ArrayType)item.ReferencedType).ElementType;
            throw new InvalidOperationException("Not an array type");
        }

    }
}
