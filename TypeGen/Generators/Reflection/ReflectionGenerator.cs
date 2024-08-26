using System;
using System.Collections.Generic;
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

        public bool StringEnum_All { get; set; }
        public ReflectionGenerator(
            NamingStrategy namingStrategy = null,
            GenerationStrategy generationStrategy = null)
            : base(namingStrategy ?? new NamingStrategy(), generationStrategy ?? new GenerationStrategy())
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
            if (NewtonsoftJsonHelper.IsJArray(type))
                return new ArrayType(new AnyType()) { ExtraData = { { SOURCETYPE_KEY, type } } };
            if (NewtonsoftJsonHelper.IsFromNewton(type))
                return new AnyType() { ExtraData = { { SOURCETYPE_KEY, type } } };
            if (GenerationStrategy.IsStringEnum(type))
                return GenerateStringUnion(type);
            return base.TypeGenerator(type);
        }

        protected override TypescriptTypeReference GenerateTypeFromProperty(PropertyInfo pi)
        {
            if (GenerationStrategy.IsStringEnum(pi))
            {
                return GenerateStringUnion(pi.PropertyType);
            }
            return base.GenerateTypeFromProperty(pi);
        }
        public virtual TypescriptTypeReference GenerateStringUnion(Type type)
        {
            if (StringEnum_All)
                return this.GenerateStringUnionWithAll(type);
            else
                return this.GenerateStringUnionSimple(type);
        }
        public virtual TypescriptTypeReference GenerateStringUnionSimple(Type type)
        {
            var name = NamingStrategy.GetEnumName(type);
            var strEnumType = new TypeDefType(name) { ExtraData = { { SOURCETYPE_KEY, type } } };

            GenerationStrategy.TargetModule.Members.Add(strEnumType);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            strEnumType.RawStatements.Add(
                String.Join(" | ", fields.Select(field => $"'{field.Name}'"))
                );
            var tref = new TypescriptTypeReference(strEnumType);
            AddMap(type, tref);
            return tref;
        }
        public virtual TypescriptTypeReference GenerateStringUnionWithAll(Type type)
        {
            var name = NamingStrategy.GetEnumName(type);
            //[export] const {enumName}_All = ['Field1', 'Field2', ...]
            var nameAll = name + "_All";
            var strAllType = new RawStatements() { ExtraData = { { SOURCETYPE_KEY, type } } };
            GenerationStrategy.TargetModule.Members.Add(strAllType);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            strAllType.Add("const ", nameAll, " = ");
            strAllType.Add("[");
            strAllType.Add(String.Join(", ", fields.Select(field => $"'{field.Name}'")));
            strAllType.Add("]");

            //[export] type {enumName} = (typeof {enumName}_All)[number]
            var strEnumType = new TypeDefType(name) { ExtraData = { { SOURCETYPE_KEY, type } } };
            GenerationStrategy.TargetModule.Members.Add(strEnumType);
            strEnumType.RawStatements.Add("(", "typeof ", nameAll, ")", "[number]");
            var tref = new TypescriptTypeReference(strEnumType);
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

}
