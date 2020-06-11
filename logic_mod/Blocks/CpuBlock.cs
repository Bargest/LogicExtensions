using Besiege;
using Logic.Script;
using Modding;
using Modding.Common;
using Modding.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    public class CpuBlock : Modding.BlockScript
    {
        public class ScriptText : MCustom<string>
        {
            public ScriptText(string displayName, string key) : base(displayName, key, "")
            {
            }

            public override string DeSerializeValue(XData scriptData)
            {
                return (scriptData as XString)?.Value ?? "";
            }

            public override XData SerializeValue(string value)
            {
                return new XString("bmt-" + Key, value);
            }
        }

        public override bool EmulatesAnyKeys => true;
        const int MaxGas = 500;
        MachineHandler machineHandler;
        public Logic ModContext;
        public Interpreter Interp = new Interpreter();
        MSlider Gas = null;
        public ScriptText Script;

        long timeoutId;
        Dictionary<long, bool> PrevState = null;
        Dictionary<long, float> TriggerValues = null;
        Dictionary<long, int> TriggetMode = null;
        Dictionary<long, float> Timeouts;
        public Dictionary<long, MExtKey> PIO = new Dictionary<long, MExtKey>();

        static Texture2D texture2D2 = null;
        static ModTexture glowTex = null;

        bool burnStarted = false;
        bool burnFinished = false;

        float lastError = 0;

        public override void SafeAwake()
        {
            ModContext = SingleInstance<Logic>.Instance;
            Gas = AddSlider("Gas", "gas", 100.0f, 1.0f, MaxGas);
            Script = new ScriptText("script", "script");
            BlockBehaviour.AddCustom(Script);
            // DO NOT CALL AddPIO here, because it sends invalid machine state in multiverse after simulation start
            machineHandler = ModContext.GetMachineHandler(BlockBehaviour);
            machineHandler.AddCpuBlock(this);
        }

        void InitRender()
        {
            if (glowTex == null)
                glowTex = ModResource.GetTexture("glow");

            if (glowTex != null && glowTex.Loaded && !glowTex.HasError && texture2D2 == null)
            {
                texture2D2 = glowTex.Texture;
            }

            if (texture2D2 != null)
            {
                var renderer = base.transform.FindChild("Vis").gameObject.GetComponentInChildren<Renderer>();
                renderer.material.SetTexture("_EmissMap", texture2D2);
            }
        }

        int UnusedPioId()
        {
            for (int i = 0; true; ++i)
                if (!PIO.ContainsKey(i))
                    return i;
        }

        public void AfterEdit(MapperType mapper)
        {
            // We cannot use undo system for PIOs, because they are dymanic
            // trying to save them will result in exception on 'undo' while
            // getting mapper type inside OnEditField
            if (mapper is MCustom<string>)
                BlockMapper.OnEditField(BlockBehaviour, mapper);
            else
            {
                Player localPlayer = Player.GetLocalPlayer();
                if (localPlayer == null || localPlayer.IsHost)
                    return;

                var tempdata = new XDataHolder();
                BlockBehaviour.OnSave(tempdata);
                tempdata.Encode(out byte[] dataBytes);

                var message = ModContext.CpuInfoMessage.CreateMessage(
                    this.BlockBehaviour,
                    dataBytes
                );
                ModNetworking.SendToHost(message);
            }
        }

        public void AfterEdit_ServerRecv(byte[] data)
        {
            var xholder = new XDataHolder();
            xholder.Decode(data, 0);
            BlockBehaviour.OnLoad(xholder, CopyMode.All);
            //BlockBehaviour.ParentMachine.UndoSystem.EditBlockField(BlockBehaviour.Guid, xdata, xdata);
        }

        public MExtKey AddPIO()
        {
            var newId = UnusedPioId();
            var name = "pio" + newId.ToString("00");
            var key = new MExtKey(name, name, KeyCode.O, BlockBehaviour, true);
            PIO[newId] = key;
            BlockBehaviour.KeyList.Add(key);

            AfterEdit(key);
            return key;
        }

        public void RemovePIO(List<MExtKey> keys)
        {
            foreach (var k in keys.Select(x => PIO.Where(y => y.Value == x).Select(y => (int?)y.Key).FirstOrDefault()).Where(x => x != null))
                PIO.Remove(k.Value);
            foreach (var k in keys)
            {
                BlockBehaviour.KeyList.Remove(k);
                AfterEdit(k);
            }
        }

        public Exception ApplyScript(string text)
        {
            var e = SetScript(text);
            if (e != null)
                return e;

            AfterEdit(Script);
            return null;
        }


        public override void OnSave(XDataHolder data)
        {
            base.OnSave(data);
            data.Write(Script.Serialize());
            foreach (var key in PIO.Values)
                data.Write(key.Serialize());
        }

        public override void OnLoad(XDataHolder data)
        {
            base.OnLoad(data);

            Script.DeSerialize(data.Read("bmt-" + Script.Key));
            CheckScript(Script.Value);

            var newPio = data.ReadAll().Where(x => x.Key.StartsWith("bmt-pio")).Select(x =>
            {
                var key = new MExtKey(x.Key.Substring(4), x.Key.Substring(4), KeyCode.None, BlockBehaviour, true);
                key.DeSerialize(x);
                if (!long.TryParse(key.Key.Replace("pio", ""), out long id))
                    return null;
                return new KeyValuePair<long, MExtKey>?(new KeyValuePair<long, MExtKey>(id, key));
            }).Where(x => x != null).ToDictionary(x => x.Value.Key, x => x.Value.Value);
            // merge into old pio because we don't want to replace MExtKey every time
            BlockBehaviour.KeyList.Clear();
            foreach (var kp in newPio)
            {
                if (PIO.ContainsKey(kp.Key))
                {
                    PIO[kp.Key].CopyFrom(kp.Value);
                    BlockBehaviour.KeyList.Add(PIO[kp.Key]);
                }
                else
                {
                    PIO[kp.Key] = kp.Value;
                    BlockBehaviour.KeyList.Add(kp.Value);
                }
            }
        }

        public Exception CheckScript(string text)
        {
            try
            {
                var lexer = new Lexer(text);
                var parser = new Parser(lexer);
                var root = parser.Parse();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public Exception SetScript(string text)
        {
            var e = CheckScript(text);
            if (e != null)
                return e;
            Script.Value = text;
            return null;
        }

        void AfterInterrupt(long irqId)
        {
            // cleanup fired timeouts
            if (irqId < 0)
                Interp.RemoveIrqHandler(irqId);
        }

        void OnCoreException(long irqId, object exc)
        {
            Debug.Log("Unhandled exception " + (irqId >= 0 ? $" in irq {irqId}" : "") + ": " + exc);
            lastError = Time.time;
            //BlockBehaviour.fireTag.Ignite(); // lol
        }

        bool TryGetFloat(object arg, out float value)
        {
            return Block.TryGetFloat(arg, out value);
        }
        bool TryGetLong(object arg, out long value)
        {
            return Block.TryGetLong(arg, out value);
        }

        int printCount;
        float lastPrint;

        public void LogMessage(string message)
        {
            ModConsole.Log(message);
        }

        string PrintObject(object x, int level)
        {
            if (x is Dictionary<string, object> d)
            {
                if (level >= 1)
                    return "<object>";

                return "{\n  " + $"{string.Join(",\n  ", d.Select(y => $"{y.Key}: {PrintObject(y.Value, level + 1)}").ToArray())}" + "\n}";
            }
            else if (x is List<object> a)
            {
                if (level >= 1)
                    return "<array>";
                return "[\n  " + $"{string.Join(",\n  ", a.Select(y => $"{PrintObject(y, level + 1)}").ToArray())}" + "\n]";
            }
            else
            {
                return x?.ToString() ?? "null";
            }
        }

        public object Print(VarCtx ctx, object[] x)
        {
            if (x.Length == 0)
                return null;

            if (Time.time - lastPrint < 1)
            {
                if (printCount >= 50)
                {
                    if (printCount == 50)
                    {
                        ++printCount;
                        Debug.LogWarning($"Warning: cpu is emitting too may log lines, cool down");
                    }
                    return null;
                }
            }
            else
            {
                printCount = 0;
                lastPrint = Time.time;
            }
            ++printCount;
            var logLine = PrintObject(x[0], 0);
            var blockPlayer = BlockBehaviour.ParentMachine == null ? null : Player.From(BlockBehaviour.ParentMachine.PlayerID);
            if (blockPlayer == null || blockPlayer.IsHost)
            {
                LogMessage(logLine);
            }
            else
            {
                // send log to client's machine
                var message = ModContext.CpuLogMessage.CreateMessage(
                    BlockBehaviour,
                    logLine
                );
                ModNetworking.SendTo(blockPlayer, message);
            }
            return null;
        }

        public object Typeof(VarCtx ctx, object[] x)
        {
            if (x.Length == 0)
                return null;
            if (x[0] is FuncCtx)
                return "function";
            if (x[0] is long)
                return "int";
            if (x[0] is float)
                return "float";
            if (x[0] == Block.Undefined)
                return "undefined";
            return "object";
        }

        public object Int(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetLong(x[0], out long v))
                throw new Exception("Invalid value");
            return v;
        }
        public object Float(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return v;
        }
        public object Str(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid value");
            return x[0]?.ToString();
        }
        public object Sin(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Sin(v);
        }
        public object Cos(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Cos(v);
        }
        public object Tan(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Tan(v);
        }

        public object Asin(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Asin(v);
        }
        public object Acos(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Acos(v);
        }
        public object Atan(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Atan(v);
        }
        public object Log(VarCtx ctx, object[] x)
        {
            if (x.Length < 2 || !TryGetFloat(x[0], out float v) || !TryGetFloat(x[1], out float newBase))
                throw new Exception("Invalid value");
            return (float)Math.Log(v, newBase);
        }

        public object Keys(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                throw new Exception("Invalid object");
            if (x[0] is object[] objArr)
            {
                var arr = new object[objArr.Length];
                for (long i = 0; i < arr.Length; ++i)
                    arr[i] = i;
                return arr;
            }
            if (x[0] is Dictionary<string, object> dict)
            {
                return dict.Keys.Select(y => (object)y).ToList();
            }
            return new object[0];
        }

        public object Abs(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Abs(v);
        }
        public object Pow(VarCtx ctx, object[] x)
        {
            if (x.Length < 2 || !TryGetFloat(x[0], out float v) || !TryGetFloat(x[1], out float y))
                throw new Exception("Invalid value");
            return (float)Math.Pow(v, y);
        }
        public object Sqrt(VarCtx ctx, object[] x)
        {
            if (x.Length < 1 || !TryGetFloat(x[0], out float v))
                throw new Exception("Invalid value");
            return (float)Math.Sqrt(v);
        }


        public void Cli(VarCtx ctx, object[] x)
        {
            Interp.Cli();
        }

        public void Sti(VarCtx ctx, object[] x)
        {
            Interp.Sti();
        }

        public void Asleep(VarCtx ctx, object[] x)
        {
            Interp.PauseThread(null, null);
        }

        public object Irqv(VarCtx ctx, object[] x)
        {
            if (x.Length < 2)
                return 0;
            if (!TryGetLong(x[0], out long irq) || irq < 0)
                return 0;

            if (!(x[1] is FuncCtx fn))
            {
                if (x[1] != null)
                    return 0;

                Interp.RemoveIrqHandler(irq);
                return 1;
            }

            Interp.RegisterIrqHandler(irq, fn);
            if (PIO.ContainsKey(irq))
            {
                TriggerValues[irq] = 0;
                TriggetMode[irq] = 0;
                if (x.Length > 2 && TryGetFloat(x[2], out float level))
                {
                    TriggerValues[irq] = level;
                    if (x.Length > 3 && TryGetLong(x[3], out long front) && front >= 0 && front <= 2)
                        TriggetMode[irq] = (int)front;
                }
            }
            return 1;
        }

        public object Irq(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                return 0;
            if (!TryGetLong(x[0], out long irq) || irq < 0)
                return 0;
            Interp.AddInterrupt(irq, x.Skip(1).ToArray());
            return 1;
        }

        public object In(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                return -1;
            if (!TryGetLong(x[0], out long pio) || !PIO.ContainsKey(pio))
                return -1;

            var r = machineHandler.ReadValue(PIO[(int)pio]);
            return r;
        }

        public object ReadSensor(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                return null;
            if (!TryGetLong(x[0], out long pio) || !PIO.ContainsKey(pio))
                return null;

            var emu = machineHandler.GetExtEmulator(PIO[(int)pio]);
            var parent = emu?.Parent;
            if (parent == null)
                return null;
            if (parent is ExtSensorBlock sensor)
                return sensor.GetTargetObject();
            if (parent is ExtAltimeterBlock alt)
                return alt.GetPos();
            if (parent is ExtSpeedometerBlock speed)
                return speed.GetV();
            if (parent is ExtAnglometerBlock ang)
                return ang.GetAng();
            return null;
        }

        public object Out(VarCtx ctx, object[] x)
        {
            if (x.Length < 2)
                return 0;
            if (!TryGetLong(x[0], out long pio) || !PIO.ContainsKey(pio))
                return 0;
            if (!TryGetFloat(x[1], out float outv))
                return 0;
            
            outv = Mathf.Clamp01(outv);
            if (float.IsNaN(outv))
            {
                Print(ctx, new object[] { "Warning: output NaN to " + pio + " failed" });
                return 0;
            }   
            PIO[(int)pio].SetOutValue(BlockBehaviour, outv);
            return 1;
        }
        public object SetTimeout(VarCtx ctx, object[] x)
        {
            if (x.Length < 2 || !(x[1] is FuncCtx callback))
                return 0;
            if (!TryGetFloat(x[0], out float seconds))
                return 0;

            //Debug.Log($"setTimeout({seconds}, {callback.FunctionProto.GetName()})");
            var newTimeout = timeoutId++;
            Timeouts[newTimeout] = Time.time + seconds;
            Interp.RegisterIrqHandler(-newTimeout, callback);
            return newTimeout;
        }

        public object ClearTimeout(VarCtx ctx, object[] x)
        {
            if (x.Length < 1)
                return 0;
            if (!TryGetLong(x[0], out long timerId) || timerId <= 0)
                return 0;
            Timeouts.Remove(timerId);
            Interp.RemoveIrqHandler(-timerId);
            return 1;
        }

        public override void OnSimulateStart()
        {
            printCount = 0;
            lastPrint = Time.time;
            lastError = 0;
            PrevState = PIO.ToDictionary(x => x.Key, x => false);
            TriggerValues = PIO.ToDictionary(x => x.Key, x => 0.0f);
            TriggetMode = PIO.ToDictionary(x => x.Key, x => 0);
            timeoutId = 1;
            Timeouts = new Dictionary<long, float>();
            var func = Interp.PrepareScript(Script.Value);
            Interp.SetUnhandledExceptionHandler(OnCoreException);
            Interp.SetInterruptCompleteHandler(AfterInterrupt);

            SingleInstance<CpuApi>.Instance.Attach(this, func);
            Interp.SetScript(func);
        }

        public override void SimulateUpdateAlways()
        {
            InitRender();
            if (burnStarted || burnFinished)
                return;

            var secondsFromError = Time.time - lastError;
            const double errMaxTime = 1;
            const double errDecreaseTime = 4;

            double factor;
            if (secondsFromError > errMaxTime + errDecreaseTime)
                factor = 0;
            else if (secondsFromError < errMaxTime)
                factor = 1;
            else
                factor = (errDecreaseTime - (secondsFromError - errMaxTime)) / errDecreaseTime;
            
            VisualController.SetGlowLevel(new Color(2.0f, 1.5f, 0) * (float)factor, 1.0f);
        }

        public override void SimulateFixedUpdateHost()
        {
            if (Time.timeScale == 0 || !IsSimulating)
                return;

            if (!burnStarted)
            {
                if (BlockBehaviour.fireTag.burning)
                    burnStarted = true;
            }
            else
            {
                if (!burnFinished && !BlockBehaviour.fireTag.burning)
                {
                    burnFinished = true;
                    foreach (var btn in PIO.Values)
                        btn.SetOutValue(BlockBehaviour, 0);
                }
            }

            if (!burnFinished)
            {
                // fire interrupts
                foreach (var i in PIO.Keys)
                {
                    var state = machineHandler.ReadValue(PIO[i]) > TriggerValues[i];
                    if (state != PrevState[i])
                    {
                        PrevState[i] = state;
                        var correctFront = true;
                        if (TriggetMode.ContainsKey(i))
                        {
                            correctFront = TriggetMode[i] == 0
                                || (TriggetMode[i] == 1 && state)
                                || (TriggetMode[i] == 2 && !state);
                        }
                        if (correctFront)
                            Interp.AddInterrupt(i, new object[] { TriggerValues[i], state ? (long) 1 : 0 });
                    }
                }
                // fire timeouts
                // timeout is implemented as interrupt with negative id
                var fireTimeouts = Timeouts.Where(x => x.Value <= Time.time).Select(x => x.Key).ToList();
                foreach (var id in fireTimeouts)
                {
                    Interp.AddInterrupt(-id, null);
                    Timeouts.Remove(id);
                }
                var gas = (int)Gas.Value;
                if (gas > MaxGas)
                    gas = MaxGas;
                Interp.ContinueScript(gas);
            }
        }

        public override void OnSimulateStop()
        {
        }

        KeyInputController InputController = null;
        private void RegisterCpu(KeyInputController input)
        {
            InputController = input;
            foreach (var key in PIO.Values)
            {
                foreach (var kc in key.ResolveKeys().Where(x => x <= MExtKey.MaxKey).Select(x => (KeyCode)x))
                {
                    input.AddMKey(BlockBehaviour, key, kc);
                    input.Add(kc);
                }
                key.SetInputController(input);
                key.SetKeycodes(input, machineHandler.IsAnyEmulating);
                machineHandler.AddExtKeyEmulator(key);
                machineHandler.AddKey(input, BlockBehaviour, key);
            }
        }
        private void UnregisterCpu()
        {
            InputController = null;
            foreach (var key in PIO.Values)
                key.ResetKeycodes();
        }

        public static void Create(Logic logic)
        {
            logic.AddKeyRegistrer(typeof(CpuBlock), (b, k) => logic.GetMachineHandler(b).GetCpuBlock(b).RegisterCpu(k), (b) => logic.GetMachineHandler(b).GetCpuBlock(b).UnregisterCpu());
        }
    }
}
