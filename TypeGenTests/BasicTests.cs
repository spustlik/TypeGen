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
            var cls = new ClassType("testClass");
            cls.Members.Add(new PropertyMember("testProperty")
            {
                MemberType = new TypescriptTypeReference(new StringType())
            });
            cls.Members.Add(new PropertyMember("testProperty2")
            {                
                Accessibility = AccessibilityEnum.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            cls.Members.Add(new PropertyMember("testProperty3")
            {
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
            var cls = new ClassType("testClass");
            cls.Extends = new TypescriptTypeReference("baseClass1");
            cls.GenericParameters.Add(new GenericParameter("T1"));
            cls.GenericParameters.Add(new GenericParameter("T2"));
            cls.Implementations.Add(new TypescriptTypeReference("IType1"));
            cls.Implementations.Add(new TypescriptTypeReference("IType2"));
            Assert.AreEqual(@"class testClass<T1, T2> extends baseClass1 implements IType1, IType2 {
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
            var intf = new InterfaceType("testClass");
            intf.Members.Add(new PropertyMember("testProperty")
            {
                MemberType = new TypescriptTypeReference(new StringType())
            });
            intf.Members.Add(new PropertyMember("testProperty2")
            {                
                Accessibility = AccessibilityEnum.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            intf.Members.Add(new PropertyMember("testProperty3")
            {                
                Accessibility = AccessibilityEnum.Public,
                MemberType = new TypescriptTypeReference(new BoolType())
            });
            var fun = new FunctionDeclarationMember("myFn") { ResultType = PrimitiveType.String, Accessibility = AccessibilityEnum.Public };
            intf.Members.Add(fun);
            Assert.AreEqual(@"interface testClass {
    testProperty: string;
    private testProperty2?: number;
    public testProperty3: boolean;
    public myFn(): string;
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
            var cls = new ClassType("testFunctions" );
            {
                var fn = new FunctionDeclarationMember("fn1" );
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember("fn2") { ResultType = new ArrayType(cls) };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember("fn3") { Parameters = {
                        new FunctionParameter("a") { ParameterType = PrimitiveType.Number },
                        new FunctionParameter("b") { ParameterType = PrimitiveType.Boolean, IsOptional = true},
                        new FunctionParameter("c"),
                        new FunctionParameter("d") { IsRest = true, ParameterType = new ArrayType(PrimitiveType.String)},
                    } };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionDeclarationMember("fn4")
                {
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
                var fn = new FunctionMember("fn5", new RawStatements("return true;"))
                {
                    Parameters = { new FunctionParameter("arg") { ParameterType = PrimitiveType.Boolean } },
                };
                cls.Members.Add(fn);
            }
            {
                var fn = new FunctionMember("fn6", null)
                {                    
                    Parameters = { new FunctionParameter("arg") { ParameterType = PrimitiveType.String, DefaultValue = new RawStatements("'42'") } },
                };
                cls.Members.Add(fn);
            }

            Assert.AreEqual(@"
class testFunctions {
    fn1();
    fn2(): testFunctions[];
    fn3(a: number, b?: boolean, c, ...d: string[]);
    fn4<T, T2 extends testFunctions>(p1: T2);
    fn5(arg: boolean) {
        return true;
    }
    fn6(arg: string = '42') {
    }
}
".Trim(), testGen(cls));

        }
        [TestMethod]
        public void TestRawStatements()
        {

            var c = new ClassType("test");

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
            var e = new EnumType("xTypes");
            e.Members.Add(new EnumMember("Type1", null));
            e.Members.Add(new EnumMember("Type2", 42 ));
            e.Members.Add(new EnumMember("Type3", null) { Value = new RawStatements("0x40") });
            e.Members.Add(new EnumMember("Type4", null));
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
            var m = new TypescriptModule("testModule");
            var cls = new ClassType("class1");
            cls.Members.Add(new PropertyMember("Property1") { MemberType = PrimitiveType.Boolean });            
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

        [TestMethod]
        public void TestComments()
        {
            var m = new TypescriptModule("testModule") { Comment = "module" };
            var cls = new ClassType("class1");
            cls.Members.Add(new PropertyMember("Property1") { MemberType = PrimitiveType.Boolean, Comment = "property\nsecond line" });
            m.Members.Add(cls);
            m.Members.Last().Comment = "class";
            m.Members.Last().IsExporting = true;
            m.Members.Add(new RawStatements() { Statements = { "function test() : ", cls, " { return null; }" } });
            m.Members.Last().Comment = "raw";
            cls.Members.Add(new FunctionMember("fn",
                new RawStatements("/*comment*/\n", "dosomething();\n", "//comment") )
            {
                Comment = "function",
                Parameters = {
                    new FunctionParameter("x") {ParameterType = PrimitiveType.Boolean, Comment = "param" }
                }
            });
            cls.Members.Last().Comment = "function";
            var g = new OutputGenerator();
            g.GenerateComments = true;
            g.Generate(m);
            Assert.AreEqual(null, 
                Helper.StringCompare(@"
/* module */
module testModule {
    /* class */
    export class class1 {
        /* property
         * second line 
         */
        Property1: boolean;
        /* function */
        fn(/* param */x: boolean) {
            /*comment*/
            dosomething();
            //comment
        }
    }
    /* raw */
    function test() : class1 { return null; }
}
", g.Output));
        }
    }
}
