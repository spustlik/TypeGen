using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class KnockoutMappedGenerator
    {
        private List<InterfaceType> _toGenerate = new List<InterfaceType>();
        private HashSet<ClassType> _generated = new HashSet<ClassType>();
        private Dictionary<InterfaceType, ClassType> _map = new Dictionary<InterfaceType, ClassType>();
        private TypescriptModule _targetModule;

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
            foreach (var item in interfaces)
            {
                AddToQueue(item);
            }
            while (_toGenerate.Count > 0)
            {
                var intf = _toGenerate[0];
                _toGenerate.RemoveAt(0);
                var cls = GetClass(intf);
                _generated.Add(cls);
                GenerateClassContent(cls, intf);
            }
        }

        public virtual void GenerateClassContent(ClassType cls, InterfaceType intf)
        {
            if (intf.IsExtending)
            {
                var baseIntf = (InterfaceType)intf.ExtendsTypes.First().ReferencedType;
                cls.Extends = GetClass(baseIntf);
                if (intf.ExtendsTypes.Count() > 1)
                {
                    //cls.Comment+="WARNING: interface is extending "+String.Join(",", intf.ExtendsTypes);
                }
            }
            GenerateMembers(cls, intf);
        }

        protected virtual void GenerateMembers(ClassType targetClass, DeclarationBase source)
        {
            foreach (var prop in source.Members.OfType<PropertyMember>())
            {
                var clsMember = new PropertyMember(prop.Name);                
                var koParam = MapReference(prop.MemberType, out var koType);
                clsMember.Initialization = new RawStatements(koType, "<", koParam, ">()");
                targetClass.Members.Add(clsMember);
            }

        }

        protected virtual TypescriptTypeReference MapReference(TypescriptTypeReference typeRef, out string koType)
        {
            koType = "ko.observable";
            if (typeRef.ReferencedType is ArrayType arr)
            {
                koType = "ko.observableArray";
                typeRef = arr.ElementType;
            }
            if (typeRef.ReferencedType is InterfaceType intf)
            {
                if (_map.TryGetValue(intf, out var mapped))
                {
                    typeRef = mapped;
                }
                else
                {
                    //generate?
                    typeRef = GetClass(intf);
                }
            }
            return typeRef;
        }

        public ClassType GetClass(InterfaceType intf)
        {
            if (_map.TryGetValue(intf, out var cls))
                return cls;
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
