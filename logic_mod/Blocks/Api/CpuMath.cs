using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Blocks.Api
{
    public class CpuMath : ApiNamespace
    {
        public override List<CpuApiFunc> Api => new List<CpuApiFunc>
        {
            new CpuApiFunc("abs", false, "absolute value",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply abs") } },
                (c) => Abs
            ),
            new CpuApiFunc("sqrt", false, "square root",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply square root") } },
                (c) => Sqrt
            ),
            new CpuApiFunc("pow", false, "power x^y",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "x", new CpuApiFunc.ArgInfo("float", "value to apply") },
                    { "y", new CpuApiFunc.ArgInfo("float", "power") } },
                (c) => Pow
            ),
            new CpuApiFunc("sin", false, "sin trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply sin") } },
                (c) => Sin
            ),
            new CpuApiFunc("cos", false, "cos trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply cos") } },
                (c) => Cos
            ),
            new CpuApiFunc("tan", false, "tan trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply tan") } },
                (c) => Tan
            ),
            new CpuApiFunc("asin", false, "asin trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply asin") } },
                (c) => Asin
            ),
            new CpuApiFunc("acos", false, "acos trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply acos") } },
                (c) => Acos
            ),
            new CpuApiFunc("atan", false, "atan trigonometry function",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("float", "value to apply atan") } },
                (c) => Atan
            ),
            new CpuApiFunc("log", false, "logarifm",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "value", new CpuApiFunc.ArgInfo("float", "value to apply logarifm") },
                    { "newBase", new CpuApiFunc.ArgInfo("float", "logarifm base") }
                },
                (c) => Log
            )
        };

        public object Abs(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Abs(v);
        }
        public object Pow(VarCtx ctx, object[] x)
        {
            if (x.Length < 2 || !BlockUtils.TryGetFloat(x[0], out float v) || !BlockUtils.TryGetFloat(x[1], out float y))
                throw new Exception("Invalid value");
            return (float)Math.Pow(v, y);
        }
        public object Sqrt(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Sqrt(v);
        }
        public object Sin(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Sin(v);
        }
        public object Cos(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Cos(v);
        }
        public object Tan(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Tan(v);
        }

        public object Asin(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Asin(v);
        }
        public object Acos(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Acos(v);
        }
        public object Atan(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Atan(v);
        }
        public object Log(VarCtx ctx, object[] x)
        {
            if (x.Length < 2 || !BlockUtils.TryGetFloat(x[0], out float v) || !BlockUtils.TryGetFloat(x[1], out float newBase))
                throw new Exception("Invalid value");
            return (float)Math.Log(v, newBase);
        }
    }
}
