using System;
using System.Reflection;

namespace TypeGen.Generators
{
    class KoGenerationStrategy : IGenerationStrategy
    {
        public TypescriptModule TargetModule { get; set; }

        public KoGenerationStrategy()
        {
            TargetModule = new TypescriptModule("Observables");
        }

        public virtual bool ShouldGenerateClass(Type type)
        {
            return type.IsClass;
        }

        protected virtual bool IsSystemType(Type type)
        {
            return type.Assembly == typeof(object).Assembly || type.Namespace.StartsWith("System");
        }

        public virtual bool ShouldGenerateProperties(DeclarationBase decl, Type type)
        {
            return true;
        }

        public virtual bool ShouldGenerateProperty(DeclarationBase decl, PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetGetMethod().IsStatic)
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
            //m.Comment = "generated from " + decl.ExtraData[ReflectionGeneratorBase.SOURCETYPE_KEY];
            TargetModule.Members.Add(m);
        }

        public virtual void AddDeclaration(EnumType decl)
        {
            throw new InvalidOperationException("Enums should be reused from another module");
            //var m = new DeclarationModuleElement(decl);
            //m.Comment = "generated from " + decl.ExtraData[ReflectionGeneratorBase.SOURCETYPE_KEY];
            //TargetModule.Members.Add(m);
        }

        public virtual RawStatements GenerateLiteral(object value, TypescriptTypeReference targetType)
        {
            return GenerationStrategy.GenerateRawLiteral(value, targetType);
        }

        public virtual bool ShouldGenerateMethod(DeclarationBase decl, MethodInfo method)
        {
            return false;
        }

        public virtual bool ShouldGenerateMethods(DeclarationBase decl, Type type)
        {
            return false;
        }

        public bool IsStringEnum(MemberInfo info)
        {
            return NewtonsoftJsonHelper.IsNewtonsoftStringEnum(info);
        }
    }


}
