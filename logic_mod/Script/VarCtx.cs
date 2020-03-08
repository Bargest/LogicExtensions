using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Script
{
    public class VarCtx
    {
        public enum BreakMode
        {
            None = 0,
            Break = 1,
            Continue = 2,
            Return = 3,
            Throw = 4
        }

        public Interpreter Interp;
        public VarCtx ParentScope;
        public VarCtx ParentOnStack;
        public BuiltExpression PC;
        public Dictionary<string, object> Vars = new Dictionary<string, object>();
        public BreakMode Break = BreakMode.None;
        public FuncCtx Func = null;
        public Block CurBlock;
        public object ScriptException = null;

        public VarCtx(VarCtx parentOnStack, VarCtx parScope, Block block) : this(parentOnStack, parScope, block, parScope?.Interp)
        {
        }

        public VarCtx(VarCtx parentOnStack, VarCtx parScope, Block block, Interpreter interp)
        {
            CurBlock = block;
            ParentOnStack = parentOnStack;
            ParentScope = parScope;
            Func = parScope?.Func;
            Interp = interp;
        }

        public void AddVar(string name, object value)
        {
            Vars[name] = value;
        }
    }

    public class FuncCtx
    {
        public Function FunctionProto;
        public VarCtx ParentScope;
        public object ReturnValue = null;
        public bool Returned = false;
    }

    public class Function
    {
        public string[] ArgNames = new string[0];
        public string Name = null;
        public Block Body = null;
        public Func<VarCtx, object[], object> Native = null;

        public string GetName()
        {
            return Name ?? "<anonymous>";
        }
    }
}
