using System;
using Esprima.Ast;
using Jint.Native;

namespace Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintNewExpression : JintExpression
    {
        private JintExpression _calleeExpression;
        private JintExpression[] _jintArguments = new JintExpression[0];
        private bool _hasSpreads;

        public JintNewExpression(Engine engine, NewExpression expression) : base(engine, expression)
        {
            _initialized = false;
        }

        protected override void Initialize()
        {
            var expression = (NewExpression) _expression;
            _calleeExpression = Build(_engine, expression.Callee);

            if (expression.Arguments.Count <= 0)
            {
                return;
            }

            _jintArguments = new JintExpression[expression.Arguments.Count];
            for (var i = 0; i < _jintArguments.Length; i++)
            {
                var argument = expression.Arguments[i];
                _jintArguments[i] = Build(_engine, argument);
                _hasSpreads |= argument.Type == Nodes.SpreadElement;
            }
        }

        protected override object EvaluateInternal()
        {
            JsValue[] arguments;
            if (_jintArguments.Length == 0)
            {
                arguments = new JsValue[0];
            }
            else if (_hasSpreads)
            {
                arguments = BuildArgumentsWithSpreads(_jintArguments);
            }
            else
            {
                arguments = _engine._jsValueArrayPool.RentArray(_jintArguments.Length);
                BuildArguments(_jintArguments, arguments);
            }

            // todo: optimize by defining a common abstract class or interface
            var jsValue = _calleeExpression.GetValue();
            if (!(jsValue is IConstructor callee))
            {
                return ExceptionHelper.ThrowTypeError<object>(_engine, "The object can't be used as constructor.");
            }

            // construct the new instance using the Function's constructor method
            var instance = callee.Construct(arguments, jsValue);

            _engine._jsValueArrayPool.ReturnArray(arguments);

            return instance;
        }
    }
}