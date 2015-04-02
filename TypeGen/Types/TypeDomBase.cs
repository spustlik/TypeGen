using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class TypeDomBase
    {
        public Dictionary<string, object> ExtraData { get; private set; }
        protected TypeDomBase()
        {
             ExtraData = new Dictionary<string, object>();
        }
    }
}
