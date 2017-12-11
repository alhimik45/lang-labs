using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Language.Scan;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Language.Analyzer
{
    public class PredSyntaxAnalyzer
    {
        private readonly Scanner sc;
        private readonly List<Lexema> magaz = new List<Lexema>();
        private Dictionary<LexType, Dictionary<LexType, PredType>> table;
        private List<Rule> rules;

        public PredSyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
            table = JsonConvert.DeserializeObject<Dictionary<LexType, Dictionary<LexType, PredType>>>(
                File.ReadAllText("t.txt"));
            var gr = File.ReadAllText("gr.txt");
            rules = gr.Split("\n")
                .Select(r =>
                {
                    var parts = r.Split("->").Select(q => q.Trim()).ToList();
                    return new Rule
                    {
                        n = parts[0],
                        l = parts[1].Split("|").Select(li => li.Split(" ").ToArray()).ToArray()
                    };
                }).ToList();
            var neterms = rules.Select(r => r.n);
            foreach (var rule in rules)
            {
                rule.l = rule.l.Select(r => r.Where(q => !string.IsNullOrWhiteSpace(q)).ToArray())
                    .Where(a => a.Length > 0)
                    .ToArray();
                var ch = true;
                while (ch)
                {
                    ch = false;
                    var rr = new List<string[]>();
                    foreach (var l in rule.l)
                    {
                        var i = l.ToList().FindIndex(r => r == "Enterm");
                        if (i > -1)
                        {
                            var ff = l.ToList();
                            ff[i] = "Tneterm";
                            rr.Add(ff.ToArray());
                            var gg = l.ToList();
                            gg.RemoveAt(i);
                            rr.Add(gg.ToArray());
                            ch = true;
                        }
                        else
                        {
                            rr.Add(l);
                        }
                    }
                    rule.l = rr.ToArray();
                }
            }

            foreach (var rule in rules)
            {
                rule.ll = rule.l.Select(r => r.Where(q => !string.IsNullOrWhiteSpace(q))
                    .Select(a => neterms.Contains(a) ? "Tneterm" : a)
                    .Select(a => JsonConvert.DeserializeObject<Lexema>(
                        @"{""Type"":""" + a.Replace("for", "Tfor").Replace("int", "TintType")
                            .Replace("long", "TlongIntType").Replace("char", "TcharType")
                            .Replace("void", "TvoidType").Replace(";", "Tdelim").Replace("(", "Tlparen")
                            .Replace(")", "Trparen").Replace(",", "Tcomma").Replace("{", "Tlbracket")
                            .Replace("}", "Trbracket").Replace("+", "Tplus").Replace("-", "Tminus")
                            .Replace("*", "Tmul").Replace("/", "Tdiv").Replace("%", "Tmod").Replace("&", "Tand")
                            .Replace("\\", "Tor").Replace("^", "Txor").Replace("~", "Tnot").Replace("=", "Teq")
                            .Replace("<<", "Tlshift").Replace(">>", "Trshift").Replace("#", "Tend")
                            .Replace(">", "Gt").Replace("<", "Lt").Replace("идентификатор", "Tident")
                            .Replace("десятичная", "Tintd").Replace("шестнадцатеричная", "Tinth")
                            .Replace("восьмеричная", "Tinto").Replace("символьная", "Tchar") + @"""}").Type)
                    .ToArray()).ToArray();
            }

//            throw new Exception();
        }

        public void Check()
        {
            Lexema l;
            sc.PushState();
            l = sc.Next();
            if (l.Type == LexType.Tend)
            {
                sc.PopState();
            }
            else
            {
                sc.DropState();
            }

            var f = true;
            while (f)
            {
                var top = magaz.NN().LastOrDefault() ?? new Lexema
                {
                    Line = l.Line,
                    Symbol = l.Symbol,
                    Tok = l.Tok,
                    Type = LexType.Tend
                };
                var rel = table.GetValueOrDefault(top.Type, new Dictionary<LexType, PredType>())
                    .GetValueOrDefault(l.Type, PredType.Nil);
                switch (rel)
                {
                    case PredType.Nil:
                        if (l.Type == LexType.Tneterm)
                        {
                            throw new ParseException(l.Tok, l);
                        }
                        throw new ParseException(l);
                    case PredType.Lt:
                    case PredType.Eq:
                        magaz.Add(l);
                        sc.PushState();
                        l = sc.Next();
                        if (l.Type == LexType.Tend)
                        {
                            sc.PopState();
                        }
                        else
                        {
                            sc.DropState();
                        }

                        break;
                    case PredType.Gt:
                        var osn = new List<Lexema>();
                        while (magaz.NN().Count > 1 &&
                               table[magaz.NN()[magaz.NN().Count - 2].Type][magaz.NN()[magaz.NN().Count - 1].Type] ==
                               PredType.Eq)
                        {
                            osn.Add(magaz.Last());
                            magaz.RemoveAt(magaz.Count - 1);
                        }

                        while (magaz.Last().Type == LexType.Tneterm)
                        {
                            osn.Add(magaz.Last());
                            magaz.RemoveAt(magaz.Count - 1);
                        }

                        osn.Add(magaz.Last());
                        magaz.RemoveAt(magaz.Count - 1);
                        while (magaz.LastOrDefault()?.Type == LexType.Tneterm)
                        {
                            if(!magaz.Last().TTok.StartsWith("Unexpected  Tdelim") && !magaz.Last().TTok.StartsWith("Unexpected  Tfor"))
                                osn.Add(magaz.Last());
                            
                            magaz.RemoveAt(magaz.Count - 1);
                        }

                        osn.Reverse();
                        var ol = osn.Select(ll => ll.Type);
                        var ff = false;
                        if (rules.Any(rule => rule.ll.FirstOrDefault(r => r.SequenceEqual(ol)) != null))
                        {
                            if(magaz.Count != 0)
                                magaz.Add(new Lexema
                                {
                                    Type = LexType.Tneterm,
                                    Tok = $"Unexpected  {Enum.GetName(typeof(LexType),l.Type)}",
                                    TTok = $"Unexpected  {Enum.GetName(typeof(LexType),osn.First(oo => oo.Type != LexType.Tneterm).Type)}",
                                    Line = l.Line,
                                    Symbol = l.Symbol
                                });
                            ff = true;
                        }

                        if (!ff)
                        {
                            if (osn.First().Type == LexType.Tneterm)
                            {
                                throw new ParseException(osn.First().Tok, osn.First());                                
                            }
                            throw new ParseException(osn.First());
                        }

//                        Console.WriteLine(string.Join(" ", osn.Select(e=>e.Tok)));
                        if (l.Type == LexType.Tend && magaz.NN().Count == 0)
                        {
                            f = false;
                        }

                        break;
                    default:
                        throw new InvalidOperationException($"wtf: {rel}");
                }
            }
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PredType
    {
        Nil,
        Lt,
        Eq,
        Gt
    }

    public class Rule
    {
        public string n;
        public string[][] l;
        public LexType[][] ll;
    }
}