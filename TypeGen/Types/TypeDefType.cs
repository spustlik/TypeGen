using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class TypeDefType : TypescriptTypeBase
    {
        public string Name { get; set; }
        public RawStatements RawStatements { get; } = new RawStatements();
        public TypeDefType(string name, params RawStatementContent[] statements)
        {
            Name = name;
            RawStatements.Add(statements);
        }
        public override string ToString()
        {
            return $"type {Name} = {RawStatements}";
        }
    }
}
