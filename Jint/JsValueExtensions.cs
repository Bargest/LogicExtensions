using System.Runtime.CompilerServices;
using Jint.Native;
using Jint.Runtime;

namespace Jint
{
    public static class JsValueExtensions
    {
        
        public static bool AsBoolean(this JsValue value)
        {
            if (value._type != InternalTypes.Boolean)
            {
                ThrowWrongTypeException(value, "boolean");
            }

            return ((JsBoolean) value)._value;
        }

        
        public static double AsNumber(this JsValue value)
        {
            if (!value.IsNumber())
            {
                ThrowWrongTypeException(value, "number");
            }

            return ((JsNumber) value)._value;
        }

        
        internal static int AsInteger(this JsValue value)
        {
            return (int) ((JsNumber) value)._value;
        }

        
        public static string AsString(this JsValue value)
        {
            if (!value.IsString())
            {
                ThrowWrongTypeException(value, "string");
            }

            return value.ToString();
        }

        private static void ThrowWrongTypeException(JsValue value, string expectedType)
        {
            ExceptionHelper.ThrowArgumentException($"Expected {expectedType} but got {value._type}");
        }
    }
}