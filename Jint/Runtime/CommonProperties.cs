using Jint.Native;

namespace Jint.Runtime
{
    public static class CommonProperties
    {
        public static readonly JsString Arguments = new JsString("arguments");
        public static readonly JsString Caller = new JsString("caller");
        public static readonly JsString Callee = new JsString("callee");
        public static readonly JsString Constructor = new JsString("constructor");
        public static readonly JsString Eval = new JsString("eval");
        public static readonly JsString Infinity = new JsString("Infinity");
        public static readonly JsString Length = new JsString("length");
        public static readonly JsString Name = new JsString("name");
        public static readonly JsString Prototype = new JsString("prototype");
        public static readonly JsString Size = new JsString("size");
        public static readonly JsString Next = new JsString("next");
        public static readonly JsString Done = new JsString("done");
        public static readonly JsString Value = new JsString("value");
        public static readonly JsString Return = new JsString("return");
        public static readonly JsString Set = new JsString("set");
        public static readonly JsString Get = new JsString("get");
        public static readonly JsString Writable = new JsString("writable");
        public static readonly JsString Enumerable = new JsString("enumerable");
        public static readonly JsString Configurable = new JsString("configurable");
    }
}