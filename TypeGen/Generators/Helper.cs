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

        public static Type ExtractAsyncTaskType(Type type)
        {
            if (type == typeof(Task))
                return typeof(void);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

    }
}
