using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private const string JSONIGNORE = "System.Text.Json.Serialization.JsonIgnoreAttribute";
        private const string JSONSTRINGENUMCONVERTER = "System.Text.Json.Serialization.JsonStringEnumConverter";
        private const string JSONCONVERTER = "System.Text.Json.Serialization.JsonConverterAttribute";

        internal static bool IsIgnored(PropertyInfo info)
        {
            if (!HasJsonIgnore(info, out var conditionType))
                return false;
            if (conditionType == "Always")
                return true;
            if (conditionType == "Never")
                return false;
            return false; //WhenWritingDefault or WhenWritingNull 
        }

        private static bool HasJsonIgnore(PropertyInfo info, out string conditionType)
        {
            var at = info
                .GetCustomAttributes(true)
                .FirstOrDefault(x => x.GetType().IsTypeBaseOrSelf(JSONIGNORE));
            if (at == null)
            {
                conditionType = null;
                return false;
            }
            conditionType = ((dynamic)at).Condition.ToString();
            return true;
        }

        internal static bool IsStringEnum(MemberInfo info)
        {
            if (!HasJsonConverter(info, out var converterType))
                return false;

            return converterType.IsTypeBaseOrSelf(JSONSTRINGENUMCONVERTER);
        }
        internal static bool HasJsonConverter(ICustomAttributeProvider info, out Type converter)
        {
            var at = info.GetCustomAttributes(true)
                         .FirstOrDefault(x => x.GetType().IsTypeBaseOrSelf(JSONCONVERTER));
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
