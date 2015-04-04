using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{

    public class KnockoutGenerator
    {
        public static void MakeObservable(TypescriptModule module)
        {
            foreach (var item in module.Members.OfType<DeclarationModuleElement>())
            {
                if (item.Declaration is InterfaceType)
                {
                    MakeObservable((InterfaceType)item.Declaration);
                }
                else if (item.Declaration is ClassType)
                {
                    MakeObservable((ClassType)item.Declaration);
                }
            }
        }

        private static void MakeObservable(ClassType classType)
        {
            foreach (var item in classType.Members.OfType<PropertyMember>())
            {
                MakeObservableInitialization(item);
            }
        }

        private static void MakeObservableInitialization(PropertyMember item)
        {
            if (item.Initialization != null)
            {
                var type = item.MemberType;
                item.MemberType = null;
                var init = item.Initialization;
                item.Initialization = new RawStatements();
                item.Initialization.Add("ko.observable");
                item.Initialization.Add("(");
                item.Initialization.Statements.AddRange(init.Statements);
                item.Initialization.Add(")");
            }
            else
            {
                var type = item.MemberType;
                item.MemberType = null;
                item.Initialization = new RawStatements();
                if (type != null && type.ReferencedType != null && type.ReferencedType is ArrayType)
                {
                    item.Initialization.Add("ko.observableArray<");
                    item.Initialization.Add(ExtractArrayElement(type));
                    item.Initialization.Add(">()");
                }
                else
                {
                    item.Initialization.Add("ko.observable<");
                    item.Initialization.Add(type);
                    item.Initialization.Add(">()");
                }
            }
        }

        private static void MakeObservable(InterfaceType interfaceType)
        {
            foreach (var item in interfaceType.Members.OfType<PropertyMember>())
            {
                if (item.MemberType != null && item.MemberType.ReferencedType != null && item.MemberType.ReferencedType is ArrayType)
                {
                    item.MemberType = new TypescriptTypeReference("KnockoutObservableArray") { GenericParameters = { ExtractArrayElement(item.MemberType) } };
                }
                else
                {
                    item.MemberType = new TypescriptTypeReference("KnockoutObservable") { GenericParameters = { item.MemberType } };
                }
            }
        }

        private static TypescriptTypeReference ExtractArrayElement(TypescriptTypeReference item)
        {
            if (item.ReferencedType != null && item.ReferencedType is ArrayType)
                return ((ArrayType)item.ReferencedType).ElementType;
            throw new InvalidOperationException("Not array type");
        }

        private Dictionary<DeclarationBase, ClassType> _map;
        
        public void GenerateObservableModule(TypescriptModule source, TypescriptModule target)
        {
            _map = new Dictionary<DeclarationBase, ClassType>();
            foreach (var item in source.Members.OfType<DeclarationModuleElement>())
            {
                if (item.Declaration !=null)
                {
                    var cls = new ClassType(item.Declaration.Name);
                    _map[item.Declaration] = cls;
                    target.Members.Add(cls);
                }
            }

            foreach (var pair in _map)
            {
                GenerateObservable(pair.Key, pair.Value);
            }

        }

        private void GenerateObservable(DeclarationBase src, ClassType target)
        {
            foreach (var item in src.GenericParameters)
            {
                target.GenericParameters.Add(new GenericParameter(item.Name) { Constraint = MapType(item.Constraint) });
            }

            foreach (var item in src.ExtendsTypes)
            {
                target.ExtendsTypes.Add(MapType(item));
            }
            var cls = src as ClassType;
            if (cls != null)
            {
                foreach (var item in cls.Implementations)
                {
                    target.Implementations.Add(MapType(item));
                }
            }
            foreach (var item in src.Members.OfType<PropertyMember>())
            {
                var property = new PropertyMember(item.Name);
                property.Accessibility = item.Accessibility;
                property.Initialization = MapRaw(item.Initialization);
                //property.IsOptional = item.IsOptional;
                property.MemberType = MapType(item.MemberType);
                MakeObservableInitialization(property);
                target.Members.Add(property);
            }
        }

        private RawStatements MapRaw(RawStatements source)
        {
            if (source == null)
                return null;
            var result = new RawStatements();
            foreach (var s in source.Statements)
            {
                var sr = s as RawStatementTypeReference;
                if (sr!=null)
                {
                    result.Add(MapType(sr.TypeReference));
                }
                else
                {
                    result.Add(s);
                }
            }
            return result;
        }

        private TypescriptTypeReference MapType(TypescriptTypeReference r)
        {
            if (r == null)
                return null;
            TypescriptTypeReference result;
            if (r.Raw != null)
            {
                result = new TypescriptTypeReference(MapRaw(r.Raw));
            }
            else if (r.TypeName != null)
            {
                result = new TypescriptTypeReference(r.TypeName);
            }
            else if (r.ReferencedType != null && r.ReferencedType is DeclarationBase)
            {
                result = new TypescriptTypeReference(MapDeclaration((DeclarationBase)r.ReferencedType));
            }
            else
            {
                result = new TypescriptTypeReference(r.ReferencedType);
            }
            foreach (var item in r.GenericParameters)
            {
                result.GenericParameters.Add(MapType(item));
            }
            return result;
        }

        private DeclarationBase MapDeclaration(DeclarationBase declarationBase)
        {
            ClassType rt;
            if (_map.TryGetValue(declarationBase, out rt))
            {
                return rt;
            }
            return declarationBase;
        }

    }
}
