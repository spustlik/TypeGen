using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class TsBase
    {
        public Dictionary<string, object> ExtraData { get; private set; }
        protected TsBase()
        {
             ExtraData = new Dictionary<string, object>();
        }
    }
}
