using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public partial class ReflectionGenerator : ReflectionGeneratorBase
    {
        public new NamingStrategy NamingStrategy { get { return (NamingStrategy)base.NamingStrategy; } }
        public new GenerationStrategy GenerationStrategy { get { return (GenerationStrategy)base.GenerationStrategy; } }
        public TypescriptModule Module { get { return GenerationStrategy.TargetModule; } }

        public ReflectionGenerator() : base(new NamingStrategy(), new GenerationStrategy())
        {
        }

        public void GenerateTypes(IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                GenerateFromType(t);
            }
        }
        protected override TypescriptTypeReference TypeGenerator(Type type)
        {
            if (type.IsTypeBaseOrSelf("Newtonsoft.Json.Linq.JArray"))
                return new ArrayType(new AnyType()) { ExtraData = { { SOURCETYPE_KEY, type } } };
            if (type.Namespace.StartsWith("Newtonsoft.", StringComparison.InvariantCulture))
                return new AnyType() { ExtraData = { { SOURCETYPE_KEY, type } } };
            if (type.IsEnum && NewtonsoftJsonHelper.IsNewtonsoftStringEnum(type))
                return GenerateStringUnion(type);
            return base.TypeGenerator(type);
        }

        protected virtual TypescriptTypeReference GenerateStringUnion(Type type)
        {
            var name = NamingStrategy.GetEnumName(type);
            var senumType = new TypeDefType(name) { ExtraData = { { SOURCETYPE_KEY, type } } };

            GenerationStrategy.TargetModule.Members.Add(senumType);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            senumType.RawStatements.Add(
                String.Join(" | ", fields.Select(field => $"'{field.Name}'"))
                );
            var tref = new TypescriptTypeReference(senumType);
            AddMap(type, tref);
            return tref;
        }
    }


    public class NamingStrategy : IReflectedNamingStrategy
    {
        public string InterfacePrefixForClasses { get; set; }
        public string InterfacePrefix { get; set; }
        public LetterCasing FirstLetterCasing { get; set; }

        public NamingStrategy()
        {
            InterfacePrefix = "I";
            InterfacePrefixForClasses = "I";
        }

        public virtual string GetPropertyName(PropertyInfo property)
        {
            if (NewtonsoftJsonHelper.IsNewtonsoftProperty(property, out var name))
                return name;
            return NamingHelper.FirstLetter(FirstLetterCasing, property.Name);
        }

        public virtual string GetInterfaceName(Type type)
        {
            var n = NamingHelper.GetNonGenericTypeName(type);
            if (type.IsInterface)
            {
                n = NamingHelper.RemovePrefix(InterfacePrefix, n, LetterCasing.Upper);
                n = InterfacePrefix + NamingHelper.FirstLetter(FirstLetterCasing, n);
            }
            else
            {
                n = InterfacePrefixForClasses + NamingHelper.FirstLetter(FirstLetterCasing, n);
            }
            return n;
        }

        public virtual string GetClassName(Type type)
        {
            return NamingHelper.FirstLetter(FirstLetterCasing, NamingHelper.GetNonGenericTypeName(type));
        }

        public virtual string GetEnumName(Type type)
        {
            return NamingHelper.FirstLetter(FirstLetterCasing, type.Name);
        }

        public virtual string GetEnumMemberName(FieldInfo value)
        {
            return NamingHelper.FirstLetter(FirstLetterCasing, value.Name);
        }

        public virtual string GetGenericArgumentName(Type garg)
        {
            return garg.Name;
        }

        public virtual string GetMethodName(MethodInfo method)
        {
            return method.Name;
        }
    }

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
