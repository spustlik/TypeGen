using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class ReflectionGenerator : ReflectionGeneratorBase
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

        public void MakeAllExportable()
        {
            foreach (var element in Module.Members)
            {
                element.IsExporting = true;
            }
        }

    }

    public enum LetterCasing
    {
        None,
        Lower,
        Upper
    }
    public class NamingStrategy : INamingStrategy
    {
        public string InterfacePrefix { get; set; }
        public LetterCasing FirstLetterCasing { get; set; }
        public NamingStrategy()
        {
            InterfacePrefix = "I";
        }
        protected virtual string FirstLetter(string name)
        {
            switch (FirstLetterCasing)
            {
                case LetterCasing.Lower:
                    return name.Substring(0, 1).ToLower() + name.Substring(1);
                case LetterCasing.Upper:
                    return name.Substring(0, 1).ToUpper() + name.Substring(1);
                case LetterCasing.None:
                default:
                    return name;
            }
        }

        public virtual string GetPropertyName(PropertyInfo property)
        {
            return FirstLetter(property.Name);
        }

        public virtual string GetInterfaceName(Type type)
        {
            var n = GetNonGenericTypeName(type);
            if (!n.StartsWith(InterfacePrefix))
                n = InterfacePrefix + FirstLetter(n);
            else
                n = FirstLetter(n);
            return n;
        }

        public virtual string GetClassName(Type type)
        {
            return FirstLetter(GetNonGenericTypeName(type));
        }

        private string GetNonGenericTypeName(Type type)
        {
            if (type.IsGenericType)
                return type.Name.Substring(0, type.Name.IndexOf('`'));
            return type.Name;
        }

        public virtual string GetEnumName(Type type)
        {
            return FirstLetter(type.Name);
        }

        public virtual string GetEnumMemberName(FieldInfo value)
        {
            return FirstLetter(value.Name);
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

        public GenerationStrategy()
        {
            TargetModule = new TypescriptModule("GeneratedModule");
        }

        public virtual bool ShouldGenerateClass(Type type)
        {
            return GenerateClasses;
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
            return true;
        }

        public virtual bool ShouldGenerateImplementedInterfaces(DeclarationBase decl, Type type)
        {
            return true;
        }

        public virtual void AddDeclaration(DeclarationBase decl)
        {
            TargetModule.Members.Add(decl);
        }

        public virtual void AddDeclaration(EnumType decl)
        {
            TargetModule.Members.Add(decl);
        }

        public virtual RawStatements GenerateLiteral(object value, TypescriptTypeReference type)
        {
            //can use JsonConvert.Serialize object (with casting to target type)
            if (value is string)
                return new RawStatements("'" + value.ToString().Replace("'", "''") + "'");
            if (value is bool)
                return new RawStatements((bool)value ? "true" : "false");
            if (value is float)
                return GenerateLiteral((double)(float)value, type);
            if (value is int)
                return GenerateLiteral((double)(int)value, type);
            if (value is double)
                return new RawStatements(((double)value).ToString(CultureInfo.InvariantCulture));
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
