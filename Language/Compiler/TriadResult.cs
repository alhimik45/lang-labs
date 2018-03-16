﻿namespace Language.Compiler
{
    public class TriadResult : IResult
    {
        public int Index { get; set; }

        public static TriadResult Of(int index)
        {
            return new TriadResult {Index = index};
        }

        public override string ToString()
        {
            return $"({Index})";
        }
    }
}