using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Logic.Script
{
    public class Interpreter : IDisposable
    {
        public BuiltExpression[] CompileList(Block cb, ExprNode e)
        {
            BuiltExpression[] args;
            if (e is EmptyNode)
                args = new BuiltExpression[0];
            else if (e is CommaNode cn)
                args = cn.Values.Select(x => Compile(cb, x)).ToArray();
            else
                args = new[] { Compile(cb, e) };
            return args;
        }

        public BuiltExpression Compile(Block cb, ExprNode e)
        {
            if (e == null)
                return null;

            switch (e.OpType)
            {
                case ExprType.Empty:
                    return new BuiltExpression(Block.Const(null), e);
                case ExprType.Ident:
                    return new BuiltExpression(cb.EReadVar((e as IdentNode).Val), e);
                case ExprType.String:
                    return new BuiltExpression(Block.Const((e as StringNode).Str), e);
                case ExprType.Int:
                    return new BuiltExpression(Block.Const((e as IntNode).Val), e);
                case ExprType.Float:
                    return new BuiltExpression(Block.Const((e as FloatNode).Val), e);
                case ExprType.Null:
                    return new BuiltExpression(Block.Const(null), e);
                case ExprType.Undef:
                    return new BuiltExpression(Block.Const(Block.Undefined), e);
                case ExprType.Dict:
                    var eDict = e as DictNode;
                    return new BuiltExpression(cb.EInitDict(eDict.Values.ToDictionary(x => x.Key, x => Compile(cb, x.Value)?.Expr)), e);
                case ExprType.NewArray:
                    var arrNode = e as ArrayNode;
                    if (arrNode.Length != null)
                        return new BuiltExpression(cb.ENewArray(Compile(cb, arrNode.Length)?.Expr), e);
                    return new BuiltExpression(Block.EArrayInit(CompileList(cb, arrNode.Content).Select(x => x.Expr)), e);
                case ExprType.DictGet:
                    var eDictGet = e as DictGetNode;
                    return new BuiltExpression(cb.EReadDict(Compile(cb, eDictGet.Left)?.Expr, Compile(cb, eDictGet.Right)?.Expr), e);
                case ExprType.Call:
                    var eCall = e as CallNode;
                    return new BuiltExpression(cb.CallFunc(Compile(cb, eCall.Left)?.Expr, CompileList(cb, eCall.Right).Select(x => x.Expr)), e);
                case ExprType.Return:
                    // TODO: check return handling
                    return new BuiltExpression(cb.Return(Compile(cb, (e as ReturnNode).RetValue)?.Expr), e);
                case ExprType.Throw:
                    return new BuiltExpression(cb.Throw(Compile(cb, (e as ThrowNode).ThrowValue)?.Expr), e);
                case ExprType.Break:
                    return new BuiltExpression(cb.Break(), e);
                case ExprType.Continue:
                    return new BuiltExpression(cb.Continue(), e);
                case ExprType.TryCatch:
                    var eTry = e as TryCatchNode;
                    var tb = cb.ChildTryBlock();
                    tb.Try = Compile(tb, eTry.Try);
                    tb.Catch = Compile(tb, eTry.Catch);
                    tb.ExceptionName = eTry.VarName;
                    return new BuiltExpression(cb.RunChildBlock(tb), e);
                case ExprType.IfElse:
                    var eIf = e as IfElseNode;
                    var cond = cb.CheckCond(Compile(cb, eIf.Cond)?.Expr);
                    return new BuiltExpression(Block.IfElse(cond, Compile(cb, eIf.If)?.Expr, Compile(cb, eIf.Else)?.Expr), e);
                case ExprType.Loop:
                    var lb = cb.ChildLoopBlock();
                    var eLoop = e as LoopNode;
                    lb.Init = Compile(lb, eLoop.Init);
                    lb.Cond = Compile(lb, eLoop.Cond);
                    lb.Inc = Compile(lb, eLoop.Inc);
                    // TODO: this will cause useless context in case of SequenceNode as body
                    lb.EAdd(Compile(lb, eLoop.Body));
                    return new BuiltExpression(cb.RunChildBlock(lb), e);
                case ExprType.Seq:
                    var sb = cb.ChildBlock();
                    foreach (var x in (e as Sequence).Values)
                        sb.EAdd(Compile(sb, x));
                    return new BuiltExpression(cb.RunChildBlock(sb), e);
                case ExprType.Func:
                    var b = cb.ChildBlock();
                    var eFunc = e as FuncNode;
                    string[] argList;
                    if (eFunc.Args is EmptyNode)
                        argList = new string[0];
                    else if (eFunc.Args is DeclareVarNode dn)
                        argList = new[] { dn.Ident.Val };
                    else if (eFunc.Args is CommaNode cn)
                    {
                        if (cn.Values.Any(x => !(x is DeclareVarNode)))
                            throw new Exception("Can't compile func declaraion: invalid expression in args");
                        argList = cn.Values.Select(x => (x as DeclareVarNode).Ident.Val).ToArray();
                    }
                    else
                        throw new Exception("Can't compile func declaraion: invalid arg expression " + eFunc.Args.OpType);
                    // TODO: this will cause useless context in case of SequenceNode as body
                    b.EAdd(Compile(b, eFunc.Body));
                    return new BuiltExpression(cb.CreateFunc(b, eFunc.Name, argList), e);
                case ExprType.DeclareVar:
                    var eDecl = (e as DeclareVarNode);
                    return new BuiltExpression(cb.EDeclareVar(eDecl.Ident.Val, Compile(cb, eDecl.Value)?.Expr), e);
                case ExprType.Unary:
                    var eUArifm = e as UnaryExprNode;
                    return new BuiltExpression(cb.UnaryArifm(Compile(cb, eUArifm.Left)?.Expr, GetUnaryOperation(eUArifm.ArifmOp)), e);
                case ExprType.Arifm:
                    var eArifm = e as ArifmExprNode;
                    return new BuiltExpression(cb.Arifm(Compile(cb, eArifm.Left)?.Expr, Compile(cb, eArifm.Right)?.Expr, GetOperation(eArifm.ArifmOp)), e);
                case ExprType.AssignVar:
                    var eAssign = e as AsgVarNode;
                    return new BuiltExpression(cb.EAssignVar(eAssign.Target.Val, Compile(cb, eAssign.Right)?.Expr), e);
                case ExprType.AssignDict:
                    var eAsgDict = e as AsgDictNode;
                    return new BuiltExpression(cb.EAssignDict(Compile(cb, eAsgDict.Left)?.Expr, Compile(cb, eAsgDict.Key)?.Expr, Compile(cb, eAsgDict.Right)?.Expr), e);
                case ExprType.AsgArifm:
                    var eaArifm = e as AsgArifmVarNode;
                    var arifmOp = GetOperation(eaArifm.ArifmOp);
                    if (arifmOp == null)
                    {
                        arifmOp = GetUnaryOperation(eaArifm.ArifmOp);
                        switch (eaArifm.ArifmOp)
                        {
                            case Lexer.TokenType.TOK_PLUSPLUS:
                            case Lexer.TokenType.TOK_MINUSMINUS:
                                return new BuiltExpression(cb.EAssignVarArifm(eaArifm.Target.Val, Compile(cb, eaArifm.Right)?.Expr, arifmOp, true), e);
                            case Lexer.TokenType.FTOK_PLUSPLUS_PREF:
                            case Lexer.TokenType.FTOK_MINUSMINUS_PREF:
                                return new BuiltExpression(cb.EAssignVarArifm(eaArifm.Target.Val, Compile(cb, eaArifm.Right)?.Expr, arifmOp, false), e);
                        }
                        throw new Exception("Can't compile assign arifm " + eaArifm.ArifmOp);
                    }
                    return new BuiltExpression(cb.EAssignVarArifm(eaArifm.Target.Val, Compile(cb, eaArifm.Right)?.Expr, arifmOp, false), e);
                case ExprType.AsgArifmDict:
                    var edArifm = e as AsgArifmDictNode;
                    var adrifmOp = GetOperation(edArifm.ArifmOp);
                    if (adrifmOp == null)
                    {
                        adrifmOp = GetUnaryOperation(edArifm.ArifmOp);
                        switch (edArifm.ArifmOp)
                        {
                            case Lexer.TokenType.TOK_PLUSPLUS:
                            case Lexer.TokenType.TOK_MINUSMINUS:
                                return new BuiltExpression(cb.EAssignDictArifm(Compile(cb, edArifm.Left)?.Expr, Compile(cb, edArifm.Key)?.Expr, Compile(cb, edArifm.Right)?.Expr, adrifmOp, true), e);
                            case Lexer.TokenType.FTOK_PLUSPLUS_PREF:
                            case Lexer.TokenType.FTOK_MINUSMINUS_PREF:
                                return new BuiltExpression(cb.EAssignDictArifm(Compile(cb, edArifm.Left)?.Expr, Compile(cb, edArifm.Key)?.Expr, Compile(cb, edArifm.Right)?.Expr, adrifmOp, false), e);
                        }
                        throw new Exception("Can't compile assign dict arifm " + edArifm.ArifmOp);
                    }
                    return new BuiltExpression(cb.EAssignDictArifm(Compile(cb, edArifm.Left)?.Expr, Compile(cb, edArifm.Key)?.Expr, Compile(cb, edArifm.Right)?.Expr, adrifmOp, false), e);
                    //case ExprType.Comma:
                    //    break;
            }
            throw new Exception("Can't compile " + e);
        }
        ArifmOperation GetUnaryOperation(Lexer.TokenType op)
        {
            switch (op)
            {
                case Lexer.TokenType.TOK_PLUS: return new Positive();
                case Lexer.TokenType.TOK_MINUS: return new Negative();
                case Lexer.TokenType.TOK_NOTAR: return new Not();
                case Lexer.TokenType.TOK_NOT: return new LogNot();
                case Lexer.TokenType.TOK_PLUSPLUS: return new Inc();
                case Lexer.TokenType.TOK_MINUSMINUS: return new Dec();
                case Lexer.TokenType.FTOK_PLUSPLUS_PREF: return new Inc();
                case Lexer.TokenType.FTOK_MINUSMINUS_PREF: return new Dec();
            }
            return null;
        }

        ArifmOperation GetOperation(Lexer.TokenType op)
        {
            switch (op)
            {
                case Lexer.TokenType.TOK_PLUS: return new Addition();
                case Lexer.TokenType.TOK_MINUS: return new Subtraction();
                case Lexer.TokenType.TOK_MUL: return new Multiplication();
                case Lexer.TokenType.TOK_DIV: return new Division();
                case Lexer.TokenType.TOK_MOD: return new Modulo();
                case Lexer.TokenType.TOK_SHL: return new Shl();
                case Lexer.TokenType.TOK_SHR: return new Shr();
                case Lexer.TokenType.TOK_AND: return new And();
                case Lexer.TokenType.TOK_OR: return new Or();
                case Lexer.TokenType.TOK_XOR: return new Xor();
                case Lexer.TokenType.TOK_LOGAND: return new LogAnd();
                case Lexer.TokenType.TOK_LOGOR: return new LogOr();

                case Lexer.TokenType.TOK_LESS: return new Below();
                case Lexer.TokenType.TOK_LESSEQ: return new BelowEq();
                case Lexer.TokenType.TOK_GREATER: return new Above();
                case Lexer.TokenType.TOK_GREATEQ: return new AboveEq();
                case Lexer.TokenType.TOK_EQUAL: return new Equal();
                case Lexer.TokenType.TOK_NOTEQ: return new NEqual();
            }
            return null;
        }
        class UnitySyncContext : SynchronizationContext
        {
            Interpreter Interp;
            public UnitySyncContext(Interpreter intr)
            {
                Interp = intr;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                throw new NotImplementedException();
            }
            public override void Send(SendOrPostCallback d, object state)
            {
                Interp.PauseThread(d, state);
            }
        }

        public FuncCtx ScriptEntry;
        public object Result;
        public EventWaitHandle ResumeTask = new EventWaitHandle(false, EventResetMode.AutoReset);
        public EventWaitHandle TaskPaused = new EventWaitHandle(false, EventResetMode.AutoReset);
        public Thread InterpThread;

        Queue<long> Interrupts = new Queue<long>();
        Dictionary<long, object[]> InterruptArgs = new Dictionary<long, object[]>();
        Dictionary<long, FuncCtx> InterruptHandlers = new Dictionary<long, FuncCtx>();

        private UnitySyncContext SyncCtx;
        private bool Terminate = false;
        private SendOrPostCallback cb = null;
        private object state = null;
        private Exception cbE = null;
        public int Gas = 0;
        Action<long, object> OnException = null;
        Action<long> AfterInterrupt = null;
        bool InterruptsEnabled = true;

        public Interpreter()
        {
            SyncCtx = new UnitySyncContext(this);
            InterpThread = new Thread(ThreadEntry);
            InterpThread.Start();
        }

        class TerminationException : Exception
        {

        }

        public void SyncDebug(object o)
        {
            PauseThread(d => Debug.Log(o), null);
        }

        public void PauseThread(SendOrPostCallback d, object s)
        {
            cb = d;
            state = s;
            cbE = null;
            TaskPaused.Set();
            ResumeTask.WaitOne();
            if (cbE != null)
                throw cbE;
            if (Terminate)
                throw new TerminationException();
        }

        public void AddInterrupt(long i, object[] args)
        {
            if (!Interrupts.Contains(i))
            {
                if (args == null)
                    InterruptArgs[i] = new object[] { i };
                else
                    InterruptArgs[i] = (new object[] { i }).Concat(args).ToArray();
                Interrupts.Enqueue(i);
            }
        }

        public void RegisterIrqHandler(long id, FuncCtx handler)
        {
            InterruptHandlers[id] = handler;
        }

        public void RemoveIrqHandler(long id)
        {
            InterruptHandlers.Remove(id);
        }

        public void Cli()
        {
            InterruptsEnabled = false;
        }

        public void Sti()
        {
            InterruptsEnabled = true;
        }

        public void AddExtVariable(FuncCtx root, string name, object variable)
        {
            var rc = root.ParentScope;
            rc.AddVar(name, variable);
        }

        FuncCtx CreateFunc(VarCtx scope, string name, Function func)
        {
            func.Name = name;
            var result = new FuncCtx
            {
                FunctionProto = func,
                ParentScope = scope
            };
            return result;
        }

        FuncCtx CreateFunc(VarCtx scope, string name, Func<VarCtx, object[], object> func)
        {
            return CreateFunc(scope, name, new Function
            {
                ArgNames = new string[] { },
                Native = func
            });
        }

        FuncCtx CreateSynchronizedFunc(VarCtx scope, string name, Func<VarCtx, object[], object> func)
        {
            return CreateFunc(scope, name, (c, d) =>
            {
                object result = null;
                SyncCtx.Send((state) => result = func(c, d), null);
                return result;
            }); ;
        }

        public FuncCtx CreateFunc(FuncCtx root, string name, Func<VarCtx, object[], object> func, bool synchronized)
        {
            return synchronized ? CreateSynchronizedFunc(root.ParentScope, name, func) : CreateFunc(root.ParentScope, name, func);
        }

        public void AddExtFunc(FuncCtx root, string name, Func<VarCtx, object[], object> func, bool synchronized)
        {
            var wrap = CreateFunc(root, name, func, synchronized);
            AddExtVariable(root, name, wrap);
        }

        public FuncCtx PrepareScript(string script)
        {
            var lexer = new Lexer(script);
            var parser = new Parser(lexer);
            var root = parser.Parse();

            //Debug.Log(root.Print(""));

            var rb = new Block();
            rb.EAdd(Compile(rb, root));
            rb.Type = Block.BlockType.Func;

            var rc = new VarCtx(null, null, null, this);
            var main = CreateFunc(rc, "main", new Function { Body = rb });
            rc.AddVar("main", main);
            return main;
        }

        VarCtx EnterInterpreter(FuncCtx entry, object[] args)
        {
            var resultHolder = new VarCtx(null, null, null, this);
            try
            {
                Result = Block.CallFunc(resultHolder, entry, args);
            }
            catch (Exception e)
            {
                resultHolder.Break = VarCtx.BreakMode.Throw;
                resultHolder.ScriptException = new RuntimeException(e.Message, resultHolder);
            }
            if (resultHolder.Break == VarCtx.BreakMode.Throw)
            {
                if (OnException == null)
                    SyncDebug("Unhandleded exception: " + resultHolder.ScriptException);
                else
                    SyncCtx.Send((d) => OnException(-1, resultHolder.ScriptException), null);
            }
            return resultHolder;
        }

        public void IrqPoll()
        {
            // exit
            if (Gas <= 0)
                PauseThread(null, null);

            // handle interrupts
            if (!InterruptsEnabled)
                return;

            Cli();
            while (Interrupts.Count > 0)
            {
                var irq = Interrupts.Dequeue();
                if (InterruptHandlers.ContainsKey(irq))
                {
                    var args = InterruptArgs.ContainsKey(irq) ? InterruptArgs[irq] : new object[] { irq };
                    InterruptArgs.Remove(irq);
                    EnterInterpreter(InterruptHandlers[irq], args);
                    if (AfterInterrupt != null)
                        SyncCtx.Send((d) => AfterInterrupt(irq), null);
                }
            }
            Sti();
        }

        public void ThreadEntry()
        {
            while (true)
            {
                try
                {
                    ResumeTask.WaitOne();
                    if (Terminate)
                        throw new TerminationException();
                    IrqPoll();
                    var entry = ScriptEntry;
                    if (entry != null)
                        EnterInterpreter(entry, null);
                    else
                        Result = null;
                }
                catch (TerminationException te)
                {
                    // Exited
                    Terminate = true;
                }
                catch (Exception e)
                {
                    //Debug.Log(e);
                    SyncDebug(e);
                }
                finally
                {
                    TaskPaused.Set();
                }
                if (Terminate)
                    break;
            }
        }

        public void SetUnhandledExceptionHandler(Action<long, object> onException)
        {
            OnException = onException;
        }

        public void SetInterruptCompleteHandler(Action<long> afterInterrupt)
        {
            AfterInterrupt = afterInterrupt;
        }

        public void SetScript(FuncCtx func)
        {
            ScriptEntry = func;
        }

        bool firstLogAfterTerminate = false;
        public object ContinueScript(int gas)
        {
            if (Terminate)
            {
                if (!firstLogAfterTerminate)
                {
                    firstLogAfterTerminate = true;
                    Debug.LogWarning($"CPU is terminated");
                }
                return null;
            }
            Result = null;
            Gas = gas;
            ResumeTask.Set();
            var waitResult = TaskPaused.WaitOne(1000);
            while (waitResult)
            {
                if (cb == null)
                    break; // exit
                // handle sync context
                try
                {
                    cb(state);
                }
                catch (Exception e)
                {
                    cbE = e;
                }
                cb = null;
                ResumeTask.Set();
                waitResult = TaskPaused.WaitOne(1000);
            }
            if (!waitResult)
            {
                Debug.LogError("Fatal: wait CPU FAILED, CPU will be stopped");
                ResumeTask.Set();
                Terminate = true;
            }
            ScriptEntry = null;
            if (Gas == 0)
                return null;
            return Result;
        }

        public void Dispose()
        {
            Terminate = true;
            ResumeTask.Set();
        }
    }
}
