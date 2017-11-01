using System;
using System.Collections.Generic;

namespace Language.Analyzer
{
    public class Env : IDisposable
    {
        private readonly List<Dictionary<string, VarInfo>> e;

        public Env(List<Dictionary<string, VarInfo>> e)
        {
            this.e = e;
        }

        public void Dispose()
        {
            e.RemoveAt(e.Count - 1);
        }
    }
}