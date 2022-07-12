using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;

namespace TypeGen.Generators
{
    public class CoreGenerationStrategy : GenerationStrategy
    {
        public override bool IsStringEnum(MemberInfo info)
        {
            return CoreJsonHelper.IsStringEnum(info)
                || base.IsStringEnum(info);
        }

        public override bool ShouldGenerateProperty(DeclarationBase decl, PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetGetMethod().IsStatic)
                return false;
            if (CoreJsonHelper.IsIgnored(propertyInfo))
                return false;
            
            return base.ShouldGenerateProperty(decl, propertyInfo);
        }

    }

    internal class CoreJsonHelper
    {
        internal static bool IsIgnored(PropertyInfo propertyInfo)
        {
            return propertyInfo
                .GetCustomAttributes()
                .Any(at => at.GetType().IsTypeBaseOrSelf("System.Text.Json.Serialization.JsonIgnoreAttribute"));            
        }

        internal static bool IsStringEnum(MemberInfo info)
        {
            if (!HasJsonConverter(info, out var converterType))
                return false;
            
            return converterType.IsTypeBaseOrSelf("System.Text.Json.Serialization.JsonStringEnumConverter");
        }
        internal static bool HasJsonConverter(ICustomAttributeProvider info, out Type converter)
        {
            var at = info.GetCustomAttributes(true)
                         .FirstOrDefault(x => x.GetType()
                            .IsTypeBaseOrSelf("System.Text.Json.Serialization.JsonConverterAttribute"));
            if (at == null)
            {
                converter = null;
                return false;
            }

            converter = ((dynamic)at).ConverterType;
            return converter != null;

        }

    }
}
