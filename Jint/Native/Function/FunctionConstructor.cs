﻿using Esprima;
using Esprima.Ast;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
    public sealed class FunctionConstructor : FunctionInstance, IConstructor
    {
        private static readonly ParserOptions ParserOptions = new ParserOptions { AdaptRegexp = true, Tolerant = false };
        private static readonly JsString _functionName = new JsString("Function");

        private FunctionInstance _throwTypeError;
        private static readonly char[] ArgumentNameSeparator = new[] { ',' };

        private FunctionConstructor(Engine engine)
            : base(engine, _functionName)
        {
        }

        public static FunctionConstructor CreateFunctionConstructor(Engine engine)
        {
            var obj = new FunctionConstructor(engine)
            {
                PrototypeObject = FunctionPrototype.CreatePrototypeObject(engine)
            };

            // The initial value of Function.prototype is the standard built-in Function prototype object

            // The value of the [[Prototype]] internal property of the Function constructor is the standard built-in Function prototype object
            obj._prototype = obj.PrototypeObject;

            obj._prototypeDescriptor = new PropertyDescriptor(obj.PrototypeObject, PropertyFlag.AllForbidden);
            obj._length = new PropertyDescriptor(JsNumber.One, PropertyFlag.Configurable);

            return obj;
        }

        public FunctionPrototype PrototypeObject { get; private set; }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            return Construct(arguments, thisObject);
        }

        public ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            var argCount = arguments.Length;
            string p = "";
            string body = "";

            if (argCount == 1)
            {
                body = TypeConverter.ToString(arguments[0]);
            }
            else if (argCount > 1)
            {
                var firstArg = arguments[0];
                p = TypeConverter.ToString(firstArg);
                for (var k = 1; k < argCount - 1; k++)
                {
                    var nextArg = arguments[k];
                    p += "," + TypeConverter.ToString(nextArg);
                }

                body = TypeConverter.ToString(arguments[argCount-1]);
            }

            IFunction function;
            try
            {
                var functionExpression = "function f(" + p + ") { " + body + "}";
                var parser = new JavaScriptParser(functionExpression, ParserOptions);
                function = (IFunction) parser.ParseScript().Body[0];
            }
            catch (ParserException)
            {
                return ExceptionHelper.ThrowSyntaxError<ObjectInstance>(_engine);
            }

            var functionObject = new ScriptFunctionInstance(
                Engine,
                function,
                _engine.GlobalEnvironment,
                function.Strict);

            return functionObject;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-13.2
        /// </summary>
        /// <param name="functionDeclaration"></param>
        /// <returns></returns>
        public FunctionInstance CreateFunctionObject(FunctionDeclaration functionDeclaration, LexicalEnvironment env)
        {
            var functionObject = new ScriptFunctionInstance(
                Engine,
                functionDeclaration,
                env, 
                functionDeclaration.Strict || _engine._isStrict);

            return functionObject;
        }

        public FunctionInstance ThrowTypeError
        {
            get
            {
                if (!ReferenceEquals(_throwTypeError, null))
                {
                    return _throwTypeError;
                }

                _throwTypeError = new ThrowTypeError(Engine);
                return _throwTypeError;
            }
        }

        public object Apply(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.Length != 2)
            {
                ExceptionHelper.ThrowArgumentException("Apply has to be called with two arguments.");
            }

            var func = thisObject.TryCast<ICallable>();
            var thisArg = arguments[0];
            var argArray = arguments[1];

            if (func is null)
            {
                return ExceptionHelper.ThrowTypeError<object>(Engine);
            }

            if (argArray.IsNullOrUndefined())
            {
                return func.Call(thisArg, Arguments.Empty);
            }

            var argArrayObj = argArray.TryCast<ObjectInstance>();
            if (ReferenceEquals(argArrayObj, null))
            {
                ExceptionHelper.ThrowTypeError(Engine);
            }

            var len = argArrayObj.Get(CommonProperties.Length, argArrayObj);
            var n = TypeConverter.ToUint32(len);
            var argList = new JsValue[n];
            for (var index = 0; index < n; index++)
            {
                var indexName = TypeConverter.ToString(index);
                var nextArg = argArrayObj.Get(JsString.Create(indexName), argArrayObj);
                argList[index] = nextArg;
            }
            return func.Call(thisArg, argList);
        }
    }
}