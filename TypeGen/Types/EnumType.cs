using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class EnumType : TypescriptTypeBase
    {
        public string Name { get; set; }
        public List<EnumMember> Members { get; private set; }
        public EnumType(string name)
        {
            Name = name;
            Members = new List<EnumMember>();
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("enum ");
            sb.Append(Name);
            return sb.ToString();
        }
    }

    public sealed class EnumMember : TypeDomBase
    {
        public string Name { get; set; }
        public RawStatements Value { get; set; }
        public EnumMember(string name, int? value)
        {
            Name = name;
            if (value != null)
            {
                Value = new RawStatements(value.ToString());
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            if (Value!=null)
            {
                sb.Append(" = ");
                sb.Append(Value.ToString());
            }
            return sb.ToString();
        }
    }
}
