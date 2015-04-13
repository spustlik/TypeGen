using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class KnockoutOptions
    {
        public bool Enable_JsConversion_Functions { get; set; }
        public bool JsConversion_Function_PropertyExistenceCheck { get; set; }
        public virtual string GetInterfaceName(DeclarationBase source)
        {
            var name = source.Name;
            name = NamingHelper.RemovePrefix("I", name, LetterCasing.Upper);
            name = "IObservable" + NamingHelper.FirstLetter(LetterCasing.Upper, name);
            return name;
        }
        public virtual string GetClassName(DeclarationBase source)
        {
            var name = source.Name;
            name = NamingHelper.FirstLetter(LetterCasing.Lower, name);
            return name;
        }
    }

    public class KnockoutGenerator
    {
        #region Static makers
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

        private void MakeObservableInterfaceProperty(PropertyMember property)
        {
            property.Initialization = null;
            var type = property.MemberType;
            if (type != null && type.ReferencedType != null)
            {
                if (type.ReferencedType is ArrayType)
                {
                    property.MemberType = new TypescriptTypeReference("KnockoutObservableArray") { GenericParameters = { ExtractArrayElement(type) } };
                }
                else
                {
                    property.MemberType = new TypescriptTypeReference("KnockoutObservable") { GenericParameters = { type } };
                }
            }
            else
            {
                property.MemberType = new TypescriptTypeReference("KnockoutObservable<any>");
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

        #endregion
        private static TypescriptTypeReference ExtractArrayElement(TypescriptTypeReference item)
        {
            if (item.ReferencedType != null && item.ReferencedType is ArrayType)
                return ((ArrayType)item.ReferencedType).ElementType;
            throw new InvalidOperationException("Not array type");
        }

        public KnockoutOptions Options { get; private set; }

        class MapItem
        {
            public ClassType Class;
            public InterfaceType Interface;
        }
        private Dictionary<DeclarationBase, MapItem> _map;
        private TypescriptModule _target;
        private Stack<bool> _interfaceMapping;
        public KnockoutGenerator()
        {
            _interfaceMapping = new Stack<bool>();
            _map = new Dictionary<DeclarationBase, MapItem>();
            Options = new KnockoutOptions();
        }

        public void GenerateObservableModule(TypescriptModule source, TypescriptModule target, bool interfaces)
        {
            // src class      -> class       base class -> class              implemented interfaces -> implements obsInterface 
            // src interface  ->  !implementation class implemented interfaces -> implements observableInterface + code implementation (makes sense, interface defines api for server, I want to create model )
            // src class      -> interface   base class -> obsInterface       implemented interfaces -> implements obsInterface (makes sense, I want to implement model myself, interfaces defines content)
            // src interface  -> interface   base class -> obsInterface       implemented interfaces -> implements obsInterface 

            _target = target;
            BeginInterfaceMapping(interfaces);
            try
            {
                foreach (var item in source.Members.OfType<DeclarationModuleElement>().Where(d => d.Declaration != null))
                {
                    MapDeclaration(item.Declaration, createMap: true);
                }
            }
            finally
            {
                EndInterfaceMapping();
            }

        }

        private void GenerateObservableInterface(DeclarationBase source, InterfaceType target)
        {
            foreach (var extends in source.GetExtends())
            {
                target.ExtendsTypes.Add(MapType(extends));
            }
            if (source is ClassType)
            {
                foreach (var implements in ((ClassType)source).Implementations)
                {
                    target.ExtendsTypes.Add(MapType(implements));
                }
            }
            GenerateObservableBase(source, target);
        }

        private void GenerateObservableClass(DeclarationBase source, ClassType target)
        {
            if (source is ClassType)
            {
                GenerateObservableClassFromClass((ClassType)source, target);
            }
            else if (source is InterfaceType)
            {
                GenerateObservableClassFromInterface((InterfaceType)source, target);
            }
            else
            {
                throw new NotImplementedException("Generating observable class from " + source);
            }
        }

        private void GenerateObservableClassFromInterface(InterfaceType src, ClassType target)
        {
            if (src.ExtendsTypes.Any(t => t.ReferencedType is ClassType))
            {
                throw new Exception("Interface " + src.Name + " cannot extend class");
            }
            foreach (var item in src.ExtendsTypes)
            {
                if (item.ReferencedType is InterfaceType)
                {
                    // interface inherits from another interfaces, so make this class implement them                    
                    BeginInterfaceMapping(true);
                    try
                    {
                        var mapped = MapType(item);
                        target.Implementations.Add(mapped);
                        // implementation code
                        var intf = (InterfaceType)item.ReferencedType;
                        target.Members.Add(new RawDeclarationMember(new RawStatements("// implementation of " + mapped)));
                        foreach (var member in intf.Members.OfType<PropertyMember>())
                        {
                            GenerateObservableProperty(target, member);
                        }
                    }
                    finally
                    {
                        EndInterfaceMapping();
                    }
                }
                else
                {
                    target.Implementations.Add(MapType(item));
                }
            }

            GenerateObservableBase(src, target);
            if (Options.Enable_JsConversion_Functions)
            {
                AddFromJsFunction(src, target);
            }

        }

        private void GenerateObservableClassFromClass(ClassType src, ClassType target)
        {
            BeginInterfaceMapping(true);
            try
            {
                foreach (var item in src.Implementations)
                {
                    target.Implementations.Add(MapType(item));
                }
            }
            finally
            {
                EndInterfaceMapping();
            }
            GenerateObservableBase(src, target);
            if (Options.Enable_JsConversion_Functions)
            {
                AddFromJsFunction(src, target);
            }
        }

        private void GenerateObservableBase(DeclarationBase src, DeclarationBase target)
        {
            foreach (var item in src.GenericParameters)
            {
                target.GenericParameters.Add(new GenericParameter(item.Name) { Constraint = MapType(item.Constraint) });
            }

            foreach (var srcItem in src.Members.OfType<PropertyMember>())
            {
                GenerateObservableProperty(target, srcItem);
            }
        }

        private void GenerateObservableProperty(DeclarationBase target, PropertyMember source)
        {
            var property = new PropertyMember(source.Name);
            property.Accessibility = source.Accessibility;
            property.Initialization = MapRaw(source.Initialization);
            //property.IsOptional = item.IsOptional;
            property.MemberType = MapType(source.MemberType);
            MakeObservableProperty(property);
            target.Members.Add(property);
        }

        private void MakeObservableProperty(PropertyMember property)
        {
            if (_interfaceMapping.Peek())
            {
                MakeObservableInterfaceProperty(property);
            }
            else
            {
                MakeObservableInitialization(property);
            }
        }

        private void AddFromJsFunction(DeclarationBase src, ClassType target)
        {
            var fromJS = new RawStatements();
            if (target.Extends != null)
            {
                fromJS.Add("super.fromJS(obj);\n");
            }
            foreach (var targetItem in target.Members.OfType<PropertyMember>())
            {
                var srcItem = src.Members.OfType<PropertyMember>().First(x => x.Name == targetItem.Name);
                TypescriptTypeBase itemType = null;
                if (srcItem.MemberType != null && srcItem.MemberType.ReferencedType != null)
                    itemType = srcItem.MemberType.ReferencedType;

                if (Options.JsConversion_Function_PropertyExistenceCheck)
                {
                    fromJS.Add("if (obj." + srcItem.Name + ") { ");
                }

                if (itemType is ArrayType)
                {
                    var elementType = ExtractArrayElement(srcItem.MemberType);
                    if (elementType != null && elementType.ReferencedType != null && elementType.ReferencedType is DeclarationBase)
                    {
                        fromJS.Add("this." + srcItem.Name + "(obj." + srcItem.Name + ".map(item=>new ", elementType, "().fromJS(item)))");
                    }
                    else
                    {
                        //todo:conversions
                        fromJS.Add("this." + srcItem.Name + "(obj." + srcItem.Name + ")");
                    }
                }
                else
                {
                    if (itemType == PrimitiveType.Date)
                    {
                        fromJS.Add("this." + srcItem.Name + "(new Date(obj." + srcItem.Name + "))");
                    }
                    else
                    {
                        fromJS.Add("this." + srcItem.Name + "(obj." + srcItem.Name + ")");
                    }
                }
                if (Options.JsConversion_Function_PropertyExistenceCheck)
                {
                    fromJS.Add("};");
                }
                fromJS.Add("\n");
            }
            fromJS.Add("return this;");
            target.Members.Add(new FunctionMember("fromJS", fromJS) { Parameters = { new FunctionParameter("obj") } });
        }

        private RawStatements MapRaw(RawStatements source)
        {
            if (source == null)
                return null;
            var result = new RawStatements();
            foreach (var s in source.Statements)
            {
                var sr = s as RawStatementTypeReference;
                if (sr != null)
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

        private void BeginInterfaceMapping(bool isInterface)
        {
            _interfaceMapping.Push(isInterface);
        }

        private void EndInterfaceMapping()
        {
            _interfaceMapping.Pop();
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
            else if (r.ReferencedType != null && r.ReferencedType is ArrayType)
            {
                var array = (ArrayType)r.ReferencedType;
                result = new TypescriptTypeReference(new ArrayType(MapType(array.ElementType)));
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

        private DeclarationBase MapDeclaration(DeclarationBase declarationBase, bool createMap = true)
        {
            MapItem mapItem;
            if (!_map.TryGetValue(declarationBase, out mapItem) && !createMap)
                return declarationBase;

            if (mapItem == null)
            {
                mapItem = new MapItem();
                _map[declarationBase] = mapItem;
            }
            if (_interfaceMapping.Peek())
            {
                if (mapItem.Interface == null)
                {
                    mapItem.Interface = new InterfaceType(Options.GetInterfaceName(declarationBase));
                    _target.Members.Add(mapItem.Interface);
                    GenerateObservableInterface(declarationBase, mapItem.Interface);
                }
                return mapItem.Interface;
            }
            else
            {
                if (mapItem.Class == null)
                {
                    mapItem.Class = new ClassType(Options.GetClassName(declarationBase));
                    _target.Members.Add(mapItem.Class);
                    GenerateObservableClass(declarationBase, mapItem.Class);
                }
                return mapItem.Class;
            }
        }

    }
}
