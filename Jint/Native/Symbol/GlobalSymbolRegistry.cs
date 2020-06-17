﻿using System.Collections.Generic;

namespace Jint.Native.Symbol
{
    public class GlobalSymbolRegistry
    {
        public static readonly JsSymbol HasInstance = new JsSymbol("Symbol.hasInstance");
        public static readonly JsSymbol IsConcatSpreadable = new JsSymbol("Symbol.isConcatSpreadable");
        public static readonly JsSymbol Iterator = new JsSymbol("Symbol.iterator");
        public static readonly JsSymbol Match = new JsSymbol("Symbol.match");
        public static readonly JsSymbol MatchAll = new JsSymbol("Symbol.matchAll");
        public static readonly JsSymbol Replace = new JsSymbol("Symbol.replace");
        public static readonly JsSymbol Search = new JsSymbol("Symbol.search");
        public static readonly JsSymbol Species = new JsSymbol("Symbol.species");
        public static readonly JsSymbol Split = new JsSymbol("Symbol.split");
        public static readonly JsSymbol ToPrimitive = new JsSymbol("Symbol.toPrimitive");
        public static readonly JsSymbol ToStringTag = new JsSymbol("Symbol.toStringTag");
        public static readonly JsSymbol Unscopables = new JsSymbol("Symbol.unscopables");

        // engine-specific created by scripts
        private Dictionary<JsValue, JsSymbol> _customSymbolLookup;

        internal bool TryGetSymbol(JsValue key, out JsSymbol symbol)
        {
            symbol = null;
            return _customSymbolLookup != null
                   && _customSymbolLookup.TryGetValue(key, out symbol);
        }

        internal void Add(JsSymbol symbol)
        {
            if (_customSymbolLookup == null)
                _customSymbolLookup = new Dictionary<JsValue, JsSymbol>();
            _customSymbolLookup[symbol._value] = symbol;
        }

        internal JsSymbol CreateSymbol(JsValue description)
        {
            return new JsSymbol(description);
        }
    }
}