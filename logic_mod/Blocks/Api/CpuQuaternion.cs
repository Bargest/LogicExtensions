using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks.Api
{
    public class CpuQuaternion : ApiDescription
    {
        public const string Name = "Quaternion";

        public override List<CpuApiProperty> InstanceFields => new List<CpuApiProperty>
        {
            new CpuApiValue("x", 0, new ArgInfo("float", "x coord")),
            new CpuApiValue("y", 0, new ArgInfo("float", "y coord")),
            new CpuApiValue("z", 0, new ArgInfo("float", "z coord")),
            new CpuApiValue("w", 0, new ArgInfo("float", "w coord")),
            new CpuApiFunc("mul", false, "multiply quaternion",
                new Dictionary<string, ArgInfo>{
                    { "b", new ArgInfo("Quaternion", "quaternion or vector to multiply") }
                },
                (c) => (t, x) => QuatMult(c, t, x)
            ),
            new CpuApiFunc("inv", false, "inverse of quaternion",
                new Dictionary<string, ArgInfo> { },
                (c) => (t, x) => QuatInv(c, t, x)
            ),
        };
        public override List<CpuApiProperty> StaticFields => new List<CpuApiProperty>
        {
            new CpuApiFunc(ApiObjectConstructor.ConstructorMethodName, false, "create quaternion",
                new Dictionary<string, ArgInfo>{
                    { "x", new ArgInfo("float", "x coord") },
                    { "y", new ArgInfo("float", "y coord") },
                    { "z", new ArgInfo("float", "z coord") },
                    { "w", new ArgInfo("float", "w coord") }
                },
                (c) => CreateThis
            )
        };

        public JsValue CreateThis(JsValue thizArg, JsValue[] args)
        {
            var thiz = ((ObjectInstance)thizArg);
            var instf = InstanceFields;
            for (int i = 0; i < 4; ++i)
            {
                if (i >= args.Length)
                    return thizArg;
                float value = BlockUtils.TryGetFloat(args[i], out value) ? value : 0;
                thiz.Set(instf[i].Name, value);
            }
            return thizArg;
        }
        /// <summary>
        /// Multiply quaternion with either another quaternion or a 3d vector.
        /// </summary>
        /// <returns>
        /// Vector if second argument is vector and quaternion if second argument is quaternion
        /// </returns>
        public JsValue QuatMult(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");

            var q1obj = ((ObjectInstance)thizArg);
            var q2obj = ((ObjectInstance)x[0]);
            var q1 = BlockUtils.ToQuat(q1obj);
            if (q2obj.HasProperty("w"))
            {
                var q2 = BlockUtils.ToQuat(q2obj);
                return BlockUtils.Quat2Obj(c, q1 * q2);
            }
            else
            {
                var v = BlockUtils.ToVector3(q2obj);
                return BlockUtils.Vec2Obj(c, q1 * v);
            }
        }

        /// <summary>
        /// Inverts quaternion
        /// </summary>
        /// <returns>
        /// inverted quaternion
        /// </returns>
        public JsValue QuatInv(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            var thiz = ((ObjectInstance)thizArg);
            var q1 = BlockUtils.ToQuat(thiz);

            return BlockUtils.Quat2Obj(c, Quaternion.Inverse(q1));
        }
    }
}
