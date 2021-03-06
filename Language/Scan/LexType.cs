﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Language.Scan
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LexType
    {
        Tident = 1,
        Tintd,
        Tinth,
        Tinto,
        Tchar,
        Tfor,
        TintType,
        TlongIntType,
        TcharType,
        TvoidType,
        Tmain,
        Tdelim,
        Tcomma,
        Tlparen,
        Trparen,
        Tlbracket,
        Trbracket,
        Tplus,
        Tminus,
        Tmul,
        Tdiv,
        Tmod,
        Tand,
        Tor,
        Txor,
        Tnot,
        Teq,
        Tlshift,
        Trshift,
        Tneterm,
        Tend = 666,
        Tendd = 667,
        Terr = 9000
    }}