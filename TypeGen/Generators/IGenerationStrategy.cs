using System;
using System.Reflection;

namespace TypeGen
{
    public interface IGenerationStrategy
    {
        bool ShouldGenerateBaseClass(DeclarationBase decl, Type type, Type baseType);
        bool ShouldGenerateClass(Type type);
        bool ShouldGenerateImplementedInterfaces(DeclarationBase decl, Type type);
        bool ShouldGenerateMethod(DeclarationBase decl, MethodInfo method);
        bool ShouldGenerateMethods(DeclarationBase decl, Type type);
        bool ShouldGenerateProperties(DeclarationBase decl, Type type);
        bool ShouldGenerateProperty(DeclarationBase decl, PropertyInfo propertyInfo);

        void AddDeclaration(EnumType decl);
        void AddDeclaration(DeclarationBase decl);
        RawStatements GenerateLiteral(object value, TypescriptTypeReference type);

    }
}