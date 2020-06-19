﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Esprima.Ast;
using Jint.Native;
using Jint.Native.Number;
using Jint.Native.Number.Dtoa;
using Jint.Native.Object;
using Jint.Native.String;
using Jint.Native.Symbol;
using Jint.Pooling;

namespace Jint.Runtime
{
    [Flags]
    public enum Types
    {
        None = 0,
        Undefined = 1,
        Null = 2,
        Boolean = 4,
        String = 8,
        Number = 16,
        Symbol = 64,
        Object = 128
    }

    [Flags]
    public enum InternalTypes
    {
        // should not be used, used for empty match
        None = 0,

        Undefined = 1,
        Null = 2,

        // primitive  types range start
        Boolean = 4,
        String = 8,
        Number = 16,
        Integer = 32,
        Symbol = 64,

        // primitive  types range end
        Object = 128,

        // internal usage
        ObjectEnvironmentRecord = 512,
        RequiresCloning = 1024,

        Primitive = Boolean | String | Number | Integer | Symbol,
        InternalFlags = ObjectEnvironmentRecord | RequiresCloning
    }

    public static class TypeConverter
    {
        // how many decimals to check when determining if double is actually an int
        private const double DoubleIsIntegerTolerance = double.Epsilon * 100;

        internal static readonly string[] intToString = new string[1024];
        private static readonly string[] charToString = new string[256];

