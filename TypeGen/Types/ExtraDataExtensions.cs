using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public static class ExtraDataExtensions
    {
        public static Dictionary<string, object> Merge(this Dictionary<string, object> target, Dictionary<string, object> source)
        {
            if (target == null || source == null)
                return target;
            foreach (var item in source)
            {
                if (!target.ContainsKey(item.Key))
                {
                    target[item.Key] = item.Value;
                }
            }
            return target;
        }
    }
}
