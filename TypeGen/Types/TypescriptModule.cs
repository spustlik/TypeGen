using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class TypescriptModule : TypeDomBase
    {
        public string Comment { get; set; }
        public string Name { get; set; }
        public List<ModuleElement> Members { get; internal set; }
        public TypescriptModule(string name)
        {
            Name = name;
            Members = new List<ModuleElement>();
        }

        public void MakeAllExportable(bool isExporting)
        {
            foreach (var element in Members)
            {
                element.IsExporting = isExporting;
            }
        }

        public override string ToString()
        {
            return "module " + Name + " (" + Members.Count + " members)";
        }

    }

}
