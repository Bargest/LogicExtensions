﻿using System;
using System.Collections.Generic;
using Jint.Collections;
using Jint.Native.Object;
using Jint.Native.Symbol;
using Jint.Pooling;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Jint.Runtime.Interpreter.Expressions;

using static System.String;

namespace Jint.Native.Array
{
    /// <summary>
    /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.4.4
    /// </summary>
    public sealed class ArrayPrototype : ArrayInstance
    {
        private ArrayConstructor _arrayConstructor;

        private ArrayPrototype(Engine engine) : base(engine)
        {
        }

        public static ArrayPrototype CreatePrototypeObject(Engine engine, ArrayConstructor arrayConstructor)
        {
            var obj = new ArrayPrototype(engine)
            {
                _prototype = engine.Object.PrototypeObject,
                _length = new PropertyDescriptor(JsNumber.PositiveZero, PropertyFlag.Writable),
                _arrayConstructor = arrayConstructor,
            };
            return obj;
        }

        protected override void Initialize()
        {
            var unscopables = new ObjectInstance(_engine)
            {
                _prototype = null
            };

            unscopables.SetDataProperty("copyWithin", JsBoolean.True);
            unscopables.SetDataProperty("entries", JsBoolean.True);
            unscopables.SetDataProperty("fill", JsBoolean.True);
            unscopables.SetDataProperty("find", JsBoolean.True);
            unscopables.SetDataProperty("findIndex", JsBoolean.True);
            unscopables.SetDataProperty("flat", JsBoolean.True);
            unscopables.SetDataProperty("flatMap", JsBoolean.True);
            unscopables.SetDataProperty("includes", JsBoolean.True);
            unscopables.SetDataProperty("keys", JsBoolean.True);
            unscopables.SetDataProperty("values", JsBoolean.True);
            
            const PropertyFlag propertyFlags = PropertyFlag.Writable | PropertyFlag.Configurable;
            var properties = new PropertyDictionary(30, checkExistingKeys: false)
            {
                ["constructor"] = new PropertyDescriptor(_arrayConstructor, PropertyFlag.NonEnumerable),
                ["toString"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "toString", ToString, 0, PropertyFlag.Configurable), propertyFlags),
                ["toLocaleString"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "toLocaleString", ToLocaleString, 0, PropertyFlag.Configurable), propertyFlags),
                ["concat"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "concat", Concat, 1, PropertyFlag.Configurable), propertyFlags),
                ["copyWithin"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "copyWithin", CopyWithin, 2, PropertyFlag.Configurable), propertyFlags),
                ["entries"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "entries", Iterator, 0, PropertyFlag.Configurable), propertyFlags),
                ["fill"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "fill", Fill, 1, PropertyFlag.Configurable), propertyFlags),
                ["join"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "join", Join, 1, PropertyFlag.Configurable), propertyFlags),
                ["pop"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "pop", Pop, 0, PropertyFlag.Configurable), propertyFlags),
                ["push"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "push", Push, 1, PropertyFlag.Configurable), propertyFlags),
                ["reverse"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "reverse", Reverse, 0, PropertyFlag.Configurable), propertyFlags),
                ["shift"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "shift", Shift, 0, PropertyFlag.Configurable), propertyFlags),
                ["slice"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "slice", Slice, 2, PropertyFlag.Configurable), propertyFlags),
                ["sort"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "sort", Sort, 1, PropertyFlag.Configurable), propertyFlags),
                ["splice"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "splice", Splice, 2, PropertyFlag.Configurable), propertyFlags),
                ["unshift"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "unshift", Unshift, 1, PropertyFlag.Configurable), propertyFlags),
                ["includes"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "includes", Includes, 1, PropertyFlag.Configurable), propertyFlags),
                ["indexOf"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "indexOf", IndexOf, 1, PropertyFlag.Configurable), propertyFlags),
                ["lastIndexOf"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "lastIndexOf", LastIndexOf, 1, PropertyFlag.Configurable), propertyFlags),
                ["every"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "every", Every, 1, PropertyFlag.Configurable), propertyFlags),
                ["some"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "some", Some, 1, PropertyFlag.Configurable), propertyFlags),
                ["forEach"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "forEach", ForEach, 1, PropertyFlag.Configurable), propertyFlags),
                ["map"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "map", Map, 1, PropertyFlag.Configurable), propertyFlags),
                ["filter"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "filter", Filter, 1, PropertyFlag.Configurable), propertyFlags),
                ["reduce"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "reduce", Reduce, 1, PropertyFlag.Configurable), propertyFlags),
                ["reduceRight"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "reduceRight", ReduceRight, 1, PropertyFlag.Configurable), propertyFlags),
                ["find"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "find", Find, 1, PropertyFlag.Configurable), propertyFlags),
                ["findIndex"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "findIndex", FindIndex, 1, PropertyFlag.Configurable), propertyFlags),
                ["keys"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "keys", Keys, 0, PropertyFlag.Configurable), propertyFlags),
                ["values"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "values", Values, 0, PropertyFlag.Configurable), propertyFlags)
            };
            SetProperties(properties);

            var symbols = new SymbolDictionary(2)
            {
                [GlobalSymbolRegistry.Iterator] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "iterator", Values, 1), propertyFlags),
                [GlobalSymbolRegistry.Unscopables] = new PropertyDescriptor(unscopables, PropertyFlag.Configurable)
            };
            SetSymbols(symbols);
        }
        
        private ObjectInstance Keys(JsValue thisObj, JsValue[] arguments)
        {
            if (thisObj is ObjectInstance oi && oi.IsArrayLike)
            {
                return _engine.Iterator.ConstructArrayLikeKeyIterator(oi);
            }

            return ExceptionHelper.ThrowTypeError<ObjectInstance>(_engine, "cannot construct iterator");
        }

        internal ObjectInstance Values(JsValue thisObj, JsValue[] arguments)
        {
            if (thisObj is ObjectInstance oi && oi.IsArrayLike)
            {
                return _engine.Iterator.ConstructArrayLikeValueIterator(oi);
            }

            return ExceptionHelper.ThrowTypeError<ObjectInstance>(_engine, "cannot construct iterator");
        }

        private ObjectInstance Iterator(JsValue thisObj, JsValue[] arguments)
        {
            if (thisObj is ObjectInstance oi)
            {
                return _engine.Iterator.Construct(oi);
            }

            return ExceptionHelper.ThrowTypeError<ObjectInstance>(_engine, "cannot construct iterator");
        }

        private JsValue Fill(JsValue thisObj, JsValue[] arguments)
        {
            if (thisObj.IsNullOrUndefined())
            {
                ExceptionHelper.ThrowTypeError(_engine, "Cannot convert undefined or null to object");
            }

            var operations = ArrayOperations.For(thisObj as ObjectInstance);
            var length = operations.GetLength();

            var value = arguments.At(0);

            var start = ConvertAndCheckForInfinity(arguments.At(1), 0);

            var relativeStart = TypeConverter.ToInteger(start);
            uint actualStart;
            if (relativeStart < 0)
            {
                actualStart = (uint) System.Math.Max(length + relativeStart, 0);
            }
            else
            {
                actualStart = (uint) System.Math.Min(relativeStart, length);
            }

            var end = ConvertAndCheckForInfinity(arguments.At(2), length);
            var relativeEnd = TypeConverter.ToInteger(end);
            uint actualEnd;
            if (relativeEnd < 0)
            {
                actualEnd = (uint) System.Math.Max(length + relativeEnd, 0);
            }
            else
            {
                actualEnd = (uint) System.Math.Min(relativeEnd, length);
            }

            for (var i = actualStart; i < actualEnd; ++i)
            {
                operations.Set(i, value, updateLength: false, throwOnError: false);
            }

            return thisObj;
        }

        private JsValue CopyWithin(JsValue thisObj, JsValue[] arguments)
        {
            // Steps 1-2.
            if (thisObj.IsNullOrUndefined())
            {
                return ExceptionHelper.ThrowTypeError<JsValue>(_engine, "this is null or not defined");
            }

            JsValue target = arguments.At(0);
            JsValue start = arguments.At(1);
            JsValue end = arguments.At(2);

            var operations = ArrayOperations.For(thisObj as ObjectInstance);
            var initialLength = operations.GetLength();
            var len = ConvertAndCheckForInfinity(initialLength, 0);

            var relativeTarget = ConvertAndCheckForInfinity(target, 0);

            var to = relativeTarget < 0 ?
                System.Math.Max(len + relativeTarget, 0) :
                System.Math.Min(relativeTarget, len);

            var relativeStart = ConvertAndCheckForInfinity(start, 0);

            var from = relativeStart < 0 ?
                System.Math.Max(len + relativeStart, 0) :
                System.Math.Min(relativeStart, len);

            var relativeEnd = ConvertAndCheckForInfinity(end, len);

            var final = relativeEnd < 0 ?
                System.Math.Max(len + relativeEnd, 0) :
                System.Math.Min(relativeEnd, len);

            var count = System.Math.Min(final - from, len - to);

            var direction = 1;

            if (from < to && to < from + count) 
            {
                direction = -1;
                from += (uint) count - 1;
                to += (uint) count - 1;
            }

            while (count > 0)
            {
                var fromPresent = operations.HasProperty((ulong) from);
                if (fromPresent)
                {
                    var fromValue = operations.Get((ulong) from);
                    operations.Set((ulong) to, fromValue, updateLength: true, throwOnError: true);
                }
                else
                {
                    operations.DeletePropertyOrThrow((ulong) to);
                }
                from = (uint) (from + direction);
                to = (uint) (to + direction);
                count--;
            }

            return thisObj;
        }

        long ConvertAndCheckForInfinity(JsValue jsValue, long defaultValue)
        {
            if (jsValue.IsUndefined())
            {
                return defaultValue;
            }

            var num = TypeConverter.ToNumber(jsValue);

            if (double.IsPositiveInfinity(num))
            {
                return long.MaxValue;
            }

            return (long) num;
        }

        private JsValue LastIndexOf(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();
            if (len == 0)
            {
                return -1;
            }

            var n = arguments.Length > 1 
                ? TypeConverter.ToInteger(arguments[1]) 
                : len - 1;
            double k;
            if (n >= 0)
            {
                k = System.Math.Min(n, len - 1); // min
            }
            else
            {
                k = len - System.Math.Abs(n);
            }

            if (k < 0 || k > uint.MaxValue)
            {
                return -1;
            }

            var searchElement = arguments.At(0);
            var i = (uint) k;
            for (;; i--)
            {
                var kPresent = o.HasProperty(i);
                if (kPresent)
                {
                    var elementK = o.Get(i);
                    var same = JintBinaryExpression.StrictlyEqual(elementK, searchElement);
                    if (same)
                    {
                        return i;
                    }
                }

                if (i == 0)
                {
                    break;
                }
            }

            return -1;
        }

        private JsValue Reduce(JsValue thisObj, JsValue[] arguments)
        {
            var callbackfn = arguments.At(0);
            var initialValue = arguments.At(1);

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLength();

            var callable = GetCallable(callbackfn);

            if (len == 0 && arguments.Length < 2)
            {
                ExceptionHelper.ThrowTypeError(Engine);
            }

            var k = 0;
            JsValue accumulator = Undefined;
            if (arguments.Length > 1)
            {
                accumulator = initialValue;
            }
            else
            {
                var kPresent = false;
                while (kPresent == false && k < len)
                {
                    if (kPresent = o.TryGetValue((uint) k, out var temp))
                    {
                        accumulator = temp;
                    }

                    k++;
                }

                if (kPresent == false)
                {
                    ExceptionHelper.ThrowTypeError(Engine);
                }
            }

            var args = new JsValue[4];
            args[3] = o.Target;
            while (k < len)
            {
                var i = (uint) k;
                if (o.TryGetValue(i, out var kvalue))
                {
                    args[0] = accumulator;
                    args[1] = kvalue;
                    args[2] = i;
                    accumulator = callable.Call(Undefined, args);
                }

                k++;
            }

            return accumulator;
        }

        private JsValue Filter(JsValue thisObj, JsValue[] arguments)
        {
            var callbackfn = arguments.At(0);
            var thisArg = arguments.At(1);

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLength();

            var callable = GetCallable(callbackfn);

            var a = Engine.Array.ArraySpeciesCreate(TypeConverter.ToObject(_engine, thisObj), 0);
            var operations = ArrayOperations.For(a);

            uint to = 0;
            var args = _engine._jsValueArrayPool.RentArray(3);
            args[2] = o.Target;
            for (uint k = 0; k < len; k++)
            {
                if (o.TryGetValue(k, out var kvalue))
                {
                    args[0] = kvalue;
                    args[1] = k;
                    var selected = callable.Call(thisArg, args);
                    if (TypeConverter.ToBoolean(selected))
                    {
                        operations.Set(to, kvalue, updateLength: false, throwOnError: false);
                        to++;
                    }
                }
            }

            operations.SetLength(to);
            _engine._jsValueArrayPool.ReturnArray(args);

            return a;
        }

        private JsValue Map(JsValue thisObj, JsValue[] arguments)
        {
            if (thisObj is ArrayInstance arrayInstance && !arrayInstance.HasOwnProperty(CommonProperties.Constructor))
            {
                return arrayInstance.Map(arguments);
            }

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();

            if (len > ArrayOperations.MaxArrayLength)
            {
                ExceptionHelper.ThrowRangeError(_engine, "Invalid array length");;
            }

            var callbackfn = arguments.At(0);
            var thisArg = arguments.At(1);
            var callable = GetCallable(callbackfn);

            var a = ArrayOperations.For(Engine.Array.ArraySpeciesCreate(TypeConverter.ToObject(_engine, thisObj), (uint) len));
            var args = _engine._jsValueArrayPool.RentArray(3);
            args[2] = o.Target;
            for (uint k = 0; k < len; k++)
            {
                if (o.TryGetValue(k, out var kvalue))
                {
                    args[0] = kvalue;
                    args[1] = k;
                    var mappedValue = callable.Call(thisArg, args);
                    a.Set(k, mappedValue, updateLength: false, throwOnError: false);
                }
            }
            _engine._jsValueArrayPool.ReturnArray(args);
            return a.Target;
        }

        private JsValue ForEach(JsValue thisObj, JsValue[] arguments)
        {
            var callbackfn = arguments.At(0);
            var thisArg = arguments.At(1);

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLength();

            var callable = GetCallable(callbackfn);

            var args = _engine._jsValueArrayPool.RentArray(3);
            args[2] = o.Target;
            for (uint k = 0; k < len; k++)
            {
                if (o.TryGetValue(k, out var kvalue))
                {
                    args[0] = kvalue;
                    args[1] = k;
                    callable.Call(thisArg, args);
                }
            }
            _engine._jsValueArrayPool.ReturnArray(args);

            return Undefined;
        }

        private JsValue Includes(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();

            if (len == 0)
            {
                return false;
            }

            var searchElement = arguments.At(0);
            var fromIndex = arguments.At(1, 0);

            var n = TypeConverter.ToNumber(fromIndex);
            n = n > ArrayOperations.MaxArrayLikeLength
                ? ArrayOperations.MaxArrayLikeLength
                : n;

            var k = (ulong) System.Math.Max(
                n >= 0 
                    ? n
                    : len - System.Math.Abs(n), 0);

            bool SameValueZero(JsValue x, JsValue y)
            {
                return x == y || (x is JsNumber xNum && y is JsNumber yNum && double.IsNaN(xNum._value) && double.IsNaN(yNum._value));
            }

            while (k < len)
            {
                var value = o.Get(k);
                if (SameValueZero(value, searchElement))
                {
                    return true;
                }
                k++;
            }
            return false;
        }

        private JsValue Some(JsValue thisObj, JsValue[] arguments)
        {
            var target = TypeConverter.ToObject(Engine, thisObj);
            return target.FindWithCallback(arguments, out _, out _, false);
        }

        private JsValue Every(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            ulong len = o.GetLongLength();

            if (len == 0)
            {
                return JsBoolean.True;
            }

            var callbackfn = arguments.At(0);
            var thisArg = arguments.At(1);
            var callable = GetCallable(callbackfn);

            var args = _engine._jsValueArrayPool.RentArray(3);
            args[2] = o.Target;
            for (uint k = 0; k < len; k++)
            {
                if (o.TryGetValue(k, out var kvalue))
                {
                    args[0] = kvalue;
                    args[1] = k;
                    var testResult = callable.Call(thisArg, args);
                    if (false == TypeConverter.ToBoolean(testResult))
                    {
                        return JsBoolean.False;
                    }
                }
            }
            _engine._jsValueArrayPool.ReturnArray(args);

            return JsBoolean.True;
        }

        private JsValue IndexOf(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();
            if (len == 0)
            {
                return -1;
            }

            var startIndex = arguments.Length > 1
                ? TypeConverter.ToNumber(arguments[1]) 
                : 0;

            if (startIndex > uint.MaxValue)
            {
                return -1;
            }

            ulong k;
            if (startIndex < 0)
            {
                var abs = System.Math.Abs(startIndex);
                ulong temp = len - (uint) abs;
                if (abs > len || temp < 0)
                {
                    temp = 0;
                }

                k = temp;
            }
            else
            {
                k = (ulong) startIndex;
            }

            if (k >= len)
            {
                return -1;
            }

            ulong smallestIndex = o.GetSmallestIndex(len);
            if (smallestIndex > k)
            {
                k = smallestIndex;
            }

            var searchElement = arguments.At(0);
            for (; k < len; k++)
            {
                var kPresent = o.HasProperty(k);
                if (kPresent)
                {
                    var elementK = o.Get(k);
                    var same = JintBinaryExpression.StrictlyEqual(elementK, searchElement);
                    if (same)
                    {
                        return k;
                    }
                }
            }

            return -1;
        }

        private JsValue Find(JsValue thisObj, JsValue[] arguments)
        {
            var target = TypeConverter.ToObject(Engine, thisObj);
            target.FindWithCallback(arguments, out _, out var value, true);
            return value;
        }

        private JsValue FindIndex(JsValue thisObj, JsValue[] arguments)
        {
            var target = TypeConverter.ToObject(Engine, thisObj);
            if (target.FindWithCallback(arguments, out var index, out _, true))
            {
                return index;
            }
            return -1;
        }

        private JsValue Splice(JsValue thisObj, JsValue[] arguments)
        {
            var start = arguments.At(0);
            var deleteCount = arguments.At(1);

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();
            var relativeStart = TypeConverter.ToInteger(start);

            ulong actualStart;
            if (relativeStart < 0)
            {
                actualStart = (ulong) System.Math.Max(len + relativeStart, 0);
            }
            else
            {
                actualStart = (ulong) System.Math.Min(relativeStart, len);
            }

            var items = new JsValue[0];
            ulong insertCount;
            ulong actualDeleteCount;
            if (arguments.Length == 0)
            {
                insertCount = 0;
                actualDeleteCount = 0;
            }
            else if (arguments.Length == 1)
            {
                insertCount = 0;
                actualDeleteCount = len - actualStart;
            }
            else
            {
                insertCount = (ulong) (arguments.Length - 2);
                var dc = TypeConverter.ToInteger(deleteCount);
                actualDeleteCount = (ulong) System.Math.Min(System.Math.Max(dc,0), len - actualStart);

                items = new JsValue[0];
                if (arguments.Length > 2)
                {
                    items = new JsValue[arguments.Length - 2];
                    System.Array.Copy(arguments, 2, items, 0, items.Length);
                }
            }

            if (len + insertCount - actualDeleteCount > ArrayOperations.MaxArrayLikeLength)
            {
                return ExceptionHelper.ThrowTypeError<JsValue>(_engine, "Invalid array length");
            }

            var instance = Engine.Array.ArraySpeciesCreate(TypeConverter.ToObject(_engine, thisObj), actualDeleteCount);
            var a = ArrayOperations.For(instance);
            for (uint k = 0; k < actualDeleteCount; k++)
            {
                var index = actualStart + k;
                if (o.HasProperty(index))
                {
                    var fromValue = o.Get(index);
                    a.CreateDataPropertyOrThrow(k, fromValue);
                }
            }
            a.SetLength((uint) actualDeleteCount);

            var length = len - actualDeleteCount + (uint) items.Length;
            o.EnsureCapacity(length);
            if ((ulong) items.Length < actualDeleteCount)
            {
                for (ulong k = actualStart; k < len - actualDeleteCount; k++)
                {
                    var from = k + actualDeleteCount;
                    var to = k + (ulong) items.Length;
                    if (o.HasProperty(from))
                    {
                        var fromValue = o.Get(from);
                        o.Set(to, fromValue, updateLength: false, throwOnError: false);
                    }
                    else
                    {
                        o.DeletePropertyOrThrow(to);
                    }
                }

                for (var k = len; k > len - actualDeleteCount + (ulong) items.Length; k--)
                {
                    o.DeletePropertyOrThrow(k - 1);
                }
            }
            else if ((ulong) items.Length > actualDeleteCount)
            {
                for (var k = len - actualDeleteCount; k > actualStart; k--)
                {
                    var from = k + actualDeleteCount - 1;
                    var to =  k + (ulong) items.Length - 1;
                    if (o.HasProperty(from))
                    {
                        var fromValue = o.Get(from);
                        o.Set(to, fromValue, updateLength: false, throwOnError: true);
                    }
                    else
                    {
                        o.DeletePropertyOrThrow(to);
                    }
                }
            }

            for (uint k = 0; k < items.Length; k++)
            {
                var e = items[k];
                o.Set(k + actualStart, e, updateLength: false, throwOnError: true);
            }

            o.SetLength(length);
            return a.Target;
        }

        private JsValue Unshift(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();
            var argCount = (uint) arguments.Length;

            if (len + argCount > ArrayOperations.MaxArrayLikeLength)
            {
                return ExceptionHelper.ThrowTypeError<JsValue>(_engine, "Invalid array length");
            }

            o.EnsureCapacity(len + argCount);
            var minIndex = o.GetSmallestIndex(len);
            for (var k = len; k > minIndex; k--)
            {
                var from = k - 1;
                var to = k + argCount - 1;
                if (o.TryGetValue(from, out var fromValue))
                {
                    o.Set(to, fromValue, false, true);
                }
                else
                {
                    o.DeletePropertyOrThrow(to);
                }
            }

            for (uint j = 0; j < argCount; j++)
            {
                o.Set(j, arguments[j], false, true);
            }

            o.SetLength(len + argCount);
            return len + argCount;
        }

        private JsValue Sort(JsValue thisObj, JsValue[] arguments)
        {
            if (!thisObj.IsObject())
            {
                ExceptionHelper.ThrowTypeError(_engine, "Array.prorotype.sort can only be applied on objects");
            }

            var obj = ArrayOperations.For(thisObj.AsObject());

            var compareArg = arguments.At(0);
            ICallable compareFn = null;
            if (!compareArg.IsUndefined())
            {
                if (compareArg.IsNull() || !(compareArg is ICallable))
                {
                    ExceptionHelper.ThrowTypeError(_engine, "The comparison function must be either a function or undefined");
                }

                compareFn = (ICallable) compareArg;
            }

            var len = obj.GetLength();
            if (len <= 1)
            {
                return obj.Target;
            }

            int Comparer(JsValue x, JsValue y)
            {
                if (ReferenceEquals(x, null))
                {
                    return 1;
                }

                if (ReferenceEquals(y, null))
                {
                    return -1;
                }

                var xUndefined = x.IsUndefined();
                var yUndefined = y.IsUndefined();
                if (xUndefined && yUndefined)
                {
                    return 0;
                }

                if (xUndefined)
                {
                    return 1;
                }

                if (yUndefined)
                {
                    return -1;
                }

                if (compareFn != null)
                {
                    var s = TypeConverter.ToNumber(compareFn.Call(Undefined, new[] {x, y}));
                    if (s < 0)
                    {
                        return -1;
                    }

                    if (s > 0)
                    {
                        return 1;
                    }

                    return 0;
                }

                var xString = TypeConverter.ToString(x);
                var yString = TypeConverter.ToString(y);

                var r = CompareOrdinal(xString, yString);
                return r;
            }

            var array = new JsValue[len];
            for (uint i = 0; i < (uint) array.Length; ++i)
            {
                var value = obj.TryGetValue(i, out var temp)
                    ? temp
                    : null;
                array[i] = value;
            }

            // don't eat inner exceptions
            try
            {
                System.Array.Sort(array, Comparer);
            }
            catch (InvalidOperationException e)
            {
                throw e.InnerException;
            }

            for (uint i = 0; i < (uint) array.Length; ++i)
            {
                if (!ReferenceEquals(array[i], null))
                {
                    obj.Set(i, array[i], updateLength: false, throwOnError: false);
                }
                else
                {
                    obj.DeletePropertyOrThrow(i);
                }
            }

            return obj.Target;
        }

        internal JsValue Slice(JsValue thisObj, JsValue[] arguments)
        {
            var start = arguments.At(0);
            var end = arguments.At(1);

            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();

            var relativeStart = TypeConverter.ToInteger(start);
            ulong k;
            if (relativeStart < 0)
            {
                k = (ulong) System.Math.Max(len + relativeStart, 0);
            }
            else
            {
                k = (ulong) System.Math.Min(TypeConverter.ToInteger(start), len);
            }

            ulong final;
            if (end.IsUndefined())
            {
                final = (ulong) TypeConverter.ToNumber(len);
            }
            else
            {
                double relativeEnd = TypeConverter.ToInteger(end);
                if (relativeEnd < 0)
                {
                    final = (ulong) System.Math.Max(len + relativeEnd, 0);
                }
                else
                {
                    final = (ulong) System.Math.Min(TypeConverter.ToInteger(relativeEnd), len);
                }
            }

            if (k < final && final - k > ArrayOperations.MaxArrayLength)
            {
                ExceptionHelper.ThrowRangeError(_engine, "Invalid array length");;
            }

            var length = (uint) System.Math.Max(0, (long) final - (long) k);
            var a = Engine.Array.ArraySpeciesCreate(TypeConverter.ToObject(_engine, thisObj), length);
            if (thisObj is ArrayInstance ai && a is ArrayInstance a2)
            {
                a2.CopyValues(ai, (uint) k, 0, length);
            }
            else
            {
                // slower path
                var operations = ArrayOperations.For(a);
                for (uint n = 0; k < final; k++, n++)
                {
                    if (o.TryGetValue(k, out var kValue))
                    {
                        operations.Set(n, kValue, updateLength: false, throwOnError: false);
                    }
                }
            }
            a.DefineOwnProperty(CommonProperties.Length, new PropertyDescriptor(length, PropertyFlag.None));

            return a;
        }

        private JsValue Shift(JsValue thisObj, JsValue[] arg2)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLength();
            if (len == 0)
            {
                o.SetLength(0);
                return Undefined;
            }

            var first = o.Get(0);
            for (uint k = 1; k < len; k++)
            {
                var to = k - 1;
                if (o.TryGetValue(k, out var fromVal))
                {
                    o.Set(to, fromVal, updateLength: false, throwOnError: false);
                }
                else
                {
                    o.DeletePropertyOrThrow(to);
                }
            }

            o.DeletePropertyOrThrow(len - 1);
            o.SetLength(len - 1);

            return first;
        }

        private JsValue Reverse(JsValue thisObj, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLongLength();
            var middle = (ulong) System.Math.Floor(len / 2.0);
            uint lower = 0;
            while (lower != middle)
            {
                var upper = len - lower - 1;

                var lowerExists = o.HasProperty(lower);
                var lowerValue = lowerExists ? o.Get(lower) : null;

                var upperExists = o.HasProperty(upper);
                var upperValue = upperExists ? o.Get(upper) : null;
                
                if (lowerExists && upperExists)
                {
                    o.Set(lower, upperValue, updateLength: true, throwOnError: true);
                    o.Set(upper, lowerValue, updateLength: true, throwOnError: true);
                }

                if (!lowerExists && upperExists)
                {
                    o.Set(lower, upperValue, updateLength: true, throwOnError: true);
                    o.DeletePropertyOrThrow(upper);
                }

                if (lowerExists && !upperExists)
                {
                    o.DeletePropertyOrThrow(lower);
                    o.Set(upper, lowerValue, updateLength: true, throwOnError: true);
                }

                lower++;
            }

            return o.Target;
        }

        private JsValue Join(JsValue thisObj, JsValue[] arguments)
        {
            var separator = arguments.At(0);
            var o = ArrayOperations.For(Engine, thisObj);
            var len = o.GetLength();
            if (separator.IsUndefined())
            {
                separator = ",";
            }

            var sep = TypeConverter.ToString(separator);

            // as per the spec, this has to be called after ToString(separator)
            if (len == 0)
            {
                return JsString.Empty;
            }

            string StringFromJsValue(JsValue value)
            {
                return value.IsNullOrUndefined()
                    ? ""
                    : TypeConverter.ToString(value);
            }

            var s = StringFromJsValue(o.Get(0));
            if (len == 1)
            {
                return s;
            }

            using (var sb = StringBuilderPool.Rent())
            {
                sb.Builder.Append(s);
                for (uint k = 1; k < len; k++)
                {
                    sb.Builder.Append(sep);
                    sb.Builder.Append(StringFromJsValue(o.Get(k)));
                }

                return sb.ToString();
            }
        }

        private JsValue ToLocaleString(JsValue thisObj, JsValue[] arguments)
        {
            var array = ArrayOperations.For(Engine, thisObj);
            var len = array.GetLength();
            const string separator = ",";
            if (len == 0)
            {
                return JsString.Empty;
            }

            JsValue r;
            if (!array.TryGetValue(0, out var firstElement) || firstElement.IsNull() || firstElement.IsUndefined())
            {
                r = JsString.Empty;
            }
            else
            {
                var elementObj = TypeConverter.ToObject(Engine, firstElement);
                var func = elementObj.Get("toLocaleString", elementObj) as ICallable ?? ExceptionHelper.ThrowTypeError<ICallable>(_engine);

                r = func.Call(elementObj, Arguments.Empty);
            }

            for (uint k = 1; k < len; k++)
            {
                string s = r + separator;
                if (!array.TryGetValue(k, out var nextElement) || nextElement.IsNull())
                {
                    r = JsString.Empty;
                }
                else
                {
                    var elementObj = TypeConverter.ToObject(Engine, nextElement);
                    var func = elementObj.Get("toLocaleString", elementObj) as ICallable ?? ExceptionHelper.ThrowTypeError<ICallable>(_engine);
                    r = func.Call(elementObj, Arguments.Empty);
                }

                r = s + r;
            }

            return r;
        }

        private JsValue Concat(JsValue thisObj, JsValue[] arguments)
        {
            var o = TypeConverter.ToObject(Engine, thisObj);
            var items = new List<JsValue>(arguments.Length + 1) {o};
            items.AddRange(arguments);

            // try to find best capacity
            bool hasObjectSpreadables = false;
            uint capacity = 0;
            for (var i = 0; i < items.Count; i++)
            {
                uint increment;
                if (!(items[i] is ObjectInstance objectInstance))
                {
                    increment = 1;
                }
                else
                {
                    var isConcatSpreadable = objectInstance.IsConcatSpreadable;
                    hasObjectSpreadables |= isConcatSpreadable;
                    increment = isConcatSpreadable ? ArrayOperations.For(objectInstance).GetLength() : 1; 
                }
                capacity += increment;
            }

            uint n = 0;
            var a = Engine.Array.ArraySpeciesCreate(TypeConverter.ToObject(_engine, thisObj), capacity);
            var aOperations = ArrayOperations.For(a);
            for (var i = 0; i < items.Count; i++)
            {
                var e = items[i];
                if (e is ArrayInstance eArray
                    && eArray.IsConcatSpreadable
                    && a is ArrayInstance a2)
                {
                    a2.CopyValues(eArray, 0, n, eArray.GetLength());
                    n += eArray.GetLength();
                }
                else if (hasObjectSpreadables
                         && e is ObjectInstance oi 
                         && oi.IsConcatSpreadable)
                {
                    var operations = ArrayOperations.For(oi);
                    var len = operations.GetLength();
                    for (uint k = 0; k < len; k++)
                    {
                        operations.TryGetValue(k, out var subElement);
                        aOperations.Set(n, subElement, updateLength: false, throwOnError: false);
                        n++;
                    }
                }
                else
                {
                    aOperations.Set(n, e, updateLength: false, throwOnError: false);
                    n++;
                }
            }

            // this is not in the specs, but is necessary in case the last element of the last
            // array doesn't exist, and thus the length would not be incremented
            a.DefineOwnProperty(CommonProperties.Length, new PropertyDescriptor(n, PropertyFlag.None));

            return a;
        }

        private JsValue ToString(JsValue thisObj, JsValue[] arguments)
        {
            var array = TypeConverter.ToObject(Engine, thisObj);

            ICallable func;
            func = array.Get("join", array).TryCast<ICallable>(x =>
            {
                func = Engine.Object.PrototypeObject.Get("toString", array).TryCast<ICallable>(y => ExceptionHelper.ThrowArgumentException());
            });

            if (array.IsArrayLike == false || func == null)
                return _engine.Object.PrototypeObject.ToObjectString(array, Arguments.Empty);

            return func.Call(array, Arguments.Empty);
        }

        private JsValue ReduceRight(JsValue thisObj, JsValue[] arguments)
        {
            var callbackfn = arguments.At(0);
            var initialValue = arguments.At(1);

            var o = ArrayOperations.For(TypeConverter.ToObject(_engine, thisObj));
            var len = o.GetLongLength();

            var callable = GetCallable(callbackfn);

            if (len == 0 && arguments.Length < 2)
            {
                ExceptionHelper.ThrowTypeError(Engine);
            }

            long k = (long) (len - 1);
            JsValue accumulator = Undefined;
            if (arguments.Length > 1)
            {
                accumulator = initialValue;
            }
            else
            {
                var kPresent = false;
                while (kPresent == false && k >= 0)
                {
                    if ((kPresent = o.TryGetValue((ulong) k, out var temp)))
                    {
                        accumulator = temp;
                    }

                    k--;
                }

                if (kPresent == false)
                {
                    ExceptionHelper.ThrowTypeError(Engine);
                }
            }

            var jsValues = new JsValue[4];
            jsValues[3] = o.Target;
            for (; k >= 0; k--)
            {
                if (o.TryGetValue((ulong) k, out var kvalue))
                {
                    jsValues[0] = accumulator;
                    jsValues[1] = kvalue;
                    jsValues[2] = k;
                    accumulator = callable.Call(Undefined, jsValues);
                }
            }

            return accumulator;
        }

        public JsValue Push(JsValue thisObject, JsValue[] arguments)
        {
            if (thisObject is ArrayInstance arrayInstance)
            {
                return arrayInstance.Push(arguments);
            }

            var o = ArrayOperations.For(thisObject as ObjectInstance);
            var n = o.GetLongLength();

            if (n + (ulong) arguments.Length > ArrayOperations.MaxArrayLikeLength)
            {
                return ExceptionHelper.ThrowTypeError<JsValue>(_engine, "Invalid array length");
            }

            // cast to double as we need to prevent an overflow
            foreach (var a in arguments)
            {
                o.Set(n, a, false, false);
                n++;
            }

            o.SetLength(n);
            return n;
        }

        public JsValue Pop(JsValue thisObject, JsValue[] arguments)
        {
            var o = ArrayOperations.For(Engine, thisObject);
            ulong len = o.GetLongLength();
            if (len == 0)
            {
                o.SetLength(0);
                return Undefined;
            }

            len = len - 1;
            JsValue element = o.Get(len);
            o.DeletePropertyOrThrow(len);
            o.SetLength(len);
            return element;
        }
    }
}