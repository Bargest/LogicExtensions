using Jint.Native;
using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Blocks.Api
{
    public class CpuApiProperty
    {
        public readonly string Name;
        public CpuApiProperty(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public struct ArgInfo
    {
        public readonly string Type;
        public readonly string Info;

        public ArgInfo(string t, string i)
        {
            Type = t;
            Info = i;
        }
    }

    public class CpuApiValue : CpuApiProperty
    {
        public readonly JsValue Value;
        public readonly ArgInfo Info;
        public CpuApiValue(string n, JsValue v, ArgInfo i) : base(n)
        {
            Value = v;
            Info = i;
        }
        public override string ToString()
        {
            return $"{Info.Type} {Name}: {Info.Info}";
        }
    }

    public class CpuApiFunc : CpuApiProperty
    {
        public readonly bool Sync;
        public readonly string Help;
        public readonly Dictionary<string, ArgInfo> Arguments;
        public readonly Func<CpuBlock, JsValue, JsValue[], JsValue> Implementation;

        public CpuApiFunc(string n, bool sync, string h, Dictionary<string, ArgInfo> args, Action<CpuBlock, JsValue, JsValue[]> impl)
            : this(n, sync, h, args, (c, t, a) =>
            {
                impl(c, t, a);
                return null;
            })
        {

        }

        public CpuApiFunc(string n, bool sync, string h, Dictionary<string, ArgInfo> args, Func<CpuBlock, JsValue, JsValue[], JsValue> impl)
            : base(n)
        {
            Help = h;
            Sync = sync;
            if (!Sync)
                Implementation = impl;
            else
                Implementation = (c, t, a) =>
                {
                    JsValue result = null;
                    c.Interp.Executor.PauseThread((state) => result = impl(c, t, a), null);
                    return result;
                };
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
