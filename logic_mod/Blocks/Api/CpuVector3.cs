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
    public class CpuVector3 : ApiDescription
    {
        public const string Name = "Vector3";

        public override List<CpuApiProperty> InstanceFields => new List<CpuApiProperty>
        {
            new CpuApiValue("x", 0, new ArgInfo("float", "x coord")),
            new CpuApiValue("y", 0, new ArgInfo("float", "y coord")),
            new CpuApiValue("z", 0, new ArgInfo("float", "z coord")),
            new CpuApiFunc("add", false, "sum of two 3d vectors",
                new Dictionary<string, ArgInfo>{
                    { "v", new ArgInfo("Vector3", "rhs Vector3 to add") }
                },
                VecAdd
            ),
            new CpuApiFunc("mul", false, "multiply vector by scalalr",
                new Dictionary<string, ArgInfo>{
                    { "c", new ArgInfo("float", "scalar to multiply") }
                },
                VecMul
            ),
            new CpuApiFunc("dot", false, "dot product of two 3d vectors",
                new Dictionary<string, ArgInfo>{
                    { "v", new ArgInfo("Vector3", "rhs Vector3 in product") }
                },
                VecDot
            ),
            new CpuApiFunc("cross", false, "cross product of two 3d vectors",
                new Dictionary<string, ArgInfo>{
                    { "v", new ArgInfo("Vector3", "rhs Vector3 in product") }
                },
                VecCross
            ),
            new CpuApiFunc("magnitude", false, "magnitude of 3d vector",
                new Dictionary<string, ArgInfo> { },
                VecMag
            ),
        };
        public override List<CpuApiProperty> StaticFields => new List<CpuApiProperty>
        {
            new CpuApiFunc(ApiObjectConstructor.ConstructorMethodName, false, "create Vector3",
                new Dictionary<string, ArgInfo>{
                    { "x", new ArgInfo("float", "x coord") },
                    { "y", new ArgInfo("float", "y coord") },
                    { "z", new ArgInfo("float", "z coord") }
                },
                CreateThis
            )
        };

        public JsValue CreateThis(CpuBlock c, JsValue thizArg, JsValue[] args)
        {
            var thiz = ((ObjectInstance)thizArg);
            var instf = InstanceFields;
            for (int i = 0; i < 3; ++i)
            {
                if (i >= args.Length)
                    return thizArg;
                float value = BlockUtils.TryGetFloat(args[i], out value) ? value : 0;
                thiz.Set(instf[i].Name, value);
            }
            return thizArg;
        }

        /// <summary>
        /// Calculates the vector cross product
        /// </summary>
        /// <param name="c"></param>
        /// <param name="thizArg"></param>
        /// <param name="x"></param>
        /// <returns>
        /// The cross product of two 3D vectors.
        /// </returns>
        public JsValue VecCross(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");

            var v1obj = ((ObjectInstance)thizArg);
            var v2obj = ((ObjectInstance)x[0]);
            Vector3 v1 = BlockUtils.ToVector3(v1obj);
            Vector3 v2 = BlockUtils.ToVector3(v2obj);
            return BlockUtils.Vec2Obj(c, Vector3.Cross(v1, v2));
        }

        /// <summary>
        /// Calculates the vector dot product
        /// </summary>
        /// <param name="c"></param>
        /// <param name="thizArg"></param>
        /// <param name="x"></param>
        /// <returns>
        /// The dot product of two 3D vectors.
        /// </returns>
        public JsValue VecDot(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");

            var v1obj = ((ObjectInstance)thizArg);
            var v2obj = ((ObjectInstance)x[0]);
            Vector3 v1 = BlockUtils.ToVector3(v1obj);
            Vector3 v2 = BlockUtils.ToVector3(v2obj);
            return Vector3.Dot(v1, v2);
        }

        /// <summary>
        /// Multiply vector3 by scalar
        /// </summary>
        /// <param name="c"></param>
        /// <param name="thizArg"></param>
        /// <param name="x"></param>
        /// <returns>
        /// The result of a vector3 multiplied by scalar.
        /// </returns>
        public JsValue VecMul(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");

            var vobj = ((ObjectInstance)thizArg);
            float f;
            if (!BlockUtils.TryGetFloat(x[0], out f))
                throw new Exception("Invalid value");
            Vector3 v = BlockUtils.ToVector3(vobj);
            return BlockUtils.Vec2Obj(c, v * f);
        }

        /// <summary>
        /// Adds two 3d vectors
        /// </summary>
        /// <param name="c"></param>
        /// <param name="thizArg"></param>
        /// <param name="x"></param>
        /// <returns>
        /// The sum product of two 3D vectors.
        /// </returns>
        public JsValue VecAdd(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");

            var v1obj = ((ObjectInstance)thizArg);
            var v2obj = ((ObjectInstance)x[0]);
            Vector3 v1 = BlockUtils.ToVector3(v1obj);
            Vector3 v2 = BlockUtils.ToVector3(v2obj);
            return BlockUtils.Vec2Obj(c, v1 + v2);
        }

        /// <summary>
        /// Magnitude of 3d Vector
        /// </summary>
        /// <param name="c"></param>
        /// <param name="thizArg"></param>
        /// <param name="x"></param>
        /// <returns>
        /// The magnitude of Vector3
        /// </returns>
        public JsValue VecMag(CpuBlock c, JsValue thizArg, JsValue[] x)
        {
            var vobj = ((ObjectInstance)thizArg);
            Vector3 v = BlockUtils.ToVector3(vobj);
            return v.magnitude;
        }
    }
}
