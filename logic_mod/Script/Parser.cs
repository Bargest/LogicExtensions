using Modding.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;

namespace Logic.Script
{
    public class Parser
    {
        public class ParserException : Exception
        {
            public Lexer.Position Pos;
            public ParserException(string msg, Lexer.Position pos) : base(msg)
            {
                Pos = pos;
            }
        }

        Lexer Lexer;

        public Parser(Lexer lexer)
        {
            Lexer = lexer;
        }

        FuncNode FuncDecl(string name)
        {
            var pos = Lexer.Describe();
            if (Lexer.Token != Lexer.TokenType.TOK_LPAR)
                throw new ParserException("( expected", Lexer.Describe());
            Lexer.NextToken();
            var argList = Declare();
            if (Lexer.Token != Lexer.TokenType.TOK_RPAR)
                throw new ParserException(") expected", Lexer.Describe());
            Lexer.NextToken();
            var body = Statement();
            return new FuncNode(name, argList, body, pos);
        }

        DictNode DeclDict()
        {
            var pos = Lexer.Describe();
            Dictionary<string, ExprNode> dictData = new Dictionary<string, ExprNode>();
            while (Lexer.Token != Lexer.TokenType.TOK_RBRA) 
            { 
                if (Lexer.Token != Lexer.TokenType.TOK_ID)
                    throw new ParserException("Identifier expected", Lexer.Describe());
                var field = Lexer.Ident.ToString();
                Lexer.NextToken();
                if (Lexer.Token != Lexer.TokenType.TOK_DDOT)
                    throw new ParserException(": expected", Lexer.Describe());
                Lexer.NextToken();
                var expr = Expr_1();
                dictData[field] = expr;
                if (Lexer.Token == Lexer.TokenType.TOK_COMMA)
                    Lexer.NextToken();
            }

            return new DictNode(dictData, pos);
        }

