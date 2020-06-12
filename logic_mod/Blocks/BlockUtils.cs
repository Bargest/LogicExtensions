using System;
using System.Collections.Generic;
using UnityEngine;
using Logic.Script;
namespace Logic.Blocks
{
    /* 
     * A utility class for helper functions
     */
    public static class BlockUtils
    {
        public static bool TryGetFloat(object arg, out float value)
        {
            value = 0;
            if (arg is float flev)
            {
                value = flev;
                return true;
            }
            else if (arg is long ilev)
            {
                value = ilev;
                return true;
            }
            else if (arg is string str)
                return float.TryParse(str, out value);
            return false;
        }

        public static bool TryGetLong(object arg, out long value)
        {
            value = 0;
            if (arg is float flev)
            {
                value = (long)flev;
                return true;
            }
            else if (arg is long ilev)
            {
                value = ilev;
                return true;
            }
            else if (arg is string str)
                return long.TryParse(str, out value);
            return false;
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
