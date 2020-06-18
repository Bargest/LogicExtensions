using Esprima.Ast;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintCallExpression : JintExpression
    {
        private readonly bool _isDebugMode;
        private readonly int _maxRecursionDepth;

        private CachedArgumentsHolder _cachedArguments;
        private bool _cached;

        private readonly JintExpression _calleeExpression;
        private bool _hasSpreads;

        public JintCallExpression(Engine engine, CallExpression expression) : base(engine, expression)
        {
            _initialized = false;
            _isDebugMode = engine.Options.IsDebugMode;
            _maxRecursionDepth = engine.Options.MaxRecursionDepth;
            _calleeExpression = Build(engine, expression.Callee);
        }

        protected override void Initialize()
        {
            var expression = (CallExpression) _expression;
            var cachedArgumentsHolder = new CachedArgumentsHolder
            {
                JintArguments = new JintExpression[expression.Arguments.Count]
            };

            bool CanSpread(Node e)
            {
                return e?.Type == Nodes.SpreadElement
                    || e is AssignmentExpression ae && ae.Right?.Type == Nodes.SpreadElement;
            }

            bool cacheable = true;
            for (var i = 0; i < expression.Arguments.Count; i++)
            {
                var expressionArgument = expression.Arguments[i];
                cachedArgumentsHolder.JintArguments[i] = Build(_engine, expressionArgument);
                cacheable &= expressionArgument.Type == Nodes.Literal;
                _hasSpreads |= CanSpread(expressionArgument);
                if (expressionArgument is ArrayExpression ae)
                {
                    for (var elementIndex = 0; elementIndex < ae.Elements.Count; elementIndex++)
                    {
                        _hasSpreads |= CanSpread(ae.Elements[elementIndex]);
                    }
                }
            }

            if (cacheable)
            {
                _cached = true;
                var arguments = new JsValue[0];
                if (cachedArgumentsHolder.JintArguments.Length > 0)
                {
                    arguments = new JsValue[cachedArgumentsHolder.JintArguments.Length];
                    BuildArguments(cachedArgumentsHolder.JintArguments, arguments);
                }

                cachedArgumentsHolder.CachedArguments = arguments;
            }

            _cachedArguments = cachedArgumentsHolder;
        }

        protected override object EvaluateInternal()
        {
            var callee = _calleeExpression.Evaluate();
            var expression = (CallExpression) _expression;

            if (_isDebugMode)
            {
                _engine.DebugHandler.AddToDebugCallStack(expression);
            }

            // todo: implement as in http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.4

            var cachedArguments = _cachedArguments;
            var arguments = new JsValue[0];
            if (_cached)
            {
                arguments = cachedArguments.CachedArguments;
            }
            else
            {
                if (cachedArguments.JintArguments.Length > 0)
                {
                    if (_hasSpreads)
                    {
                        arguments = BuildArgumentsWithSpreads(cachedArguments.JintArguments);
                    }
                    else
                    {
                        arguments = _engine._jsValueArrayPool.RentArray(cachedArguments.JintArguments.Length);
                        BuildArguments(cachedArguments.JintArguments, arguments);
                    }
                }
            }


            var func = _engine.GetValue(callee, false);
            var r = callee as Reference;

            var stackItem = new CallStackElement(expression, func, r?.GetReferencedName()?.ToString() ?? "anonymous function");
            var recursionDepth = _engine.CallStack.Push(stackItem);

            JsValue result = null;
            if (_maxRecursionDepth >= 0)
            {
                if (recursionDepth > _maxRecursionDepth)
                    ExceptionHelper.ThrowRecursionDepthOverflowException(_engine.CallStack, stackItem.ToString());
            }

            if (func._type == InternalTypes.Undefined)
                ExceptionHelper.ThrowTypeError(_engine, r == null ? "" : $"Object has no method '{r.GetReferencedName()}'");

            if (!func.IsObject())
            {
                if (_engine._referenceResolver == null || !_engine._referenceResolver.TryGetCallable(_engine, callee, out func))
                    ExceptionHelper.ThrowTypeError(_engine, r == null ? "" : $"Property '{r.GetReferencedName()}' of object is not a function");
            }

            var callable = func as ICallable;
            if (callable == null)
            {
                var message = $"{r?.GetReferencedName() ?? ""} is not a function";
                ExceptionHelper.ThrowTypeError<object>(_engine, message);
            }

            var evalCalled = false;
            var thisObject = Undefined.Instance;
            if (r != null)
            {
                var baseValue = r.GetBase();
                if ((baseValue._type & InternalTypes.ObjectEnvironmentRecord) == 0)
                {
                    thisObject = r.GetThisValue();
                }
                else
                {
                    var env = (EnvironmentRecord)baseValue;
                    thisObject = env.ImplicitThisValue();
                }

                // is it a direct call to eval ? http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.1.1
                if (r.GetReferencedName() == CommonProperties.Eval && callable is EvalFunctionInstance instance)
                {
                    result = instance.Call(thisObject, arguments, true);
                    evalCalled = true;
                }
            }
            if (!evalCalled)
            {
                result = callable.Call(thisObject, arguments);
                if (_isDebugMode)
                    _engine.DebugHandler.PopDebugCallStack();
            }

            _engine.CallStack.Pop();

            if (!_cached && arguments.Length > 0)
                _engine._jsValueArrayPool.ReturnArray(arguments);

            _engine._referencePool.Return(r);
            return result;
        }

        private class CachedArgumentsHolder
        {
            internal JintExpression[] JintArguments;
            internal JsValue[] CachedArguments;
        }
    }
}