using System;
using System.Collections.Generic;

namespace Language.Analyzer
{
    public class Env : IDisposable
    {
        private readonly List<Dictionary<string, VarInfo>> e;
        private readonly List<string> envowners;

        public Env(List<Dictionary<string, VarInfo>> e, List<string> envowners)
        {
            this.e = e;
            this.envowners = envowners;
        }

        public void Dispose()
        {
            e.RemoveAt(e.Count - 1);
            envowners.RemoveAt(envowners.Count - 1);
        }
    }
}