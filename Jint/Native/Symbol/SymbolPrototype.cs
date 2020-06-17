﻿using Jint.Collections;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Interop;

namespace Jint.Native.Symbol
{
    /// <summary>
    /// http://www.ecma-international.org/ecma-262/5.1/#sec-15.5.4
    /// </summary>
    public sealed class SymbolPrototype : ObjectInstance
    {
        private SymbolConstructor _symbolConstructor;

        private SymbolPrototype(Engine engine)
            : base(engine)
        {
        }

        public static SymbolPrototype CreatePrototypeObject(Engine engine, SymbolConstructor symbolConstructor)
        {
            var obj = new SymbolPrototype(engine)
            {
                _prototype = engine.Object.PrototypeObject,
                _symbolConstructor = symbolConstructor
            };

            return obj;
        }

        protected override void Initialize()
        {
            const PropertyFlag lengthFlags = PropertyFlag.Configurable;
            const PropertyFlag propertyFlags = PropertyFlag.Configurable;
            SetProperties(new PropertyDictionary(5, checkExistingKeys: false)
            {
                ["length"] = new PropertyDescriptor(JsNumber.PositiveZero, propertyFlags),
                ["constructor"] = new PropertyDescriptor(_symbolConstructor, PropertyFlag.Configurable | PropertyFlag.Writable),
                ["description"] = new GetSetPropertyDescriptor(new ClrFunctionInstance(Engine, "description", Description, 0, lengthFlags), Undefined, propertyFlags),
                ["toString"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "toString", ToSymbolString, 0, lengthFlags), PropertyFlag.Configurable | PropertyFlag.Writable),
                ["valueOf"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "valueOf", ValueOf, 0, lengthFlags), PropertyFlag.Configurable | PropertyFlag.Writable)
            });

            SetSymbols(new SymbolDictionary(1)
                {
                    [GlobalSymbolRegistry.ToPrimitive] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "[Symbol.toPrimitive]", ToPrimitive, 1, lengthFlags), propertyFlags), [GlobalSymbolRegistry.ToStringTag] = new PropertyDescriptor(new JsString("Symbol"), propertyFlags)
                }
            );
        }

        private JsValue Description(JsValue thisObject, JsValue[] arguments)
        {
            var sym = ThisSymbolValue(thisObject);
            return sym._value;
        }

        private JsValue ToSymbolString(JsValue thisObject, JsValue[] arguments)
        {
            var sym = ThisSymbolValue(thisObject);
            return new JsString(SymbolDescriptiveString(sym));
        }

        private JsValue ValueOf(JsValue thisObject, JsValue[] arguments)
        {
            return ThisSymbolValue(thisObject);
        }

        private JsValue ToPrimitive(JsValue thisObject, JsValue[] arguments)
        {
            return ThisSymbolValue(thisObject);
        }

        private static string SymbolDescriptiveString(JsSymbol symbol) => symbol.ToString();

        private JsSymbol ThisSymbolValue(JsValue thisObject)
        {
            if (thisObject is JsSymbol s)
            {
                return s;
            }

            if (thisObject is SymbolInstance instance)
            {
                return instance.SymbolData;
            }

            return ExceptionHelper.ThrowTypeError<JsSymbol>(_engine);
        }
    }
}
