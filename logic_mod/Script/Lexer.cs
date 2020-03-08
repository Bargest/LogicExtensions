using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Script
{
    public class Lexer
    {
        public enum TokenType
        {
            TOK_NONE, TOK_NUM, TOK_FLOAT, TOK_HEX, TOK_BIN, TOK_TRUE, TOK_FALSE, TOK_NULL, TOK_UNDEF,
            TOK_STRING, TOK_ID, TOK_FUNC, TOK_RETURN, TOK_VAR, TOK_IF, TOK_ELSE, TOK_WHILE, TOK_DO, TOK_FOR, TOK_LBRA, TOK_RBRA, TOK_LPAR, TOK_RPAR, TOK_LSB,
            TOK_RSB, TOK_ARRAY, TOK_TRY, TOK_CATCH, TOK_FINALLY, TOK_THROW, //TOK_ARRLEN,
            TOK_PLUS, TOK_MINUS, TOK_MUL, TOK_DIV, TOK_MOD, TOK_SHL, TOK_SHR, TOK_NOTAR, TOK_AND, TOK_OR, TOK_XOR,
            TOK_ASSIGN, TOK_PLUSPLUS, TOK_PLUSA, TOK_MINUSA, TOK_MINUSMINUS, TOK_MULA, TOK_DIVA, TOK_MODA, TOK_SHLA, TOK_SHRA, /*TOK_NOTARA, */TOK_ANDA, TOK_ORA, TOK_XORA,
            TOK_LOGAND, TOK_LOGOR,
            TOK_NOT, TOK_LESS, TOK_GREATER, TOK_EQUAL, TOK_GREATEQ, TOK_LESSEQ, TOK_NOTEQ, TOK_DOT, TOK_COMMA, TOK_SEMICOLON, TOK_CONTINUE, TOK_BREAK, TOK_BAR, TOK_EOF, TOK_ERR, TOK_DDOT,

            FTOK_PLUSPLUS_PREF, FTOK_MINUSMINUS_PREF
            //FTOK_UMINUS, FTOK_UPLUS, FTOK_CONST, FTOK_DICTASG, FTOK_ASGARIFM, FTOK_DICTASGARIFM, FTOK_NEWDICT, FTOK_CALL
        }
        public enum SpecialSymbol
        {
            BAR = '#', LBRA = '{', RBRA = '}', EQUAL = '=', SEMICOLON = ';', LPAR = '(', RPAR = ')', LSB = '[', RSB = ']',
            QUOT = '\"', SQUOT = '\'', PLUS = '+', MINUS = '-', LESS = '<', GREATER = '>', EXCLAM = '!', TILD = '~', AMPER = '&', OR = '|', COMMA = ',',
            XOR = '^', DIV = '/', MUL = '*', MOD = '%', DOT = '.', DDOT = ':'
        };

        static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            {"null",     TokenType.TOK_NULL },
            {"true",     TokenType.TOK_TRUE },
            {"false",    TokenType.TOK_FALSE },
            {"undefined",TokenType.TOK_UNDEF },
            {"if",       TokenType.TOK_IF },
            {"else",     TokenType.TOK_ELSE},
            {"do",       TokenType.TOK_DO},
            {"while",    TokenType.TOK_WHILE},
            {"for",      TokenType.TOK_FOR},
            {"var",      TokenType.TOK_VAR},
            {"function", TokenType.TOK_FUNC},
            {"return",   TokenType.TOK_RETURN},
            {"continue", TokenType.TOK_CONTINUE},
            {"break",    TokenType.TOK_BREAK},
            {"array",    TokenType.TOK_ARRAY},
            {"throw",    TokenType.TOK_THROW},
            {"try",      TokenType.TOK_TRY},
            {"catch",    TokenType.TOK_CATCH},
            {"finally",  TokenType.TOK_FINALLY}
        };

        public TokenType Token;
        public StringBuilder Ident;
        string Input;
        public int pos = 1, offs = 0, line = 1, ppos = 1;
        char ch;
        public long Val;
        public double FloatVal;

        public class Position
        {
            public int Line, Offset;
            public int Pos, Length;
            public string Ident;

            public override string ToString()
            {
                //return $"at {Pos}..{Pos+Length}, line {Line}, offset {Offset}";
                return $"near {Line}:{Offset}";
            }
        }

        public Position Describe()
        {
            return new Position
            {
                Line = line,
                Offset = offs,
                Pos = pos,
                Length = pos - ppos,
                Ident = Ident?.ToString()
            };
        }

        public Lexer(string str)
        {
            Input = str;
            ch = Input.Length == 0 ? '\0' : Input[0];
            Token = TokenType.TOK_NONE;
            Ident = new StringBuilder();
        }

        public void NextChar()
        {
            if (pos >= Input.Length)
                ch = '\0';
            else
                ch = Input[pos++];
        }

        public TokenType NextToken()
        {
            long intval = 0;
            double floatVal = 0, denom = 0;
            Token = TokenType.TOK_NONE;
            while (Token == TokenType.TOK_NONE)
            {
                ppos = pos;
                switch (ch)
                {
                    case '\0': Token = TokenType.TOK_EOF; break;
                    case '\t':
                        NextChar();
                        break;
                    case ' ':
                        NextChar();
                        break;
                    case '\n':
                        ++line;
                        NextChar();
                        // recalculate offset
                        offs = - pos + ppos;
                        break;
                    case '\r':
                        NextChar();
                        break;
                    case (char)SpecialSymbol.BAR: Token = TokenType.TOK_BAR; NextChar(); break;
                    case (char)SpecialSymbol.LBRA: Token = TokenType.TOK_LBRA; NextChar(); break;
                    case (char)SpecialSymbol.RBRA: Token = TokenType.TOK_RBRA; NextChar(); break;
                    case (char)SpecialSymbol.SEMICOLON: Token = TokenType.TOK_SEMICOLON; NextChar(); break;
                    case (char)SpecialSymbol.LPAR: Token = TokenType.TOK_LPAR; NextChar(); break;
                    case (char)SpecialSymbol.RPAR: Token = TokenType.TOK_RPAR; NextChar(); break;
                    case (char)SpecialSymbol.PLUS:
                        Token = TokenType.TOK_PLUS; NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL)
                        {
                            Token = TokenType.TOK_PLUSA; NextChar();
                        }
                        else if (ch == (char)SpecialSymbol.PLUS)
                        {
                            Token = TokenType.TOK_PLUSPLUS; NextChar();
                        }
                        break;
                    case (char)SpecialSymbol.MINUS:
                        {
                            Token = TokenType.TOK_MINUS; NextChar();
                            if (ch == (char)SpecialSymbol.EQUAL)
                            {
                                Token = TokenType.TOK_MINUSA; NextChar();
                            }
                            else if (ch == (char)SpecialSymbol.MINUS)
                            {
                                Token = TokenType.TOK_MINUSMINUS; NextChar();
                            }
                        }
                        break;
                    case (char)SpecialSymbol.OR:
                        Token = TokenType.TOK_OR;
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL)
                        {
                            Token = TokenType.TOK_ORA; NextChar();
                        }
                        else if (ch == (char)SpecialSymbol.OR)
                        {
                            Token = TokenType.TOK_LOGOR; NextChar();
                        }
                        break;
                    case (char)SpecialSymbol.AMPER:
                        Token = TokenType.TOK_AND;
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL)
                        {
                            Token = TokenType.TOK_ANDA; NextChar();
                        }
                        else if (ch == (char)SpecialSymbol.AMPER)
                        {
                            Token = TokenType.TOK_LOGAND; NextChar();
                        }
                        break;
                    case (char)SpecialSymbol.XOR: Token = TokenType.TOK_XOR; NextChar(); if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_XORA; NextChar(); } break;
                    case (char)SpecialSymbol.MUL: Token = TokenType.TOK_MUL; NextChar(); if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_MULA; NextChar(); } break;
                    case (char)SpecialSymbol.MOD: Token = TokenType.TOK_MOD; NextChar(); if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_MODA; NextChar(); } break;
                    case (char)SpecialSymbol.COMMA: Token = TokenType.TOK_COMMA; NextChar(); break;
                    case (char)SpecialSymbol.DOT: Token = TokenType.TOK_DOT; NextChar(); break;
                    case (char)SpecialSymbol.DDOT: Token = TokenType.TOK_DDOT; NextChar(); break;
                    case (char)SpecialSymbol.LSB: Token = TokenType.TOK_LSB; NextChar(); break;
                    case (char)SpecialSymbol.RSB: Token = TokenType.TOK_RSB; NextChar(); break;
                    case (char)SpecialSymbol.DIV:
                        NextChar();
                        if (ch == (char)SpecialSymbol.DIV)
                        {
                            while (ch != '\n' && ch != '\0')
                                NextChar();
                        }
                        else
                        {
                            if (ch == (char)SpecialSymbol.EQUAL)
                            {
                                Token = TokenType.TOK_DIVA;
                                NextChar();
                            }
                            else
                            {
                                Token = TokenType.TOK_DIV;
                            }
                        }
                        break;
                    case (char)SpecialSymbol.EQUAL:
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_EQUAL; NextChar(); }
                        else Token = TokenType.TOK_ASSIGN;
                        break;
                    case (char)SpecialSymbol.LESS:
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_LESSEQ; NextChar(); }
                        else if (ch == (char)SpecialSymbol.LESS)
                        {
                            Token = TokenType.TOK_SHL; NextChar();
                            if (ch == (char)SpecialSymbol.EQUAL)
                            {
                                Token = TokenType.TOK_SHLA; NextChar();
                            }
                        }
                        else Token = TokenType.TOK_LESS;
                        break;
                    case (char)SpecialSymbol.GREATER:
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_GREATEQ; NextChar(); }
                        else if (ch == (char)SpecialSymbol.GREATER)
                        {
                            Token = TokenType.TOK_SHR; NextChar();
                            if (ch == (char)SpecialSymbol.EQUAL)
                            {
                                Token = TokenType.TOK_SHRA; NextChar();
                            }
                        }
                        else Token = TokenType.TOK_GREATER;
                        break;
                    case (char)SpecialSymbol.EXCLAM:
                        NextChar();
                        if (ch == (char)SpecialSymbol.EQUAL) { Token = TokenType.TOK_NOTEQ; NextChar(); }
                        else Token = TokenType.TOK_NOT;
                        break;
                    case (char)SpecialSymbol.TILD:
                        NextChar();
                        Token = TokenType.TOK_NOTAR;
                        //if (ch == (char)SpecialSymbol.EQUAL)
                        //{
                        //    Token = TokenType.TOK_NOTARA; NextChar();
                        //}
                        break;
                    case (char)SpecialSymbol.SQUOT:
                    case (char)SpecialSymbol.QUOT:
                        Ident = new StringBuilder();
                        var endChar = (ch == (char)SpecialSymbol.SQUOT) ? '\'' : '\"';
                        NextChar();
                        while (ch != endChar && ch != '\0')
                        {
                            if (ch == '\\')
                            {
                                NextChar();
                                switch (ch)
                                {
                                    case '\\': Ident.Append('\\'); break;
                                    case '\"': Ident.Append('\"'); break;
                                    case '\'': Ident.Append('\''); break;
                                    case  '0': Ident.Append('\0'); break;
                                    case  't': Ident.Append('\t'); break;
                                    case  'r': Ident.Append('\r'); break;
                                    case  'n': Ident.Append('\n'); break;
                                }
                            }
                            else
                            {
                                Ident.Append(ch);
                            }
                            NextChar();
                        }
                        NextChar();
                        Token = TokenType.TOK_STRING;
                        break;
                    /*case (char)SpecialSymbol.SQUOT:
                        Ident = new StringBuilder();
                        NextChar();
                        while (ch != '\'')
                        {
                            if (ch == '\\')
                            {
                                NextChar();
                                switch (ch)
                                {
                                    case '\\': Ident.Append('\\'); break;
                                    case '\"': Ident.Append('\"'); break;
                                    case '\'': Ident.Append('\"'); break;
                                    case  '0': Ident.Append('\0'); break;
                                    case  't': Ident.Append('\t'); break;
                                    case  'r': Ident.Append('\r'); break;
                                    case  'n': Ident.Append('\n'); break;
                                }
                            }
                            else
                            {
                                Ident.Append(ch);
                            }
                            NextChar();
                        }
                        NextChar();
                        if (Ident.Length != 1)
                            Token = TokenType.TOK_ERR;
                        else
                        {
                            Val = Ident[0];
                            Token = TokenType.TOK_NUM;
                        }
                        break;*/
                    default:
                        if (ch == '0')
                        {
                            Ident = new StringBuilder();
                            intval = 0;
                            Val = intval;
                            Token = TokenType.TOK_NUM;
                            NextChar();
                            if (ch == 'x')
                            {
                                NextChar();
                                intval = 0;
                                int len = 0;
                                Token = TokenType.TOK_HEX;
                                while (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch == '_')
                                {
                                    if (!(ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f' || ch >= 'A' && ch <= 'F'))
                                        Token = TokenType.TOK_ERR;
                                    ++len;
                                    if (ch <= '9')
                                        intval = (intval << 4) + ch - '0';//intval = i64shl(intval, 4) + ch - '0';
                                    else if (ch <= 'F')
                                        intval = (intval << 4) + ch - 'A' + 10;//intval = i64shl(intval, 4) + ch - 'a' + 10;
                                    else
                                        intval = (intval << 4) + ch - 'a' + 10;//intval = i64shl(intval, 4) + ch - 'a' + 10;
                                    Ident.Append(ch);
                                    NextChar();
                                }
                                Val = intval;
                                if (len == 0)
                                    Token = TokenType.TOK_ERR;
                                break;
                            }
                            else if (ch == 'b')
                            {
                                NextChar();
                                intval = 0;
                                int len = 0;
                                while (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch == '_')
                                {
                                    if (ch != '0' && ch != '1')
                                        Token = TokenType.TOK_ERR;
                                    ++len;
                                    //intval = i64shl(intval, 1) + ch - '0';
                                    intval = (intval << 1) + ch - '0';
                                    Ident.Append(ch);
                                    NextChar();
                                }
                                Val = intval;
                                Token = TokenType.TOK_BIN;
                                if (len == 0)
                                    Token = TokenType.TOK_ERR;
                                break;
                            }
                        }
                        if (ch >= '0' && ch <= '9' || ch == '.')
                        {
                            Ident = new StringBuilder();
                            Token = TokenType.TOK_NUM;
                            intval = 0;
                            while (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch == '.')
                            {
                                if (!(ch >= '0' && ch <= '9' || ch == '.'))
                                    Token = TokenType.TOK_ERR;
                                if (Token != TokenType.TOK_ERR)
                                {
                                    if (ch != '.')
                                    {
                                        if (Token != TokenType.TOK_FLOAT)
                                            intval = intval * 10 + ch - '0';
                                        else
                                        {
                                            floatVal += ((byte)(ch - '0')) * denom;
                                            denom /= 10.0;
                                        }
                                    }
                                    else
                                    {
                                        if (Token != TokenType.TOK_FLOAT)
                                        {
                                            floatVal = intval;
                                            denom = 0.1;
                                            Token = TokenType.TOK_FLOAT;
                                        }
                                        else
                                        {
                                            // two dots in one number
                                            Token = TokenType.TOK_ERR;
                                        }
                                    }
                                }
                                Ident.Append(ch);
                                NextChar();
                            }
                            if (Token == TokenType.TOK_FLOAT)
                                FloatVal = floatVal;
                            else
                                Val = intval;
                        }
                        else if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch == '_')
                        {
                            Ident = new StringBuilder();
                            while ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || (ch == '_'))
                            {
                                Ident.Append(ch);
                                NextChar();
                            }
                            var ids = Ident.ToString();
                            if (Keywords.ContainsKey(ids))
                                Token = Keywords[ids];
                            else
                                Token = TokenType.TOK_ID;
                        }
                        else if (Token == TokenType.TOK_NONE)
                        {
                            Token = TokenType.TOK_ERR;
                        }
                        break;
                }
                offs += pos - ppos;
            }
            return Token;
        }
    }
}
