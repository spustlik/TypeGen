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
        public void TestClassesToObservableInterfaces()
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
}
