using Logic.Script;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks.Api
{
    public abstract class ApiNamespace
    {
        public abstract List<CpuApiFunc> Api { get; }
    }
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
    class CpuApi : SingleInstance<CpuApi>
    {
        ApiNamespace RootApi;
        Dictionary<string, ApiNamespace> ApiNamespaces;

        public override string Name => "CpuApi";

        public void Attach(CpuBlock block, FuncCtx ctx)
        {
            foreach (var api in RootApi.Api)
                block.Interp.AddExtFunc(ctx, api.Name, api.ImplementationFactory(block), api.Sync);

            foreach (var kp in ApiNamespaces)
                block.Interp.AddExtVariable(ctx, kp.Key, kp.Value.Api.ToDictionary(x => x.Name, x => (object)block.Interp.CreateFunc(ctx, x.Name, x.ImplementationFactory(block), x.Sync)));
        }

        public IEnumerable<string> GetHelp()
        {
            return RootApi.Api.Select(x => x.ToString())
                .Concat(ApiNamespaces.SelectMany(x => new[] { $"{x.Key} module:" }.Concat(x.Value.Api.Select(y => x.Key+"."+y.ToString()))));
        }

        public CpuApi()
        {
            RootApi = new CpuRoot();
            ApiNamespaces = new Dictionary<string, ApiNamespace>
            {
                { "Math", new CpuMath() },
                { "Object", new CpuObject() }
            };
        }

    }
}