        static TypeConverter()
        {
            for (var i = 0; i < intToString.Length; ++i)
            {
                intToString[i] = i.ToString();
            }

            for (var i = 0; i < charToString.Length; ++i)
            {
                var c = (char) i;
                charToString[i] = c.ToString();
            }
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-toprimitive
        /// </summary>
        public static JsValue ToPrimitive(JsValue input, Types preferredType = Types.None)
        {
            if (!(input is ObjectInstance oi))
            {
                return input;
            }

            JsString hint;
            switch (preferredType)
            {
                case Types.String: hint = JsString.StringString; break;
                case Types.Number: hint = JsString.NumberString; break;
                default: hint = JsString.DefaultString; break;
            };

            var exoticToPrim = oi.GetMethod(GlobalSymbolRegistry.ToPrimitive);
            if (exoticToPrim is object)
            {
                var str = exoticToPrim.Call(oi, new JsValue[] { hint });
                if (str.IsPrimitive())
                {
                    return str;
                }

                if (str.IsObject())
                {
                    return ExceptionHelper.ThrowTypeError<JsValue>(oi.Engine, "Cannot convert object to primitive value");
                }
            }

            return OrdinaryToPrimitive(oi, preferredType == Types.None ? Types.Number :  preferredType);
        }

        private static readonly JsString[] StringHintCallOrder = { (JsString) "toString", (JsString) "valueOf"};
        private static readonly JsString[] NumberHintCallOrder = { (JsString) "valueOf", (JsString) "toString"};
        
        /// <summary>
        /// https://tc39.es/ecma262/#sec-ordinarytoprimitive
        /// </summary>
        internal static JsValue OrdinaryToPrimitive(ObjectInstance input, Types hint = Types.None)
        {
            var callOrder = new JsString[0];
            if (hint == Types.String)
            {
                callOrder = StringHintCallOrder;
            }

            if (hint == Types.Number)
            {
                callOrder = NumberHintCallOrder;
            }

            foreach (var property in callOrder)
            {
                var method = input.Get(property) as ICallable;
                if (method is object)
                {
                    var val = method.Call(input, Arguments.Empty);
                    if (val.IsPrimitive())
                    {
                        return val;
                    }
                }
 
            }

            return ExceptionHelper.ThrowTypeError<JsValue>(input.Engine);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-9.2
        /// </summary>
        public static bool ToBoolean(JsValue o)
        {
            var type = o._type & ~InternalTypes.InternalFlags;
            switch (type)
            {
                case InternalTypes.Boolean:
                    return ((JsBoolean) o)._value;
                case InternalTypes.Undefined:
                case InternalTypes.Null:
                    return false;
                case InternalTypes.Integer:
                    return (int) ((JsNumber) o)._value != 0;
                case InternalTypes.Number:
                    var n = ((JsNumber) o)._value;
                    return n != 0 && !double.IsNaN(n);
                case InternalTypes.String:
                    return !((JsString) o).IsNullOrEmpty();
                default:
                    return true;
            }
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-9.3
        /// </summary>
        public static double ToNumber(JsValue o)
        {
            return o.IsNumber()
                ? ((JsNumber) o)._value
                : ToNumberUnlikely(o);
        }

        private static double ToNumberUnlikely(JsValue o)
        {
            var type = o._type & ~InternalTypes.InternalFlags;
            switch (type)
            {
                case InternalTypes.Undefined: return double.NaN;
                case InternalTypes.Null: return 0;
                case InternalTypes.Object when o is IPrimitiveInstance p: return ToNumber(ToPrimitive(p.PrimitiveValue, Types.Number));
                case InternalTypes.Boolean: return (((JsBoolean)o)._value ? 1 : 0);
                case InternalTypes.String: return ToNumber(o.ToString());
                case InternalTypes.Symbol:
                // TODO proper TypeError would require Engine instance and a lot of API changes
                    return ExceptionHelper.ThrowTypeErrorNoEngine<double>("Cannot convert a Symbol value to a number");
                default: return ToNumber(ToPrimitive(o, Types.Number));
            };
        }

        private static double ToNumber(string input)
        {
            // eager checks to save time and trimming
            if (string.IsNullOrEmpty(input))
            {
                return 0;
            }

            char first = input[0];
            if (input.Length == 1 && first >= '0' && first <= '9')
            {
                // simple constant number
                return first - '0';
            }

            var s = StringPrototype.TrimEx(input);

            if (s.Length == 0)
            {
                return 0;
            }

            if (s.Length == 8 || s.Length == 9)
            {
                if ("+Infinity" == s || "Infinity" == s)
                {
                    return double.PositiveInfinity;
                }

                if ("-Infinity" == s)
                {
                    return double.NegativeInfinity;
                }
            }

            // todo: use a common implementation with JavascriptParser
            try
            {
                if (s.Length > 2 && s[0] == '0' && char.IsLetter(s[1]))
                {
                    int fromBase = 0;
                    if (s[1] == 'x' || s[1] == 'X')
                    {
                        fromBase = 16;
                    }

                    if (s[1] == 'o' || s[1] == 'O')
                    {
                        fromBase = 8;
                    }

                    if (s[1] == 'b' || s[1] == 'B')
                    {
                        fromBase = 2;
                    }

                    if (fromBase > 0)
                    {
                        return Convert.ToInt32(s.Substring(2), fromBase);
                    }
                }

                var start = s[0];
                if (start != '+' && start != '-' && start != '.' && !char.IsDigit(start))
                {
                    return double.NaN;
                }

                double n = double.Parse(s,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
                    NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
                if (s.StartsWith("-") && n == 0)
                {
                    return -0.0;
                }

                return n;
            }
            catch (OverflowException)
            {
                return s.StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity;
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-tolength
        /// </summary>
        public static ulong ToLength(JsValue o)
        {
            var len = ToInteger(o);
            if (len <= 0)
            {
                return 0;
            }

            return (ulong) Math.Min(len, NumberConstructor.MaxSafeInteger);
        }

        /// <summary>
        /// https://tc39.es/ecma262/#sec-tointeger
        /// </summary>
        public static double ToInteger(JsValue o)
        {
            var number = ToNumber(o);

            if (double.IsNaN(number))
            {
                return 0;
            }

            if (number == 0 || double.IsInfinity(number))
            {
                return number;
            }

            return (long) number;
        }

        internal static double ToInteger(string o)
        {
            var number = ToNumber(o);

            if (double.IsNaN(number))
            {
                return 0;
            }

            if (number == 0 || double.IsInfinity(number))
            {
                return number;
            }

            return (long) number;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-9.5
        /// </summary>
        public static int ToInt32(JsValue o)
        {
            return o._type == InternalTypes.Integer
                ? o.AsInteger()
                : (int) (uint) ToNumber(o);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-9.6
        /// </summary>
        public static uint ToUint32(JsValue o)
        {
            return o._type == InternalTypes.Integer
                ? (uint) o.AsInteger()
                : (uint) ToNumber(o);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-9.7
        /// </summary>
        public static ushort ToUint16(JsValue o)
        {
            return  o._type == InternalTypes.Integer
                ? (ushort) (uint) o.AsInteger()
                : (ushort) (uint) ToNumber(o);
        }

        
        internal static string ToString(long i)
        {
            return i >= 0 && i < intToString.Length
                ? intToString[i]
                : i.ToString();
        }

        
        internal static string ToString(int i)
        {
            return i >= 0 && i < intToString.Length
                ? intToString[i]
                : i.ToString();
        }

        
        internal static string ToString(uint i)
        {
            return i < (uint) intToString.Length
                ? intToString[i]
                : i.ToString();
        }

        
        internal static string ToString(char c)
        {
            return c >= 0 && c < charToString.Length
                ? charToString[c]
                : c.ToString();
        }

        
        internal static string ToString(ulong i)
        {
            return i >= 0 && i < (ulong) intToString.Length
                ? intToString[i]
                : i.ToString();
        }

        
        internal static string  ToString(double d)
        {
            if (d > long.MinValue && d < long.MaxValue  && Math.Abs(d % 1) <= DoubleIsIntegerTolerance)
            {
                // we are dealing with integer that can be cached
                return ToString((long) d);
            }

            using (var stringBuilder = StringBuilderPool.Rent())
            {
                // we can create smaller array as we know the format to be short
                return NumberPrototype.NumberToString(d, new DtoaBuilder(17), stringBuilder.Builder);
            }
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/6.0/#sec-topropertykey
        /// </summary>
        
        public static JsValue ToPropertyKey(JsValue o)
        {
            const InternalTypes stringOrSymbol = InternalTypes.String | InternalTypes.Symbol;
            return (o._type & stringOrSymbol) != 0
                ? o
                : ToPropertyKeyNonString(o);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static JsValue ToPropertyKeyNonString(JsValue o)
        {
            const InternalTypes stringOrSymbol = InternalTypes.String | InternalTypes.Symbol;
            var primitive = ToPrimitive(o, Types.String);
            return (primitive._type & stringOrSymbol) != 0
                ? primitive
                : ToStringNonString(primitive);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/6.0/#sec-tostring
        /// </summary>
        
        public static string ToString(JsValue o)
        {
            return o.IsString() ? o.ToString() : ToStringNonString(o);
        }

        
        internal static JsString ToJsString(JsValue o)
        {
            if (o is JsString s)
            {
                return s;
            }
            return JsString.Create(ToStringNonString(o));
        }

        private static string ToStringNonString(JsValue o)
        {
            var type = o._type & ~InternalTypes.InternalFlags;
            switch(type)
            {
                case InternalTypes.Boolean: return ((JsBoolean)o)._value ? "true" : "false";
                case InternalTypes.Integer: return ToString((int)((JsNumber)o)._value);
                case InternalTypes.Number: return ToString(((JsNumber)o)._value);
                case InternalTypes.Symbol: return ExceptionHelper.ThrowTypeErrorNoEngine<string>("Cannot convert a Symbol value to a string");
                case InternalTypes.Undefined: return Undefined.Text;
                case InternalTypes.Null: return Null.Text;
                case InternalTypes.Object when o is IPrimitiveInstance p: return ToString(ToPrimitive(p.PrimitiveValue, Types.String));
                case InternalTypes.Object when o is Interop.IObjectWrapper p: return p.Target?.ToString();
                default: return ToString(ToPrimitive(o, Types.String));
            };
        }

        
        public static ObjectInstance ToObject(Engine engine, JsValue value)
        {
            var type = value._type & ~InternalTypes.InternalFlags;
            switch (type)
            {
                case InternalTypes.Object: return (ObjectInstance)value;
                case InternalTypes.Boolean: return engine.Boolean.Construct(((JsBoolean)value)._value);
                case InternalTypes.Number: return engine.Number.Construct(((JsNumber)value)._value);
                case InternalTypes.Integer: return engine.Number.Construct(((JsNumber)value)._value);
                case InternalTypes.String: return engine.String.Construct(value.ToString());
                case InternalTypes.Symbol: return engine.Symbol.Construct(((JsSymbol)value));
                default: return ExceptionHelper.ThrowTypeError<ObjectInstance>(engine);
            };
        }
        
        internal static void CheckObjectCoercible(
            Engine engine,
            JsValue o,
            MemberExpression expression,
            string referenceName)
        {
            if (o._type < InternalTypes.Boolean && (engine.Options.ReferenceResolver?.CheckCoercible(o)).GetValueOrDefault() != true)
            {
                ThrowTypeError(engine, o, expression, referenceName);
            }
        }

        private static void ThrowTypeError(
            Engine engine,
            JsValue o,
            MemberExpression expression,
            string referencedName)
        {
            if (referencedName == null)
                referencedName = "The value";
            var message = $"{referencedName} is {o}";
            throw new JavaScriptException(engine.TypeError, message).SetCallstack(engine, expression.Location);
        }

        public static void CheckObjectCoercible(Engine engine, JsValue o)
        {
            if (o._type < InternalTypes.Boolean)
            {
                ExceptionHelper.ThrowTypeError(engine);
            }
        }

        public static IEnumerable<KeyValuePair<MethodBase, JsValue[]>> FindBestMatch<T>(Engine engine, T[] methods, Func<T, bool, JsValue[]> argumentProvider) where T : MethodBase
        {
            // TODO: this can ruin CLR binding
            yield break;
            /*
            List<KeyValuePair<T, JsValue[]>> matchingByParameterCount = null;
            foreach (var m in methods)
            {
                bool hasParams = false;
                var parameterInfos = m.GetParameters();
                foreach (var parameter in parameterInfos)
                {
                    if (Attribute.IsDefined(parameter, typeof(ParamArrayAttribute)))
                    {
                        hasParams = true;
                        break;
                    }
                }

                var arguments = argumentProvider(m, hasParams);
                if (parameterInfos.Length == arguments.Length)
                {
                    if (methods.Length == 0 && arguments.Length == 0)
                    {
                        yield return new KeyValuePair<MethodBase, JsValue[]>(m, arguments);
                        yield break;
                    }

                    if (matchingByParameterCount == null)
                        matchingByParameterCount = new List<KeyValuePair<T, JsValue[]>>();
                    matchingByParameterCount.Add(new KeyValuePair<T, JsValue[]>(m, arguments));
                }
                else if (parameterInfos.Length > arguments.Length)
                {
                    // check if we got enough default values to provide all parameters (or more in case some default values are provided/overwritten)
                    var defaultValuesCount = 0;
                    foreach (var param in parameterInfos)
                    {
                        if (param.DefaultValue != null) defaultValuesCount++;
                    }

                    if (parameterInfos.Length <= arguments.Length + defaultValuesCount)
                    {
                        // create missing arguments from default values

                        var argsWithDefaults = new List<JsValue>(arguments);
                        for (var i = arguments.Length; i < parameterInfos.Length; i++)
                        {
                            var param = parameterInfos[i];
                            var value = JsValue.FromObject(engine, param.DefaultValue);
                            argsWithDefaults.Add(value);
                        }

                        matchingByParameterCount = matchingByParameterCount ?? new List<KeyValuePair<T, JsValue[]>>();
                        matchingByParameterCount.Add(new KeyValuePair<T, JsValue[]>(m, argsWithDefaults.ToArray()));
                    }
                }
            }

            if (matchingByParameterCount == null)
            {
                yield break;
            }

            foreach (var tuple in matchingByParameterCount)
            {
                var perfectMatch = true;
                var parameters = tuple.Key.GetParameters();
                var arguments = tuple.Value;
                for (var i = 0; i < arguments.Length; i++)
                {
                    var arg = arguments[i].ToObject();
                    var paramType = parameters[i].ParameterType;
                    if (arg == null)
                    {
                        if (!TypeIsNullable(paramType))
                        {
                            perfectMatch = false;
                            break;
                        }
                    }
                    else if (arg.GetType() != paramType)
                    {
                        perfectMatch = false;
                        break;
                    }
                }

                if (perfectMatch)
                {
                    yield return new KeyValuePair<MethodBase, JsValue[]>(tuple.Key, arguments);
                    yield break;
                }
            }

            for (var i = 0; i < matchingByParameterCount.Count; i++)
            {
                var tuple = matchingByParameterCount[i];
                yield return new KeyValuePair<MethodBase, JsValue[]>(tuple.Key, tuple.Value);
            }*/
        }

        public static bool TypeIsNullable(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }
    }
}
