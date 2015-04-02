﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class TypescriptTypeBase : TypeDomBase
    {
    }


    //TODO: generic instance
    public class TypescriptTypeReference : TypeDomBase
    {
        public TypescriptTypeBase ReferencedType { get; private set; }
        public string TypeName { get; private set; }
        public List<TypescriptTypeReference> GenericParameters { get; private set; }
        private TypescriptTypeReference()
        {
            GenericParameters = new List<TypescriptTypeReference>();
        }
        public TypescriptTypeReference(TypescriptTypeBase type) : this()
        {
            ReferencedType = type;
        }
        public TypescriptTypeReference(string typeName) : this()
        {
            TypeName = typeName;
        }

        public override string ToString()
        {
            return TypeName ?? ReferencedType.ToString();
        }
        public static implicit operator TypescriptTypeReference(TypescriptTypeBase type)
        {
            return new TypescriptTypeReference(type);
        }

    }
}