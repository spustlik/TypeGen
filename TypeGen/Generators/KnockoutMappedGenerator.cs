using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    /// <summary>
    /// generates classes with usage of ko.observable and ko.observableArray 
    /// using "map" and on-demand generation of referenced classes
    /// Is is using typescript interfaces module as a input. Interfaces can be generated using ReflectionGenerator.
    /// </summary>
    public class KnockoutMappedGenerator
    {
        public static string KO_TYPE = "knockout_type";
        public static string KO_ITEMTYPE = "knockout_itemtype";
        public static string OPTIMIZE_ENTER =
            //"var o = ko.observable;\n" +
            //"var a = ko.observableArray;\n" +
            "function addMembers(obj, members:string[], arrays:string[]) {\n" +
            "  for(var i=0;i<members.length;i++){\n" +
            "     obj[members[i]] = ko.observable();\n" +
            "  }\n" +
            "  for(var i=0;i<arrays.length;i++){\n" +
            "     obj[arrays[i]] = ko.observableArray();\n" +
            "  }\n" +
            "}\n";
        private List<InterfaceType> _toGenerate = new List<InterfaceType>();
        private HashSet<ClassType> _generated = new HashSet<ClassType>();
        private Dictionary<InterfaceType, DeclarationBase> _map = new Dictionary<InterfaceType, DeclarationBase>();
        private TypescriptModule _targetModule;

        public bool Optimized { get; set; }
        public KnockoutMappedGenerator(TypescriptModule targetModule)
        {
            this._targetModule = targetModule;
        }

        public static TypescriptModule GenerateModule(TypescriptModule sourceModule)
        {
            return GenerateModule(sourceModule, new TypescriptModule("Observables"));
        }

        public static TypescriptModule GenerateModule(TypescriptModule sourceModule, TypescriptModule targetModule)
        {
            var g = new KnockoutMappedGenerator(targetModule);
            g.GenerateClasses(sourceModule.Members
                    .OfType<DeclarationModuleElement>()
                    .Where(x => x.Declaration != null)
                    .Select(x => x.Declaration)
                    .OfType<InterfaceType>());
            return targetModule;
        }

        public void GenerateClasses(IEnumerable<InterfaceType> interfaces)
        {
            if (Optimized)
            {
                _targetModule.Members.Add(new RawStatements(OPTIMIZE_ENTER));
            }
            foreach (var item in interfaces)
            {
                AddToQueue(item);
            }
            while (_toGenerate.Count > 0)
            {
                var intf = _toGenerate[0];
                _toGenerate.RemoveAt(0);
                var target = GetInterfaceMap(intf);
                var cls = (ClassType)target;
                _generated.Add(cls);
                GenerateClassContent(cls, intf);
            }
        }

        public virtual void GenerateClassContent(ClassType cls, InterfaceType intf)
        {
            if (intf.IsExtending)
            {
                var baseIntf = (InterfaceType)intf.ExtendsTypes.First().ReferencedType;
                cls.Extends = GetInterfaceMap(baseIntf);
                if (intf.ExtendsTypes.Count() > 1)
                {
                    //cls.Comment+="WARNING: interface is extending "+String.Join(",", intf.ExtendsTypes);
                }
            }
            if (Optimized)
            {
                GenerateOptimizedMembers(cls, intf);
                return;
            }
            GenerateMembers(cls, intf);
        }

        protected virtual void GenerateOptimizedMembers(ClassType targetClass, DeclarationBase source)
        {
            var members = new List<string>();
            var arrays = new List<string>();
            foreach (var prop in source.Members.OfType<PropertyMember>())
            {
                var obsType = MapReference(prop.MemberType, out var koType, out var isArray);
                if (!isArray)
                    members.Add(prop.Name);
                else
                    arrays.Add(prop.Name);
                var clsMember = new PropertyMember(prop.Name);
                clsMember.MemberType = new TypescriptTypeReference(isArray ? "KnockoutObservableArray" : "KnockoutObservable") { GenericParameters = { obsType } };
                clsMember.ExtraData.Merge(prop.ExtraData);
                clsMember.ExtraData[KO_ITEMTYPE] = obsType;
                clsMember.ExtraData[KO_TYPE] = koType;
                targetClass.Members.Add(clsMember);
            }
            var ctor = new RawStatements("constructor() {\n");
            if (targetClass.Extends != null)
                ctor.Add("    super();\n");
            ctor.Add("    addMembers(this, ");
            ctor.Add("[");
            ctor.Add(String.Join(", ", members.Select(x => "'" + x + "'")));
            ctor.Add("], [");
            ctor.Add(String.Join(", ", arrays.Select(x => "'" + x + "'")));
            ctor.Add("]");
            ctor.Add(");\n", "}");
            targetClass.Members.Add(ctor);
        }

        protected virtual void GenerateMembers(ClassType targetClass, DeclarationBase source)
        {
            foreach (var prop in source.Members.OfType<PropertyMember>())
            {
                GenerateMember(targetClass, prop);
            }
        }

        protected virtual void GenerateMember(ClassType targetClass, PropertyMember prop)
        {
            var clsMember = CreateMember(prop);
            if (clsMember != null)
                targetClass.Members.Add(clsMember);
        }

        protected virtual PropertyMember CreateMember(PropertyMember prop)
        {
            var clsMember = new PropertyMember(prop.Name);
            var obsType = MapReference(prop.MemberType, out var koType, out _);
            clsMember.Initialization = new RawStatements(koType, "<", obsType, ">()");
            //clsMember.Comment =  ReflectionGeneratorBase.GetGeneratedType(prop.MemberType) + "";
            clsMember.ExtraData.Merge(prop.ExtraData);
            clsMember.ExtraData[KO_ITEMTYPE] = obsType;
            clsMember.ExtraData[KO_TYPE] = koType;

            return clsMember;
        }

        protected virtual TypescriptTypeReference MapReference(TypescriptTypeReference typeRef, out string koType, out bool isArray)
        {
            koType = "ko.observable";
            if (typeRef.ReferencedType is ArrayType arr)
            {
                koType = "ko.observableArray";
                isArray = true;
                return MapReference(arr.ElementType, out _, out _);
            }
            isArray = false;
            if (typeRef.ReferencedType is InterfaceType intf)
            {
                return Remap(intf, typeRef);
            }
            return typeRef;
        }

        private TypescriptTypeReference Remap(InterfaceType sourceType, TypescriptTypeReference typeRef)
        {
            if (!_map.TryGetValue(sourceType, out var mapped))
            {
                //generate?
                mapped = GetInterfaceMap(sourceType);
            }
            var result = new TypescriptTypeReference(mapped);
            if (mapped.IsGeneric)
            {
                result.GenericParameters.AddRange(typeRef.GenericParameters);
            }
            return result;
        }

        public DeclarationBase GetInterfaceMap(InterfaceType intf)
        {
            if (_map.TryGetValue(intf, out var result))
                return result;
            return AddToQueue(intf);
        }

        private ClassType AddToQueue(InterfaceType intf)
        {
            var cls = new ClassType(GetClassName(intf));
            _map[intf] = cls;
            _targetModule.Members.Add(cls);
            _toGenerate.Add(intf);
            return cls;
        }

        public void AddMap(InterfaceType intf, DeclarationBase target)
        {
            _map[intf] = target;
        }

        protected virtual string GetClassName(InterfaceType intf)
        {
            if (intf.Name.StartsWith("I"))
            {
                return intf.Name.Substring(1);
            }
            return intf.Name;
        }
    }

}
