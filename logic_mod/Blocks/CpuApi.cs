using Logic.Script;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    class CpuApi : SingleInstance<CpuApi>
    {
        public class CpuApiFunc
        {
            public struct ArgInfo
            {
                public string Type;
                public string Info;

                public ArgInfo(string t, string i)
                {
                    Type = t;
                    Info = i;
                }
            }

            public string Name;
            public bool Sync;
            public string Help;
            public Dictionary<string, ArgInfo> Arguments;
            public Func<CpuBlock, Func<VarCtx, object[], object>> ImplementationFactory;

            public CpuApiFunc(string n, bool sync, string h, Dictionary<string, ArgInfo> args, Func<CpuBlock, Action<VarCtx, object[]>> impl)
                : this(n, sync, h, args, (c) =>
                {
                    var m = impl(c);
                    return (ctx, x) => { m(ctx, x); return null; };
                })
            {

            }

            public CpuApiFunc(string n, bool sync, string h, Dictionary<string, ArgInfo> args, Func<CpuBlock, Func<VarCtx, object[], object>> impl)
            {
                Name = n;
                Help = h;
                Sync = sync;
                ImplementationFactory = impl;
                Arguments = args;
            }

            public override string ToString()
            {
                var result = $"{Name}({string.Join(", ", Arguments.Keys.ToArray())})";
                if (Help != null)
                    result += $"\n    {Help}";
                if (Arguments.Count > 0)
                    result += $"\n    Arguments:" +
                        string.Join("", Arguments.Select(x => $"\n        {x.Value.Type} {x.Key}:\t{x.Value.Info}").ToArray());
                return result;
            }
        }


        public class ApiNamespace
        {
            public List<CpuApiFunc> Api;
        }

        List<CpuApiFunc> RootApi;
        Dictionary<string, ApiNamespace> ApiNamespaces;

        public override string Name => "CpuApi";

        public void Attach(CpuBlock block, FuncCtx ctx)
        {
            foreach (var api in RootApi)
                block.Interp.AddExtFunc(ctx, api.Name, api.ImplementationFactory(block), api.Sync);

            foreach (var kp in ApiNamespaces)
                block.Interp.AddExtVariable(ctx, kp.Key, kp.Value.Api.ToDictionary(x => x.Name, x => (object)block.Interp.CreateFunc(ctx, x.Name, x.ImplementationFactory(block), x.Sync)));
        }

        public IEnumerable<string> GetHelp()
        {
            return RootApi.Select(x => x.ToString())
                .Concat(ApiNamespaces.SelectMany(x => new[] { $"{x.Key} module:" }.Concat(x.Value.Api.Select(y => x.Key+"."+y.ToString()))));
        }

        public CpuApi()
        {
            RootApi = new List<CpuApiFunc>
            {
                new CpuApiFunc("print", true, "print message to console",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{ { "msg", new CpuApiFunc.ArgInfo("object", "message to print") } },
                    (c) => c.Print
                ),
                new CpuApiFunc("typeof", true, "get object type",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{ { "obj", new CpuApiFunc.ArgInfo("object", "object to get type") } },
                    (c) => c.Typeof
                ),
                new CpuApiFunc("int", true, "convert to integer",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                    (c) => c.Int
                ),
                new CpuApiFunc("float", true, "convert to floating point number",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                    (c) => c.Float
                ),
                new CpuApiFunc("str", true, "convert to string",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                    (c) => c.Str
                ),
                new CpuApiFunc("cli", false, "clear interrupts",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{},
                    (c) => c.Cli
                ),
                new CpuApiFunc("sti", false, "set interrupts",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{},
                    (c) => c.Sti
                ),
                new CpuApiFunc("asleep", false, "pause execution until next frame",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{},
                    (c) => c.Asleep
                ),
                new CpuApiFunc("irqv", true, "set IRQ vector",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") } ,
                        { "handler", new CpuApiFunc.ArgInfo("func", "irq callback as function (id, treshold, front)") },
                        { "threshold", new CpuApiFunc.ArgInfo("float", "(default=0) pio threshold in the range of [0..1)") },
                        { "mode", new CpuApiFunc.ArgInfo("int", "(default=0) 0 - trigger on each front, 1 - uprising, 2 - downfalling ") }
                    },
                    (c) => c.Irqv
                ),
                new CpuApiFunc("irq", false, "trigger IRQ vector",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "pio", new CpuApiFunc.ArgInfo("int", "PIO id (= irq id)") } ,
                    },
                    (c) => c.Irq
                ),
                new CpuApiFunc("readSensor", false, "read sensor (emulator) measuers",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "pio", new CpuApiFunc.ArgInfo("int", "PIO id, that sensor is emulating") } ,
                    },
                    (c) => c.ReadSensor
                ),
                new CpuApiFunc("in", false, "read value from PIO",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") },
                    },
                    (c) => c.In
                ),
                new CpuApiFunc("out", false, "write value to PIO",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") },
                        { "value", new CpuApiFunc.ArgInfo("float", "value [0..1] to write") },
                    },
                    (c) => c.Out
                ),
                new CpuApiFunc("setTimeout", true, "set callback that will be executed after timeout",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "timeout", new CpuApiFunc.ArgInfo("float", "seconds until triggering timeout") },
                        { "callback", new CpuApiFunc.ArgInfo("func", "function that will be executed") },
                    },
                    (c) => c.SetTimeout
                ),
                 new CpuApiFunc("clearTimeout", true, "cancel timeout, set by setTimeout",
                    new Dictionary<string, CpuApiFunc.ArgInfo>{
                        { "timeoutId", new CpuApiFunc.ArgInfo("int", "timeout id, returned by setTimeout") },
                    },
                    (c) => c.ClearTimeout
                ),
            };

            ApiNamespaces = new Dictionary<string, ApiNamespace>
            {
                { "Math", new ApiNamespace
                    {
                        Api =  new List<CpuApiFunc>
                        {
                            new CpuApiFunc("abs", false, "absolute value",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply abs") } },
                                (c) => c.Abs
                            ),
                            new CpuApiFunc("sqrt", false, "square root",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply square root") } },
                                (c) => c.Sqrt
                            ),
                            new CpuApiFunc("pow", false, "power x^y",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{
                                    { "x", new CpuApiFunc.ArgInfo("float", "value to apply") },
                                    { "y", new CpuApiFunc.ArgInfo("float", "power") } },
                                (c) => c.Pow
                            ),
                            new CpuApiFunc("sin", false, "sin trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply sin") } },
                                (c) => c.Sin
                            ),
                            new CpuApiFunc("cos", false, "cos trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply cos") } },
                                (c) => c.Cos
                            ),
                            new CpuApiFunc("tan", false, "tan trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply tan") } },
                                (c) => c.Tan
                            ),
                            new CpuApiFunc("asin", false, "asin trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply asin") } },
                                (c) => c.Asin
                            ),
                            new CpuApiFunc("acos", false, "acos trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply acos") } },
                                (c) => c.Acos
                            ),
                            new CpuApiFunc("atan", false, "atan trigonometry function",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply atan") } },
                                (c) => c.Atan
                            ),
                            new CpuApiFunc("log", false, "logarifm",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{
                                    { "value", new CpuApiFunc.ArgInfo("float", "value to apply logarifm") },
                                    { "newBase", new CpuApiFunc.ArgInfo("float", "logarifm base") }
                                },
                                (c) => c.Log
                            )
                        }
                    }
                },
                { "Object", new ApiNamespace
                    {
                        Api =  new List<CpuApiFunc>
                        {
                            new CpuApiFunc("keys", false, "get array of object keys",
                                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "obj", new CpuApiFunc.ArgInfo("object", "object to get keys") } },
                                (c) => c.Keys
                            )
                        }
                    }
                }
            };
        }

    }
}
