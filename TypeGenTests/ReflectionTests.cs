using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using TypeGen;
using TypeGen.Reflection;

namespace TypeGenTests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void TestIntf()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateInterface(typeof(TestingClass));

            var g = new OutputGenerator();
            g.Generate(rg.GenerationStrategy.TargetModule);

            Assert.AreEqual(
@"module GeneratedModule {
    interface ITestingClass extends ITestingClassBase {
        Property3: MyEnum;
        Property4?: number;
        Property5: string[];
        Property6: ITestingClass;
        Property7: ITestingClass[];
        Property8: ITestingClass[];
    }
    interface ITestingClassBase extends IMyInterface {
        Property1: number;
        Property2: string;
    }
    interface IMyInterface {
        Property2: string;
    }
    enum MyEnum {
        Value1 = 0,
        Value2 = 1,
        Value3 = 10,
        Value4 = 11
    }
}", g.Output);
        }


        [TestMethod]
        public void TestClassGenericToIntf()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateInterface(typeof(PagedAminUser));
            rg.GenerateInterface(typeof(PagedCompany));

            var g = new OutputGenerator();
            g.Generate(rg.GenerationStrategy.TargetModule);
            Assert.AreEqual(
@"
module GeneratedModule {
    interface IPagedAminUser extends IPagedModel<IAdminUser> {
    }
    interface IPagedModel<T> {
        TotalCount: number;
        Values: T[];
    }
    interface IAdminUser {
        Name: string;
        Login: string;
    }
    interface IPagedCompany extends IPagedModel<ICompanyModel> {
    }
    interface ICompanyModel {
        VAT: string;
        Name: string;
    }
}".Trim(), g.Output.Trim());

        }

        [TestMethod]
        public void TestGenerics2()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateInterface(typeof(Test1<int>));

            var g = new OutputGenerator();
            g.Generate(rg.GenerationStrategy.TargetModule);
            Assert.AreEqual(
@"
module GeneratedModule {
    interface ITest1<Int32> {
        T1: IGenList<string>;
        T2: IGenList<number>;
        T3: IGenList<IGenList<boolean>>;
        T4: IGenList<number[]>[];
    }
    interface IGenList<TI> {
        Values: IGenTest<TI>[];
    }
    interface IGenTest<T> {
        Value: T;
    }
}".Trim(), g.Output.Trim());
        }


        [TestMethod]
        public void TestMethods()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateMethods = true;
            rg.GenerateInterface(typeof(TestGenMethods<>));

            var g = new OutputGenerator();
            g.Generate(rg.GenerationStrategy.TargetModule);

            Assert.AreEqual(@"
module GeneratedModule {
    interface ITestGenMethods<T> {
        function Test1(input: T): string;
        function Test2<T2>(input: T2, withDefault?: string = '42'): boolean;
        function Test3(x: number, ...args: string[]): void;
    }
}".Trim(), g.Output.Trim());
        }
    }

    public class TestGenMethods<T>
    {
        public string Test1(T input)
        {
            throw new NotImplementedException();
        }

        public bool Test2<T2>(T2 input, string withDefault = "42")
        {
            throw new NotImplementedException();
        }

        public void Test3(int? x, params string[] args)
        {
            throw new NotImplementedException();
        }

    }


    public class GenTest<T>
    {
        public T Value { get; set; }
    }
    public class GenList<TI>
    {
        public IEnumerable<GenTest<TI>> Values { get; set; }
    }

    public class Test1<T>
    {
        public GenList<string> T1 { get; set; }
        public GenList<Nullable<int>> T2 { get; set; }
        public GenList<GenList<bool>> T3 { get; set; }
        public IEnumerable<GenList<IEnumerable<T>>> T4{ get; set; }
    }

    public class PagedModel<T>
    {
        public int TotalCount { get; set; }
        public T[] Values { get; set; }
    }

    public class AdminUser
    {
        public string Name { get; set; }
        public string Login { get; set; }
    }
    public class PagedAminUser : PagedModel<AdminUser> { }

    public class CompanyModel
    {
        public string VAT { get; set; }
        public string Name { get; set; }
    }
    public class PagedCompany : PagedModel<CompanyModel> { }

    public interface IMyInterface
    {
        string Property2 { get; set; }
    }
    public class TestingClassBase : IMyInterface
    {
        public int Property1 { get; set; }
        public string Property2 { get; set; }
    }

    public enum MyEnum
    {
        Value1,
        Value2,
        Value3 = 10,
        Value4
    }
    public class TestingClass : TestingClassBase
    {
        public MyEnum Property3 { get; set; }
        public int? Property4 { get; set; }
        public string[] Property5 { get; set; }
        public TestingClass Property6 { get; set; }
        public TestingClass[] Property7 { get; set; }
        public List<TestingClass> Property8 { get; set; }
    }
}
