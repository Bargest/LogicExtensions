using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Script
{
    public enum ExprType
    {
        Unknown, Empty, Ident, String, Int, Float, Null, Undef,
        Comma, Unary, Arifm,
        AssignVar, AssignDict, AsgArifm, AsgArifmDict, Dict, NewArray, DictGet,
        Call, Return, Throw, TryCatch, Break, Continue, IfElse, Loop, Seq, Func, DeclareVar
    }

    public class ExprNode
    {
        public readonly Lexer.Position Pos;
        public readonly ExprType OpType;

        protected ExprNode(ExprType t, Lexer.Position pos)
        {
            OpType = t;
            Pos = pos;
        }

        public virtual IEnumerable<ExprNode> AllChildren()
        {
            return new ExprNode[0];
        }

        // Debug
        public string Print(string ident)
        {
            var args = string.Join($",\n", AllChildren().Where(x => x != null).Select(x => x.Print(ident + "  ")).ToArray());
            if (args == "")
                return $"{ident}{OpType.ToString()}()";
            return $"{ident}{OpType.ToString()}(\n" +
                   $"{args}\n" +
                   $"{ident})";
        }
    }

    public class LRExprNode : ExprNode
    {
        public ExprNode Left;
        public ExprNode Right;

        protected LRExprNode(ExprNode l, ExprNode r, ExprType t, Lexer.Position pos) : base(t, pos)
        {
            Left = l;
            Right = r;
        }

        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Left;
            yield return Right;
        }
    }

    public class EmptyNode : ExprNode
    {
        public EmptyNode(Lexer.Position pos) : base(ExprType.Empty, pos)
        {
        }
    }
    public class BreakNode : ExprNode
    {
        public BreakNode(Lexer.Position pos) : base(ExprType.Break, pos)
        {
        }
    }
    public class ContinueNode : ExprNode
    {
        public ContinueNode(Lexer.Position pos) : base(ExprType.Continue, pos)
        {
        }
    }
    public class UnaryExprNode : ExprNode
    {
        public ExprNode Left;
        public Lexer.TokenType ArifmOp;

        public UnaryExprNode(ExprNode l, Lexer.TokenType op, Lexer.Position pos) : base(ExprType.Unary, pos)
        {
            Left = l;
            ArifmOp = op;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Left;
        }
    }

    public class ArifmExprNode : LRExprNode
    {
        public Lexer.TokenType ArifmOp;

        public ArifmExprNode(ExprNode l, ExprNode r, Lexer.TokenType op, Lexer.Position pos) : base(l, r, ExprType.Arifm, pos)
        {
            ArifmOp = op;
        }
    }
    public class AssignNode : LRExprNode
    {
        protected AssignNode(ExprNode l, ExprNode r, ExprType t, Lexer.Position pos) : base(l, r, t, pos)
        {
        }
    }

    public class AsgVarNode : AssignNode
    {
        public IdentNode Target => (IdentNode)Left;
        public AsgVarNode(IdentNode l, ExprNode r, Lexer.Position pos) : base(l, r, ExprType.AssignVar, pos)
        {
        }
    }

    public class AsgDictNode : AssignNode
    {
        public ExprNode Key;
        public AsgDictNode(ExprNode l, ExprNode m, ExprNode r, Lexer.Position pos) : base(l, r, ExprType.AssignDict, pos)
        {
            Key = m;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Key;
        }
    }

    public class AsgArifmNode : AssignNode
    {
        public Lexer.TokenType ArifmOp;

        protected AsgArifmNode(ExprNode l, ExprNode r, Lexer.TokenType op, ExprType t, Lexer.Position pos) : base(l, r, t, pos)
        {
            ArifmOp = op;
        }
    }

    public class AsgArifmDictNode : AsgArifmNode
    {
        public ExprNode Key;
        public AsgArifmDictNode(ExprNode l, ExprNode m, ExprNode r, Lexer.TokenType op, Lexer.Position pos) : base(l, r, op, ExprType.AsgArifmDict, pos)
        {
            Key = m;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Key;
        }
    }

    public class AsgArifmVarNode : AsgArifmNode
    {
        public IdentNode Target => (IdentNode)Left;
        public AsgArifmVarNode(IdentNode l, ExprNode r, Lexer.TokenType op, Lexer.Position pos) : base(l, r, op, ExprType.AsgArifm, pos)
        {
        }
    }
    public class IfElseNode : ExprNode
    {
        public ExprNode Cond;
        public ExprNode If;
        public ExprNode Else;

        public IfElseNode(ExprNode cond, ExprNode bodyIf, ExprNode bodyElse, Lexer.Position pos) : base(ExprType.IfElse, pos)
        {
            Cond = cond;
            If = bodyIf;
            Else = bodyElse;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Cond;
            yield return If;
            yield return Else;
        }
    }

    public class LoopNode : ExprNode
    {
        public ExprNode Init;
        public ExprNode Cond;
        public ExprNode Inc;
        public ExprNode Body;

        public LoopNode(ExprNode init, ExprNode cond, ExprNode inc, ExprNode body, Lexer.Position pos) : base(ExprType.Loop, pos)
        {
            Init = init;
            Cond = cond;
            Inc = inc;
            Body = body;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Init;
            yield return Cond;
            yield return Inc;
            yield return Body;
        }
    }

    public class IdentNode : ExprNode
    {
        public string Val;

        public IdentNode(string v, Lexer.Position pos) : base(ExprType.Ident, pos)
        {
            Val = v;
        }
    }

    public class ConstNode : ExprNode
    {
        protected ConstNode(ExprType t, Lexer.Position pos) : base(t, pos)
        {
        }
    }

    public class ObjectNode : ConstNode
    {
        protected ObjectNode(ExprType t, Lexer.Position pos) : base(t, pos)
        {
        }
    }

    public class UndefinedNode : ConstNode
    {
        public UndefinedNode(Lexer.Position pos) : base(ExprType.Undef, pos)
        {
        }
    }
    public class NullNode : ObjectNode
    {
        public NullNode(Lexer.Position pos) : base(ExprType.Null, pos)
        {
        }
    }

    public class NumberNode : ConstNode
    {
        protected NumberNode(ExprType t, Lexer.Position pos) : base(t, pos)
        {
        }
    }

    public class StringNode : ObjectNode
    {
        public string Str;
        public StringNode(string s, Lexer.Position pos) : base(ExprType.String, pos)
        {
            Str = s;
        }
    }
    public class IntNode : ConstNode
    {
        public long Val;
        public IntNode(long value, Lexer.Position pos) : base(ExprType.Int, pos)
        {
            Val = value;
        }
    }
    public class FloatNode : ConstNode
    {
        public float Val;
        public FloatNode(float value, Lexer.Position pos) : base(ExprType.Float, pos)
        {
            Val = value;
        }
    }

    public class DictNode : ObjectNode
    {
        public Dictionary<string, ExprNode> Values;
        public DictNode(Dictionary<string, ExprNode> val, Lexer.Position pos) : base(ExprType.Dict, pos)
        {
            Values = val;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            foreach (var x in Values.Values)
                yield return x;
        }
    }
    public class Sequence : ExprNode
    {
        public List<ExprNode> Values;
        public Sequence(List<ExprNode> v, Lexer.Position pos) : base(ExprType.Seq, pos)
        {
            Values = v;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            foreach (var x in Values)
                yield return x;
        }
    }

    public class TryCatchNode : ExprNode
    {
        public ExprNode Try, Catch;
        public string VarName;
        public TryCatchNode(ExprNode t, ExprNode c, string varName, Lexer.Position pos) : base(ExprType.TryCatch, pos)
        {
            Try = t;
            Catch = c;
            VarName = varName;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Try;
            yield return Catch;
        }
    }

    public class CommaNode : ExprNode
    {
        public List<ExprNode> Values;
        public CommaNode(List<ExprNode> vals, Lexer.Position pos) : base(ExprType.Comma, pos)
        {
            Values = vals;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            foreach (var x in Values)
                yield return x;
        }
    }

    public class FuncNode : ObjectNode
    {
        public string Name;
        public ExprNode Args;
        public ExprNode Body;
        public FuncNode(string name, ExprNode args, ExprNode body, Lexer.Position pos) : base(ExprType.Func, pos)
        {
            Name = name;
            Args = args;
            Body = body;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Args;
            yield return Body;
        }
    }

    public class ArrayNode : ObjectNode
    {
        public ExprNode Content;
        public ExprNode Length;

        public ArrayNode(ExprNode vals, ExprNode length, Lexer.Position pos) : base(ExprType.NewArray, pos)
        {
            Content = vals;
            Length = length;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Content;
            yield return Length;
        }
    }

    public class DictGetNode : LRExprNode
    {
        public DictGetNode(ExprNode l, ExprNode r, Lexer.Position pos) : base(l, r, ExprType.DictGet, pos)
        {
        }
    }
    public class CallNode : LRExprNode
    {
        public CallNode(ExprNode l, ExprNode r, Lexer.Position pos) : base(l, r, ExprType.Call, pos)
        {
        }
    }
    public class ReturnNode : ExprNode
    {
        public ExprNode RetValue;
        public ReturnNode(ExprNode l, Lexer.Position pos) : base(ExprType.Return, pos)
        {
            RetValue = l;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return RetValue;
        }
    }
    public class ThrowNode : ExprNode
    {
        public ExprNode ThrowValue;
        public ThrowNode(ExprNode l, Lexer.Position pos) : base(ExprType.Throw, pos)
        {
            ThrowValue = l;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return ThrowValue;
        }
    }

    public class DeclareVarNode : ExprNode
    {
        public IdentNode Ident;
        public ExprNode Value;
        public DeclareVarNode(IdentNode name, ExprNode value, Lexer.Position pos) : base(ExprType.DeclareVar, pos)
        {
            Ident = name;
            Value = value;
        }
        public override IEnumerable<ExprNode> AllChildren()
        {
            foreach (var x in base.AllChildren())
                yield return x;
            yield return Ident;
            yield return Value;
        }
    }
}
