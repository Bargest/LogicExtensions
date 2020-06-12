using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Blocks.Api
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
}
