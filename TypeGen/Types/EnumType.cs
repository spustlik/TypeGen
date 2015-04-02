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
        public EnumType()
        {
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

    public sealed class EnumMember : TsBase
    {
        public string Name { get; set; }
        public int? Value { get; set; }
        public bool IsHexLiteral { get; set; }
        //TODO: there is "computed member" in spec
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            if (Value.HasValue)
            {
                sb.Append(" = ");
                sb.Append(IsHexLiteral ? "0x" + Value.Value.ToString("X") : Value.Value.ToString());
            }
            return sb.ToString();
        }
    }
}
