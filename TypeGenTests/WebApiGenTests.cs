using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TypeGen;
using TypeGen.Generators;
using TypeGen.Generators.WebApi;

namespace TypeGenTests
{
    [TestClass]
    public class WebApiGenTests
    {
        ActionModel action(string name, string urlTemplate, params ParameterModel[] parameters)
        {
            var result = new ActionModel()
            {
                Name = name,
                UrlTemplate = urlTemplate
            };
            result.Parameters.AddRange(parameters);
            return result;
        }
        ParameterModel parameter(string name, bool isOptional = false, bool isData = false, bool isUrl = false, Type type = null, string urlName = null)
        {
            return new ParameterModel()
            {
                Name = name,
                UrlName = urlName ?? name,
                Type = type ?? typeof(string),
                IsUrlParam = isUrl,
                IsOptional = isOptional,
                IsData = isData,
            };
        }
        class MyModel
        {
            public string Title { get; set; }
            public int Id { get; set; }
        }
        [TestMethod]
        public void TestOptionalParams()
        {
            var gen = new ReflectionGenerator();
            var apigen = new WebApiProxyGenerator2(gen);
            var apiModule = new TypescriptModule("Api");
            var controllers = new[] {
                new ControllerModel()
                {
                    Name = "MyController",
                    Actions = {
                        action("MyAction", "/api/my/index"),
                        action("MyAction2", "/api/my/a2/{id}",
                                parameter("id", isUrl:true),
                                parameter("o1",isOptional:true, type:typeof(int)),
                                parameter("o2", isOptional:true, type:typeof(string)),
                                parameter("save", isData:true, type:typeof(MyModel))
                        ),
                        action("MyAction3", "/api/some/{id}/more/{key}/{notfound}/end",
                            parameter("id",isUrl:true),
                            parameter("key",isUrl:true)
                        ),
                        action("MyAction4", "/api/m4/{id}",
                            parameter("idObject",isUrl:true, urlName:"id"),
                            //param at method with [FromUri] or [FromBody?], but not in url
                            parameter("p1", isOptional:false, urlName:"param1"),
                            parameter("p2", isOptional:true, urlName:"param2")
                        )
                    }
                }
            };
            apigen.GenerateControllers(controllers, apiModule);
            //Assert.AreEqual(1, apiModule.Members.Count);
            var outputGen = new OutputGenerator();
            outputGen.GenerateComments = false;
            outputGen.Generate(apiModule);
            var s = outputGen.Output.Trim();
            //TODO:
            // why copy of opt params ? why not use opt.param, but opt['param'] ?
            var expected = @"
module Api {
    export class GeneratedProxy extends base.ProxyBase {
        public My = {
            MyActionAsync: (): Promise<void> => this.call(`/api/my/index`, {}),
            MyAction2Async: (id: string, save: IMyModel, opt: { o1?: number, o2?: string } = {}): Promise<void> => this.call(`/api/my/a2/${id}`, opt, save/* DATA */),
            MyAction3Async: (id: string, key: string): Promise<void> => this.call(`/api/some/${id}/more/${key}/{notfound}/end`, {}),
            MyAction4Async: (idObject: string, p1: string, opt: { p2?: string } = {}): Promise<void> => this.call(`/api/m4/${idObject}`, { param1: p1, param2: opt.p2 })
        };
    }
}".Trim();

            Assert.AreEqual(expected, s);
            var diffs = expected.Split('\n').Select((ex, i) => new { ex, act = s.Split('\n')[i] }).Where(x => x.ex != x.act).ToArray();
            Assert.AreEqual(0, diffs.Length);
        }


        [TestMethod]
        public void TestUrlParamRegex()
        {
            var r = new Regex(@"{[^}]+}", RegexOptions.Compiled);
            var m = r.Matches("/api/test/nic");
            Assert.IsNotNull(m);
            Assert.AreEqual(0, m.Count);
            m = r.Matches("/api/{param1}");
            Assert.AreEqual(1, m.Count);
            Assert.AreEqual("{param1}", m[0].Value);
            m = r.Matches("/api/{param/ppp}/");
            Assert.AreEqual(1, m.Count);
            Assert.AreEqual("{param/ppp}", m[0].Value);
            m = r.Matches("/api/{param-1}/neco");
            Assert.AreEqual("{param-1}", m[0].Value);
            Assert.AreEqual(1, m.Count);
            m = r.Matches("/api/{param1}/neco/{param2}");
            Assert.AreEqual(2, m.Count);
            Assert.AreEqual("{param1}", m[0].Value);
            Assert.AreEqual("{param2}", m[1].Value);

            var s = r.Split("/api/test");
            Assert.AreEqual(1, s.Length);
            Assert.AreEqual("/api/test", s[0]);
            s = r.Split("/api/{param1}/neco/{param2}");
            Assert.AreEqual(3, s.Length);
            Assert.AreEqual("/api/", s[0]);
            Assert.AreEqual("/neco/", s[1]);
            Assert.AreEqual("", s[2]);


            var parts = r.SplitToParts("/api/test").ToArray();
            Assert.AreEqual(1, parts.Length);
            Assert.AreEqual("/api/test", parts[0]);
            Assert.AreEqual("/api/~{param1}~/neco/~{param2}", String.Join("~", r.SplitToParts("/api/{param1}/neco/{param2}")));
            Assert.AreEqual("/api/~{param1/spec}~{param2}~/konec", String.Join("~", r.SplitToParts("/api/{param1/spec}{param2}/konec")));
        }
    }
}
