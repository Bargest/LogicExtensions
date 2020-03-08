using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Script
{
    public class ArifmOperation
    {
        protected bool Unary = false;
        protected Dictionary<Type, Func<object, object, object>> Ops;
        
        protected class DumbNullType
        {

        }

        public object Arifm(VarCtx ctx, object a, object b)
        {
            try
            {
                if (Unary)
                    return UnaryArifm(ctx, a);

                Type aType = a == null ? typeof(DumbNullType) : a.GetType();
                Type bType = b == null ? typeof(DumbNullType) : b.GetType();
                Type maxType = null;
                foreach (var kp in Ops)
                {
                    //ctx.Interp.SyncDebug($"{aType} + {bType} ? {kp.Key}");
                    if (kp.Key.IsAssignableFrom(aType) || kp.Key.IsAssignableFrom(bType))
                    {
                        maxType = kp.Key;
                        break;
                    }
                };
                if (maxType == null)
                    throw new Exception("Can't cast types");
                
                if (maxType.IsInterface)
                    return Ops[maxType](a, b);

                if (maxType.IsPrimitive)
                {
                    a = Convert.ChangeType(a, maxType);
                    b = Convert.ChangeType(b, maxType);
                }
                else if (maxType == typeof(string))
                {
                    a = a.ToString();
                    b = b.ToString();
                }
                //var ao = a == null ? null : (maxType == typeof(object) ? a : Convert.ChangeType(a, maxType));
                //var bo = b == null ? null : (maxType == typeof(object) ? b : Convert.ChangeType(b, maxType));
                var result = Ops[maxType](a, b);
                //ctx.Interp.SyncDebug($"{a}({aType}) {this} {b}({bType}) -> {result}");
                return result;
            }
            catch (Exception e)
            {
                return Block.Throw(ctx, e.Message);
            }
        }

        private object UnaryArifm(VarCtx ctx, object a)
        {
            Type maxType = null;
            Type aType = a == null ? typeof(DumbNullType) : a.GetType();
            foreach (var kp in Ops)
            {
                if (kp.Key.IsAssignableFrom(aType))
                {
                    maxType = kp.Key;
                    break;
                }
            };
            if (maxType == null)
                throw new Exception("Can't cast type");
            if (maxType.IsInterface)
                return Ops[maxType](a, null);

            //var ao = a == null ? null : Convert.ChangeType(a, maxType);
            return Ops[maxType](a, null);
        }
    }
    public class Addition : ArifmOperation
    {
        public Addition()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(DumbNullType), ApplyNull },
                { typeof(string), ApplyString },
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyNull(object aa, object bb) {
            if (aa == null)
                return bb;
            else
                return aa;
        }
        public object ApplyInt(object aa, object bb) { return (long)aa + (long)bb; }
        public object ApplyFloat(object aa, object bb) { return (float)aa + (float)bb; }
        public object ApplyString(object aa, object bb) { return (string)aa + bb?.ToString(); }
    }
    public class Subtraction : ArifmOperation
    {
        public Subtraction()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa - (long)bb; }
        public object ApplyFloat(object aa, object bb) { return (float)aa - (float)bb; }
    }
    public class Multiplication : ArifmOperation
    {
        public Multiplication()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa * (long)bb; }
        public object ApplyFloat(object aa, object bb) { return (float)aa * (float)bb; }
    }
    public class Division : ArifmOperation
    {
        public Division()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa / (long)bb; }
        public object ApplyFloat(object aa, object bb) { return (float)aa / (float)bb; }
    }
    public class Modulo : ArifmOperation
    {
        public Modulo()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa % (long)bb; }
    }
    public class Inc : ArifmOperation
    {
        public Inc()
        {
            Unary = true;
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa) + 1; }
    }
    public class Dec : ArifmOperation
    {
        public Dec()
        {
            Unary = true;
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa) - 1; }
    }
    public class Shl : ArifmOperation
    {
        public Shl()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa << (int)bb; }
    }
    public class Shr : ArifmOperation
    {
        public Shr()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa >> (int)bb; }
    }
    public class And : ArifmOperation
    {
        public And()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa & (long)bb; }
    }
    public class Or : ArifmOperation
    {
        public Or()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa | (long)bb; }
    }
    public class Positive : ArifmOperation
    {
        public Positive()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyNumber },
                { typeof(float), ApplyNumber },
                { typeof(object), ApplyObject },
            };
        }
        public object ApplyNumber(object aa, object bb) { return aa; }
        public object ApplyObject(object aa, object bb) { return Convert.ToSingle(aa); }
    }
    public class Negative : ArifmOperation
    {
        public Negative()
        {
            Unary = true;
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
                { typeof(float), ApplyFloat },
                { typeof(object), ApplyObject },
            };
        }
        public object ApplyInt(object aa, object bb) { return -(long)aa; }
        public object ApplyFloat(object aa, object bb) { return -(float)aa; }
        public object ApplyObject(object aa, object bb) { return -Convert.ToSingle(aa); }
    }

    public class Not : ArifmOperation
    {
        public Not()
        {
            Unary = true;
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return ~(long)aa; }
    }
    public class LogNot : ArifmOperation
    {
        public LogNot()
        {
            Unary = true;
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(DumbNullType), (a,b) => (long)1 },
                { typeof(long), ApplyInt }
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa == 0 ? (long)1 : 0; }
    }
    public class LogAnd : ArifmOperation
    {
        public LogAnd()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
                { typeof(object), ApplyObject },
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa != 0 && (long)bb != 0) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return (aa != null && bb != null) ? (long)1 : 0; }
    }
    public class LogOr : ArifmOperation
    {
        public LogOr()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
                { typeof(object), ApplyObject },
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa != 0 || (long)bb != 0) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return (aa != null || bb != null) ? (long)1 : 0; }
    }
    public class Xor : ArifmOperation
    {
        public Xor()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(long), ApplyInt },
            };
        }
        public object ApplyInt(object aa, object bb) { return (long)aa ^ (long)bb; }
    }

    public class Equal : ArifmOperation
    {
        public Equal()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(object), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa == (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa == (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) {
            return Equals(aa, bb) ? (long)1 : 0; }
    }
    public class NEqual : ArifmOperation
    {
        public NEqual()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(object), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa != (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa != (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return !Equals(aa, bb) ? (long)1 : 0; }
    }

    public class Above : ArifmOperation
    {
        public Above()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(IComparable), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa > (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa > (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return ((IComparable)aa).CompareTo(bb) > 0 ? (long)1 : 0; }
    }
    public class Below : ArifmOperation
    {
        public Below()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(IComparable), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa < (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa < (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return ((IComparable)aa).CompareTo(bb) < 0 ? (long)1 : 0; }
    }
    public class AboveEq : ArifmOperation
    {
        public AboveEq()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(IComparable), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa >= (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa >= (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return ((IComparable)aa).CompareTo(bb) >= 0 ? (long)1 : 0; }
    }
    public class BelowEq : ArifmOperation
    {
        public BelowEq()
        {
            Ops = new Dictionary<Type, Func<object, object, object>>
            {
                { typeof(float), ApplyFloat },
                { typeof(long), ApplyInt },
                { typeof(IComparable), ApplyObject }
            };
        }
        public object ApplyInt(object aa, object bb) { return ((long)aa <= (long)bb) ? (long)1 : 0; }
        public object ApplyFloat(object aa, object bb) { return ((float)aa <= (float)bb) ? (long)1 : 0; }
        public object ApplyObject(object aa, object bb) { return ((IComparable)aa).CompareTo(bb) <= 0 ? (long)1 : 0; }
    }
}