        ExprNode Term()
        {
            var pos = Lexer.Describe();
            if (Lexer.Token == Lexer.TokenType.TOK_ID)
            {
                var name = Lexer.Ident.ToString();
                Lexer.NextToken();
                return new IdentNode(name, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_NUM
              || Lexer.Token == Lexer.TokenType.TOK_HEX
              || Lexer.Token == Lexer.TokenType.TOK_BIN)
            {
                var val = Lexer.Val;
                Lexer.NextToken();
                return new IntNode(val, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_FLOAT)
            {
                var val = Lexer.FloatVal;
                Lexer.NextToken();
                return new FloatNode((float)val, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_STRING)
            {
                var str = Lexer.Ident.ToString();
                Lexer.NextToken();
                return new StringNode(str, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_LPAR)
                return ParenExpr();
            if (Lexer.Token == Lexer.TokenType.TOK_FUNC)
            {
                Lexer.NextToken();
                string funcName = null;
                if (Lexer.Token == Lexer.TokenType.TOK_ID)
                {
                    funcName = Lexer.Ident.ToString();
                    Lexer.NextToken();
                }
                return FuncDecl(funcName);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_LSB)
            {
                Lexer.NextToken();
                if (Lexer.Token == Lexer.TokenType.TOK_RSB)
                {
                    Lexer.NextToken();
                    return new ArrayNode(null, new IntNode(0, pos), pos);
                }
                var content = Expr();
                if (Lexer.Token != Lexer.TokenType.TOK_RSB)
                    throw new ParserException("] expected", Lexer.Describe());
                Lexer.NextToken();
                return new ArrayNode(content, null, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_LBRA)
            {
                Lexer.NextToken();
                var n = DeclDict();
                if (Lexer.Token != Lexer.TokenType.TOK_RBRA)
                    throw new ParserException("} expected", Lexer.Describe());
                Lexer.NextToken();
                return n;
            }
            if (Lexer.Token == Lexer.TokenType.TOK_ARRAY)
            {
                Lexer.NextToken();
                var n = ParenExpr();
                return new ArrayNode(null, n, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_NULL)
            {
                Lexer.NextToken();
                return new NullNode(pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_UNDEF)
            {
                Lexer.NextToken();
                return new UndefinedNode(pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_TRUE)
            {
                Lexer.NextToken();
                return new IntNode(1, pos);
            }
            if (Lexer.Token == Lexer.TokenType.TOK_FALSE)
            {
                Lexer.NextToken();
                return new IntNode(0, pos);
            }
            throw new ParserException("Invalid term " + Lexer.Token, Lexer.Describe());
        }

        ExprNode Expr_P()
        {
            var n = Term();
            while (Lexer.Token == Lexer.TokenType.TOK_LSB
                || Lexer.Token == Lexer.TokenType.TOK_LPAR
                || Lexer.Token == Lexer.TokenType.TOK_DOT)
            {
                var pos = Lexer.Describe();
                if (Lexer.Token == Lexer.TokenType.TOK_LSB)
                {
                    Lexer.NextToken();
                    var n1 = Expr();
                    if (Lexer.Token != Lexer.TokenType.TOK_RSB)
                        throw new ParserException("] expected", Lexer.Describe());
                    n = new DictGetNode(n, n1, pos);
                    Lexer.NextToken();
                }
                else if (Lexer.Token == Lexer.TokenType.TOK_DOT)
                {
                    var pos2 = Lexer.Describe();
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_ID)
                        throw new ParserException("Identifier expected", Lexer.Describe());
                    var field = Lexer.Ident.ToString();
                    n = new DictGetNode(n, new StringNode(field, pos2), pos);
                    Lexer.NextToken();
                }
                else
                {
                    var n1 = ParenExpr();
                    n = new CallNode(n, n1, pos);
                }
            }
            return n;
        }

        ExprNode Expr_U()
        {
            ExprNode n;
            var t = Lexer.Token;
            switch (t)
            {
                case Lexer.TokenType.TOK_NOT:
                case Lexer.TokenType.TOK_NOTAR:
                case Lexer.TokenType.TOK_MINUS:
                case Lexer.TokenType.TOK_PLUS:
                    var pos = Lexer.Describe();
                    Lexer.NextToken();
                    n = Expr_U();
                    return new UnaryExprNode(n, t, pos);
                default:
                    return Expr_P();
            }
        }

        ExprNode Expr_5()
        {
            var n = Expr_U();
            while (Lexer.Token == Lexer.TokenType.TOK_MUL
                || Lexer.Token == Lexer.TokenType.TOK_DIV
                || Lexer.Token == Lexer.TokenType.TOK_MOD)
            {
                var pos = Lexer.Describe();
                var t = Lexer.Token;
                Lexer.NextToken();
                var n1 = Expr_U();
                n = new ArifmExprNode(n, n1, t, pos);
            }
            return n;
        }

        ExprNode Expr_4()
        {
            var n = Expr_5();
            while (Lexer.Token == Lexer.TokenType.TOK_PLUS
                || Lexer.Token == Lexer.TokenType.TOK_MINUS)
            {
                var pos = Lexer.Describe();
                var t = Lexer.Token;
                Lexer.NextToken();
                var n1 = Expr_5();
                n = new ArifmExprNode(n, n1, t, pos);
            }
            return n;
        }

        ExprNode Expr_4_1()
        {
            var n = Expr_4();
            while (Lexer.Token == Lexer.TokenType.TOK_SHL
                || Lexer.Token == Lexer.TokenType.TOK_SHR)
            {
                var pos = Lexer.Describe();
                var t = Lexer.Token;
                Lexer.NextToken();
                var n1 = Expr_4();
                n = new ArifmExprNode(n, n1, t, pos);
            }
            return n;
        }

        ExprNode Expr_3()
        {
            var n = Expr_4_1();
            while (Lexer.Token == Lexer.TokenType.TOK_LESS
                || Lexer.Token == Lexer.TokenType.TOK_GREATER
                || Lexer.Token == Lexer.TokenType.TOK_GREATEQ
                || Lexer.Token == Lexer.TokenType.TOK_LESSEQ)
            {
                var pos = Lexer.Describe();
                var t = Lexer.Token;
                Lexer.NextToken();
                var n1 = Expr_4_1();
                n = new ArifmExprNode(n, n1, t, pos);
            }
            return n;
        }

        ExprNode Expr_3_1()
        {
            var n = Expr_3();
            while (Lexer.Token == Lexer.TokenType.TOK_EQUAL || Lexer.Token == Lexer.TokenType.TOK_NOTEQ)
            {
                var pos = Lexer.Describe();
                var t = Lexer.Token;
                Lexer.NextToken();
                var n1 = Expr_3();
                n = new ArifmExprNode(n, n1, t, pos);
            }
            return n;
        }

        ExprNode Expr_2()
        {
            var n = Expr_3_1();
            while (Lexer.Token == Lexer.TokenType.TOK_AND)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                var n1 = Expr_3_1();
                n = new ArifmExprNode(n, n1, Lexer.TokenType.TOK_AND, pos);
            }
            return n;
        }

        ExprNode Expr_2_1()
        {
            var n = Expr_2();
            while (Lexer.Token == Lexer.TokenType.TOK_XOR)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                var n1 = Expr_2();
                n = new ArifmExprNode(n, n1, Lexer.TokenType.TOK_XOR, pos);
            }
            return n;
        }

        ExprNode Expr_2_2()
        {
            var n = Expr_2_1();
            while (Lexer.Token == Lexer.TokenType.TOK_OR)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                var n1 = Expr_2_1();
                n = new ArifmExprNode(n, n1, Lexer.TokenType.TOK_OR, pos);
            }
            return n;
        }
        ExprNode Expr_2_3()
        {
            var n = Expr_2_2();
            while (Lexer.Token == Lexer.TokenType.TOK_LOGAND)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                var n1 = Expr_2_2();
                n = new ArifmExprNode(n, n1, Lexer.TokenType.TOK_LOGAND, pos);
            }
            return n;
        }

        ExprNode Expr_2_4()
        {
            var n = Expr_2_3();
            while (Lexer.Token == Lexer.TokenType.TOK_LOGOR)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                var n1 = Expr_2_3();
                n = new ArifmExprNode(n, n1, Lexer.TokenType.TOK_LOGOR, pos);
            }
            return n;
        }

        AssignNode MakeIdentAssign(IdentNode n, ExprNode n1, Lexer.TokenType op, Lexer.Position pos)
        {
            if (op == Lexer.TokenType.TOK_ASSIGN)
                return new AsgVarNode(n, n1, pos);
            switch (op)
            {
                case Lexer.TokenType.TOK_PLUS:    
                case Lexer.TokenType.TOK_MINUS:   
                case Lexer.TokenType.TOK_MUL:     
                case Lexer.TokenType.TOK_DIV:     
                case Lexer.TokenType.TOK_MOD:     
                case Lexer.TokenType.TOK_SHL:     
                case Lexer.TokenType.TOK_SHR:     
                //case Lexer.TokenType.TOK_NOTAR:   
                case Lexer.TokenType.TOK_AND:     
                case Lexer.TokenType.TOK_OR:      
                case Lexer.TokenType.TOK_XOR:     
                case Lexer.TokenType.TOK_PLUSPLUS:
                case Lexer.TokenType.TOK_MINUSMINUS:
                case Lexer.TokenType.FTOK_PLUSPLUS_PREF:
                case Lexer.TokenType.FTOK_MINUSMINUS_PREF:
                    return new AsgArifmVarNode(n, n1, op, pos);
            }
            throw new ParserException("Can't assign to " + n.OpType, pos);
        }
        AssignNode MakeDictAssign(DictGetNode n, ExprNode n1, Lexer.TokenType op, Lexer.Position pos)
        {
            if (op == Lexer.TokenType.TOK_ASSIGN)
                return new AsgDictNode(n.Left, n.Right, n1, pos);
            switch (op)
            {
                case Lexer.TokenType.TOK_PLUS:
                case Lexer.TokenType.TOK_MINUS:
                case Lexer.TokenType.TOK_MUL:
                case Lexer.TokenType.TOK_DIV:
                case Lexer.TokenType.TOK_MOD:
                case Lexer.TokenType.TOK_SHL:
                case Lexer.TokenType.TOK_SHR:
                //case Lexer.TokenType.TOK_NOTAR:
                case Lexer.TokenType.TOK_AND:
                case Lexer.TokenType.TOK_OR:
                case Lexer.TokenType.TOK_XOR:
                case Lexer.TokenType.TOK_PLUSPLUS:
                case Lexer.TokenType.TOK_MINUSMINUS:
                case Lexer.TokenType.FTOK_PLUSPLUS_PREF:
                case Lexer.TokenType.FTOK_MINUSMINUS_PREF:
                    return new AsgArifmDictNode(n.Left, n.Right, n1, op, pos);
            }
            throw new ParserException("Can't assign to " + n.OpType, pos);
        }

        AssignNode MakeAssign(ExprNode n, ExprNode n1, Lexer.TokenType op, Lexer.Position pos)
        {
            if (n.OpType == ExprType.Ident)
                return MakeIdentAssign(n as IdentNode, n1, op, pos);
            else if (n.OpType == ExprType.DictGet)
                return MakeDictAssign(n as DictGetNode, n1, op, pos);
            throw new ParserException("Can't assign to " + n.OpType, pos);
        }

        ExprNode Expr_1()
        {
            var pp = Lexer.TokenType.TOK_NONE;
            var ppPos = Lexer.Describe();
            if (Lexer.Token == Lexer.TokenType.TOK_MINUSMINUS || Lexer.Token == Lexer.TokenType.TOK_PLUSPLUS)
            {
                pp = Lexer.Token;
                Lexer.NextToken();
            }
            var n = Expr_2_4();
            if (pp != Lexer.TokenType.TOK_NONE)
                n = MakeAssign(n, null, pp == Lexer.TokenType.TOK_MINUSMINUS ? Lexer.TokenType.FTOK_MINUSMINUS_PREF : Lexer.TokenType.FTOK_PLUSPLUS_PREF, ppPos);

            if (Lexer.Token >= Lexer.TokenType.TOK_ASSIGN && Lexer.Token <= Lexer.TokenType.TOK_XORA)
            {
                var tok = Lexer.Token;
                var tokDescr = Lexer.Describe();
                Lexer.NextToken();
                if (tok == Lexer.TokenType.TOK_MINUSMINUS || tok == Lexer.TokenType.TOK_PLUSPLUS)
                {
                    n = MakeAssign(n, null, tok, tokDescr);
                }
                else
                {
                    var n2 = Expr_1();
                    if (tok == Lexer.TokenType.TOK_ASSIGN)
                    {
                        n = MakeAssign(n, n2, tok, tokDescr);
                    }
                    else
                    {
                        switch (tok)
                        {
                            case Lexer.TokenType.TOK_PLUSA: tok = Lexer.TokenType.TOK_PLUS; break;
                            case Lexer.TokenType.TOK_MINUSA: tok = Lexer.TokenType.TOK_MINUS; break;
                            case Lexer.TokenType.TOK_MULA: tok = Lexer.TokenType.TOK_MUL; break;
                            case Lexer.TokenType.TOK_DIVA: tok = Lexer.TokenType.TOK_DIV; break;
                            case Lexer.TokenType.TOK_MODA: tok = Lexer.TokenType.TOK_MOD; break;
                            case Lexer.TokenType.TOK_SHLA: tok = Lexer.TokenType.TOK_SHL; break;
                            case Lexer.TokenType.TOK_SHRA: tok = Lexer.TokenType.TOK_SHR; break;
                            //case Lexer.TokenType.TOK_NOTARA:   tok = Lexer.TokenType.TOK_NOTAR; break;
                            case Lexer.TokenType.TOK_ANDA: tok = Lexer.TokenType.TOK_AND; break;
                            case Lexer.TokenType.TOK_ORA: tok = Lexer.TokenType.TOK_OR; break;
                            case Lexer.TokenType.TOK_XORA: tok = Lexer.TokenType.TOK_XOR; break;
                            case Lexer.TokenType.TOK_ASSIGN:
                                break;
                            default:
                                throw new ParserException("Unexpected arifm assign type " + tok, tokDescr);
                        }
                        n = MakeAssign(n, n2, tok, tokDescr);
                    }
                }
            }
            return n;
        }

        ExprNode ParseComma(Func<ExprNode> ParseExpr)
        {
            List<ExprNode> list = null;
            var n = ParseExpr();
            Lexer.Position pos = Lexer.Describe();
            while (Lexer.Token == Lexer.TokenType.TOK_COMMA)
            {
                Lexer.NextToken();
                if (list == null)
                {
                    list = new List<ExprNode>();
                    list.Add(n);
                }
                var n1 = ParseExpr();
                list.Add(n1);
            }
            if (list == null)
                return n;
            return new CommaNode(list, pos);
        }

        ExprNode Expr()
        {
            return ParseComma(Expr_1);
        }

        ExprNode ParenExpr()
        {
            if (Lexer.Token != Lexer.TokenType.TOK_LPAR)
                throw new ParserException("( expected", Lexer.Describe());

            Lexer.NextToken();
            if (Lexer.Token == Lexer.TokenType.TOK_RPAR)
            {
                var pos = Lexer.Describe();
                Lexer.NextToken();
                return new EmptyNode(pos);
            }

            var n = Expr();
            if (Lexer.Token != Lexer.TokenType.TOK_RPAR)
                throw new ParserException(") expected", Lexer.Describe());
            Lexer.NextToken();
            return n;
        }

        DeclareVarNode DeclareVar()
        {
            if (Lexer.Token != Lexer.TokenType.TOK_ID)
                throw new ParserException("Identifier expected", Lexer.Describe());

            var pos = Lexer.Describe();
            var name = Lexer.Ident.ToString();
            ExprNode val = null;
            Lexer.NextToken();
            if (Lexer.Token == Lexer.TokenType.TOK_ASSIGN)
            {
                Lexer.NextToken();
                val = Expr_1();
            }
            return new DeclareVarNode(new IdentNode(name, pos), val, pos);
        }

        ExprNode Declare()
        {
            if (Lexer.Token != Lexer.TokenType.TOK_ID)
                return new EmptyNode(Lexer.Describe());

            return ParseComma(DeclareVar);
        }

        ExprNode DeclExpr()
        {
            if (Lexer.Token == Lexer.TokenType.TOK_VAR)
            {
                Lexer.NextToken();
                return Declare();
            }
            return Expr();
        }

        ExprNode Statement()
        {
            List<ExprNode> nodes;
            ExprNode n, n1 = null, n2 = null, n3 = null;
            string strval;
            Lexer.Position pos = Lexer.Describe();
            switch (Lexer.Token)
            {
                case Lexer.TokenType.TOK_IF:
                    Lexer.NextToken();
                    n1 = ParenExpr();
                    n2 = Statement();
                    if (Lexer.Token ==Lexer.TokenType.TOK_ELSE)
                    {
                        Lexer.NextToken();
                        n3 = Statement();
                    }
                    return new IfElseNode(n1, n2, n3, pos);
                case Lexer.TokenType.TOK_FOR:
                    // get LBRA
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_LPAR)
                        throw new ParserException("( expected", Lexer.Describe());
                    Lexer.NextToken();
                    // first should be init expression
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        n1 = DeclExpr();
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    // condiniton expression
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        n2 = Expr();
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    // inc expression
                    if (Lexer.Token != Lexer.TokenType.TOK_RPAR)
                        n3 = Expr();
                    if (Lexer.Token != Lexer.TokenType.TOK_RPAR)
                        throw new ParserException($") expected", Lexer.Describe());
                    Lexer.NextToken();
                    // and last is statement
                    return new LoopNode(n1, n2, n3, Statement(), pos);
                case Lexer.TokenType.TOK_WHILE:
                    Lexer.NextToken();
                    n2 = ParenExpr();
                    return new LoopNode(null, n2, null, Statement(), pos);
                case Lexer.TokenType.TOK_LBRA:
                    Lexer.NextToken();
                    nodes = new List<ExprNode>();
                    while (Lexer.Token != Lexer.TokenType.TOK_RBRA)
                        nodes.Add(Statement());
                    Lexer.NextToken();
                    return new Sequence(nodes, pos);
                case Lexer.TokenType.TOK_TRY:
                    Lexer.NextToken();
                    n1 = Statement();
                    // TODO: implement 'finally'
                    if (Lexer.Token != Lexer.TokenType.TOK_CATCH)
                        throw new ParserException($"'catch' expected", Lexer.Describe());
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_LPAR)
                        throw new ParserException("( expected", Lexer.Describe());
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_ID)
                        throw new ParserException("Identifier expected", Lexer.Describe());
                    var name = Lexer.Ident.ToString();
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_RPAR)
                        throw new ParserException(") expected", Lexer.Describe());
                    Lexer.NextToken();
                    n2 = Statement();
                    return new TryCatchNode(n1, n2, name, pos);
                case Lexer.TokenType.TOK_SEMICOLON:
                    Lexer.NextToken();
                    return new EmptyNode(pos);
                case Lexer.TokenType.TOK_FUNC:
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_ID)
                        throw new ParserException("Identifier expected", Lexer.Describe());
                    var pos2 = Lexer.Describe();
                    strval = Lexer.Ident.ToString();
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_LPAR)
                        throw new ParserException("( expected", Lexer.Describe());
                    n1 = FuncDecl(strval);
                    return new DeclareVarNode(new IdentNode(strval, pos2), n1, pos);
                case Lexer.TokenType.TOK_RETURN:
                    Lexer.NextToken();
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        n1 = Expr();
                    n = new ReturnNode(n1, pos);
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    return n;
                case Lexer.TokenType.TOK_CONTINUE:
                    Lexer.NextToken();
                    if (Lexer.Token !=Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    return new ContinueNode(pos);
                case Lexer.TokenType.TOK_BREAK:
                    Lexer.NextToken();
                    if (Lexer.Token !=Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    return new BreakNode(pos);
                case Lexer.TokenType.TOK_THROW:
                    Lexer.NextToken();
                    n1 = Expr();
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    return new ThrowNode(n1, pos);
                default:
                    n1 = DeclExpr();
                    if (Lexer.Token != Lexer.TokenType.TOK_SEMICOLON)
                        throw new ParserException("; expected", Lexer.Describe());
                    Lexer.NextToken();
                    return n1;
            }
            throw new ParserException("Can't parse statement: " + Lexer.Token, Lexer.Describe());
        }

        public ExprNode Parse()
        {
            List<ExprNode> nodes = new List<ExprNode>();
            var pos = Lexer.Describe();
            Lexer.NextToken();
            while (Lexer.Token != Lexer.TokenType.TOK_EOF)
                nodes.Add(Statement());
            if (nodes.Count == 0)
                return new EmptyNode(pos);
            if (nodes.Count == 1)
                return nodes[0];
            return new Sequence(nodes, pos);
        }
    }
}
