using System;
using System.Collections.Generic;
using UnityEngine;
using Logic.Script;
using Jint.Native;
using Jint.Runtime;
using Logic.Blocks.Api;
using Jint.Native.Object;
using Jint.Native.Function;
using System.Linq;

namespace Logic.Blocks
{
    /* 
     * A utility class for helper functions
     */
    public static class BlockUtils
    {
        public static bool TryGetFloat(JsValue arg, out float value)
        {
            value = 0;
            try
            {
                value = (float)TypeConverter.ToNumber(arg);
            }
            catch
            {
                return false;
            }
            return true;
           
        }

        public static bool TryGetLong(JsValue arg, out long value)
        {
            value = 0;
            try
            {
                value = (long)TypeConverter.ToInteger(arg);
            }
            catch
            {
                return false;
            }
            return true;
        }

        // Anything can be cast to a bool, so no need to "try" here.
        public static bool GetBool(JsValue arg)
        {
            return TypeConverter.ToBoolean(arg);
        }

        static string[] vecKeys = new[] { "x", "y", "z", "w" };
        static string[] quatKeys = new[] { "x", "y", "z", "w" };

        public static Quaternion ToQuat(ObjectInstance obj)
        {
            var keys = quatKeys.Select(y => (float)TypeConverter.ToNumber(obj.Get(y))).ToArray();
            return new Quaternion(keys[0], keys[1], keys[2], keys[3]);
        }

        public static Vector3 ToVector3(ObjectInstance obj)
        {
            var keys = vecKeys.Select(y => (float)TypeConverter.ToNumber(obj.Get(y))).ToArray();
            return new Vector3(keys[0], keys[1], keys[2]);
        }

        public static ObjectInstance Quat2Obj(CpuBlock b, Quaternion q)
        {
            var quat = b.Interp.Global.Get(CpuQuaternion.Name) as IConstructor;
            return quat.Construct(new JsValue[] { q.x, q.y, q.z, q.w }, null);
        }

        public static JsValue Vec2Obj(CpuBlock b, Vector3 v)
        {
            return JsValue.FromObject(b.Interp, new Dictionary<string, object>
            {
                { "x", v.x },
                { "y", v.y },
                { "z", v.z }
            });
        }
    }
}
