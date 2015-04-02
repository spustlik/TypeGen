using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public class ReflectionGenerator
    {
        private Dictionary<Type, TypescriptTypeBase> _typeMap = new Dictionary<Type, TypescriptTypeBase>();
        private List<DeclarationBase> _generatedDeclarations = new List<DeclarationBase>();

        private List<EnumType> _generatedEnums = new List<EnumType>();
        public const string SOURCETYPE_KEY = "SOURCE_TYPE";
        public const string SOURCEPROPERTY_KEY = "SOURCE_PROPERTY";

        public NamingStrategy NamingStrategy { get; set; }
        public ClassGenerationStrategy GenerationStrategy { get; set; }

        public IEnumerable<DeclarationBase> Declarations { get { return _generatedDeclarations; } }
        public IEnumerable<EnumType> Enums { get { return _generatedEnums; } }

        public ReflectionGenerator()
        {
            _typeMap = new Dictionary<Type, TypescriptTypeBase>()
            {
                { typeof(int), PrimitiveType.Number },
                { typeof(double), PrimitiveType.Number },
                { typeof(float), PrimitiveType.Number },
                { typeof(string), PrimitiveType.String },
                { typeof(bool), PrimitiveType.Boolean },
                { typeof(DateTime), PrimitiveType.Date },
            };
            NamingStrategy = new NamingStrategy();            
            GenerationStrategy = new ClassGenerationStrategy();
        }

        public void GenerateTypes(IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                ConverType(t);
            }
        }

        public TypescriptTypeReference ConverType(Type type)
        {
            TypescriptTypeBase result;
            if (_typeMap.TryGetValue(type, out result))
                return result;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                var r = ConverType(Nullable.GetUnderlyingType(type));
                r.ExtraData[SOURCETYPE_KEY] = type;
                return r;
            }
            if (type.IsPrimitive)
                return PrimitiveType.Number; //others are in dictionary            
            if (type.IsGenericParameter)
            {
                //TODO:?!?
                //return new GenericParameter() { Name = NamingStrategy.GetGenericArgumentName(type), ExtraData = { { SOURCETYPE_KEY, type } } };
                return new TypescriptTypeReference(NamingStrategy.GetGenericArgumentName(type)) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (type.IsArray)
            {
                if (type.GetArrayRank() == 1)
                    return new ArrayType() { ElementType = ConverType(type.GetElementType()), ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsConstructedGenericType)
                    return new ArrayType() { ElementType = ConverType(type.GetGenericArguments()[0]), ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (type.IsEnum)
            {
                return GenerateEnum(type);
            }
            if (type.IsClass || type.IsInterface)
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var tref = ConverType(type.GetGenericTypeDefinition());
                    tref.GenericParameters.AddRange(type.GenericTypeArguments.Select(a => ConverType(a)));
                    return tref;
                }
                return GenerateClassDeclaration(type);
            }

            return new AnyType() { ExtraData = { { SOURCETYPE_KEY, type } } };
        }


        private DeclarationBase GenerateClassDeclaration(Type type)
        {
            if (GenerationStrategy.ShouldGenerateClass(type))
            {
                return GenerateClass(type);
            }
            else
            {
                return GenerateInterface(type);
            }
        } 
        public InterfaceType GenerateInterface(Type type)
        {
            var result = new InterfaceType() { Name = NamingStrategy.GetInterfaceName(type) };
            GenerateDeclarationBase(result, type);
            //TODO: methods declarations
            return result;
        }

        public ClassType GenerateClass(Type type)
        {
            var result = new ClassType() { Name = NamingStrategy.GetClassName(type)};
            GenerateDeclarationBase(result, type);
            //TODO: implements
            //TODO: methods?
            return result;
        }

        private void GenerateDeclarationBase(DeclarationBase result, Type type)
        {
            result.ExtraData[SOURCETYPE_KEY] = type;
            _typeMap[type] = result;
            _generatedDeclarations.Add(result);

            //TODO: generics
            if (type.IsGenericType)
            {
                foreach (var garg in type.GetGenericArguments())
                {
                    result.GenericParameters.Add(new GenericParameter() { Name = NamingStrategy.GetGenericArgumentName(garg), ExtraData = { { SOURCETYPE_KEY, garg} } });
                }                
            }
            //extends from .Base 
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                if (GenerationStrategy.ShouldGenerateBaseClass(result, type, type.BaseType))
                {
                    result.ExtendsTypes.Add(ConverType(type.BaseType));
                }
            }

            //implemented interfaces
            if (GenerationStrategy.ShouldGenerateImplementedInterfaces(result, type))
            {
                var allInterfaces = type.GetInterfaces();
                var implemented = allInterfaces.Where(intf => type.GetInterfaceMap(intf).TargetMethods.Any(m => m.DeclaringType == type)).ToArray();
                foreach (var intf in implemented)
                {
                    result.ExtendsTypes.Add(ConverType(intf));
                }
            }

            //properties
            if (GenerationStrategy.ShouldGenerateProperties(result, type))
            {
                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static;
                var allProps = type.GetProperties(flags);
                foreach (var pi in allProps)
                {
                    if (!GenerationStrategy.ShouldGenerateProperty(result, pi))
                        continue;
                    var p = GenerateProperty(pi);
                    result.Members.Add(p);
                }
            }            
        }

        public DeclarationMember GenerateProperty(PropertyInfo pi)
        {
            var pm = new PropertyMember()
            {
                Name = NamingStrategy.GetPropertyName(pi),
                ExtraData = { { SOURCEPROPERTY_KEY, pi } },
                IsOptional = Nullable.GetUnderlyingType(pi.PropertyType) != null,
                MemberType = ConverType(pi.PropertyType)
            };
            return pm;
        }

        public TypescriptTypeReference GenerateEnum(Type type)
        {
            var enumType = new EnumType() { Name = NamingStrategy.GetEnumName(type), ExtraData = { { SOURCETYPE_KEY, type } } };
            _typeMap[type] = enumType;
            _generatedEnums.Add(enumType);
            var values = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var value in values)
            {
                enumType.Members.Add(GenerateEnumMember(value));
            }
            return enumType;
        }

        public EnumMember GenerateEnumMember(FieldInfo value)
        {
            var v = (int)value.GetValue(null);
            var ev = new EnumMember() { Name = NamingStrategy.GetEnumMemberName(value), Value = v, ExtraData = { { SOURCEPROPERTY_KEY, value } } };
            return ev;
        }


    }

    public class ClassGenerationStrategy
    {
        public virtual bool ShouldGenerateClass(Type type)
        {
            return false;
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
    }

    public class NamingStrategy
    {
        protected virtual string FirstLetter(string name)
        {
            //leave unchanged
            return name;
        }

        public virtual string GetPropertyName(PropertyInfo property)
        {
            return FirstLetter(property.Name);
        }

        public virtual string GetInterfaceName(Type type)
        {
            var n = GetNonGenericTypeName(type);
            if (!n.StartsWith("I"))
                n = "I" + FirstLetter(n);
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
    }


}
