using Esprima.Ast;
using Jint.Native;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime.Interpreter.Expressions
{
    internal sealed class JintUpdateExpression : JintExpression
    {
        private JintExpression _argument;
        private int _change;
        private bool _prefix;

        private JintIdentifierExpression _leftIdentifier;
        private bool _evalOrArguments;

        public JintUpdateExpression(Engine engine, UpdateExpression expression) : base(engine, expression)
        {
            _initialized = false;
        }

        protected override void Initialize()
        {
            var expression = (UpdateExpression) _expression;
            _prefix = expression.Prefix;
            _argument = Build(_engine, expression.Argument);
            if (expression.Operator == UnaryOperator.Increment)
            {
                _change = 1;
            }
            else if (expression.Operator == UnaryOperator.Decrement)
            {
                _change = - 1;
            }
            else
            {
                ExceptionHelper.ThrowArgumentException();
            }

            _leftIdentifier = _argument as JintIdentifierExpression;
            _evalOrArguments = _leftIdentifier?.HasEvalOrArguments == true;
        }

        protected override object EvaluateInternal()
        {
            var fastResult = _leftIdentifier != null
                ? UpdateIdentifier()
                : null;

            return fastResult ?? UpdateNonIdentifier();
        }

        private object UpdateNonIdentifier()
        {
            if (!(_argument.Evaluate() is Reference reference))
            {
                return ExceptionHelper.ThrowTypeError<object>(_engine, "Invalid left-hand side expression");
            }

            reference.AssertValid(_engine);

            var value = _engine.GetValue(reference, false);
            var isInteger = value._type == InternalTypes.Integer;
            var newValue = isInteger
                ? JsNumber.Create(value.AsInteger() + _change)
                : JsNumber.Create(TypeConverter.ToNumber(value) + _change);

            _engine.PutValue(reference, newValue);
            _engine._referencePool.Return(reference);

            return _prefix
                ? newValue
                : (isInteger ? value : JsNumber.Create(TypeConverter.ToNumber(value)));
        }

        private JsValue UpdateIdentifier()
        {
            var strict = StrictModeScope.IsStrictModeCode;
            var name = _leftIdentifier._expressionName;
            var env = _engine.ExecutionContext.LexicalEnvironment;
            if (LexicalEnvironment.TryGetIdentifierEnvironmentWithBindingValue(
                env,
                name,
                strict,
                out var environmentRecord,
                out var value))
            {
                if (strict && _evalOrArguments)
                {
                    ExceptionHelper.ThrowSyntaxError(_engine);
                }

                var isInteger = value._type == InternalTypes.Integer;
                var newValue = isInteger
                    ? JsNumber.Create(value.AsInteger() + _change)
                    : JsNumber.Create(TypeConverter.ToNumber(value) + _change);

                environmentRecord.SetMutableBinding(name.Key.Name, newValue, strict);
                return _prefix
                    ? newValue
                    : (isInteger ? value : JsNumber.Create(TypeConverter.ToNumber(value)));
            }

            return null;
        }
    }
}