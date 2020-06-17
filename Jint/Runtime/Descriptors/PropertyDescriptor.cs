﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jint.Collections;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors.Specialized;

namespace Jint.Runtime.Descriptors
{
    [DebuggerDisplay("Value: {Value}, Flags: {Flags}")]
    public class PropertyDescriptor
    {
        public static readonly PropertyDescriptor Undefined = new UndefinedPropertyDescriptor();

        internal PropertyFlag _flags;
        internal JsValue _value;

        protected PropertyDescriptor(PropertyFlag flags)
        {
            _flags = flags;
        }

        protected internal PropertyDescriptor(JsValue value, PropertyFlag flags) : this(flags)
        {
            if ((_flags & PropertyFlag.CustomJsValue) != 0)
            {
                CustomValue = value;
            }
            _value = value;
        }

        public PropertyDescriptor(JsValue value, bool? writable, bool? enumerable, bool? configurable)
        {
            if ((_flags & PropertyFlag.CustomJsValue) != 0)
            {
                CustomValue = value;
            }
            _value = value;

            if (writable != null)
            {
                Writable = writable.Value;
                WritableSet = true;
            }

            if (enumerable != null)
            {
                Enumerable = enumerable.Value;
                EnumerableSet = true;
            }

            if (configurable != null)
            {
                Configurable = configurable.Value;
                ConfigurableSet = true;
            }
        }

        public PropertyDescriptor(PropertyDescriptor descriptor)
        {
            Value = descriptor.Value;

            Enumerable = descriptor.Enumerable;
            EnumerableSet = descriptor.EnumerableSet;

            Configurable = descriptor.Configurable;
            ConfigurableSet = descriptor.ConfigurableSet;

            Writable = descriptor.Writable;
            WritableSet = descriptor.WritableSet;
        }

        public virtual JsValue Get => null;
        public virtual JsValue Set => null;

