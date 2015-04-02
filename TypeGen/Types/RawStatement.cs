using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class RawStatements : TsBase
    {
        public List<RawStatementBase> Statements { get; private set; }
        public RawStatements(IEnumerable<RawStatementBase> values = null)
        {
            Statements = new List<RawStatementBase>();
            if (values != null)
            {
                Add(values);
            }
        }
        public void Add(IEnumerable<RawStatementBase> values)
        {
            Statements.AddRange(values);
        }
        public void Add(params RawStatementBase[] values)
        {
            Statements.AddRange(values);
        }
        public void Add(RawStatements values)
        {
            Add(values.Statements);
        }

        public void Add(TypescriptTypeReference tref)
        {
            Add((RawStatementBase)tref);
        }
    }

    public abstract class RawStatementBase : TsBase
    {
        public static RawStatements operator +(RawStatementBase left, string right)
        {
            return new RawStatements() { Statements = { left, right } };
        }
        public static RawStatements operator +(string left, RawStatementBase right)
        {
            return new RawStatements() { Statements = { left, right } };
        }
        public static RawStatements operator +(RawStatementBase left, RawStatementBase right)
        {
            return new RawStatements() { Statements = { left, right } };
        }
        public static implicit operator RawStatementBase(TypescriptTypeReference type)
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
