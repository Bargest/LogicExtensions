using System;
using System.Collections.Generic;
using UnityEngine;
using Logic.Script;
using Jint.Native;
using Jint.Runtime;

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
        public static bool GetBool(object arg)
        {
            if (arg is bool b)
                return b;
            if (arg is long i)
                return i != 0;
            if (arg is float f)
                return f != 0;
            if (arg == Block.Undefined)
                return false;
            return arg != null;
        }

        public static Dictionary<string, object> Quat2Dict(Quaternion q)
        {
            return new Dictionary<string, object>
            {
                { "w", q.w },
                { "x", q.x },
                { "y", q.y },
                { "z", q.z }
            };
        }

        public static Dictionary<string, object> Vec2Dict(Vector3 v)
        {
            return new Dictionary<string, object>
            {
                { "x", v.x },
                { "y", v.y },
                { "z", v.z }
            };
        }
    }
}