        public bool Enumerable
        {
            
            get => (_flags & PropertyFlag.Enumerable) != 0;
            
            set
            {
                _flags |= PropertyFlag.EnumerableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Enumerable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Enumerable);
                }
            }
        }

        public bool EnumerableSet
        {
            
            get => (_flags & (PropertyFlag.EnumerableSet | PropertyFlag.Enumerable)) != 0;
            
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.EnumerableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.EnumerableSet);
                }
            }
        }

        public bool Writable
        {
            
            get => (_flags & PropertyFlag.Writable) != 0;
            
            set
            {
                _flags |= PropertyFlag.WritableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Writable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Writable);
                }
            }
        }

        public bool WritableSet
        {
            
            get => (_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0;
            
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.WritableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.WritableSet);
                }
            }
        }

        public bool Configurable
        {
            
            get => (_flags & PropertyFlag.Configurable) != 0;
            
            set
            {
                _flags |= PropertyFlag.ConfigurableSet;
                if (value)
                {
                    _flags |= PropertyFlag.Configurable;
                }
                else
                {
                    _flags &= ~(PropertyFlag.Configurable);
                }
            }
        }

        public bool ConfigurableSet
        {
            
            get => (_flags & (PropertyFlag.ConfigurableSet | PropertyFlag.Configurable)) != 0;
            
            private set
            {
                if (value)
                {
                    _flags |= PropertyFlag.ConfigurableSet;
                }
                else
                {
                    _flags &= ~(PropertyFlag.ConfigurableSet);
                }
            }
        }

        public JsValue Value
        {
            
            get
            {
                if ((_flags & PropertyFlag.CustomJsValue) != 0)
                {
                    return CustomValue;
                }

                return _value;
            }
            
            set
            {
                if ((_flags & PropertyFlag.CustomJsValue) != 0)
                {
                    CustomValue = value;
                }
                _value = value;
            }
        }

        protected internal virtual JsValue CustomValue
        {
            get => null;
            set => ExceptionHelper.ThrowNotImplementedException();
        }

        internal PropertyFlag Flags
        {
            
            get { return _flags; }
        }

        public static PropertyDescriptor ToPropertyDescriptor(Engine engine, JsValue o)
        {
            var obj = o.TryCast<ObjectInstance>();
            if (ReferenceEquals(obj, null))
            {
                ExceptionHelper.ThrowTypeError(engine);
            }

            var getProperty = obj.GetProperty(CommonProperties.Get);
            var hasGetProperty = getProperty != Undefined;
            var setProperty = obj.GetProperty(CommonProperties.Set);
            var hasSetProperty = setProperty != Undefined;

            if ((obj.HasProperty(CommonProperties.Value) || obj.HasProperty(CommonProperties.Writable)) &&
                (hasGetProperty || hasSetProperty))
            {
                ExceptionHelper.ThrowTypeError(engine);
            }

            var desc = hasGetProperty || hasSetProperty
                ? new GetSetPropertyDescriptor(null, null, PropertyFlag.None)
                : new PropertyDescriptor(PropertyFlag.None);

            var enumerableProperty = obj.GetProperty(CommonProperties.Enumerable);
            if (enumerableProperty != Undefined)
            {
                desc.Enumerable = TypeConverter.ToBoolean(obj.UnwrapJsValue(enumerableProperty));
                desc.EnumerableSet = true;
            }

            var configurableProperty = obj.GetProperty(CommonProperties.Configurable);
            if (configurableProperty != Undefined)
            {
                desc.Configurable = TypeConverter.ToBoolean(obj.UnwrapJsValue(configurableProperty));
                desc.ConfigurableSet = true;
            }

            var valueProperty = obj.GetProperty(CommonProperties.Value);
            if (valueProperty != Undefined)
            {
                desc.Value = obj.UnwrapJsValue(valueProperty);
            }

            var writableProperty = obj.GetProperty(CommonProperties.Writable);
            if (writableProperty != Undefined)
            {
                desc.Writable = TypeConverter.ToBoolean(obj.UnwrapJsValue(writableProperty));
                desc.WritableSet = true;
            }

            if (hasGetProperty)
            {
                var getter = obj.UnwrapJsValue(getProperty);
                if (!getter.IsUndefined() && getter.TryCast<ICallable>() == null)
                {
                    ExceptionHelper.ThrowTypeError(engine);
                }

                ((GetSetPropertyDescriptor) desc).SetGet(getter);
            }

            if (hasSetProperty)
            {
                var setter = obj.UnwrapJsValue(setProperty);
                if (!setter.IsUndefined() && setter.TryCast<ICallable>() == null)
                {
                    ExceptionHelper.ThrowTypeError(engine);
                }

                ((GetSetPropertyDescriptor) desc).SetSet(setter);
            }

            if (!ReferenceEquals(desc.Get, null))
            {
                if (!ReferenceEquals(desc.Value, null) || desc.WritableSet)
                {
                    ExceptionHelper.ThrowTypeError(engine);
                }
            }

            return desc;
        }

        public static JsValue FromPropertyDescriptor(Engine engine, PropertyDescriptor desc)
        {
            if (ReferenceEquals(desc, Undefined))
            {
                return Native.Undefined.Instance;
            }

            var obj = engine.Object.Construct(Arguments.Empty);
            var properties = new PropertyDictionary(4, checkExistingKeys: false);

            if (desc.IsDataDescriptor())
            {
                properties["value"] =  new PropertyDescriptor(desc.Value ?? Native.Undefined.Instance, PropertyFlag.ConfigurableEnumerableWritable);
                properties["writable"] = new PropertyDescriptor(desc.Writable, PropertyFlag.ConfigurableEnumerableWritable);
            }
            else
            {
                properties["get"] = new PropertyDescriptor(desc.Get ?? Native.Undefined.Instance, PropertyFlag.ConfigurableEnumerableWritable);
                properties["set"] = new PropertyDescriptor(desc.Set ?? Native.Undefined.Instance, PropertyFlag.ConfigurableEnumerableWritable);
            }

            properties["enumerable"] = new PropertyDescriptor(desc.Enumerable, PropertyFlag.ConfigurableEnumerableWritable);
            properties["configurable"] = new PropertyDescriptor(desc.Configurable, PropertyFlag.ConfigurableEnumerableWritable);

            obj.SetProperties(properties);
            return obj;
        }

        
        public bool IsAccessorDescriptor()
        {
            return !ReferenceEquals(Get, null) || !ReferenceEquals(Set, null);
        }

        
        public bool IsDataDescriptor()
        {
            return (_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0
                   || (_flags & PropertyFlag.CustomJsValue) != 0 && !ReferenceEquals(CustomValue, null)
                   || !ReferenceEquals(_value, null);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.10.3
        /// </summary>
        /// <returns></returns>
        
        public bool IsGenericDescriptor()
        {
            return !IsDataDescriptor() && !IsAccessorDescriptor();
        }

        
        internal bool TryGetValue(ObjectInstance thisArg, out JsValue value)
        {
            value = JsValue.Undefined;

            // IsDataDescriptor logic inlined
            if ((_flags & (PropertyFlag.WritableSet | PropertyFlag.Writable)) != 0)
            {
                var val = (_flags & PropertyFlag.CustomJsValue) != 0
                    ? CustomValue
                    : _value;

                if (!ReferenceEquals(val, null))
                {
                    value = val;
                    return true;
                }
            }

            if (this == Undefined)
            {
                return false;
            }

            var getter = Get;
            if (!ReferenceEquals(getter, null) && !getter.IsUndefined())
            {
                // if getter is not undefined it must be ICallable
                var callable = getter.TryCast<ICallable>();
                value = callable.Call(thisArg, Arguments.Empty);
            }

            return true;
        }

        private sealed class UndefinedPropertyDescriptor : PropertyDescriptor
        {
            public UndefinedPropertyDescriptor() : base(PropertyFlag.None | PropertyFlag.CustomJsValue)
            {
            }

            protected internal override JsValue CustomValue
            {
                set => ExceptionHelper.ThrowInvalidOperationException("making changes to undefined property's descriptor is not allowed");
            }
        }

        internal sealed class AllForbiddenDescriptor : PropertyDescriptor
        {
            private static readonly PropertyDescriptor[] _cache;

            public static readonly AllForbiddenDescriptor NumberZero = new AllForbiddenDescriptor(JsNumber.Create(0));
            public static readonly AllForbiddenDescriptor NumberOne = new AllForbiddenDescriptor(JsNumber.Create(1));

            public static readonly AllForbiddenDescriptor BooleanFalse = new AllForbiddenDescriptor(JsBoolean.False);
            public static readonly AllForbiddenDescriptor BooleanTrue = new AllForbiddenDescriptor(JsBoolean.True);

            static AllForbiddenDescriptor()
            {
                _cache = new PropertyDescriptor[10];
                for (int i = 0; i < _cache.Length; ++i)
                {
                    _cache[i] = new AllForbiddenDescriptor(JsNumber.Create(i));
                }
            }

            private AllForbiddenDescriptor(JsValue value)
                : base(PropertyFlag.AllForbidden)
            {
                _value = value;
            }

            public static PropertyDescriptor ForNumber(int number)
            {
                var temp = _cache;
                return (uint) number < temp.Length
                    ? temp[number]
                    : new PropertyDescriptor(number, PropertyFlag.AllForbidden);
            }
        }
    }
}