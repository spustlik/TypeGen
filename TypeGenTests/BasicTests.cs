using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
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
                Accessibility = MemberAccessibility.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            cls.Members.Add(new PropertyMember()
            {
                Name = "testProperty3",
                Accessibility = MemberAccessibility.Public,
                MemberType = new TypescriptTypeReference(new ArrayType() { ElementType = new TypescriptTypeReference(new BoolType()) } )
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
            cls.GenericParameters.Add(new GenericParameter() { Name = "T1" });
            cls.GenericParameters.Add(new GenericParameter() { Name = "T2" });
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
                Accessibility = MemberAccessibility.Private,
                IsOptional = true,
                MemberType = new TypescriptTypeReference(new NumberType())
            });
            intf.Members.Add(new PropertyMember()
            {
                Name = "testProperty3",
                Accessibility = MemberAccessibility.Public,
                MemberType = new TypescriptTypeReference(new BoolType())
            });
            Assert.AreEqual(@"interface testClass {
    testProperty: string;
    private testProperty2?: number;
    public testProperty3: boolean;
}", testGen(intf));
        }

        private string testGen(InterfaceType cls)
        {
            var g = new OutputGenerator();
            g.Generate(cls);
            return g.Formatter.Output.ToString();
        }


        [TestMethod]
        public void TestRawStatements()
        {

            var c = new ClassType() { Name = "test" };

            var x2 = "a" + new RawStatement("b");
            var x3 = new RawStatement("a") + "b";            

            var s1 = new RawStatements() { Statements = { "xxx" } };
            var s2 = "asd" + new RawStatement("tttt");
            s1.Add(s2);
            s1.Add(new TypescriptTypeReference(c));
            s1.Add(":");
            s1.Add(c);
            var g = new OutputGenerator();
            g.Generate(s1);
            Assert.AreEqual("xxxasdtttttest:test", g.Formatter.Output.ToString());
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
    }
}
