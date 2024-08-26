using System;
using System.Globalization;
using System.Reflection;

namespace TypeGen.Generators
{
    public class GenerationStrategy : IGenerationStrategy
    {
        public TypescriptModule TargetModule { get; set; }
        /// <summary>
        /// generate classes instead of interfaces
        /// </summary>
        public bool GenerateClasses { get; set; }
        /// <summary>
        /// enable generation of methods
        /// </summary>
        public bool GenerateMethods { get; set; }

        /// <summary>
        /// enable generation of interfaces implemented in System namespace
        /// </summary>
        public bool GenerateSystemInterfaces { get; set; }

        /// <summary>
        /// add comment with source name
        /// </summary>
        public bool CommentSource { get; set; }

        public GenerationStrategy()
        {
            TargetModule = new TypescriptModule("GeneratedModule");
        }


        public virtual bool ShouldGenerateClass(Type type)
        {
            return GenerateClasses;
        }

        protected virtual bool IsSystemType(Type type)
        {
            return type.Assembly == typeof(object).Assembly || type.Namespace.StartsWith("System");
        }

        public virtual bool ShouldGenerateProperties(DeclarationBase decl, Type type)
        {
            return true;
        }

        public virtual bool IsStringEnum(MemberInfo info)
        {
            return /*type.IsEnum && */ NewtonsoftJsonHelper.IsNewtonsoftStringEnum(info);
        }

        public virtual bool ShouldGenerateProperty(DeclarationBase decl, PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetGetMethod().IsStatic)
                return false;
            if (NewtonsoftJsonHelper.IsIgnored(propertyInfo))
                return false;

            return true;
        }

        public virtual bool ShouldGenerateBaseClass(DeclarationBase decl, Type type, Type baseType)
        {
            if (IsSystemType(baseType))
                return false;
            return true;
        }

        public bool ShouldGenerateGenericTypeArgument(Type genericTypeArgument)
        {
            return true;
        }

        public bool ShouldGenerateImplementedInterface(DeclarationBase decl, Type interfaceType)
        {
            return !IsSystemType(interfaceType);
        }

        public virtual bool ShouldGenerateImplementedInterfaces(DeclarationBase decl, Type type)
        {
            return !IsSystemType(type);
        }

        public virtual void AddDeclaration(DeclarationBase decl)
        {
            var m = new DeclarationModuleElement(decl);
            if (CommentSource)
                m.Comment = "generated from " + decl.ExtraData[ReflectionGeneratorBase.SOURCETYPE_KEY];
            TargetModule.Members.Add(m);
        }

        public virtual void AddDeclaration(EnumType decl)
        {
            var m = new DeclarationModuleElement(decl);
            if (CommentSource)
                m.Comment = "generated from " + decl.ExtraData[ReflectionGeneratorBase.SOURCETYPE_KEY];
            TargetModule.Members.Add(m);
        }

        public virtual RawStatements GenerateLiteral(object value, TypescriptTypeReference targetType)
        {
            return GenerateRawLiteral(value, targetType);
        }

        public static RawStatements GenerateRawLiteral(object value, TypescriptTypeReference targetType)
        {
            //can use JsonConvert.Serialize object (with casting to target type)
            if (value is string)
                return new RawStatements("'" + value.ToString().Replace("'", "''") + "'");
            if (value is bool b)
                return new RawStatements(b ? "true" : "false");
            if (value is float f)
                return GenerateRawLiteral((double)f, targetType);
            if (value is int i)
                return GenerateRawLiteral((double)i, targetType);
            if (value is double d)
                return new RawStatements(d.ToString(CultureInfo.InvariantCulture));
            return null;
        }

        public virtual bool ShouldGenerateMethod(DeclarationBase decl, MethodInfo method)
        {
            return true;
        }

        public virtual bool ShouldGenerateMethods(DeclarationBase decl, Type type)
        {
            return GenerateMethods;
        }

    }

}
