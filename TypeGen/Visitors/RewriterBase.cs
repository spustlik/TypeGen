using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Visitors
{
    public abstract class RewriterBase
    {
        protected virtual Exception NonVisitable(object element, string message)
        {            
            return new InvalidOperationException(String.Format("Cannot visit " + message, element ?? "<null>"));
        }

        protected virtual Dictionary<string, object> RewriteExtraData(TypeDomBase source)
        {
            return new Dictionary<string, object>(source.ExtraData);
        }
        protected virtual string RewriteComment(string comment)
        {
            return comment;
        }

        public virtual TypescriptModule RewriteModule(TypescriptModule module)
        {
            var result = new TypescriptModule(module.Name)
            {
                Comment = RewriteComment(module.Comment),
                ExtraData = RewriteExtraData(module)
            };
            result.Members.AddRange(module.Members.Select(RewriteModuleElement).Where(x => x != null));
            return result;
        }

        public virtual ModuleElement RewriteModuleElement(ModuleElement element)
        {
            if (element is DeclarationModuleElement)
            {
                return RewriteDeclarationModuleElement((DeclarationModuleElement)element);
            }
            else if (element is RawModuleElement)
            {
                return RewriteRawModuleElement((RawModuleElement)element);
            }
            else
            {
                throw NonVisitable(element, "module element {0}");
            }
        }

        public virtual DeclarationModuleElement RewriteDeclarationModuleElement(DeclarationModuleElement element)
        {            
            if (element.Declaration != null)
            {
                return new DeclarationModuleElement(RewriteDeclaration(element.Declaration)) { ExtraData = RewriteExtraData(element) };
            }
            else if (element.EnumDeclaration != null)
            {
                return new DeclarationModuleElement(RewriteEnumType(element.EnumDeclaration)) { ExtraData = RewriteExtraData(element) };
            }
            else if (element.InnerModule != null)
            {
                return new DeclarationModuleElement(RewriteModule(element.InnerModule)) { ExtraData = RewriteExtraData(element) };
            }
            else
            {
                throw NonVisitable(element, "declaration module element {0}");
            }
        }

        public virtual RawModuleElement RewriteRawModuleElement(RawModuleElement element)
        {
            var result = new RawModuleElement()
            {
                Comment = RewriteComment(element.Comment),
                IsExporting = element.IsExporting,
                ExtraData = RewriteExtraData(element),
                Raw = RewriteRaw(element.Raw)
            };
            return result;
        }

        public virtual EnumType RewriteEnumType(EnumType type)
        {
            var result = new EnumType(type.Name)
            {
                ExtraData = RewriteExtraData(type)
            };
            result.Members.AddRange(type.Members.Select(RewriteEnumMember).Where(x => x != null));
            return result;
        }

        public virtual EnumMember RewriteEnumMember(EnumMember m)
        {
            var result = new EnumMember(m.Name, null)
            {
                Comment = RewriteComment(m.Comment),
                Value = RewriteRaw(m.Value),
                ExtraData = RewriteExtraData(m)
            };
            return result;
        }

        public virtual DeclarationBase RewriteDeclaration(DeclarationBase decl)
        {
            if (decl is ClassType)
            {
                return RewriteClassType((ClassType)decl);
            }
            else if (decl is InterfaceType)
            {
                return RewriteInterfaceType((InterfaceType)decl);
            }
            else
            {
                throw NonVisitable(decl, "declaration {0}");
            }
        }

        public virtual ClassType RewriteClassType(ClassType cls)
        {
            var result = new ClassType(cls.Name)
            {
                ExtraData = RewriteExtraData(cls),
                Extends = RewriteTypeReference(cls.Extends),                
            };
            result.GenericParameters.AddRange(cls.GenericParameters.Select(RewriteGenericParameter).Where(x => x != null));            
            if (cls.Extends != null)
            {
                result.Extends = RewriteTypeReference(cls.Extends);
            }
            result.Implementations.AddRange(cls.Implementations.Select(RewriteTypeReference).Where(x => x != null));
            result.Members.AddRange(cls.Members.Select(RewriteDeclarationMember).Where(x => x != null));             
            return result;
        }

        public virtual InterfaceType RewriteInterfaceType(InterfaceType intf)
        {
            var result = new InterfaceType(intf.Name)
            {
                ExtraData = RewriteExtraData(intf),                
            };
            result.GenericParameters.AddRange(intf.GenericParameters.Select(RewriteGenericParameter).Where(x => x != null));
            result.ExtendsTypes.AddRange(intf.ExtendsTypes.Select(RewriteTypeReference).Where(x => x != null));
            result.Members.AddRange(intf.Members.Select(RewriteDeclarationMember).Where(x => x != null));
            return result;
        }

        public virtual DeclarationMember RewriteDeclarationMember(DeclarationMember m)
        {
            if (m is PropertyMember)
            {
                return RewritePropertyMember((PropertyMember)m);
            }   
            else if (m is FunctionMemberBase)
            {
                return RewriteFunctionMemberBase((FunctionMemberBase)m);
            }
            else if (m is RawDeclarationMember)
            {
                return RewriteRawDeclarationMember((RawDeclarationMember)m);
            }
            else
            {
                throw NonVisitable(m, "declaration member {0}");
            }
        }

        public virtual RawDeclarationMember RewriteRawDeclarationMember(RawDeclarationMember m)
        {
            var result = new RawDeclarationMember(RewriteRaw(m.Raw))
            {
                Comment = RewriteComment(m.Comment),
                ExtraData = RewriteExtraData(m)
            };
            return result;
        }

        public virtual FunctionMemberBase RewriteFunctionMemberBase(FunctionMemberBase fn)
        {
            if (fn is FunctionDeclarationMember)
            {
                return RewriteFunctionDeclarationMember((FunctionDeclarationMember)fn);
            }
            else if (fn is FunctionMember)
            {
                return RewriteFunctionMember((FunctionMember)fn);
            }
            else
            {
                throw NonVisitable(fn, "function member {0}");
            }
        }

        public virtual FunctionDeclarationMember RewriteFunctionDeclarationMember(FunctionDeclarationMember fn)
        {
            var result = new FunctionDeclarationMember(fn.Name)
            {
                Accessibility = RewriteAccessibility(fn.Accessibility),
                Comment = RewriteComment(fn.Comment),
                ExtraData = RewriteExtraData(fn),
                ResultType = RewriteTypeReference(fn.ResultType),
            };
            result.GenericParameters.AddRange(fn.GenericParameters.Select(RewriteGenericParameter).Where(x => x != null));
            result.Parameters.AddRange(fn.Parameters.Select(RewriteFunctionParameter).Where(x => x != null));
            return result;
        }

        public virtual FunctionMember RewriteFunctionMember(FunctionMember fn)
        {
            var result = new FunctionMember(fn.Name, RewriteRaw(fn.Body))
            {
                Accessibility = RewriteAccessibility(fn.Accessibility),
                Comment = RewriteComment(fn.Comment),
                ExtraData = RewriteExtraData(fn),
                ResultType = RewriteTypeReference(fn.ResultType),                
            };
            result.GenericParameters.AddRange(fn.GenericParameters.Select(RewriteGenericParameter).Where(x => x != null));
            result.Parameters.AddRange(fn.Parameters.Select(RewriteFunctionParameter).Where(x => x != null));
            return result;
        }

        public virtual FunctionParameter RewriteFunctionParameter(FunctionParameter par)
        {
            var result = new FunctionParameter(par.Name)
            {
                Accessibility = RewriteAccessibility(par.Accessibility),
                Comment = RewriteComment(par.Comment),
                DefaultValue = RewriteRaw(par.DefaultValue),
                ExtraData = RewriteExtraData(par),
                IsOptional = par.IsOptional,
                IsRest = par.IsRest,
                ParameterType = RewriteTypeReference(par.ParameterType),
            };
            return result;
        }

        public virtual PropertyMember RewritePropertyMember(PropertyMember m)
        {
            var result = new PropertyMember(m.Name)
            {
                Accessibility = RewriteAccessibility(m.Accessibility),
                Comment = RewriteComment(m.Comment),
                ExtraData = RewriteExtraData(m),
                IsOptional = m.IsOptional,
                MemberType = RewriteTypeReference(m.MemberType),
                Initialization = RewriteRaw(m.Initialization),
            };
            return result;
        }

        public virtual AccessibilityEnum? RewriteAccessibility(AccessibilityEnum? accessibility)
        {
            return accessibility;
        }

        public virtual TypescriptTypeReference RewriteTypeReference(TypescriptTypeReference obj)
        {
            if (obj == null)
            {
                return null;
            }
            TypescriptTypeReference result;
            if (!String.IsNullOrEmpty(obj.TypeName))
            {
                result = new TypescriptTypeReference(obj.TypeName);                
            }
            else if (obj.Raw != null)
            {
                result = new TypescriptTypeReference(RewriteRaw(obj.Raw));
            }
            else
            {
                result = new TypescriptTypeReference(RewriteReference(obj.ReferencedType));
            }
            result.ExtraData = RewriteExtraData(obj);
            result.GenericParameters.AddRange(obj.GenericParameters.Select(RewriteTypeReference).Where(x => x != null));
            return result;
        }

        public virtual TypescriptTypeBase RewriteReference(TypescriptTypeBase referencedType)
        {
            if (referencedType is ArrayType)
            {
                return RewriteArrayReference((ArrayType)referencedType);
            }
            else if (referencedType is PrimitiveType)
            {
                return RewritePrimitiveReference((PrimitiveType)referencedType);
            }
            else if (referencedType is EnumType)
            {
                return RewriteEnumReference((EnumType)referencedType);
            }
            else if (referencedType is DeclarationBase)
            {
                return RewriteObjectReference((DeclarationBase)referencedType);
            }
            else
            {
                throw NonVisitable(referencedType, "referenced type {0}");
            }            
        }

        public virtual DeclarationBase RewriteObjectReference(DeclarationBase type)
        {
            //this should be overriden, if user rewrites classes or interfaces
            return type;
        }

        public virtual EnumType RewriteEnumReference(EnumType type)
        {
            //this should be overriden, if user rewrites enums
            return type;
        }

        public virtual PrimitiveType RewritePrimitiveReference(PrimitiveType type)
        {
            return type;
        }

        public virtual ArrayType RewriteArrayReference(ArrayType type)
        {
            var result = new ArrayType(RewriteTypeReference(type.ElementType)) { ExtraData = RewriteExtraData(type) };
            return result;
        }

        public virtual GenericParameter RewriteGenericParameter(GenericParameter obj)
        {
            var result = new GenericParameter(obj.Name)
            {
                Comment = RewriteComment(obj.Comment),
                Constraint = RewriteTypeReference(obj.Constraint),
                ExtraData = RewriteExtraData(obj)
            };
            return result;
        }

        public virtual RawStatements RewriteRaw(RawStatements raw)
        {
            if (raw == null)
                return null;
            var result = new RawStatements() { ExtraData = RewriteExtraData(raw) };
            result.Statements.AddRange(raw.Statements.Select(RewriteStatement).Where(x => x != null));
            return result;
        }

        public virtual RawStatementBase RewriteStatement(RawStatementBase item)
        {
            if (item is RawStatementContent)
            {
                return RewriteRawStatement((RawStatementContent)item);
            }
            else if (item is RawStatementTypeReference)
            {
                return RewriteRawTypeReference((RawStatementTypeReference)item);
            }
            else
            {
                throw NonVisitable(item, "raw statement {0}");
            }
        }

        public virtual RawStatementTypeReference RewriteRawTypeReference(RawStatementTypeReference item)
        {
            var result = new RawStatementTypeReference(RewriteTypeReference(item.TypeReference))
            {
                ExtraData = RewriteExtraData(item)
            };
            return result;
        }

        public virtual RawStatementContent RewriteRawStatement(RawStatementContent item)
        {
            var result = new RawStatementContent(item.Content) { ExtraData = RewriteExtraData(item) };
            return result;
        }
    }
}
