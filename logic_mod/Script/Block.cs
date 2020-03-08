using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;

namespace Logic.Script
{
    public class RuntimeException
    {
        public object Message;
        VarCtx ThrowContext;

        public RuntimeException(object msg, VarCtx ctx)
        {
            Message = msg;
            ThrowContext = ctx;
        }

        public struct StackLocation
        {
            public FuncCtx Func;
            public BuiltExpression PC;

            public StackLocation(FuncCtx f, BuiltExpression e)
            {
                Func = f;
                PC = e;
            }
        }

        public IEnumerable<StackLocation> StackTrace()
        {
            var ctx = ThrowContext;
            var pc = ctx?.PC;
            while (ctx != null)
            {
                if (ctx.CurBlock?.Type == Block.BlockType.Func)
                {
                    yield return new StackLocation(ctx.Func, pc);
                    ctx = ctx.ParentOnStack;
                    pc = ctx?.PC;
                }
                else
                {
                    ctx = ctx.ParentOnStack;
                }
            }
        }

        public override string ToString()
        {
            return "Exception " + Message?.ToString() + " at\n  " + string.Join("\n  ", StackTrace().Select(x => x.Func.FunctionProto.GetName() + (x.PC == null ? "" : ": " + x.PC.Source?.Pos?.ToString())).ToArray());
        }
    }

    public class BuiltExpression
    {
        public Expression Expr;
        public ExprNode Source;

        public BuiltExpression(Expression e, ExprNode src)
        {
            Expr = e;
            Source = src;
        }
    }

    public class Block
    {
        public class UndefinedType { }
        public static readonly UndefinedType Undefined = new UndefinedType();

        public enum BlockType
        {
            Plain = 0, Func = 1, Loop = 2, Try = 3
        }

        public Block ParentBlock = null;
        public List<BuiltExpression> Exprs = new List<BuiltExpression>();
        public Dictionary<BuiltExpression, Delegate> CompiledCode = new Dictionary<BuiltExpression, Delegate>();
        public BlockType Type = BlockType.Plain;
        public ParameterExpression RuntimeVarCtx = Expression.Parameter(typeof(VarCtx), "ctx");

        public static object SetVal(VarCtx varCtx, string key, object value)
        {
            var callCtx = varCtx;
            while (varCtx != null)
            {
                if (varCtx.Vars.ContainsKey(key))
                    return varCtx.Vars[key] = value;
                varCtx = varCtx.ParentScope;
            }

            return Throw(callCtx, $"{key}: undeclared variable");
        }

        public static object GetVal(VarCtx varCtx, string key)
        {
            var callCtx = varCtx;
            while (varCtx != null)
            {
                if (varCtx.Vars.ContainsKey(key))
                    return varCtx.Vars[key];
                varCtx = varCtx.ParentScope;
            }

            return Throw(callCtx, $"{key}: undeclared variable");
        }

        static string GetDictKey(object key)
        {
            string skey;
            if (key is long ik)
                skey = ik.ToString();
            else if (key is string sk)
                skey = sk;
            else
                return null;
            return skey;
        }
        static int GetArrayKey(object key)
        {
            if (key is long ik)
                return (int)ik;
            else if (key is string sk)
                return int.TryParse(sk, out int value) ? value : -1;
            return -1;
        }

