using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Blocks.Api
{
    public class CpuObject : ApiList
    {
        public override List<CpuApiFunc> Api => new List<CpuApiFunc>
        {
            new CpuApiFunc("keys", false, "get array of object keys",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "obj", new CpuApiFunc.ArgInfo("object", "object to get keys") } },
                (c) => Keys
            )
        };

        public object Keys(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid object");
            if (x[0] is List<object> objArr)
            {
                var arr = new object[objArr.Count];
                for (long i = 0; i < arr.Length; ++i)
                    arr[i] = i;
                return arr;
            }
            if (x[0] is Dictionary<string, object> dict)
            {
                return dict.Keys.Select(y => (object)y).ToList();
            }
            return new object[0];
        }
    }
}
