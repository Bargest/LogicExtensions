using Logic.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks.Api
{
    public class CpuQuat : ApiList
    {
        public override List<CpuApiFunc> Api => new List<CpuApiFunc>
        {
            new CpuApiFunc("mul", false, "multiply quaternion",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "a", new CpuApiFunc.ArgInfo("quaternion", "quaternion to multiply") },
                    { "b", new CpuApiFunc.ArgInfo("quaternion or vector", "quaternion or vector to multiply") }
                },
                (c) => QuatMult
            ),
            new CpuApiFunc("inv", false, "inverse of quaternion",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "q", new CpuApiFunc.ArgInfo("quaternion", "quaternion to invert") },
                },
                (c) => QuatInv
            ),
        };

        /// <summary>
        /// Multiply quaternion with either another quaternion or a 3d vector.
        /// </summary>
        /// <returns>
        /// Vector if second argument is vector and quaternion if second argument is quaternion
        /// </returns>
        public object QuatMult(VarCtx ctx, object[] x)
        {
            Quaternion q1, q2;
            Vector3 v;
            if (x.Length < 2 || !BlockUtils.TryGetQuat(x[0], out q1))
                throw new Exception("Invalid value");
            if (BlockUtils.TryGetQuat(x[1], out q2))
                return BlockUtils.Quat2Dict(q1 * q2);
            else if (BlockUtils.TryGetVec3(x[1], out v))
                return BlockUtils.Vec2Dict(q1 * v);
            throw new Exception("Invalid value");
        }

        /// <summary>
        /// Inverts quaternion
        /// </summary>
        /// <returns>
        /// inverted quaternion
        /// </returns>
        public object QuatInv(VarCtx ctx, object[] x)
        {
            Quaternion q;
            if (x.Length < 1 || !BlockUtils.TryGetQuat(x[0], out q))
                throw new Exception("Invalid value");
            return BlockUtils.Quat2Dict(Quaternion.Inverse(q));
        }
    }
}