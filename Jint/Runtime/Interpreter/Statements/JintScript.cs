using Esprima.Ast;

namespace Jint.Runtime.Interpreter.Statements
{
    internal sealed class JintScript : JintStatement<Script>
    {
        private readonly JintStatementList _list;

        public JintScript(Engine engine, Script script) : base(engine, script)
        {
            _list = new JintStatementList(_engine, null, _statement.Body);
        }

        protected override Completion ExecuteInternal()
        {
            return _list.Execute();
        }
    }
}