﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Jint.Collections;
using Jint.Native.Object;
using Jint.Native.String;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Interop;

namespace Jint.Native.Global
{
    public sealed class GlobalObject : ObjectInstance
    {
        private StringBuilder _stringBuilder = new StringBuilder();

        private GlobalObject(Engine engine) : base(engine)
        {
        }

        public static GlobalObject CreateGlobalObject(Engine engine)
        {
            return new GlobalObject(engine);
        }

        protected override void Initialize()
        {
            const PropertyFlag lengthFlags = PropertyFlag.Configurable;
            const PropertyFlag propertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
            var properties = new PropertyDictionary(40, checkExistingKeys: false)
            {
                ["Object"] = new PropertyDescriptor(Engine.Object, propertyFlags),
                ["Function"] = new PropertyDescriptor(Engine.Function, propertyFlags),
                ["Symbol"] = new PropertyDescriptor(Engine.Symbol, propertyFlags),
                ["Array"] = new PropertyDescriptor(Engine.Array, propertyFlags),
                ["Map"] = new PropertyDescriptor(Engine.Map, propertyFlags),
                ["Set"] = new PropertyDescriptor(Engine.Set, propertyFlags),
                ["String"] = new PropertyDescriptor(Engine.String, propertyFlags),
                ["RegExp"] = new PropertyDescriptor(Engine.RegExp, propertyFlags),
                ["Number"] = new PropertyDescriptor(Engine.Number, propertyFlags),
                ["Boolean"] = new PropertyDescriptor(Engine.Boolean, propertyFlags),
                ["Date"] = new PropertyDescriptor(Engine.Date, propertyFlags),
                ["Math"] = new PropertyDescriptor(Engine.Math, propertyFlags),
                ["JSON"] = new PropertyDescriptor(Engine.Json, propertyFlags),
                ["Error"] = new LazyPropertyDescriptor(() => Engine.Error, propertyFlags),
                ["EvalError"] = new LazyPropertyDescriptor(() => Engine.EvalError, propertyFlags),
                ["Proxy"] = new LazyPropertyDescriptor(() => Engine.Proxy, propertyFlags),
                ["RangeError"] = new LazyPropertyDescriptor(() => Engine.RangeError, propertyFlags),
                ["ReferenceError"] = new LazyPropertyDescriptor(() => Engine.ReferenceError, propertyFlags),
                ["Reflect"] = new LazyPropertyDescriptor(() => Engine.Reflect, propertyFlags),
                ["SyntaxError"] = new LazyPropertyDescriptor(() => Engine.SyntaxError, propertyFlags),
                ["TypeError"] = new LazyPropertyDescriptor(() => Engine.TypeError, propertyFlags),
                ["URIError"] = new LazyPropertyDescriptor(() => Engine.UriError, propertyFlags),
                ["NaN"] = new PropertyDescriptor(double.NaN, PropertyFlag.None),
                ["Infinity"] = new PropertyDescriptor(double.PositiveInfinity, PropertyFlag.None),
                ["undefined"] = new PropertyDescriptor(Undefined, PropertyFlag.None),
                ["parseInt"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "parseInt", ParseInt, 2, lengthFlags), propertyFlags),
                ["parseFloat"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "parseFloat", ParseFloat, 1, lengthFlags), propertyFlags),
                ["isNaN"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "isNaN", IsNaN, 1, lengthFlags), propertyFlags),
                ["isFinite"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "isFinite", IsFinite, 1, lengthFlags), propertyFlags),
                ["decodeURI"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "decodeURI", DecodeUri, 1, lengthFlags), propertyFlags),
                ["decodeURIComponent"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "decodeURIComponent", DecodeUriComponent, 1, lengthFlags), propertyFlags),
                ["encodeURI"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "encodeURI", EncodeUri, 1, lengthFlags), propertyFlags),
                ["encodeURIComponent"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "encodeURIComponent", EncodeUriComponent, 1, lengthFlags), propertyFlags),
                ["escape"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "escape", Escape, 1), propertyFlags),
                ["unescape"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "unescape", Unescape, 1), propertyFlags),
                ["globalThis"] = new PropertyDescriptor(this, propertyFlags),

                // toString is not mentioned or actually required in spec, but some tests rely on it
                ["toString"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "toString", ToStringString, 1), propertyFlags)
            };

            SetProperties(properties);
        }

        private JsValue ToStringString(JsValue thisObj, JsValue[] arguments)
        {
            return _engine.Object.PrototypeObject.ToObjectString(thisObj, Arguments.Empty);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.2
        /// </summary>
        public static JsValue ParseInt(JsValue thisObject, JsValue[] arguments)
        {
            string inputString = TypeConverter.ToString(arguments.At(0));
            var s = StringPrototype.TrimEx(inputString);

            var sign = 1;
            if (!System.String.IsNullOrEmpty(s))
            {
                if (s[0] == '-')
                {
                    sign = -1;
                }

                if (s[0] == '-' || s[0] == '+')
                {
                    s = s.Substring(1);
                }
            }

            var stripPrefix = true;

            int radix = arguments.Length > 1 ? TypeConverter.ToInt32(arguments[1]) : 0;

            if (radix == 0)
            {
                if (s.Length >= 2 && s.StartsWith("0x") || s.StartsWith("0X"))
                {
                    radix = 16;
                }
                else
                {
                    radix = 10;
                }
            }
            else if (radix < 2 || radix > 36)
            {
                return JsNumber.DoubleNaN;
            }
            else if (radix != 16)
            {
                stripPrefix = false;
            }

            if (stripPrefix && s.Length >= 2 && s.StartsWith("0x") || s.StartsWith("0X"))
            {
                s = s.Substring(2);
            }

            try
            {
                return sign * Parse(s, radix);
            }
            catch
            {
                return JsNumber.DoubleNaN;
            }

        }

        private static double Parse(string number, int radix)
        {
            if (number == "")
            {
                return double.NaN;
            }

            double result = 0;
            double pow = 1;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                double index = double.NaN;
                char digit = number[i];

                if (digit >= '0' && digit <= '9')
                {
                    index = digit - '0';
                }
                else if (digit >= 'a' && digit <= 'z')
                {
                    index = digit - 'a' + 10;
                }
                else if (digit >= 'A' && digit <= 'Z')
                {
                    index = digit - 'A' + 10;
                }

                if (double.IsNaN(index) || index >= radix)
                {
                    return Parse(number.Substring(0, i), radix);
                }

                result += index * pow;
                pow = pow * radix;
            }

            return result;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.3
        /// </summary>
        public static JsValue ParseFloat(JsValue thisObject, JsValue[] arguments)
        {
            var inputString = TypeConverter.ToString(arguments.At(0));
            var trimmedString = StringPrototype.TrimStartEx(inputString);

            var sign = 1;
            if (trimmedString.Length > 0)
            {
                if (trimmedString[0] == '-')
                {
                    sign = -1;
                    trimmedString = trimmedString.Substring(1);
                }
                else if (trimmedString[0] == '+')
                {
                    trimmedString = trimmedString.Substring(1);
                }
            }

            if (trimmedString.StartsWith("Infinity"))
            {
                return sign * double.PositiveInfinity;
            }

            if (trimmedString.StartsWith("NaN"))
            {
                return JsNumber.DoubleNaN;
            }

            var separator = (char)0;

            bool isNan = true;
            decimal number = 0;
            var i = 0;
            for (; i < trimmedString.Length; i++)
            {
                var c = trimmedString[i];
                if (c == '.')
                {
                    i++;
                    separator = '.';
                    break;
                }

                if (c == 'e' || c == 'E')
                {
                    i++;
                    separator = 'e';
                    break;
                }

                var digit = c - '0';

                if (digit >= 0 && digit <= 9)
                {
                    isNan = false;
                    number = number * 10 + digit;
                }
                else
                {
                    break;
                }
            }

            decimal pow = 0.1m;

            if (separator == '.')
            {
                for (; i < trimmedString.Length; i++)
                {
                    var c = trimmedString[i];

                    var digit = c - '0';

                    if (digit >= 0 && digit <= 9)
                    {
                        isNan = false;
                        number += digit * pow;
                        pow *= 0.1m;
                    }
                    else if (c == 'e' || c == 'E')
                    {
                        i++;
                        separator = 'e';
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            var exp = 0;
            var expSign = 1;

            if (separator == 'e')
            {
                if (i < trimmedString.Length)
                {
                    if (trimmedString[i] == '-')
                    {
                        expSign = -1;
                        i++;
                    }
                    else if (trimmedString[i] == '+')
                    {
                        i++;
                    }
                }

                for (; i < trimmedString.Length; i++)
                {
                    var c = trimmedString[i];

                    var digit = c - '0';

                    if (digit >= 0 && digit <= 9)
                    {
                        exp = exp * 10 + digit;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (isNan)
            {
                return JsNumber.DoubleNaN;
            }

            for (var k = 1; k <= exp; k++)
            {
                if (expSign > 0)
                {
                    number *= 10;
                }
                else
                {
                    number /= 10;
                }
            }

            return (double)(sign * number);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.4
        /// </summary>
        public static JsValue IsNaN(JsValue thisObject, JsValue[] arguments)
        {
            var x = TypeConverter.ToNumber(arguments.At(0));
            return double.IsNaN(x);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.5
        /// </summary>
        public static JsValue IsFinite(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.Length != 1)
            {
                return false;
            }

            var n = TypeConverter.ToNumber(arguments.At(0));
            if (double.IsNaN(n) || double.IsInfinity(n))
            {
                return false;
            }

            return true;
        }

        private static readonly HashSet<char> UriReserved = new HashSet<char>
        {
            ';', '/', '?', ':', '@', '&', '=', '+', '$', ','
        };

        private static readonly HashSet<char> UriUnescaped = new HashSet<char>
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R',
            'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_', '.', '!',
            '~', '*', '\'', '(', ')'
        };

        private static readonly HashSet<char> UnescapedUriSet = new HashSet<char>(UriReserved.Concat(UriUnescaped).Concat(new[] { '#' }));
        private static readonly HashSet<char> ReservedUriSet = new HashSet<char>(UriReserved.Concat(new[] { '#' }));

        private const string HexaMap = "0123456789ABCDEF";

        
        private static bool IsValidHexaChar(char c) => Uri.IsHexDigit(c);
        
        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.3.2
        /// </summary>
        /// <param name="thisObject"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public JsValue EncodeUri(JsValue thisObject, JsValue[] arguments)
        {
            var uriString = TypeConverter.ToString(arguments.At(0));

            return Encode(uriString, UnescapedUriSet);
        }


        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.3.4
        /// </summary>
        /// <param name="thisObject"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public JsValue EncodeUriComponent(JsValue thisObject, JsValue[] arguments)
        {
            var uriString = TypeConverter.ToString(arguments.At(0));

            return Encode(uriString, UriUnescaped);
        }

        private string Encode(string uriString, HashSet<char> unescapedUriSet)
        {
            var strLen = uriString.Length;

            //_stringBuilder.EnsureCapacity(uriString.Length);
            _stringBuilder = new StringBuilder();

            for (var k = 0; k < strLen; k++)
            {
                var c = uriString[k];
                if (unescapedUriSet != null && unescapedUriSet.Contains(c))
                {
                    _stringBuilder.Append(c);
                }
                else
                {
                    if (c >= 0xDC00 && c <= 0xDBFF)
                    {
                        ExceptionHelper.ThrowUriError(_engine);
                    }

                    int v;
                    if (c < 0xD800 || c > 0xDBFF)
                    {
                        v = c;
                    }
                    else
                    {
                        k++;
                        if (k == strLen)
                        {
                            ExceptionHelper.ThrowUriError(_engine);
                        }

                        var kChar = (int)uriString[k];
                        if (kChar < 0xDC00 || kChar > 0xDFFF)
                        {
                            ExceptionHelper.ThrowUriError(_engine);
                        }

                        v = (c - 0xD800) * 0x400 + (kChar - 0xDC00) + 0x10000;
                    }

                    byte[] octets = new byte[0];

                    if (v >= 0 && v <= 0x007F)
                    {
                        // 00000000 0zzzzzzz -> 0zzzzzzz
                        octets = new[] { (byte)v };
                    }
                    else if (v <= 0x07FF)
                    {
                        // 00000yyy yyzzzzzz ->	110yyyyy ; 10zzzzzz
                        octets = new[]
                        {
                            (byte)(0xC0 | (v >> 6)),
                            (byte)(0x80 | (v & 0x3F))
                        };
                    }
                    else if (v <= 0xD7FF)
                    {
                        // xxxxyyyy yyzzzzzz -> 1110xxxx; 10yyyyyy; 10zzzzzz
                        octets = new[]
                        {
                            (byte)(0xE0 | (v >> 12)),
                            (byte)(0x80 | ((v >> 6) & 0x3F)),
                            (byte)(0x80 | (v & 0x3F))
                        };
                    }
                    else if (v <= 0xDFFF)
                    {
                        ExceptionHelper.ThrowUriError(_engine);
                    }
                    else if (v <= 0xFFFF)
                    {
                        octets = new[]
                        {
                            (byte) (0xE0 | (v >> 12)),
                            (byte) (0x80 | ((v >> 6) & 0x3F)),
                            (byte) (0x80 | (v & 0x3F))
                        };
                    }
                    else
                    {
                        octets = new[]
                        {
                            (byte) (0xF0 | (v >> 18)),
                            (byte) (0x80 | (v >> 12 & 0x3F)),
                            (byte) (0x80 | (v >> 6 & 0x3F)),
                            (byte) (0x80 | (v >> 0 & 0x3F))
                        };
                    }

                    for (var j = 0; j < octets.Length; j++)
                    {
                        var jOctet = octets[j];
                        var x1 = HexaMap[jOctet / 16];
                        var x2 = HexaMap[jOctet % 16];
                        _stringBuilder.Append('%').Append(x1).Append(x2);
                    }
                }
            }

            return _stringBuilder.ToString();
        }

        public JsValue DecodeUri(JsValue thisObject, JsValue[] arguments)
        {
            var uriString = TypeConverter.ToString(arguments.At(0));

            return Decode(uriString, ReservedUriSet);
        }

        public JsValue DecodeUriComponent(JsValue thisObject, JsValue[] arguments)
        {
            var componentString = TypeConverter.ToString(arguments.At(0));

            return Decode(componentString, null);
        }

        private string Decode(string uriString, HashSet<char> reservedSet)
        {
            var strLen = uriString.Length;

            // _stringBuilder.EnsureCapacity(strLen);
            _stringBuilder = new StringBuilder();

            var octets = new byte[0];

            for (var k = 0; k < strLen; k++)
            {
                var C = uriString[k];
                if (C != '%')
                {
                    _stringBuilder.Append(C);
                }
                else
                {
                    var start = k;
                    if (k + 2 >= strLen)
                    {
                        ExceptionHelper.ThrowUriError(_engine);
                    }

                    if (!IsValidHexaChar(uriString[k + 1]) || !IsValidHexaChar(uriString[k + 2]))
                    {
                        ExceptionHelper.ThrowUriError(_engine);
                    }

                    var B = Convert.ToByte(uriString[k + 1].ToString() + uriString[k + 2], 16);

                    k += 2;
                    if ((B & 0x80) == 0)
                    {
                        C = (char)B;
                        if (reservedSet == null || !reservedSet.Contains(C))
                        {
                            _stringBuilder.Append(C);
                        }
                        else
                        {
                            _stringBuilder.Append(uriString, start, k - start + 1);
                        }
                    }
                    else
                    {
                        var n = 0;
                        for (; ((B << n) & 0x80) != 0; n++) ;

                        if (n == 1 || n > 4)
                        {
                            ExceptionHelper.ThrowUriError(_engine);
                        }

                        octets = octets.Length == n
                            ? octets
                            : new byte[n];

                        octets[0] = B;

                        if (k + (3 * (n - 1)) >= strLen)
                        {
                            ExceptionHelper.ThrowUriError(_engine);
                        }

                        for (var j = 1; j < n; j++)
                        {
                            k++;
                            if (uriString[k] != '%')
                            {
                                ExceptionHelper.ThrowUriError(_engine);
                            }

                            if (!IsValidHexaChar(uriString[k + 1]) || !IsValidHexaChar(uriString[k + 2]))
                            {
                                ExceptionHelper.ThrowUriError(_engine);
                            }

                            B = Convert.ToByte(uriString[k + 1].ToString() + uriString[k + 2], 16);

                            // B & 11000000 != 10000000
                            if ((B & 0xC0) != 0x80)
                            {
                                ExceptionHelper.ThrowUriError(_engine);
                            }

                            k += 2;

                            octets[j] = B;
                        }

                        _stringBuilder.Append(Encoding.UTF8.GetString(octets, 0, octets.Length));
                    }
                }
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-B.2.1
        /// </summary>
        public JsValue Escape(JsValue thisObject, JsValue[] arguments)
        {
            const string whiteList = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@*_ + -./";
            var uriString = TypeConverter.ToString(arguments.At(0));

            var strLen = uriString.Length;

            //_stringBuilder.EnsureCapacity(strLen);
            _stringBuilder = new StringBuilder();

            for (var k = 0; k < strLen; k++)
            {
                var c = uriString[k];
                if (whiteList.IndexOf(c) != -1)
                {
                    _stringBuilder.Append(c);
                }
                else if (c < 256)
                {
                    _stringBuilder.Append($"%{((int) c):X2}");
                }
                else
                {
                    _stringBuilder.Append($"%u{((int) c):X4}");
                }
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-B.2.2
        /// </summary>
        public JsValue Unescape(JsValue thisObject, JsValue[] arguments)
        {
            var uriString = TypeConverter.ToString(arguments.At(0));

            var strLen = uriString.Length;

            //_stringBuilder.EnsureCapacity(strLen);
            _stringBuilder = new StringBuilder();

            for (var k = 0; k < strLen; k++)
            {
                var c = uriString[k];
                if (c == '%')
                {
                    if (k < strLen - 6
                        && uriString[k + 1] == 'u'
                        && uriString.Skip(k + 2).Take(4).All(IsValidHexaChar))
                    {
                        c = (char)int.Parse(
                            string.Join(string.Empty, uriString.Skip(k + 2).Take(4).Select(x => x.ToString()).ToArray()),
                            NumberStyles.AllowHexSpecifier);

                        k += 5;
                    }
                    else if (k < strLen - 3
                        && uriString.Skip(k + 1).Take(2).All(IsValidHexaChar))
                    {
                        c = (char)int.Parse(
                            string.Join(string.Empty, uriString.Skip(k + 1).Take(2).Select(x => x.ToString()).ToArray()),
                            NumberStyles.AllowHexSpecifier);

                        k += 2;
                    }
                }
                _stringBuilder.Append(c);
            }

            return _stringBuilder.ToString();
        }
        
        // optimized versions with string parameter and without virtual dispatch for global environment usage

        internal bool HasProperty(Key property)
        {
            return GetOwnProperty(property) != PropertyDescriptor.Undefined;
        }

        internal PropertyDescriptor GetProperty(Key property) => GetOwnProperty(property);

        internal bool DefinePropertyOrThrow(Key property, PropertyDescriptor desc)
        {
            if (!DefineOwnProperty(property, desc))
            {
                ExceptionHelper.ThrowTypeError(_engine);
            }

            return true;
        }

        internal bool DefineOwnProperty(Key property, PropertyDescriptor desc)
        {
            var current = GetOwnProperty(property);
            if (current == desc)
            {
                return true;
            }
            
            // check fast path
            if ((current._flags & PropertyFlag.MutableBinding) != 0)
            {
                current._value = desc.Value;
                return true;
            }

            return ValidateAndApplyPropertyDescriptor(this, new JsString(property), true, desc, current);
        }

        
        internal PropertyDescriptor GetOwnProperty(Key property)
        {
            Properties.TryGetValue(property, out var descriptor);
            return descriptor ?? PropertyDescriptor.Undefined;
        }
        
        internal bool Set(Key property, JsValue value)
        {
            // here we are called only from global environment record context
            // we can take some shortcuts to be faster

            if (!_properties.TryGetValue(property, out var existingDescriptor))
            {
                _properties[property] = new PropertyDescriptor(value, PropertyFlag.ConfigurableEnumerableWritable);
                return true;
            }

            if (existingDescriptor.IsDataDescriptor())
            {
                if (!existingDescriptor.Writable || existingDescriptor.IsAccessorDescriptor())
                {
                    return false;
                }

                // check fast path
                if ((existingDescriptor._flags & PropertyFlag.MutableBinding) != 0)
                {
                    existingDescriptor._value = value;
                    return true;
                }

                // slow path
                return DefineOwnProperty(property, new PropertyDescriptor(value, PropertyFlag.None));
            }

            if (!(existingDescriptor.Set is ICallable setter))
            {
                return false;
            }

            setter.Call(this, new[] {value});

            return true;
        }
        
        
        internal void SetOwnProperty(Key property, PropertyDescriptor desc)
        {
            SetProperty(property, desc);
        }
    }
}