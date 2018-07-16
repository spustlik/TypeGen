using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class ReflectionGeneratorBase
    {
        private Dictionary<Type, TypescriptTypeReference> _typeMap = new Dictionary<Type, TypescriptTypeReference>();

        public const string SOURCETYPE_KEY = "SOURCE_TYPE";
        public const string SOURCEMEMBER_KEY = "SOURCE_PROPERTY";

        public static IEnumerable<Type> ExtractGeneratedTypes(TypescriptModule module)
        {
            return module.Members
                .OfType<DeclarationModuleElement>()
                .SelectMany(d => new TypeDomBase[] { d.Declaration, d.EnumDeclaration })
                .Where(d => d != null)
                .Select(d => GetGeneratedType(d))
                .Where(t => t != null)
                .Concat(
                    module.Members.OfType<DeclarationModuleElement>().Where(dm => dm.InnerModule != null).SelectMany(dm => ExtractGeneratedTypes(dm.InnerModule))
                )
                .ToArray();
        }
        public static Type GetGeneratedType(TypeDomBase tt)
        {
            object t;
            tt.ExtraData.TryGetValue(SOURCETYPE_KEY, out t);
            return t as Type;
        }

        public IReflectedNamingStrategy NamingStrategy { get; private set; }
        public IGenerationStrategy GenerationStrategy { get; private set; }

        public ReflectionGeneratorBase(IReflectedNamingStrategy naming, IGenerationStrategy generation)
        {
            _typeMap = new Dictionary<Type, TypescriptTypeReference>()
            {
                { typeof(void), PrimitiveType.Void },
                { typeof(object), PrimitiveType.Any },
                { typeof(int), PrimitiveType.Number },
                { typeof(decimal), PrimitiveType.Number },
                { typeof(double), PrimitiveType.Number },
                { typeof(float), PrimitiveType.Number },
                { typeof(string), PrimitiveType.String },
                { typeof(bool), PrimitiveType.Boolean },
                { typeof(DateTime), PrimitiveType.Date },
            };
            NamingStrategy = naming;
            GenerationStrategy = generation;
        }

        public void AddMap(Type type, TypescriptTypeReference reference)
        {
            _typeMap[type] = reference;
        }
        public TypescriptTypeReference GenerateFromType(Type type)
        {
            if (_typeMap.TryGetValue(type, out var result))
                return result;
            return TypeGenerator(type);
        }

        protected virtual TypescriptTypeReference TypeGenerator(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
            {
                var r = GenerateFromType(Nullable.GetUnderlyingType(type));
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
                    return new ArrayType(GenerateFromType(type.GetElementType())) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (typeof(IDictionary<,>).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
            {
                //if (type.IsConstructedGenericType)
                    return new TypescriptTypeReference(
                        new RawStatements(
                                "{",
                                "[ key: ",
                                GenerateFromType(type.GetGenericArguments()[0]),
                                "]: ",
                                GenerateFromType(type.GetGenericArguments()[1]),
                                "}"
                                )
                    ){ ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsConstructedGenericType)
                    return new ArrayType(GenerateFromType(type.GetGenericArguments()[0])) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (type.IsEnum)
            {
                return GenerateEnum(type);
            }
            if (type.IsClass || type.IsInterface)
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var tref = GenerateFromType(type.GetGenericTypeDefinition());
                    foreach (var genericTypeArgument in type.GenericTypeArguments)
                    {
                        if (GenerationStrategy.ShouldGenerateGenericTypeArgument(genericTypeArgument))
                        {
                            tref.GenericParameters.Add(GenerateFromType(genericTypeArgument));
                        }
                    }
                    return tref;
                }
                return GenerateObjectDeclaration(type);
            }

            return new AnyType() { ExtraData = { { SOURCETYPE_KEY, type } } };
        }

        protected virtual DeclarationBase GenerateObjectDeclaration(Type type)
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

        protected virtual void GenerateMethodDeclarations(Type type, DeclarationBase declaration)
        {
            //method declarations
            if (GenerationStrategy.ShouldGenerateMethods(declaration, type))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (!GenerationStrategy.ShouldGenerateMethod(declaration, method))
                        continue;
                    declaration.Members.Add(GenerateMethodDeclaration(method));
                }
            }
        }

        public virtual FunctionDeclarationMember GenerateMethodDeclaration(MethodInfo method)
        {
            var result = new FunctionDeclarationMember(NamingStrategy.GetMethodName(method)) { ExtraData = { { SOURCEMEMBER_KEY, method } } };
            if (method.ReturnType != typeof(void))
            {
                result.ResultType = GenerateFromType(method.ReturnType);
            }
            else
            {
                result.ResultType = PrimitiveType.Void;
            }
            foreach (var p in method.GetParameters())
            {
                var par = GenerateMethodParameter(p);
                result.Parameters.Add(par);
            }
            //generics
            if (method.IsGenericMethod)
            {
                foreach (var garg in method.GetGenericArguments())
                {
                    result.GenericParameters.Add(new GenericParameter(NamingStrategy.GetGenericArgumentName(garg)) { ExtraData = { { SOURCETYPE_KEY, garg } } });
                }
            }

            return result;
        }

        protected virtual FunctionParameter GenerateMethodParameter(ParameterInfo paramInfo)
        {
            var par = new FunctionParameter(paramInfo.Name)
            {
                ParameterType = GenerateFromType(paramInfo.ParameterType),
                ExtraData = { { SOURCEMEMBER_KEY, paramInfo } },
                IsOptional = paramInfo.IsOptional
            };
            //if (ParameterType)
            if (paramInfo.DefaultValue != null)
            {
                par.DefaultValue = GenerationStrategy.GenerateLiteral(paramInfo.DefaultValue, par.ParameterType);
            }
            if (paramInfo.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                par.IsRest = true;
            }
            return par;
        }

        public virtual InterfaceType GenerateInterface(Type type)
        {
            var result = new InterfaceType(NamingStrategy.GetInterfaceName(type));
            GenerateDeclarationBase(result, type);
            //extends from .Base 
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                if (GenerationStrategy.ShouldGenerateBaseClass(result, type, type.BaseType))
                {
                    result.ExtendsTypes.Add(GenerateFromType(type.BaseType));
                }
            }
            //implemented interfaces as extends
            if (GenerationStrategy.ShouldGenerateImplementedInterfaces(result, type))
            {
                var implemented = GetImplementedInterfaces(type).ToArray();
                foreach (var intf in implemented)
                {
                    if (GenerationStrategy.ShouldGenerateImplementedInterface(result, intf))
                    {
                        result.ExtendsTypes.Add(GenerateFromType(intf));
                    }
                }
            }
            result.ExtendsTypes.Sort((x, y) => String.Compare(x.ToString(), y.ToString(), StringComparison.InvariantCulture));
            GenerateProperties(type, result, skipImplementedByInterfaces: true);
            GenerateMethodDeclarations(type, result);
            return result;
        }

        protected virtual IEnumerable<Type> GetImplementedInterfaces(Type type)
        {
            return type.GetImplementedInterfaces();
        }

        public virtual ClassType GenerateClass(Type type)
        {
            var result = new ClassType(NamingStrategy.GetClassName(type));
            GenerateDeclarationBase(result, type);
            //extends from .Base 
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                if (GenerationStrategy.ShouldGenerateBaseClass(result, type, type.BaseType))
                {
                    result.Extends = GenerateFromType(type.BaseType);
                }
            }
            //implemented interfaces as implements
            if (GenerationStrategy.ShouldGenerateImplementedInterfaces(result, type))
            {
                var implemented = GetImplementedInterfaces(type);
                foreach (var intf in implemented)
                {
                    if (GenerationStrategy.ShouldGenerateImplementedInterface(result, intf))
                    {
                        result.Implementations.Add(GenerateFromType(intf));
                    }
                }
                result.Implementations.Sort((x, y) => String.Compare(x.ToString(), y.ToString()));
            }
            GenerateProperties(type, result, skipImplementedByInterfaces: false);
            GenerateMethodDeclarations(type, result);
            return result;
        }

        private void GenerateDeclarationBase(DeclarationBase result, Type type)
        {
            result.ExtraData[SOURCETYPE_KEY] = type;
            _typeMap[type] = result;
            GenerationStrategy.AddDeclaration(result);

            //generics
            if (type.IsGenericType)
            {
                var suffix = new List<string>();
                foreach (var garg in type.GetGenericArguments())
                {
                    if (garg.IsGenericParameter)
                    {
                        result.GenericParameters.Add(new GenericParameter(NamingStrategy.GetGenericArgumentName(garg)) { ExtraData = { { SOURCETYPE_KEY, garg } } });
                    }
                    else
                    {
                        suffix.Add(NamingStrategy.GetGenericArgumentName(garg));
                    }
                }
                if (suffix.Count > 0)
                {
                    result.Name += "_" + String.Join("_", suffix);
                }
            }            
        }

        protected virtual void GenerateProperties(Type type, DeclarationBase result, bool skipImplementedByInterfaces)
        {
            //properties
            if (GenerationStrategy.ShouldGenerateProperties(result, type))
            {
                var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
                foreach (var pi in allProps)
                {
                    if (!GenerationStrategy.ShouldGenerateProperty(result, pi))
                    {
                        result.Members.Add(new RawDeclarationMember(new RawStatements(string.Format("/* GenerationStrategy skipped property {0} */", pi.Name))));
                        continue;
                    }
                    if (skipImplementedByInterfaces)
                    {
                        var intf = IsPropertyImplementedByAnyInterface(pi, type);
                        if (intf!=null)
                        {
                            result.Members.Add(new RawDeclarationMember(new RawStatements(string.Format("/* Property {0} skipped, because it is already implemented by interface {1}*/", pi.Name, intf.Name))));
                            continue;
                        }
                    }
                    var p = GenerateProperty(pi);
                    result.Members.Add(p);
                }
            }
        }

        private Type IsPropertyImplementedByAnyInterface(PropertyInfo pi, Type type)
        {
            if (type.IsInterface)
                return null;
            foreach (var item in GetImplementedInterfaces(type))
            {
                var map = type.GetInterfaceMap(item);
                var methods = new[] { pi.GetGetMethod(), pi.GetSetMethod() }.Where(x=>x!=null).ToArray();
                if (methods.All(m=> map.TargetMethods.Contains(m)))
                {
                    return item;
                }
            }
            return null;
        }

        public virtual DeclarationMember GenerateProperty(PropertyInfo pi)
        {
            var pm = new PropertyMember(NamingStrategy.GetPropertyName(pi))
            {
                ExtraData = { { SOURCEMEMBER_KEY, pi } },
                IsOptional = Nullable.GetUnderlyingType(pi.PropertyType) != null,
                MemberType = GenerateFromType(pi.PropertyType)
            };
            return pm;
        }

        public virtual TypescriptTypeReference GenerateEnum(Type type)
        {
            var enumType = new EnumType(NamingStrategy.GetEnumName(type)) { ExtraData = { { SOURCETYPE_KEY, type } } };
            _typeMap[type] = enumType;
            GenerationStrategy.AddDeclaration(enumType);
            var values = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var value in values)
            {
                enumType.Members.Add(GenerateEnumMember(value));
            }
            return enumType;
        }

        public virtual EnumMember GenerateEnumMember(FieldInfo value)
        {
            var v = Convert.ToInt32(value.GetValue(null));
            var ev = new EnumMember(NamingStrategy.GetEnumMemberName(value), v) { ExtraData = { { SOURCEMEMBER_KEY, value } } };
            return ev;
        }


    }

}
