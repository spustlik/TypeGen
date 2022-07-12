using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class KnockoutReflectionGenerator : ReflectionGeneratorBase
    {
        private static string KOPROPERTY_KEY = "koproperty";
        //public new NamingStrategy NamingStrategy { get { return (NamingStrategy)base.NamingStrategy; } }
        //public new GenerationStrategy GenerationStrategy { get { return (GenerationStrategy)base.GenerationStrategy; } }
        //public TypescriptModule Module { get { return GenerationStrategy.TargetModule; } }
        public KnockoutReflectionGenerator() : base(new KoNamingStrategy(), new KoGenerationStrategy())
        {

        }

        public TypescriptModule Module
        {
            get
            {
                return ((KoGenerationStrategy)GenerationStrategy).TargetModule;
            }
        }

        public void GenerateFromTypes(params Type[] types)
        {
            foreach (var item in types)
            {
                GenerateFromType(item);
            }
        }

        public override DeclarationMember GenerateProperty(PropertyInfo pi)
        {
            var result = (PropertyMember)base.GenerateProperty(pi);
            if (result.MemberType != null)
            {
                result.ExtraData[KOPROPERTY_KEY] = result.MemberType;
                if (result.MemberType.ReferencedType is ArrayType)
                {
                    result.MemberType = new TypescriptTypeReference("KnockoutObservableArray") { GenericParameters = { result.MemberType.ExtractArrayElement() } };
                }
                else
                {
                    result.MemberType = new TypescriptTypeReference("KnockoutObservable") { GenericParameters = { result.MemberType } };
                }                
            }
            return result;
        }

        public override ClassType GenerateClass(Type type)
        {
            var result = base.GenerateClass(type);
            foreach (var item in result.Members.OfType<PropertyMember>())
            {
                if (item.MemberType!=null && item.ExtraData.ContainsKey(KOPROPERTY_KEY))
                {
                    item.Initialization = new RawStatements(item.MemberType.TypeName=="KnockoutObservable" ? "ko.observable" : "ko.observableArray", "<", item.MemberType.GenericParameters[0], ">()");
                    item.MemberType = null;
                    item.IsOptional = false;
                }
            }
            return result;
        }

        private class KoNamingStrategy : NamingStrategy
        {
            public KoNamingStrategy()
            {
                InterfacePrefix = "IObservable";
                FirstLetterCasing = LetterCasing.Lower;
            }
            public override string GetClassName(Type type)
            {
                return NamingHelper.FirstLetter(FirstLetterCasing, NamingHelper.GetNonGenericTypeName(type));
            }
        }
    }


}
