using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class KnockoutTests
    {
        [TestMethod]
        public void TestInterfacesToObservableInterfaces()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    interface ITest1 {
        Prop1: string;
        Prop2: number;
    }
    interface ITest1B extends ITest1 {
        Prop3: boolean;
        Ref: ITest1;
        PropArray: string[];
        SelfArray: ITest1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, true);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    interface ITest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface ITest1B extends ITest1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<ITest1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<ITest1B>;
    }
}".Trim(), o.Output.Trim());

        }

        [TestMethod]
        public void TestClassesToObservableInterfaces()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = true;
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    class Test1 {
        Prop1: string;
        Prop2: number;
    }
    class Test1B extends Test1 {
        Prop3: boolean;
        Ref: Test1;
        PropArray: string[];
        SelfArray: Test1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, true);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    interface Test1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface Test1B extends Test1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<Test1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<Test1B>;
    }
}
".Trim(), o.Output.Trim());

        }
        [TestMethod]
        public void TestClassesToObservableClasses()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = true;
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    class Test1 {
        Prop1: string;
        Prop2: number;
    }
    class Test1B extends Test1 {
        Prop3: boolean;
        Ref: Test1;
        PropArray: string[];
        SelfArray: Test1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, false);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    class Test1B {
        Prop3 = ko.observable<boolean>();
        Ref = ko.observable<Test1>();
        PropArray = ko.observableArray<string>();
        SelfArray = ko.observableArray<Test1B>();
    }
    class Test1 {
        Prop1 = ko.observable<string>();
        Prop2 = ko.observable<number>();
    }
}
".Trim(), o.Output.Trim());

        }




        private static ReflectionGenerator test3refl(bool sourceClasses)
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = sourceClasses;
            rg.NamingStrategy.InterfacePrefix = "";
            rg.GenerateTypes(new[] { typeof(Test3), typeof(Test3A) });
            return rg;
        }

        private static string test3reflstr(bool sourceclasses)
        {
            var rg = test3refl(sourceclasses);
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            return o1.Output;
        }
        private static string test3(bool sourceClasses, bool destClasses)
        {
            var rg = test3refl(sourceClasses);

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, !destClasses);

            var o = new OutputGenerator();
            o.Generate(observables);
            return o.Output;
        }

        [TestMethod]
        public void TestInheritance()
        {
            string result = "";
            result = test3(sourceClasses: true, destClasses: false);
            Assert.AreEqual(@"
".Trim(), result.Trim());

        }
    }


    class Test1
    {
        public string Prop1 { get; set; }
        public int Prop2 { get; set; }
    }

    class Test1B : Test1
    {
        public bool Prop3 { get; set; }
        public Test1 Ref { get; set; }

        public string[] PropArray { get; set; }
        public Test1B[] SelfArray { get; set; }
    }

    public interface ITest3A
    {
        int Prop1 { get; set; }
    }

    public interface ITest3B : ITest3A
    {
        string Prop2 { get; set; }
    }

    public interface ITest3C : ITest3A, ITest3B
    {
        string Prop3 { get; set; }
    }

    public class Test3A : ITest3A
    {
        public int Prop1 { get; set; }
    }
    public class Test3 : Test3A, ITest3C
    {
        public string Prop2 { get; set; }
        public string Prop3 { get; set; }
    }

}
