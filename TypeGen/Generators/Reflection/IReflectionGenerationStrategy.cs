using System;
using System.Reflection;

namespace TypeGen.Generators
{
    public interface IGenerationStrategy
    {
        bool ShouldGenerateBaseClass(DeclarationBase decl, Type type, Type baseType);
        bool ShouldGenerateGenericTypeArgument(TypescriptTypeBase result, Type genericTypeArgument);
        bool ShouldGenerateClass(Type type);
        bool ShouldGenerateImplementedInterfaces(DeclarationBase decl, Type type);
        bool ShouldGenerateImplementedInterface(DeclarationBase decl, Type interfaceType);
        bool ShouldGenerateMethod(DeclarationBase decl, MethodInfo method);
        bool ShouldGenerateMethods(DeclarationBase decl, Type type);
        bool ShouldGenerateProperties(DeclarationBase decl, Type type);
        bool ShouldGenerateProperty(DeclarationBase decl, PropertyInfo propertyInfo);

        void AddDeclaration(EnumType decl);
        void AddDeclaration(DeclarationBase decl);
        RawStatements GenerateLiteral(object value, TypescriptTypeReference targetType);
        
    }
}