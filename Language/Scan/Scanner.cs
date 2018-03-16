using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Language.Scan
{
    public class Scanner
    {
        private readonly string s;
        private readonly List<Lexema> lexemas;
        private readonly Stack<int> states;
        private int i;
        private Lexema curr;
        private int current;
        private int lineStart;
        private int line = 1;
        private int Symbol => i - lineStart;

        public bool HasNext => current + 1 < lexemas.Count && lexemas[current + 1].Type != LexType.Tendd;
        public Lexema Current => lexemas[current];

        public Scanner(string content)
        {
            states = new Stack<int>();
            var r = new Regex(@"(;\s*)+;");
            s = r.Replace(content, ";");
            lexemas = Scan().ToList();
            current = -1;
        }

        public Lexema Next()
        {
            if (!HasNext)
            {
                return lexemas[current + 1];
            }
            var l = lexemas[++current];
            if (l.Type == LexType.Terr)
            {
                throw new TokenException(l);
            }
            return l;
        }

        public void PushState()
        {
            states.Push(current);
        }

        public void PopState()
        {
            current = states.Pop();
        }

        public void DropState()
        {
            states.Pop();
        }

        private IEnumerable<Lexema> Scan()
        {
            while (i < s.Length)
            {
                var startI = i;
                var l = IgnoreIgnored();
                if (l != null)
                    yield return l;
                if (i >= s.Length)
                    break;
                var got = true;
                switch (s[i])
                {
                    case ';':
                        yield return LexChar(LexType.Tdelim);
                        break;
                    case ',':
                        yield return LexChar(LexType.Tcomma);
                        break;
                    case '(':
                        yield return LexChar(LexType.Tlparen);
                        break;
                    case ')':
                        yield return LexChar(LexType.Trparen);
                        break;
                    case '{':
                        yield return LexChar(LexType.Tlbracket);
                        break;
                    case '}':
                        yield return LexChar(LexType.Trbracket);
                        break;
                    case '+':
                        yield return LexChar(LexType.Tplus);
                        break;
                    case '-':
                        yield return LexChar(LexType.Tminus);
                        break;
                    case '*':
                        yield return LexChar(LexType.Tmul);
                        break;
                    case '/':
                        yield return LexChar(LexType.Tdiv);
                        break;
                    case '%':
                        yield return LexChar(LexType.Tmod);
                        break;
                    case '&':
                        yield return LexChar(LexType.Tand);
                        break;
                    case '|':
                        yield return LexChar(LexType.Tor);
                        break;
                    case '^':
                        yield return LexChar(LexType.Txor);
                        break;
                    case '~':
                        yield return LexChar(LexType.Tnot);
                        break;
                    case '=':
                        yield return LexChar(LexType.Teq);
                        break;
                    default:
                        got = false;
                        break;
                }
                if (got)
                    continue;
                if (s[i] == '<')
                {
                    StartLexema();
                    AddChar();
                    if (i < s.Length && s[i] == '<')
                    {
                        AddChar();
                        SetLexType(LexType.Tlshift);
                        yield return EndLexema();
                        continue;
                    }
                }
                if (s[i] == '>')
                {
                    StartLexema();
                    AddChar();
                    if (i < s.Length && s[i] == '>')
                    {
                        AddChar();
                        SetLexType(LexType.Trshift);
                        yield return EndLexema();
                        continue;
                    }
                }
                if (s[i] == '0')
                {
                    StartLexema();
                    AddChar();
                    yield return NNumParse();
                }
                else if (s[i] >= '1' && s[i] <= '9')
                {
                    StartLexema();
                    AddChar();
                    SetLexType(LexType.Tintd);
                    yield return DecParse();
                }
                else if (s[i] == '\'')
                {
                    StartLexema();
                    AddChar();
                    SetLexType(LexType.Tchar);
                    yield return CharParse();
                }
                else if (s[i] >= 'a' && s[i] <= 'z' || s[i] >= 'A' && s[i] <= 'Z' || s[i] >= '0' && s[i] <= '9' ||
                         s[i] == '_')
                {
                    StartLexema();
                    AddChar();
                    yield return IdentParse();
                }
                if (i == startI)
                {
                    yield return ParseBadSymbol();
                }
            }
            StartLexema();
            SetLexType(LexType.Tend);
            yield return EndLexema();
            StartLexema();
            SetLexType(LexType.Tendd);
            yield return EndLexema();
        }

        private Lexema ParseBadSymbol()
        {
            StartLexema();
            AddChar();
            SetLexType(LexType.Terr);
            return EndLexema();
        }

        private Lexema IgnoreIgnored()
        {
            int startI;
            do
            {
                startI = i;
                while (i < s.Length && (s[i] == ' ' || s[i] == '\n'))
                {
                    if (s[i] == '\n')
                    {
                        lineStart = i + 1;
                        line += 1;
                    }
                    ++i;
                }
                if (i + 1 < s.Length && s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    i += 2;
                    return IgnoreUntil(false, '\n');
                }
                if (i + 1 < s.Length && s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*')
                {
                    i += 2;
                    return IgnoreUntil(true, '*', '/');
                }
            } while (startI != i);
            return null;
        }

        private Lexema IgnoreUntil(bool returnError, params char[] waiting)
        {
            bool got = false;
            while (i < s.Length)
            {
                var all = waiting
                    .Select((e, idx) => new {e, idx})
                    .All(w => i + w.idx < s.Length && s[i + w.idx] == w.e);
                if (all)
                {
                    i += waiting.Length;
                    got = true;
                    break;
                }
                ++i;
                if (s[i] == '\n')
                {
                    lineStart = i + 1;
                    line += 1;
                }
            }
            if (!got && i >= s.Length && returnError)
            {
                StartLexema();
                SetLexType(LexType.Terr);
                return EndLexema();
            }
            return null;
        }

        private Lexema CharParse()
        {
            if (i < s.Length && (s[i] >= '0' && s[i] <= '9' || s[i] >= 'a' && s[i] <= 'z'))
            {
                AddChar();
                if (s[i] == '\'')
                {
                    AddChar();
                    return EndLexema();
                }
            }
            SetLexType(LexType.Terr);
            return EndLexema();
        }

        private Lexema IdentParse()
        {
            while (i < s.Length && (s[i] >= 'a' && s[i] <= 'z' || s[i] >= 'A' && s[i] <= 'Z' ||
                                    s[i] >= '0' && s[i] <= '9' ||
                                    s[i] == '_'))
                AddChar();
            switch (curr.Tok)
            {
                case "for":
                    SetLexType(LexType.Tfor);
                    break;
                case "int":
                    SetLexType(LexType.TintType);
                    break;
                case "long":
                    SetLexType(LexType.TlongIntType);
                    break;
                case "void":
                    SetLexType(LexType.TvoidType);
                    break;
                case "char":
                    SetLexType(LexType.TcharType);
                    break;
                default:
                    SetLexType(LexType.Tident);
                    break;
            }
            return EndLexema();
        }

        private Lexema DecParse()
        {
            while (i < s.Length && s[i] >= '0' && s[i] <= '9')
                AddChar();
            return EndLexema();
        }

        private Lexema NNumParse()
        {
            if (i < s.Length && (s[i] == 'x' || s[i] == 'X'))
            {
                AddChar();
                SetLexType(LexType.Tinth);
                return HexParse();
            }
            if (i < s.Length && s[i] >= '0' && s[i] <= '9')
            {
                AddChar();
                SetLexType(LexType.Tinto);
                return OctalParse();
            }
            if (curr.Tok == "0")
            {
                SetLexType(LexType.Tintd);
                return EndLexema();
            }
            SetLexType(LexType.Terr);
            return EndLexema();
        }

        private Lexema OctalParse()
        {
            while (i < s.Length && s[i] >= '0' && s[i] <= '7')
                AddChar();
            return EndLexema();
        }

        private Lexema HexParse()
        {
            if (i >= s.Length || (s[i] < '0' || s[i] > '9') && (s[i] < 'a' || s[i] > 'f'))
            {
                SetLexType(LexType.Terr);
                return EndLexema();
            }
            while (i < s.Length && (s[i] >= '0' && s[i] <= '9' || s[i] >= 'a' && s[i] <= 'f'))
                AddChar();
            return EndLexema();
        }

        private Lexema LexChar(LexType type)
        {
            StartLexema();
            AddChar();
            SetLexType(type);
            return EndLexema();
        }

        private void StartLexema()
        {
            curr = new Lexema();
        }

        private void AddChar()
        {
            curr.Tok += s[i++];
        }

        private void SetLexType(LexType type)
        {
            curr.Type = type;
        }

        private Lexema EndLexema()
        {
            var v = 0;
            try
            {
                if (curr.Type == LexType.Tintd)
                {
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    v = int.Parse(curr.Tok);
                }
                else if (curr.Type == LexType.Tinth && curr.Tok.Substring(2).Length > 0)
                {
                    v = int.Parse(curr.Tok.Substring(2), NumberStyles.HexNumber);
                }
                else if (curr.Type == LexType.Tinto)
                {
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    v = Convert.ToInt32(curr.Tok, 8);
                }
            }
            catch (OverflowException)
            {
                SetLexType(LexType.Terr);
            }
            var t = curr;
            
            curr = null;
            t.Line = line;
            t.IntValue = v;
            t.Symbol = Symbol;
            return t;
        }
    }
}