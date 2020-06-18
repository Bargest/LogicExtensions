using System;
using System.Globalization;
using System.Reflection;
using Jint.Native;
using Jint.Native.Function;

namespace Jint.Runtime.Interop
{
    /// <summary>
    /// Represents a FunctionInstance wrapper around a CLR method. This is used by user to pass
    /// custom methods to the engine.
    /// </summary>
    public sealed class DelegateWrapper : FunctionInstance
    {
        private static readonly JsString _name = new JsString("delegate");
        //private readonly Delegate _d;
        //private readonly bool _delegateContainsParamsArgument;
        private readonly Func<JsValue, JsValue[], JsValue> _d;

        public DelegateWrapper(Engine engine, Func<JsValue, JsValue[], JsValue> d)
            : base(engine, _name, FunctionThisMode.Global)
        {
            _d = d;
            _prototype = engine.Function.PrototypeObject;
        }

        public DelegateWrapper(Engine engine, Func<JsValue[], JsValue> d)
            : this(engine, (t, a) => d(a))
        {
        }

        public DelegateWrapper(Engine engine, Delegate d)
            : this(engine, (d is Func<JsValue[], JsValue> f) ? (t, a) => f(a) : (Func<JsValue, JsValue[], JsValue>)d)
        {
            /*var parameterInfos = _d.Method.GetParameters();

            _delegateContainsParamsArgument = false;
            foreach (var p in parameterInfos)
            {
                if (Attribute.IsDefined(p, typeof(ParamArrayAttribute)))
                {
                    _delegateContainsParamsArgument = true;
                    break;
                }
            }*/
        }

        public override JsValue Call(JsValue thisObject, JsValue[] jsArguments)
        {
            // TODO: this breaks CLR integration
            /*
            var parameterInfos = _d.Method.GetParameters();

#if NETFRAMEWORK
            if (parameterInfos.Length > 0 && parameterInfos[0].ParameterType == typeof(System.Runtime.CompilerServices.Closure))
            {
                var reducedLength = parameterInfos.Length - 1;
                var reducedParameterInfos = new ParameterInfo[reducedLength];
                Array.Copy(parameterInfos, 1, reducedParameterInfos, 0, reducedLength);
                parameterInfos = reducedParameterInfos;
            }
#endif

            int delegateArgumentsCount = parameterInfos.Length;
            int delegateNonParamsArgumentsCount = _delegateContainsParamsArgument ? delegateArgumentsCount - 1 : delegateArgumentsCount;

            int jsArgumentsCount = jsArguments.Length;
            int jsArgumentsWithoutParamsCount = Math.Min(jsArgumentsCount, delegateNonParamsArgumentsCount);

            var parameters = new object[delegateArgumentsCount];

            // convert non params parameter to expected types
            for (var i = 0; i < jsArgumentsWithoutParamsCount; i++)
            {
                var parameterType = parameterInfos[i].ParameterType;

                if (parameterType == typeof(JsValue))
                {
                    parameters[i] = jsArguments[i];
                }
                else
                {
                    parameters[i] = Engine.ClrTypeConverter.Convert(
                        jsArguments[i].ToObject(),
                        parameterType,
                        CultureInfo.InvariantCulture);
                }
            }

            // assign null to parameters not provided
            for (var i = jsArgumentsWithoutParamsCount; i < delegateNonParamsArgumentsCount; i++)
            {
                if (parameterInfos[i].ParameterType.IsValueType)
                {
                    parameters[i] = Activator.CreateInstance(parameterInfos[i].ParameterType);
                }
                else
                {
                    parameters[i] = null;
                }
            }

            // assign params to array and converts each objet to expected type
            if (_delegateContainsParamsArgument)
            {
                int paramsArgumentIndex = delegateArgumentsCount - 1;
                int paramsCount = Math.Max(0, jsArgumentsCount - delegateNonParamsArgumentsCount);

                object[] paramsParameter = new object[paramsCount];
                var paramsParameterType = parameterInfos[paramsArgumentIndex].ParameterType.GetElementType();

                for (var i = paramsArgumentIndex; i < jsArgumentsCount; i++)
                {
                    var paramsIndex = i - paramsArgumentIndex;

                    if (paramsParameterType == typeof(JsValue))
                    {
                        paramsParameter[paramsIndex] = jsArguments[i];
                    }
                    else
                    {
                        paramsParameter[paramsIndex] = Engine.ClrTypeConverter.Convert(
                            jsArguments[i].ToObject(),
                            paramsParameterType,
                            CultureInfo.InvariantCulture);
                    }
                }
                parameters[paramsArgumentIndex] = paramsParameter;
            }*/
            try
            {
                //return FromObject(Engine, _d.DynamicInvoke(jsArguments));
                return FromObject(Engine, _d(thisObject, jsArguments));
            }
            catch (Exception exception)
            {
                //ExceptionHelper.ThrowMeaningfulException(_engine, exception);
                ExceptionHelper.ThrowError(_engine, exception.Message);
                throw;
            }
        }
    }
}
