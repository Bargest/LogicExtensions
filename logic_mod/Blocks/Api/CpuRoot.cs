using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic.Blocks.Api
{
    public class CpuRoot : ApiList
    {
        public override List<CpuApiFunc> Api => new List<CpuApiFunc>
        {
            new CpuApiFunc("print", true, "print message to console",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "msg", new CpuApiFunc.ArgInfo("object", "message to print") } },
                (c) => c.Print
            ),
            new CpuApiFunc("typeof", true, "get object type",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "obj", new CpuApiFunc.ArgInfo("object", "object to get type") } },
                (c) => c.Typeof
            ),
            new CpuApiFunc("int", true, "convert to integer",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                (c) => c.Int
            ),
            new CpuApiFunc("float", true, "convert to floating point number",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                (c) => c.Float
            ),
            new CpuApiFunc("str", true, "convert to string",
                new Dictionary<string, CpuApiFunc.ArgInfo>{ { "value", new CpuApiFunc.ArgInfo("object", "value to convert") } },
                (c) => c.Str
            ),
            new CpuApiFunc("cli", false, "clear interrupts",
                new Dictionary<string, CpuApiFunc.ArgInfo>{},
                (c) => c.Cli
            ),
            new CpuApiFunc("sti", false, "set interrupts",
                new Dictionary<string, CpuApiFunc.ArgInfo>{},
                (c) => c.Sti
            ),
            new CpuApiFunc("asleep", false, "pause execution until next frame",
                new Dictionary<string, CpuApiFunc.ArgInfo>{},
                (c) => c.Asleep
            ),
            new CpuApiFunc("irqv", true, "set IRQ vector",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") } ,
                    { "handler", new CpuApiFunc.ArgInfo("func", "irq callback as function (id, treshold, front)") },
                    { "threshold", new CpuApiFunc.ArgInfo("float", "(default=0) pio threshold in the range of [0..1)") },
                    { "mode", new CpuApiFunc.ArgInfo("int", "(default=0) 0 - trigger on each front, 1 - uprising, 2 - downfalling ") }
                },
                (c) => c.Irqv
            ),
            new CpuApiFunc("irq", false, "trigger IRQ vector",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "pio", new CpuApiFunc.ArgInfo("int", "PIO id (= irq id)") } ,
                },
                (c) => c.Irq
            ),
            new CpuApiFunc("readSensor", false, "read sensor (emulator) measuers",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "pio", new CpuApiFunc.ArgInfo("int", "PIO id, that sensor is emulating") } ,
                },
                (c) => c.ReadSensor
            ),
            new CpuApiFunc("in", false, "read value from PIO",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") },
                },
                (c) => c.In
            ),
            new CpuApiFunc("out", false, "write value to PIO",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "pio", new CpuApiFunc.ArgInfo("int", "PIO id") },
                    { "value", new CpuApiFunc.ArgInfo("float", "value [0..1] to write") },
                },
                (c) => c.Out
            ),
            new CpuApiFunc("setTimeout", true, "set callback that will be executed after timeout",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "timeout", new CpuApiFunc.ArgInfo("float", "seconds until triggering timeout") },
                    { "callback", new CpuApiFunc.ArgInfo("func", "function that will be executed") },
                },
                (c) => c.SetTimeout
            ),
            new CpuApiFunc("clearTimeout", true, "cancel timeout, set by setTimeout",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "timeoutId", new CpuApiFunc.ArgInfo("int", "timeout id, returned by setTimeout") },
                },
                (c) => c.ClearTimeout
            ),
            /*
            // Debug!
            new CpuApiFunc("callCallback", false, "just call specified callback (debuging stuff)",
                new Dictionary<string, CpuApiFunc.ArgInfo>{
                    { "callback", new CpuApiFunc.ArgInfo("func", "callback") },
                },
                (c) => c.CallCallback
            ),
            */
        };
    }
}
