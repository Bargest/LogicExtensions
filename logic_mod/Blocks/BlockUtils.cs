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

        /// <summary>
        /// Tries to get a quaternion from an object.
        /// </summary>
        /// <returns>
        /// true if the object is a dictionary and has fields x, y, z, w that can
        /// be cast to float
        /// </returns>
        public static bool TryGetQuat(object arg, out Quaternion value)
        {
            value = Quaternion.identity;
            if (!(arg is Dictionary<string, object> d))
                return false;
            else
            {
                object ow, ox, oy, oz;
                if (!d.TryGetValue("w", out ow) || !d.TryGetValue("x", out ox)
                    || !d.TryGetValue("y", out oy) || !d.TryGetValue("z", out oz))
                    return false;
                float w, x, y, z;
                if (!TryGetFloat(ow, out w) || !TryGetFloat(ox, out x)
                    || !TryGetFloat(oy, out y) || !TryGetFloat(oz, out z))
                    return false;
                value = new Quaternion(x, y, z, w);
                return true;
            }
        }

        /// <summary>
        /// Tries to get a vector3 from an object.
        /// </summary>
        /// <returns>
        /// true if the object is a dictionary and has fields x, y, z that can
        /// be cast to float
        /// </returns>
        public static bool TryGetVec3(object arg, out Vector3 value)
        {
            value = Vector3.zero;
            if (!(arg is Dictionary<string, object> d))
                return false;
            else
            {
                object ox, oy, oz;
                if (!d.TryGetValue("x", out ox) || !d.TryGetValue("y", out oy)
                    || !d.TryGetValue("z", out oz))
                    return false;
                float x, y, z;
                if (!TryGetFloat(ox, out x) || !TryGetFloat(oy, out y) || !TryGetFloat(oz, out z))
                    return false;
                value = new Vector3(x, y, z);
                return true;
            }
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
