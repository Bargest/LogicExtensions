using Jint.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Blocks.Api
{
    public class CpuRoot : ApiDescription
    {
        public override List<CpuApiProperty> InstanceFields => new List<CpuApiProperty>();
        public override List<CpuApiProperty> StaticFields => new List<CpuApiProperty>
        {
            new CpuApiFunc("print", true, "print message to console",
                new Dictionary<string, ArgInfo>{ { "msg", new ArgInfo("object", "message to print") } },
                (c, t, a) => c.Print(t, a)
            ),
            new CpuApiFunc("int", true, "convert to integer",
                new Dictionary<string, ArgInfo>{ { "value", new ArgInfo("object", "value to convert") } },
                Int
            ),
            new CpuApiFunc("float", true, "convert to floating point number",
                new Dictionary<string, ArgInfo>{ { "value", new ArgInfo("object", "value to convert") } },
                Float
            ),
            new CpuApiFunc("str", true, "convert to string",
                new Dictionary<string, ArgInfo>{ { "value", new ArgInfo("object", "value to convert") } },
                Str
            ),
            new CpuApiFunc("cli", false, "clear interrupts",
                new Dictionary<string, ArgInfo>{},
                (c, t, a) => c.Cli(t, a)
            ),
            new CpuApiFunc("sti", false, "set interrupts",
                new Dictionary<string, ArgInfo>{},
                (c, t, a) => c.Sti(t, a)
            ),
            new CpuApiFunc("asleep", false, "pause execution until next frame",
                new Dictionary<string, ArgInfo>{},
                (c, t, a) => c.Asleep(t, a)
            ),
            new CpuApiFunc("irqv", true, "set IRQ vector",
                new Dictionary<string, ArgInfo>{
                    { "pio", new ArgInfo("int", "PIO id") } ,
                    { "handler", new ArgInfo("func", "irq callback as function (id, treshold, front)") },
                    { "threshold", new ArgInfo("float", "(default=0) pio threshold in the range of [0..1)") },
                    { "mode", new ArgInfo("int", "(default=0) 0 - trigger on each front, 1 - uprising, 2 - downfalling ") }
                },
                (c, t, a) => c.Irqv(t, a)
            ),
            new CpuApiFunc("irq", false, "trigger IRQ vector",
                new Dictionary<string, ArgInfo>{
                    { "pio", new ArgInfo("int", "PIO id (= irq id)") } ,
                },
                (c, t, a) => c.Irq(t, a)
            ),
            new CpuApiFunc("readSensor", false, "read sensor (emulator) measuers",
                new Dictionary<string, ArgInfo>{
                    { "pio", new ArgInfo("int", "PIO id, that sensor is emulating") } ,
                },
                (c, t, a) => c.ReadSensor(t, a)
            ),
            new CpuApiFunc("in", false, "read value from PIO",
                new Dictionary<string, ArgInfo>{
                    { "pio", new ArgInfo("int", "PIO id") },
                },
                (c, t, a) => c.In(t, a)
            ),
            new CpuApiFunc("out", false, "write value to PIO",
                new Dictionary<string, ArgInfo>{
                    { "pio", new ArgInfo("int", "PIO id") },
                    { "value", new ArgInfo("float", "value [0..1] to write") },
                },
                (c, t, a) => c.Out(t, a)
            ),
            new CpuApiFunc("setTimeout", true, "set callback that will be executed after timeout",
                new Dictionary<string, ArgInfo>{
                    { "timeout", new ArgInfo("float", "seconds until triggering timeout") },
                    { "callback", new ArgInfo("func", "function that will be executed") },
                },
                (c, t, a) => c.SetTimeout(t, a)
            ),
            new CpuApiFunc("clearTimeout", true, "cancel timeout, set by setTimeout",
                new Dictionary<string, ArgInfo>{
                    { "timeoutId", new ArgInfo("int", "timeout id, returned by setTimeout") },
                },
                (c, t, a) => c.ClearTimeout(t, a)
            ),
            /*
            // Debug!
            new CpuApiFunc("callCallback", false, "just call specified callback (debuging stuff)",
                new Dictionary<string, ArgInfo>{
                    { "callback", new ArgInfo("func", "callback") },
                },
                (c) => c.CallCallback
            ),
            */
        };

        // Legacy APIs
        public JsValue Int(CpuBlock c, JsValue ctx, JsValue[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetLong(x[0], out long v))
                throw new Exception("Invalid value");
            return v;
        }
        public JsValue Float(CpuBlock c, JsValue ctx, JsValue[] x)
        {
            if (x.Length < 1 || !BlockUtils.TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return v;
        }
        public JsValue Str(CpuBlock c, JsValue ctx, JsValue[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");
            return x[0]?.ToString();
        }
    }
}
