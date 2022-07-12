using System;
using System.Linq;
using System.Reflection;

namespace TypeGen.Generators
{
    public static class NewtonsoftJsonHelper
    {
        public static bool HasNewtonsoftConverter(object[] attributes, out Type converter)
        {
            var at = attributes.FirstOrDefault(x => x.GetType().IsTypeBaseOrSelf("Newtonsoft.Json.JsonConverterAttribute"));
            if (at == null)
            {
                converter = null;
                return false;
            }

            converter = ((dynamic)at).ConverterType;
            return converter != null;

        }
        public static bool IsNewtonsoftStringEnum(ICustomAttributeProvider info)
        {
            if (!HasNewtonsoftConverter(info.GetCustomAttributes(true), out var converter))
                return false;
            return converter.IsTypeBaseOrSelf("Newtonsoft.Json.Converters.StringEnumConverter");
        }

        public static bool IsNewtonsoftProperty(PropertyInfo property, out string name)
        {
            var jsonAt = property.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("Newtonsoft.Json.JsonPropertyAttribute"));
            if (jsonAt != null)
            {
                name = ((dynamic)jsonAt).PropertyName ?? property.Name;
                return true;
            }
            name = null;
            return false;
        }

        public static bool IsFromNewton(Type type)
        {
            return type.Namespace.StartsWith("Newtonsoft.", StringComparison.InvariantCulture);
        }

        public static bool IsJArray(Type type)
        {
            return type.IsTypeBaseOrSelf("Newtonsoft.Json.Linq.JArray");
        }

        public static bool IsIgnored(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes()
                .Count(at => at.GetType().IsTypeBaseOrSelf("Newtonsoft.Json.JsonIgnoreAttribute")) > 0;
        }
    }
}
