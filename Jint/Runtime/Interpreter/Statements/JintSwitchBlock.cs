using System.Collections.Generic;
using Esprima;
using Esprima.Ast;
using Jint.Native;
using Jint.Runtime.Environments;
using Jint.Runtime.Interpreter.Expressions;

namespace Jint.Runtime.Interpreter.Statements
{
    internal sealed class JintSwitchBlock
    {
        private readonly Engine _engine;
        private readonly NodeList<SwitchCase> _switchBlock;
        private JintSwitchCase[] _jintSwitchBlock;
        private bool _initialized;

        public JintSwitchBlock(Engine engine, NodeList<SwitchCase> switchBlock)
        {
            _engine = engine;
            _switchBlock = switchBlock;
        }

        private void Initialize()
        {
            _jintSwitchBlock = new JintSwitchCase[_switchBlock.Count];
            for (var i = 0; i < _jintSwitchBlock.Length; i++)
            {
                _jintSwitchBlock[i] = new JintSwitchCase(_engine, _switchBlock[i]);
            }
        }

        public Completion Execute(JsValue input)
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }

            JsValue v = Undefined.Instance;
            Location l = _engine._lastSyntaxNode.Location;
            JintSwitchCase defaultCase = null;
            bool hit = false;

            for (var i = 0; i < (uint) _jintSwitchBlock.Length; i++)
            {
                var clause = _jintSwitchBlock[i];

                LexicalEnvironment oldEnv = null;
                if (clause.LexicalDeclarations != null)
                {
                    oldEnv = _engine.ExecutionContext.LexicalEnvironment;
                    var blockEnv = LexicalEnvironment.NewDeclarativeEnvironment(_engine, oldEnv);
                    JintStatementList.BlockDeclarationInstantiation(blockEnv, clause.LexicalDeclarations);
                    _engine.UpdateLexicalEnvironment(blockEnv);
                }

                if (clause.Test == null)
                {
                    defaultCase = clause;
                }
                else
                {
                    var clauseSelector = clause.Test.GetValue();
                    if (JintBinaryExpression.StrictlyEqual(clauseSelector, input))
                    {
                        hit = true;
                    }
                }

                if (hit && clause.Consequent != null)
                {
                    var r = clause.Consequent.Execute();

                    if (oldEnv != null)
                    {
                        _engine.UpdateLexicalEnvironment(oldEnv);
                    }

                    if (r.Type != CompletionType.Normal)
                    {
                        return r;
                    }

                    l = r.Location;
                    v = r.Value ?? Undefined.Instance;
                }
            }

            // do we need to execute the default case ?
            if (hit == false && defaultCase != null)
            {
                LexicalEnvironment oldEnv = null;
                if (defaultCase.LexicalDeclarations != null)
                {
                    oldEnv = _engine.ExecutionContext.LexicalEnvironment;
                    var blockEnv = LexicalEnvironment.NewDeclarativeEnvironment(_engine, oldEnv);
                    JintStatementList.BlockDeclarationInstantiation(blockEnv, defaultCase.LexicalDeclarations);
                    _engine.UpdateLexicalEnvironment(blockEnv);
                }

                var r = defaultCase.Consequent.Execute();

                if (oldEnv != null)
                {
                    _engine.UpdateLexicalEnvironment(oldEnv);
                }
                if (r.Type != CompletionType.Normal)
                {
                    return r;
                }

                l = r.Location;
                v = r.Value ?? Undefined.Instance;
            }

            return new Completion(CompletionType.Normal, v, null, l);
        }

        private sealed class JintSwitchCase
        {
            internal readonly JintStatementList Consequent;
            internal readonly JintExpression Test;
            internal readonly List<VariableDeclaration> LexicalDeclarations;

            public JintSwitchCase(Engine engine, SwitchCase switchCase)
            {
                Consequent = new JintStatementList(engine, null, switchCase.Consequent);
                LexicalDeclarations = HoistingScope.GetLexicalDeclarations(switchCase);

                if (switchCase.Test != null)
                {
                    Test = JintExpression.Build(engine, switchCase.Test);
                }
            }
        }
    }
}
