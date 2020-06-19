﻿using System.Runtime.CompilerServices;
using Esprima;
using Esprima.Ast;
using Jint.Runtime.Interpreter.Expressions;

namespace Jint.Runtime.Interpreter.Statements
{
    public abstract class JintStatement<T> : JintStatement where T : Statement
    {
        internal readonly T _statement;

        protected JintStatement(Engine engine, T statement) : base(engine, statement)
        {
            _statement = statement;
        }
    }

    public abstract class JintStatement
    {
        protected readonly Engine _engine;
        private readonly Statement _statement;

        // require sub-classes to set to false explicitly to skip virtual call
        protected bool _initialized = true;

        protected JintStatement(Engine engine, Statement statement)
        {
            _engine = engine;
            _statement = statement;
        }

        public Completion Execute()
        {
            _engine.NextStatement();
            if (_statement.Type != Nodes.BlockStatement)
            {
                _engine._lastSyntaxNode = _statement;
                _engine.RunBeforeExecuteStatementChecks(_statement);
            }

            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }

            return ExecuteInternal();
        }

        protected abstract Completion ExecuteInternal();

        public Location Location => _statement.Location;

        /// <summary>
        /// Opportunity to build one-time structures and caching based on lexical context.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        protected internal static JintStatement Build(Engine engine, Statement statement)
        {
            switch(statement.Type)
            {
                case Nodes.BlockStatement: return new JintBlockStatement(engine, (BlockStatement)statement);
                case Nodes.ReturnStatement: return new JintReturnStatement(engine, (ReturnStatement)statement);
                case Nodes.VariableDeclaration: return new JintVariableDeclaration(engine, (VariableDeclaration)statement);
                case Nodes.BreakStatement: return new JintBreakStatement(engine, (BreakStatement)statement);
                case Nodes.ContinueStatement: return new JintContinueStatement(engine, (ContinueStatement)statement);
                case Nodes.DoWhileStatement: return new JintDoWhileStatement(engine, (DoWhileStatement)statement);
                case Nodes.EmptyStatement: return new JintEmptyStatement(engine, (EmptyStatement)statement);
                case Nodes.ExpressionStatement: return new JintExpressionStatement(engine, (ExpressionStatement)statement);
                case Nodes.ForStatement: return new JintForStatement(engine, (ForStatement)statement);
                case Nodes.ForInStatement: return new JintForInForOfStatement(engine, (ForInStatement)statement);
                case Nodes.ForOfStatement: return new JintForInForOfStatement(engine, (ForOfStatement)statement);
                case Nodes.IfStatement: return new JintIfStatement(engine, (IfStatement)statement);
                case Nodes.LabeledStatement: return new JintLabeledStatement(engine, (LabeledStatement)statement);
                case Nodes.SwitchStatement: return new JintSwitchStatement(engine, (SwitchStatement)statement);
                case Nodes.FunctionDeclaration: return new JintFunctionDeclarationStatement(engine, (FunctionDeclaration)statement);
                case Nodes.ThrowStatement: return new JintThrowStatement(engine, (ThrowStatement)statement);
                case Nodes.TryStatement: return new JintTryStatement(engine, (TryStatement)statement);
                case Nodes.WhileStatement: return new JintWhileStatement(engine, (WhileStatement)statement);
                case Nodes.WithStatement: return new JintWithStatement(engine, (WithStatement)statement);
                case Nodes.DebuggerStatement: return new JintDebuggerStatement(engine, (DebuggerStatement)statement);
                case Nodes.Program: return new JintScript(engine, statement as Script ?? ExceptionHelper.ThrowArgumentException<Script>("modules not supported"));
                default: return ExceptionHelper.ThrowArgumentOutOfRangeException<JintStatement>(nameof(statement.Type), $"unsupported statement type '{statement.Type}'");
            };
        }

        internal static Completion? FastResolve(StatementListItem statement)
        {
            if (statement is ReturnStatement rs && rs.Argument is Literal l)
            {
                var jsValue = JintLiteralExpression.ConvertToJsValue(l);
                if (jsValue != null)
                {
                    return new Completion(CompletionType.Return, jsValue, null, rs.Location);
                }
            }

            return null;
        }
    }
}