        public static object InitDict(string[] keys, object[] values)
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < keys.Length; ++i)
                d[keys[i]] = values[i];
            return d;
        }

        public static object SetDictVal(VarCtx ctx, object dict, object key, object value)
        {
            if (dict is Dictionary<string, object> d)
            {
                var dkey = GetDictKey(key);
                if (dkey != null)
                    return d[dkey] = value;
            }
            else if (dict is object[] arr)
            {
                var akey = GetArrayKey(key);
                if (akey >= 0)
                {
                    if (akey < arr.Length)
                        return arr[akey] = value;
                    return Undefined;
                }
            }
            return Throw(ctx, $"Can't write to {dict}[{key}]");
        }

        public static object GetDictVal(VarCtx ctx, object dict, object key)
        {
            if (dict is Dictionary<string, object> d)
            {
                var dkey = GetDictKey(key);
                if (!d.ContainsKey(dkey))
                    return Undefined;
                return d[dkey];
            }
            else if (dict is object[] arr)
            {
                if (key is string sk && sk == "length")
                    return (long)arr.Length;

                var akey = GetArrayKey(key);
                if (akey >= 0)
                {
                    if (akey < arr.Length)
                        return arr[akey];
                    return Undefined;
                }
            }
            return Throw(ctx, $"Can't read {dict}[{key}]");
        }

        public static object DeclareVar(VarCtx varCtx, string key, object value)
        {
            return varCtx.Vars[key] = value;
        }

        public static object Break(VarCtx varCtx, VarCtx.BreakMode mode)
        {
            while (varCtx != null)
            {
                varCtx.Break = mode;
                if (varCtx.CurBlock?.Type == BlockType.Loop)
                    break;
                else if (varCtx.CurBlock == null || varCtx.CurBlock.Type != BlockType.Plain)
                    throw new Exception("Break too far"); // should never happen, checks on compilation
                varCtx = varCtx.ParentOnStack;
            }
            return null;
        }

        public static object Throw(VarCtx varCtx, object v)
        {
            var exc = new RuntimeException(v, varCtx);
            while (varCtx != null)
            {
                varCtx.Break = VarCtx.BreakMode.Throw;
                varCtx.ScriptException = exc;
                if (varCtx.CurBlock != null && varCtx.CurBlock.Type == BlockType.Try)
                    break;
                varCtx = varCtx.ParentOnStack;
            }
            return Undefined;
        }

        public static object Return(VarCtx varCtx, object value)
        {
            varCtx.Func.ReturnValue = value;
            varCtx.Func.Returned = true;
            varCtx.Break = VarCtx.BreakMode.Return;
            return value;
        }

        public static object DeclareFunc(VarCtx curCtx, Function func)
        {
            var funcCtx = new FuncCtx { ParentScope = curCtx, FunctionProto = func };
            return funcCtx;
        }

        public static object NewArray(VarCtx curCtx, object length)
        {
            var len = GetArrayKey(length);
            if (len >= 0)
                return new object[len];
            return Throw(curCtx, "Invalid array length " + length);
        }

        private static void CheckBreak(Block cb)
        {
            // check if we are in loop
            while (cb != null)
            {
                if (cb.Type == BlockType.Loop)
                    break;
                else if (cb.Type != BlockType.Plain)
                    throw new Exception("Break too far");
                cb = cb.ParentBlock;
            }
            if (cb == null)
                throw new Exception("Break too far");
        }

        public static bool CheckCond(VarCtx curCtx, object v)
        {
            //curCtx.Interp.SyncDebug($"check {v}({v?.GetType()})");
            if (v is bool b)
                return b;
            if (v is long i)
                return i != 0;
            if (v is float f)
                return f != 0;
            return v != null;
        }

        public class SetValArifm
        {
            public string key;
            public ArifmOperation op;
            public bool retOriginal;
            public object Apply(VarCtx varCtx, object value)
            {
                var a = GetVal(varCtx, key);
                if (varCtx.Break != VarCtx.BreakMode.None)
                    return Undefined;
                var r = op.Arifm(varCtx, a, value);
                SetVal(varCtx, key, r);

                //varCtx.Interp.PauseThread((d) => Debug.Log($"Setvalarifm {key} {a} -> {r} {retOriginal}"), null);
                if (retOriginal)
                    return a;
                return r;
            }
        }
        public class SetDictArifm
        {
            public ArifmOperation op;
            public bool retOriginal;
            public object Apply(VarCtx varCtx, object dict, object key, object value)
            {
                var a = GetDictVal(varCtx, dict, key);
                if (varCtx.Break != VarCtx.BreakMode.None)
                    return Undefined;
                var r = op.Arifm(varCtx, a, value);
                SetDictVal(varCtx, dict, key, r);
                if (retOriginal)
                    return a;
                return r;
            }
        }
        // -----------------------------------------------------
        public static Expression Const(object c)
        {
            return Expression.Constant(c);
        }

        public Expression EDeclareVar(string name, Expression initval)
        {
            return Expression.Invoke(Const((Func<VarCtx, string, object, object>)DeclareVar), RuntimeVarCtx, Const(name), Expression.Convert(initval, typeof(object)));
        }

        public Expression EAssignVar(string name, Expression val)
        {
            return Expression.Invoke(Const((Func<VarCtx, string, object, object>)SetVal), RuntimeVarCtx, Const(name), Expression.Convert(val, typeof(object)));
        }

        public Expression EAssignVarArifm(string name, Expression val, ArifmOperation op, bool retOriginal)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object>)new SetValArifm { key = name, op = op, retOriginal = retOriginal }.Apply), RuntimeVarCtx, val == null ? Const(null) : Expression.Convert(val, typeof(object)));
        }
        public Expression EAssignDictArifm(Expression dict, Expression name, Expression val, ArifmOperation op, bool retOriginal)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object, object, object>)new SetDictArifm { op = op, retOriginal = retOriginal }.Apply), RuntimeVarCtx,
                Expression.Convert(dict, typeof(object)), Expression.Convert(name, typeof(object)), val == null ? Const(null) : Expression.Convert(val, typeof(object)));
        }

        public Expression EReadVar(string name)
        {
            return Expression.Invoke(Const((Func<VarCtx, string, object>)GetVal), RuntimeVarCtx, Const(name));
        }

        public Expression EAssignDict(Expression dict, Expression name, Expression val)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object, object, object>)SetDictVal), RuntimeVarCtx, Expression.Convert(dict, typeof(object)),
                Expression.Convert(name, typeof(object)), Expression.Convert(val, typeof(object)));
        }

        public Expression EReadDict(Expression dict, Expression name)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object, object>)GetDictVal), RuntimeVarCtx, Expression.Convert(dict, typeof(object)), Expression.Convert(name, typeof(object)));
        }

        public Expression EInitDict(Dictionary<string, Expression> init)
        {
            return Expression.Invoke(Const((Func<string[], object[], object>)InitDict), Const(init.Keys.ToArray()), EArrayInit(init.Values));
        }

        public Expression ENewArray(Expression size)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object>)NewArray), RuntimeVarCtx, Expression.Convert(size, typeof(object)));
        }

        public static Expression EArrayInit(IEnumerable<Expression> vals)
        {
            return Expression.NewArrayInit(typeof(object), vals.Select(x => Expression.Convert(x, typeof(object))).ToArray());
        }

        public Expression Break()
        {
            CheckBreak(this);
            return Expression.Invoke(Const((Func<VarCtx, VarCtx.BreakMode, object>)Break), RuntimeVarCtx, Const(VarCtx.BreakMode.Break));
        }

        public Expression Continue()
        {
            CheckBreak(this);
            return Expression.Invoke(Const((Func<VarCtx, VarCtx.BreakMode, object>)Break), RuntimeVarCtx, Const(VarCtx.BreakMode.Continue));
        }
        public Expression Throw(Expression v)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object>)Throw), RuntimeVarCtx, Expression.Convert(v, typeof(object)));
        }

        public Expression Return(Expression ret)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object>)Return), RuntimeVarCtx, ret == null ? Block.Const(null) : Expression.Convert(ret, typeof(object)));
        }

        public Expression CheckCond(Expression cond)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, bool>)CheckCond), RuntimeVarCtx, Expression.Convert(cond, typeof(object)));
        }

        public Expression Arifm(Expression a, Expression b, ArifmOperation op)
        {
            if (a.Type.IsPrimitive)
                a = Expression.Convert(a, typeof(object));
            if (b.Type.IsPrimitive)
                b = Expression.Convert(b, typeof(object));
            return Expression.Invoke(Const((Func<VarCtx, object, object, object>)op.Arifm), RuntimeVarCtx, a, b);
            //return Expression.Invoke(Const((Func<VarCtx, ArifmOperation, object, object, object>)ArifmProxy), RuntimeVarCtx, Const(op), a, b);
        }

        public Expression UnaryArifm(Expression a, ArifmOperation op)
        {
            if (a.Type.IsPrimitive)
                a = Expression.Convert(a, typeof(object));
            return Expression.Invoke(Const((Func<VarCtx, object, object, object>)op.Arifm), RuntimeVarCtx, a, Const(null));
        }

        public static Expression IfElse(Expression cond, Expression then, Expression elze)
        {
            if (then == null)
                then = Const(null);
            if (elze == null)
                elze = Const(null);
            return Expression.Condition(cond, then, elze);
        }

        public Expression CallFunc(Expression func, IEnumerable<Expression> args)
        {
            return Expression.Invoke(Const((Func<VarCtx, object, object[], object>)CallFunc), RuntimeVarCtx, func, EArrayInit(args));
        }

        protected object RunExpr(VarCtx ctx, BuiltExpression e)
        {
            if (!CompiledCode.ContainsKey(e))
                CompiledCode[e] = Expression.Lambda(e.Expr, RuntimeVarCtx).Compile();
            ctx.Interp.IrqPoll();
            ctx.Interp.Gas -= 1;
            ctx.PC = e;
            try
            {
                return CompiledCode[e].DynamicInvoke(ctx);
            }
            catch (Exception ee)
            {
                //ctx.Interp.SyncDebug(ee);
                Throw(ctx, new RuntimeException(ee.Message, ctx));
                return Undefined;
            }
        }

        public virtual object RunBlock(VarCtx ctx)
        {
            ctx.Break = VarCtx.BreakMode.None;
            object returnValue = null;
            foreach (var expr in Exprs)
            {
                returnValue = RunExpr(ctx, expr);
                if (ctx.Break != VarCtx.BreakMode.None)
                    break;
            }
            if (ctx.CurBlock?.Type == BlockType.Func)
                return ctx.Func.ReturnValue;
            return returnValue;
        }

        public object InvokeBlock(VarCtx parentCtx)
        {
            var childCtx = new VarCtx(parentCtx, parentCtx, this);
            return RunBlock(childCtx);
        }

        public Block ChildBlock()
        {
            var block = new Block();
            block.Type = BlockType.Plain;
            block.ParentBlock = this;
            return block;
        }

        public LoopBlock ChildLoopBlock()
        {
            var block = new LoopBlock();
            block.Type = BlockType.Loop;
            block.ParentBlock = this;
            return block;
        }

        public TryCatchBlock ChildTryBlock()
        {
            var block = new TryCatchBlock();
            block.Type = BlockType.Try;
            block.ParentBlock = this;
            return block;
        }

        public Expression RunChildBlock(Block b)
        {
            return Expression.Invoke(Const((Func<VarCtx, object>)b.InvokeBlock), RuntimeVarCtx);
        }

        public void EAdd(BuiltExpression e)
        {
            Exprs.Add(e);
        }

        public Expression CreateFunc(Block body, string name, string[] argNames)
        {
            var func = new Function
            {
                Body = body,
                ArgNames = argNames,
                Name = name
            };
            body.Type = BlockType.Func;
            return Expression.Invoke(Const((Func<VarCtx, Function, object>)DeclareFunc), RuntimeVarCtx, Const(func));
        }

        public Expression CreateFunc(string name, Func<VarCtx, object[], object> body)
        {
            var func = new Function
            {
                Native = body,
                Name = name
            };
            return Expression.Invoke(Const((Func<VarCtx, Function, object>)DeclareFunc), RuntimeVarCtx, Const(func));
        }

        public static object CallFunc(VarCtx ctx, object f, object[] args)
        {
            if (ctx.Break == VarCtx.BreakMode.Throw)
                return null;
            if (!(f is FuncCtx func))
                return Throw(ctx, $"{f} is not a function");

            var funcVarCtx = new VarCtx(ctx, func.ParentScope, func.FunctionProto.Body);
            if (func.FunctionProto.Body != null)
            {
                funcVarCtx.Func = func;
                funcVarCtx.Vars["arguments"] = args ?? new object[0];
                for (int i = 0; i < func.FunctionProto.ArgNames.Length; ++i)
                {
                    if (args != null && i < args.Length)
                        funcVarCtx.Vars[func.FunctionProto.ArgNames[i]] = args[i];
                    else
                        funcVarCtx.Vars[func.FunctionProto.ArgNames[i]] = null;
                }
                return func.FunctionProto.Body.RunBlock(funcVarCtx);
            }
            else
            {
                // Native
                try
                {
                    funcVarCtx.CurBlock = new Block() { Type = BlockType.Func };
                    return func.FunctionProto.Native(funcVarCtx, args);
                }
                catch (Exception e)
                {
                    return Throw(funcVarCtx, e.ToString());
                }
            }
        }
    }

    public class LoopBlock : Block
    {
        public BuiltExpression Init, Cond, Inc;

        public LoopBlock()
        {
            Type = BlockType.Loop;
        }

        public override object RunBlock(VarCtx ctx)
        {
            ctx.Break = VarCtx.BreakMode.None;
            object returnValue = null;
            if (Init != null)
                RunExpr(ctx, Init);

            if (ctx.Break == VarCtx.BreakMode.Throw)
                return null;

            while ((Cond != null) ? CheckCond(ctx, RunExpr(ctx, Cond)) : true)
            {
                if (ctx.Break == VarCtx.BreakMode.Throw)
                    return null;
                var childCtx = new VarCtx(ctx, ctx, this);
                returnValue = base.RunBlock(childCtx);
                //ctx.Interp.SyncCtx.Send((s) => Debug.Log(childCtx.Break), null);
                if (childCtx.Break == VarCtx.BreakMode.Break || childCtx.Break == VarCtx.BreakMode.Return)
                    break;
                if (ctx.Break == VarCtx.BreakMode.Throw)
                    return null;
                // "continue" does not need special handling
                if (Inc != null)
                {
                    RunExpr(ctx, Inc);
                    if (ctx.Break == VarCtx.BreakMode.Throw)
                        return null;
                }
            }
            return returnValue;
        }
    }
    public class TryCatchBlock : Block
    {
        public BuiltExpression Try;
        public BuiltExpression Catch;
        public string ExceptionName;

        public TryCatchBlock()
        {
            Type = BlockType.Try;
        }

        public override object RunBlock(VarCtx ctx)
        {
            ctx.Break = VarCtx.BreakMode.None;
            ctx.ScriptException = null;
            var result = RunExpr(ctx, Try);
            //ctx.Interp.SyncDebug(ctx.Break);
            if (ctx.Break != VarCtx.BreakMode.Throw)
                return result;

            DeclareVar(ctx, ExceptionName, ctx.ScriptException);
            ctx.Break = VarCtx.BreakMode.None;
            return RunExpr(ctx, Catch);
        }
    }
}
