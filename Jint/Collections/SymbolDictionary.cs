using Jint.Native;
using Jint.Runtime.Descriptors;

namespace Jint.Collections
{
    public sealed class SymbolDictionary : DictionarySlim<JsSymbol, PropertyDescriptor>
    {
        public SymbolDictionary()
        {
        }

        public SymbolDictionary(int capacity) : base(capacity)
        {
        }
    }
}