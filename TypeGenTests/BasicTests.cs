using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Linq;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void TestFormatter()
        {
            var f = new TextFormatter(new StringBuilder());
            f.Write("text1");
            f.Write("line1\nline2");
            f.Write("\nline3\n");
            Assert.AreEqual(@"text1line1
line2
line3
", f.Output.ToString());
        }

        [TestMethod]
        public void TestClassGen()
        {
            var cls = new ClassType() { Name = "testClass" };
            cls.Members.Add(new PropertyMember()
            {
                Name = "testProperty",
                MemberType = new TypescriptTypeReference(new StringType())
            });
            cls.Members.Add(new PropertyMember()
            {
                Name = "testProperty2",
                Accessibility = AccessibilityEnum.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            cls.Members.Add(new PropertyMember()
            {
                Name = "testProperty3",
                Accessibility = AccessibilityEnum.Public,
                MemberType = new ArrayType(PrimitiveType.Boolean)
            });
            Assert.AreEqual(@"class testClass {
    testProperty: string;
    private testProperty2?: number;
    public testProperty3: boolean[];
}", testGen(cls));
        }

        [TestMethod]
        public void TestClassGen2()
        {
            var cls = new ClassType() { Name = "testClass" };
            cls.ExtendsTypes.Add(new TypescriptTypeReference("baseClass1"));
            cls.ExtendsTypes.Add(new TypescriptTypeReference("baseClass2"));
            cls.GenericParameters.Add(new GenericParameter("T1"));
            cls.GenericParameters.Add(new GenericParameter("T2"));
            cls.Implementations.Add(new TypescriptTypeReference("IType1"));
            cls.Implementations.Add(new TypescriptTypeReference("IType2"));
            Assert.AreEqual(@"class testClass<T1, T2> extends baseClass1, baseClass2 implements IType1, IType2 {
}", testGen(cls));
        }


        private string testGen(ClassType cls)
        {
            var g = new OutputGenerator();
            g.Generate(cls);
            return g.Formatter.Output.ToString();
        }

        [TestMethod]
        public void TestInterfaceGen()
        {
            var intf = new InterfaceType() { Name = "testClass" };
            intf.Members.Add(new PropertyMember()
            {
                Name = "testProperty",
                MemberType = new TypescriptTypeReference(new StringType())
            });
            intf.Members.Add(new PropertyMember()
            {
                Name = "testProperty2",
                Accessibility = AccessibilityEnum.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            intf.Members.Add(new PropertyMember()
            {
                Name = "testProperty3",
                Accessibility = AccessibilityEnum.Public,
                MemberType = new TypescriptTypeReference(new BoolType())
            });
            var fun = new FunctionDeclarationMember() { Name = "myFn", ResultType = PrimitiveType.String };
            intf.Members.Add(fun);
            Assert.AreEqual(@"interface testClass {
    testProperty: string;
    private testProperty2?: number;
    public testProperty3: boolean;
    function myFn(): string;
}", testGen(intf));
        }

        private string testGen(InterfaceType cls)
        {
            var g = new OutputGenerator();
            g.Generate(cls);
            return g.Formatter.Output.ToString();
        }

        [TestMethod]
        public void TestFunctions()
        {
            var cls = new ClassType() { Name = "testFunctions" };
            {
                var fn = new FunctionDeclarationMember() { Name = "fn1" };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember() { Name = "fn2", ResultType = new ArrayType(cls) };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember() { Name = "fn3", Parameters = {
                        new FunctionParameter("a") { ParameterType = PrimitiveType.Number },
                        new FunctionParameter("b") { ParameterType = PrimitiveType.Boolean, IsOptional = true},
                        new FunctionParameter("c"),
                        new FunctionParameter("d") { IsRest = true, ParameterType = new ArrayType(PrimitiveType.String)},
                    } };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember()
                {
                    Name = "fn4",
                    GenericParameters = {
                        new GenericParameter("T"),
                        new GenericParameter("T2") { Constraint = cls },
                    },
                    Parameters =
                    {
                        new FunctionParameter("p1") {ParameterType = new TypescriptTypeReference("T2")},
                    }
                };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionMember() { Name = "fn5",
                    Parameters = { new FunctionParameter("arg") { ParameterType = PrimitiveType.Boolean } },
                    Body = new RawStatements("return true;")
                    };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionMember()
                {
                    Name = "fn6",
                    Parameters = { new FunctionParameter("arg") { ParameterType = PrimitiveType.String, DefaultValue = new RawStatements("'42'") } },
                };
                cls.Members.Add(fn);
            }

            Assert.AreEqual(@"
class testFunctions {
    function fn1();
    function fn2(): testFunctions[];
    function fn3(a: number, b?: boolean, c, ...d: string[]);
    function fn4<T, T2 extends testFunctions>(p1: T2);
    function fn5(arg: boolean){
        return true;
    }
    function fn6(arg: string = '42'){
    }
}
".Trim(), testGen(cls));

        }
        [TestMethod]
        public void TestRawStatements()
        {

            var c = new ClassType() { Name = "test" };

            var x2 = "a" + new RawStatement("b");
            var x3 = new RawStatement("a") + "b";            

            var s1 = new RawStatements() { Statements = { "xxx" } };
            var s2 = new RawStatements("asd", new RawStatement("tttt"));
            s1.Add(s2);
            s1.Add(new TypescriptTypeReference(c));
            s1.Add(":");
            s1.Add(c);
            var g = new OutputGenerator();
            g.Generate(s1);
            Assert.AreEqual("xxxasdtttttest:test", g.Formatter.Output.ToString());

            var test2 = new RawStatements("t1 ", c, " t2");
            g.Formatter.Output.Clear();
            g.Generate(test2);
            Assert.AreEqual("t1 test t2", g.Output);
        }

        [TestMethod]
        public void TestEnum()
        {
            var e = new EnumType() { Name = "xTypes" };
            e.Members.Add(new EnumMember() { Name = "Type1" });
            e.Members.Add(new EnumMember() { Name = "Type2", Value = 42 });
            e.Members.Add(new EnumMember() { Name = "Type3", Value = 64, IsHexLiteral = true });
            e.Members.Add(new EnumMember() { Name = "Type4" });
            var g = new OutputGenerator();
            g.Generate(e);
            Assert.AreEqual(@"enum xTypes {
    Type1,
    Type2 = 42,
    Type3 = 0x40,
    Type4
}", g.Formatter.Output.ToString());
        }


        [TestMethod]
        public void TestModule()
        {
            var m = new Module() { Name = "testModule" };
            var cls = new ClassType() { Name = "class1" };
            cls.Members.Add(new PropertyMember() { Name = "Property1", MemberType = PrimitiveType.Boolean });            
            m.Members.Add(cls);
            m.Members.Last().IsExporting = true;
            m.Members.Add(new RawStatements() { Statements = { "function test() : ", cls, " { return null; }" } });
            var g = new OutputGenerator();
            g.Generate(m);
            Assert.AreEqual(@"
module testModule {
    export class class1 {
        Property1: boolean;
    }
    function test() : class1 { return null; }
}
".Trim(), g.Output.Trim());
        }
    }
}
