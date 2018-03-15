using System;
using System.Collections.Generic;

namespace Language.Analyzer
{
    public class Env : IDisposable
    {
        private readonly List<Dictionary<string, VarInfo>> e;
        private readonly List<string> scopes;

        public Env(List<Dictionary<string, VarInfo>> e, List<string> scopes)
        {
            this.e = e;
            this.scopes = scopes;
        }

        public void Dispose()
        {
            e.RemoveAt(e.Count - 1);
            scopes.RemoveAt(scopes.Count - 1);
        }
    }
}