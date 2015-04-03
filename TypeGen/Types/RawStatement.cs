﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class RawStatements : TypeDomBase
    {
        public List<RawStatementBase> Statements { get; private set; }
        private RawStatements()
        {
            Statements = new List<RawStatementBase>();
        }
        public RawStatements(IEnumerable<RawStatementBase> values) : this()
        {            
            Add(values);
        }
        public RawStatements(params RawStatementBase[] values) : this()
        {
            Add(values);
        }
        public void Add(IEnumerable<RawStatementBase> values)
        {
            if (values!=null)
                Statements.AddRange(values);
        }
        public void Add(params RawStatementBase[] values)
        {
            if (values != null)
                Statements.AddRange(values);
        }
        public void Add(RawStatements values)
        {
            if (values != null)
                Add(values.Statements);
        }

        public void Add(TypescriptTypeReference tref)
        {
            Add((RawStatementBase)tref);
        }
    }

    public abstract class RawStatementBase : TypeDomBase
    {
        public static implicit operator RawStatementBase(TypescriptTypeReference type)
        {
            return new RawStatementTypeReference(type);
        }
        public static implicit operator RawStatementBase(ClassType type)
        {
            return new RawStatementTypeReference(type);
        }
        public static implicit operator RawStatementBase(InterfaceType type)
        {
            return new RawStatementTypeReference(type);
        }
        public static implicit operator RawStatementBase(EnumType type)
        {
            return new RawStatementTypeReference(type);
        }
        public static implicit operator RawStatementBase(string s)
        {
            return new RawStatement(s);
        }
    }

    public sealed class RawStatement : RawStatementBase
    {
        public string Content { get; set; }
        public RawStatement(string content = null)
        {
            Content = content;
        }
    }

    public sealed class RawStatementTypeReference : RawStatementBase
    {
        public TypescriptTypeReference TypeReference { get; set; }
        public RawStatementTypeReference(TypescriptTypeReference typeRef = null) 
        {
            TypeReference = typeRef;
        }
    }
}
