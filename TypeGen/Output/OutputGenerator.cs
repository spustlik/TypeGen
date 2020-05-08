using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{

    [DebuggerDisplay("{Formatter.DebuggerOutput}")]
    public class OutputGenerator
    {
        public TextFormatter Formatter { get; set; }
        public string Output { get { return Formatter.Output.ToString(); } }
        public INameResolver NameResolver { get; set; }
        public bool GenerateComments { get; set; }
        private Stack<bool> _anonymousMembers = new Stack<bool>();
        public OutputGenerator(INameResolver nameResolver = null)
        {
            _anonymousMembers.Push(false);
            GenerateComments = true;
            Formatter = new TextFormatter();
            if (nameResolver == null)
                NameResolver = new DefaultNameResolver();
            else
                NameResolver = nameResolver;
        }

        public void GenerateAmbient(TypescriptModule module)
        {
            Formatter.Write("declare ");
            GenerateModule(module, CanGenerateInAmbientModule);
        }
        public void GenerateNonAmbient(TypescriptModule module)
        {
            GenerateModule(module, e => !CanGenerateInAmbientModule(e));
        }
        public void Generate(TypescriptModule m)
        {
            GenerateModule(m);
        }

        private void GenerateModule(TypescriptModule module, Func<ModuleElement, bool> elementFilter = null)
        {
            GenerateLineComment(module.Comment);
            Formatter.Write("module ");
            Formatter.Write(module.Name);
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            GenerateModuleContent(module, elementFilter);
            Formatter.PopIndent();
            Formatter.WriteEndOfLine();
            Formatter.Write("}");
            Formatter.WriteLine();
        }

        private void GenerateLineComment(string comment)
        {
            if (String.IsNullOrEmpty(comment) || !GenerateComments)
                return;
            var lines = ParseComment(comment);
            Formatter.Write("/*");
            for (int i = 0; i < lines.Length; i++)
            {
                if (i != 0)               
                    Formatter.Write(" * ");
                Formatter.Write(lines[i]);
                if (lines.Length>1)
                    Formatter.WriteLine();
            }
            Formatter.Write(" */");
            Formatter.WriteLine();
        }

        private void GenerateInlineComment(string comment)
        {
            if (String.IsNullOrEmpty(comment) || !GenerateComments)
                return;
            var lines = ParseComment(comment);
            Formatter.Write("/*");
            for (int i = 0; i < lines.Length; i++)
            {
                if (i!=0)
                    Formatter.Write("  ");
                Formatter.Write(lines[i]);
            }
            Formatter.Write("*/");
        }

        private static string[] ParseComment(string comment)
        {
            comment = comment.Replace("\xa\xd", "\xa").Replace("\xd\xa", "\xa").Replace("\xd","\xa");
            if (!comment.StartsWith("*", StringComparison.Ordinal))
                comment = " " + comment + " ";
            return comment.Split(new[] { '\xa' });
        }

        public void GenerateModuleContent(TypescriptModule module, Func<ModuleElement, bool> elementFilter)
        {
            DefaultNameResolver.ThisModule = module;
            var members = module.Members.AsEnumerable();
            if (elementFilter!=null)
            {
                members = members.Where(elementFilter);
            }
            var ordered = OrderByInheritance(members);
            Formatter.WriteSeparated("\n", ordered, Generate);
        }

        private List<ModuleElement> OrderByInheritance(IEnumerable<ModuleElement> members)
        {
            var ordered = members.ToList();
            int i = 0;
            while (i < ordered.Count)
            {
                var md = ordered[i] as DeclarationModuleElement;
                if (md != null)
                {
                    if (md.Declaration != null)
                    {
                        var indexes = md.Declaration
                                        .GetExtends()
                                        .Where(e => e != null)
                                        .Where(et => et.ReferencedType != null)
                                        .Select(et => GetIndexInModule(ordered, et.ReferencedType))
                                        .ToArray();
                        if (indexes.Length > 0)
                        {
                            var max = indexes.Max();
                            if (max > i)
                            {
                                ordered.RemoveAt(i); //moves all next elements  -1
                                ordered.Insert(max, md);
                                continue; //do not increment
                            }
                        }
                    }
                }
                i++;
            }
            return ordered;
        }

        private int GetIndexInModule(List<ModuleElement> ordered, TypescriptTypeBase type)
        {
            return ordered.FindIndex(x => (x is DeclarationModuleElement) && (((DeclarationModuleElement)x).Declaration == type));
        }

        private bool CanGenerateInAmbientModule(ModuleElement moduleElement)
        {
            var de = moduleElement as DeclarationModuleElement;
            if (de == null)
                return true;
            return de.EnumDeclaration == null;
        }

        private void Generate(ModuleElement element)
        {
            GenerateLineComment(element.Comment);
            if (element is DeclarationModuleElement declaration)
            {
                if (element.IsExporting)
                {
                    Formatter.Write("export ");
                }
                Generate(declaration);
            }
            else if (element is RawModuleElement raw)
            {
                Generate(raw);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Cannot generate module element " + element);
            }
        }

        private void Generate(DeclarationModuleElement element)
        {
            if (element.Declaration != null)
            {
                Generate(element.Declaration);
            }
            else if (element.EnumDeclaration != null)
            {
                Generate(element.EnumDeclaration);
            }
            else if (element.InnerModule != null)
            {
                Generate(element.InnerModule);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Cannot generate declaration module element " + element);
            }
        }

        private void Generate(RawModuleElement element)
        {            
            Generate(element.Raw);
        }

        public void Generate(EnumType type)
        {
            Formatter.Write("enum ");
            Formatter.Write(type.Name);
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            Formatter.WriteSeparated(",\n", type.Members, Generate);
            Formatter.WriteEndOfLine();
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        private void Generate(EnumMember m)
        {
            GenerateLineComment(m.Comment);
            Formatter.Write(m.Name);
            if (m.Value != null)
            {
                Formatter.Write(" = ");
                Generate(m.Value);
                //Formatter.Write(m.IsHexLiteral ? String.Format("0x{0:X}", m.Value.Value) : String.Format("{0}", m.Value.Value));
            }
        }

        public void Generate(DeclarationBase decl)
        {
            if (decl is ClassType cls)
            {
                Generate(cls);
            }
            else if (decl is InterfaceType intf)
            {
                Generate(intf);
            }            
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public void Generate(ClassType cls)
        {
            Formatter.Write("class ");
            Formatter.Write(cls.Name);
            if (cls.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", cls.GenericParameters, Generate);
                Formatter.Write(">");
            }
            if (cls.Extends!=null)
            {
                Formatter.Write(" extends ");
                Generate(cls.Extends);
            }
            if (cls.IsImplementing)
            {
                Formatter.Write(" implements ");
                Formatter.WriteSeparated(", ", cls.Implementations, Generate);
            }
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            _anonymousMembers.Push(false);
            foreach (var m in cls.Members)
            {                
                Generate(m);
                Formatter.WriteEndOfLine();
            }
            _anonymousMembers.Pop();
            Formatter.PopIndent();
            Formatter.WriteEndOfLine();
            Formatter.Write("}");
        }

        public void Generate(InterfaceType cls)
        {            
            Formatter.Write("interface ");
            Formatter.Write(cls.Name);
            if (cls.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", cls.GenericParameters, Generate);
                Formatter.Write(">");
            }
            if (cls.IsExtending)
            {
                Formatter.Write(" extends ");
                Formatter.WriteSeparated(", ", cls.ExtendsTypes, Generate);
            }
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            _anonymousMembers.Push(false);
            foreach (var m in cls.Members)
            {
                Generate(m);
                Formatter.WriteEndOfLine();
            }
            _anonymousMembers.Pop();
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        private void Generate(DeclarationMember m)
        {
            GenerateLineComment(m.Comment);
            if (m is PropertyMember prop)
            {
                Generate(prop);
            }   
            else if (m is FunctionMemberBase fun)
            {
                Generate(fun);
            }
            else if (m is RawDeclarationMember raw)
            {
                Generate(raw);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(RawDeclarationMember m)
        {
            if (m.Raw != null)
            {
                Generate(m.Raw);
            }
        }

        private void Generate(FunctionMemberBase f)
        {
            var anonymousMembers = _anonymousMembers.Peek();
            //anonymous:
            // no access
            // name<T>(args):result { body }
            // name: function(args):result {body} cannot be generic
            // name: (args):result => {body} cannot be generic
            //non-anonymous
            // public name<T>(args):result { body }
            // name = function(args):result {body} cannot be generic
            // name = (args):result => {body} cannot be generic
            if (!anonymousMembers)
                Generate(f.Accessibility);
            Formatter.Write(f.Name);
            if (f.Style == FunctionStyle.Method && f.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", f.GenericParameters, Generate);
                Formatter.Write(">");
            }
            if (f.Style == FunctionStyle.Function)
            {
                Formatter.Write(anonymousMembers ? ": " : "= ");
                Formatter.Write("function ");
            }
            if (f.Style == FunctionStyle.ArrowFunction)
            {
                Formatter.Write(anonymousMembers ? ": " : "= ");
            }
            Formatter.Write("(");
            Formatter.WriteSeparated(", ", f.Parameters, Generate);
            Formatter.Write(")");
            if (f.ResultType != null)
            {
                Formatter.Write(": ");
                Generate(f.ResultType);
            }            
            if (f is FunctionDeclarationMember)
            {
                Formatter.Write(";");
            }
            else if (f is FunctionMember member)
            {
                if (f.Style == FunctionStyle.ArrowFunction)
                {
                    Formatter.Write(" => ");
                    Generate(member.Body);
                }
                else
                {
                    Formatter.Write(" {");
                    Formatter.WriteLine();
                    Formatter.PushIndent();
                    if (member.Body != null)
                    {
                        Generate(member.Body);
                    }
                    Formatter.PopIndent();
                    Formatter.WriteEndOfLine();
                    Formatter.Write("}");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(FunctionParameter par)
        {
            GenerateInlineComment(par.Comment);
            if (par.IsRest)
            {
                Formatter.Write("...");
            }
            Generate(par.Accessibility);
            Formatter.Write(par.Name);
            if (par.IsOptional)
            {
                Formatter.Write("?");
            }
            if (par.ParameterType != null)
            {
                Formatter.Write(": ");
                Generate(par.ParameterType);
            }
            if (par.DefaultValue != null)
            {
                Formatter.Write(" = ");
                Generate(par.DefaultValue);
            }
        }

        private void Generate(PropertyMember m)
        {
            Generate(m.Accessibility);
            Formatter.Write(m.Name);
            if (m.IsOptional)
                Formatter.Write("?");
            if (m.MemberType != null)
            {
                Formatter.Write(": ");
                Generate(m.MemberType);
            }
            if (m.Initialization != null)
            {
                Formatter.Write(" = ");
                Generate(m.Initialization);
            }
            Formatter.Write(";");
        }

        private void Generate(AccessibilityEnum? accessibility)
        {
            if (accessibility != null)
            {
                Formatter.Write(accessibility.Value.ToStr());
                Formatter.Write(" ");
            }
        }

        private void Generate(TypescriptTypeReference obj)
        {
            if (!String.IsNullOrEmpty(obj.TypeName))
            {
                Formatter.Write(obj.TypeName);
            }
            else if (obj.Raw != null)
            {
                Generate(obj.Raw);
            }
            else
            {
                GenerateReference(obj.ReferencedType);
            }
            if (obj.GenericParameters.Count > 0)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", obj.GenericParameters, Generate);
                Formatter.Write(">");
            }
        }

        private void GenerateReference(TypescriptTypeBase referencedType)
        {
            if (referencedType is ArrayType arr)
            {
                GenerateReference(arr);
            }
            else if (referencedType is PrimitiveType primitive)
            {
                GenerateReference(primitive);
            }
            else if (referencedType is EnumType enm)
            {
                GenerateReference(enm);
            }
            else if (referencedType is DeclarationBase declaration)
            {
                GenerateReference(declaration);
            }
            else if (referencedType is AnonymousDeclaration anonymous)
            {
                Generate(anonymous);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Cannot generate reference "+referencedType);
            }            
        }

        private void Generate(AnonymousDeclaration anonymous)
        {
            Formatter.Write("{");
            Formatter.WriteLine();
            Formatter.PushIndent();
            _anonymousMembers.Push(true);
            Formatter.WriteSeparated(",\n", anonymous.Members, Generate);
            _anonymousMembers.Pop();
            Formatter.WriteLine();
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        private void GenerateReference(DeclarationBase type)
        {            
            Formatter.Write(NameResolver.GetReferencedName(type));
        }
        private void GenerateReference(EnumType type)
        {            
            Formatter.Write(NameResolver.GetReferencedName(type));
        }

        private void GenerateReference(PrimitiveType type)
        {
            Formatter.Write(type.Name);
        }

        private void GenerateReference(ArrayType t)
        {
            Generate(t.ElementType);
            Formatter.Write("[]");
        }

        private void Generate(GenericParameter obj)
        {
            GenerateInlineComment(obj.Comment);
            Formatter.Write(obj.Name);
            if (obj.Constraint != null)
            {
                Formatter.Write(" extends ");
                Generate(obj.Constraint);
            }
        }

        public void Generate(RawStatements raw)
        {
            foreach (var item in raw.Statements)
            {
                if (item is RawStatement)
                {
                    Formatter.Write(((RawStatement)item).Content);
                }
                else if (item is RawStatementTypeReference)
                {
                    Generate(((RawStatementTypeReference)item).TypeReference);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
