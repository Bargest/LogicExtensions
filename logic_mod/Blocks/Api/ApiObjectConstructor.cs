using Jint;
using Jint.Collections;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Iterator;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks.Api
{
    public class ApiObjectPrototype : ObjectInstance
    {
        private ApiObjectConstructor _constructor;
        private Dictionary<string, CpuApiProperty> Props;
        private CpuBlock cpu;

        private ApiObjectPrototype(CpuBlock block, List<CpuApiProperty> props) : base(block.Interp)
        {
            cpu = block;
            Props = props?.ToDictionary(x => x.Name);
        }

        public static ApiObjectPrototype CreatePrototypeObject(CpuBlock block, ApiObjectConstructor constructor, List<CpuApiProperty> props)
        {
            var obj = new ApiObjectPrototype(block, props)
            {
                _prototype = block.Interp.Object.PrototypeObject,
                _constructor = constructor
            };
            return obj;
        }

        protected override void Initialize()
        {
            const PropertyFlag propertyFlags = PropertyFlag.Configurable | PropertyFlag.Writable;
            var properties = new PropertyDictionary(Props?.Count ?? 0, checkExistingKeys: false);
            if (Props != null)
            {
                foreach (var kp in Props)
                {
                    if (kp.Value is CpuApiFunc f)
                    {
                        var impl = f.ImplementationFactory(cpu);
                        properties[kp.Key] = new PropertyDescriptor(new ClrFunctionInstance(Engine, kp.Key, (t, a) => ApiObjectConstructor.WrappedApi(t, a, impl), f.Arguments.Count, PropertyFlag.Configurable), propertyFlags);
                    }
                    else if (kp.Value is CpuApiValue v)
                        properties[kp.Key] = new PropertyDescriptor(v.Value, PropertyFlag.Configurable | PropertyFlag.Writable);
                }
            }
            SetProperties(properties);
        }
    }

    public class ApiObjectConstructor : FunctionInstance, IConstructor
    {
        public const string ConstructorMethodName = "constructor";

        private CpuBlock cpu;
        public ApiObjectPrototype PrototypeObject { get; private set; }

        private Dictionary<string, CpuApiProperty> Props;

        Func<JsValue, JsValue[], JsValue> Constructor;

        public static ApiObjectConstructor CreateApiConstructor(CpuBlock cpu, string name, List<CpuApiProperty> staticProps, List<CpuApiProperty> instanceProps)
        {
            var obj = new ApiObjectConstructor(cpu, name, staticProps)
            {
                _prototype = cpu.Interp.Function.PrototypeObject,
            };
            obj.PrototypeObject = ApiObjectPrototype.CreatePrototypeObject(cpu, obj, instanceProps);
            obj._prototypeDescriptor = new PropertyDescriptor(obj.PrototypeObject, PropertyFlag.AllForbidden);
            return obj;
        }

        public static JsValue WrappedApi(JsValue t, JsValue[] a, Func<JsValue, JsValue[], JsValue> impl)
        {
            try
            {
                return impl(t, a);
            }
            catch (Exception e)
            {
                if (e is JavaScriptException)
                    throw;
                throw new JavaScriptException(e.ToString());
            }
        }

        public ApiObjectConstructor(CpuBlock c, string name, List<CpuApiProperty> staticProps) : base(c.Interp, new JsString(name))
        {
            cpu = c;
            Props = staticProps?.ToDictionary(x => x.Name);
            var properties = new PropertyDictionary(Props?.Count ?? 0, checkExistingKeys: false);
            if (Props != null)
            {
                foreach (var kp in Props)
                {
                    if (kp.Value is CpuApiFunc f)
                    {
                        if (f.Name == ConstructorMethodName)
                            Constructor = f.ImplementationFactory(cpu);
                        else
                        {
                            var impl = f.ImplementationFactory(cpu);
                            properties[kp.Key] = new PropertyDescriptor(new ClrFunctionInstance(Engine, kp.Key, (t, a) => WrappedApi(t, a, impl), f.Arguments.Count, PropertyFlag.Configurable), PropertyFlag.Configurable);
                        }
                    }
                    else if (kp.Value is CpuApiValue v)
                        properties[kp.Key] = new PropertyDescriptor(v.Value, PropertyFlag.Configurable);
                }
            }
            SetProperties(properties);
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            if (thisObject.IsUndefined())
                throw new JavaScriptException(_engine.TypeError, "Constructor requires 'new'", null);

            return Construct(arguments, thisObject);
        }

        public ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            var obj = new ObjectInstance(Engine)
            {
                _prototype = PrototypeObject
            };

            Constructor?.Invoke(obj, arguments);
            return obj;
        }
    }
    
}
