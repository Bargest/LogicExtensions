using InternalModding.Blocks;
using Modding;
using Modding.Blocks;
using Modding.Common;
using Modding.Levels;
using Selectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Logic.Blocks;
using Logic.Script;
using System.Text.RegularExpressions;
using Logic.BlockScripts;
using Jint.Native;
using Esprima;
using Jint.Native.Function;

namespace Logic
{
    public class Logic : SingleInstance<Logic>
    {
        public bool DrawSensorDebug = false;
        Material LineMaterial;
        public Dictionary<Type, Action<BlockBehaviour, KeyInputController>> Registers;
        public Dictionary<Type, Action<BlockBehaviour>> Unregisters;
        Dictionary<Type, Type> AdditionScripts = new Dictionary<Type, Type>
        {
            { typeof(FlyingController), typeof(FlyingScript) },
            { typeof(SteeringWheel), typeof(SteeringScript) },
            { typeof(CogMotorControllerHinge), typeof(CogScript) },
            { typeof(SliderCompress), typeof(PistonScript) },
            { typeof(WaterCannonController), typeof(WaterCannonScript) }
        };

        Dictionary<Machine, MachineHandler> MachineHandlers = new Dictionary<Machine, MachineHandler>();
        public MessageType CpuInfoMessage;
        public MessageType CpuLogMessage;
        public void Awake()
        {
            // Loading
            var engine = new Jint.Engine();
            //var logic = new Interpreter();
            Registers = new Dictionary<Type, Action<BlockBehaviour, KeyInputController>>();
            Unregisters = new Dictionary<Type, Action<BlockBehaviour>>();

            ModConsole.RegisterCommand("script", args => {
                var text = string.Join(" ", args);
                //var func = logic.PrepareScript(text);
                //logic.AddExtFunc(func, "print", (ctx, x) => { ModConsole.Log(x[0]?.ToString()); return null; }, true);
                //logic.SetScript(func);
                //var res = logic.ContinueScript(1000);
                Func<JsValue, JsValue[], JsValue> printCb = (thiz, x) => {
                    ModConsole.Log(x[0]?.ToObject().ToString());
                    return x[0];
                };

                JsValue curV = null;
                Func<JsValue, JsValue[], JsValue> irqv = (thiz, x) => {
                    curV = x[0];
                    return null;
                };
                var script = new JavaScriptParser(text, Jint.Engine.DefaultParserOptions).ParseScript();
                engine.SetValue("print", printCb);
                engine.SetValue("irqv", irqv);
                engine.SetScript(script);
                engine.Executor.OnLog = (x) => ModConsole.Log(x?.ToString());
                bool cli = false;
                engine.Executor.OnNextStatement = () =>
                {
                    if (cli)
                        return;
                    cli = true;
                    try
                    {
                        if (curV != null)
                            engine.Invoke(curV);
                    }
                    finally
                    {
                        cli = false;
                    }
                };
                var res = engine.ContinueScript(1000);
                ModConsole.Log(res?.ToString());
            }, "exec script");

            ModConsole.RegisterCommand("cpuapi", args =>
            {
                foreach (var line in SingleInstance<Blocks.Api.CpuApi>.Instance.GetHelp())
                    ModConsole.Log(line);
            }, "print cpu api list");

            ModConsole.RegisterCommand("sensordbg", args =>
            {
                DrawSensorDebug = args.Length < 1 ? false : args[0] == "true";
            }, "print sensor debug points");

            CpuBlock.Create(this);
            // These creator functions find corresponding block in game prefabs
            // and replace it with inheritor
            ExtLogicGate.Create(this);
            ExtAltimeterBlock.Create(this);
            ExtSpeedometerBlock.Create(this);
            ExtAnglometerBlock.Create(this);
            ExtSensorBlock.Create(this);
            ModConsole.Log($"Logic mod Awake");

            Events.OnMachineSimulationToggle += Events_OnMachineSimulationToggle;
            LineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            LineMaterial.hideFlags = HideFlags.HideAndDontSave;
            LineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            LineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            LineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            LineMaterial.SetInt("_ZWrite", 0);

            Camera.onPostRender += DrawConnectingLines;
            Events.OnBlockInit += InitBlock;

            CpuInfoMessage = ModNetworking.CreateMessageType(new DataType[]
            {
                DataType.Block,
                DataType.ByteArray
            });

            CpuLogMessage = ModNetworking.CreateMessageType(new DataType[] 
            {
                DataType.Block,
                DataType.String
            });

            ModNetworking.Callbacks[CpuInfoMessage] += (Action<Message>)((msg) =>
            {
                Player localPlayer = Player.GetLocalPlayer();
                if (msg == null || localPlayer == null || !localPlayer.IsHost)
                    return;

                var block = msg.GetData(0) as Modding.Blocks.Block;
                if (!(block?.BlockScript is CpuBlock cpu))
                    return;

                if (block.Machine == localPlayer.Machine)
                    return; // don't read updates for MY machine!

                cpu.AfterEdit_ServerRecv((byte[])msg.GetData(1));
            });

            ModNetworking.Callbacks[CpuLogMessage] += (Action<Message>)((msg) =>
            {
                if (msg == null)
                    return;

                var block = msg.GetData(0) as Modding.Blocks.Block;
                if (!(block?.BlockScript is CpuBlock cpu))
                    return;

                cpu.LogMessage((string)msg.GetData(1));
            });
        }

        private void InitBlock(Modding.Blocks.Block block)
        {
            var machine = block.Machine?.InternalObject;
            if (machine != null && !MachineHandlers.ContainsKey(machine))
                MachineHandlers[machine] = new MachineHandler(machine);

            // Registaer patches for FPIO controlled block
            foreach (var kp in AdditionScripts)
            {
                if (kp.Key.IsInstanceOfType(block.InternalObject) && block.GameObject.GetComponent(kp.Value) == null)
                    block.GameObject.AddComponent(kp.Value);
            }
        }

        public void AddKeyRegistrer(Type moddedType, Action<BlockBehaviour, KeyInputController> register, Action<BlockBehaviour> unregister)
        {
            Registers[moddedType] = register;
            Unregisters[moddedType] = unregister;
        }

        public void PlaceAdditionScripts(BlockBehaviour block)
        {
            // Reset state for all patches for FPIO controlled blocks
            foreach (var kp in AdditionScripts)
            {
                if (kp.Key.IsInstanceOfType(block))
                    (block.GetComponent(kp.Value) as AdditionScript)?.Reset();
            }
        }

        private void Events_OnMachineSimulationToggle(PlayerMachine pmachine, bool simulating)
        {
            if (pmachine == null)
                return;

            var machine = pmachine.InternalObject;
            if (!MachineHandlers.ContainsKey(machine))
                return;

            if (simulating)
                MachineHandlers[machine].Start();
            else
                MachineHandlers[machine].Stop();
        }

        public void OnDestroy()
        {

        }

        // -----------------------------------------------
        // KeyValuePair is a replacement for Tuple
        public List<KeyValuePair<Vector3, Vector3>> IncomingLines = new List<KeyValuePair<Vector3, Vector3>>();
        public List<KeyValuePair<Vector3, Vector3>> OutgoingLines = new List<KeyValuePair<Vector3, Vector3>>();

        void DrawConnectingLines(Camera cam)
        {
            if (Camera.main != cam)
                return;
            
            LineMaterial.SetPass(0);
            foreach (var point in IncomingLines)
            {
                GL.Begin(GL.LINES);
                GL.Color(Color.green);
                GL.Vertex(point.Key);
                GL.Vertex(point.Value);
                GL.End();
            }
            foreach (var point in OutgoingLines)
            {
                GL.Begin(GL.LINES);
                GL.Color(Color.red);
                GL.Vertex(point.Key);
                GL.Vertex(point.Value);
                GL.End();
            }
        }

        void ClearConnectionLines()
        {
            if (IncomingLines.Count > 0)
                IncomingLines = new List<KeyValuePair<Vector3, Vector3>>();
            if (OutgoingLines.Count > 0)
                OutgoingLines = new List<KeyValuePair<Vector3, Vector3>>();
        }

        public void Update()
        {
            if (Camera.current == null)
                return;

            // Run garbage collectors (details are inside)
            foreach (var mh in MachineHandlers.Values)
                mh.Collect();

            // The following crazy linq code collects all connection lines, that are visible while holding `
            ClearConnectionLines();
            if (Game.IsSimulating || !Input.GetKey(KeyCode.BackQuote))
                return;

            var machine = Machine.Active();
            if (machine == null || !MachineHandlers.ContainsKey(machine))
                return;

            var selectedBlocks = machine.BuildingBlocks.Where(x => x.IsSelected).ToList();
            if (selectedBlocks.Count == 0)
                return;

            var machineHandler = MachineHandlers[machine];
            var keys = machineHandler.GetKeys(machine.BuildingBlocks).Concat(machineHandler.GetCpuKeys())
                        .GroupBy(x => x.IOMode)
                        .ToDictionary(x => x.Key, x => x.SelectMany(y => machineHandler.GetMKeys(y.key).Select(z => new { y.block, key = z }))
                            .GroupBy(y => y.key).ToDictionary(y => y.Key, y => y.Select(z => z.block).ToList())
                        );
            var inputs = keys.ContainsKey(0) ? keys[0] : new Dictionary<uint, List<BlockBehaviour>>();
            var outputs = keys.ContainsKey(1) ? keys[1] : new Dictionary<uint, List<BlockBehaviour>>();
            var fullIO = keys.ContainsKey(2) ? keys[2] : new Dictionary<uint, List<BlockBehaviour>>();

            inputs = inputs.Union(fullIO).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.SelectMany(y => y.Value).Distinct().ToList());
            outputs = outputs.Union(fullIO).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.SelectMany(y => y.Value).Distinct().ToList());

            foreach (var block in selectedBlocks)
            {
                if (block.Rigidbody == null)
                    continue;

                var cpu = machineHandler.GetCpuBlock(block);
                if (cpu != null)
                {
                    foreach (var mkey in cpu.PIO.Values)
                    {
                        foreach (var target in machineHandler.GetMKeys(mkey).Where(x => inputs.ContainsKey(x))
                                .SelectMany(x => inputs[x]).Where(x => x != block).Where(x => x.Rigidbody != null))
                            OutgoingLines.Add(new KeyValuePair<Vector3, Vector3>(block.Rigidbody.position, target.Rigidbody.position));
                        foreach (var source in machineHandler.GetMKeys(mkey).Where(x => outputs.ContainsKey(x))
                                .SelectMany(x => outputs[x]).Where(x => x != block).Where(x => x.Rigidbody != null))
                            IncomingLines.Add(new KeyValuePair<Vector3, Vector3>(source.Rigidbody.position, block.Rigidbody.position));
                    }
                }
                else
                {
                    foreach (var mkey in block.MapperTypes.Where(y => y is MKey).Select(y => y as MKey))
                    {
                        if (mkey.isEmulator) // Draw outgoing lines
                        {
                            foreach (var target in machineHandler.GetMKeys(mkey).Where(x => inputs.ContainsKey(x))
                                        .SelectMany(x => inputs[x]).Where(x => x != block).Where(x => x.Rigidbody != null))
                                OutgoingLines.Add(new KeyValuePair<Vector3, Vector3>(block.Rigidbody.position, target.Rigidbody.position));
                        }
                        else // Draw incoming lines
                        {
                            foreach (var source in machineHandler.GetMKeys(mkey).Where(x => outputs.ContainsKey(x))
                                        .SelectMany(x => outputs[x]).Where(x => x != block).Where(x => x.Rigidbody != null))
                                IncomingLines.Add(new KeyValuePair<Vector3, Vector3>(source.Rigidbody.position, block.Rigidbody.position));
                        }
                    }
                }
            }
        }

        public void Simulate()
        {

        }

        public void FixedUpdate()
        {
        }

        public override string Name => "LogicExtensions";

        public MachineHandler GetMachineHandler(BlockBehaviour block)
        {
            var machine = block.ParentMachine;
            if (MachineHandlers.ContainsKey(machine))
                return MachineHandlers[machine];
            return null;
        }
        // Gui-related stuff
        // ----------------------------------------------------------------

        CpuBlock SelectedCpu = null;
        CpuBlock PrevCpu = null;
        string sourceScript = "", editedScript = "";
        int windowId;
        Rect uiRect;
        GUIStyle textStyle;
        Vector2 scroll = Vector2.zero;
        string statusText = "";
        float lastScriptChangeTime;
        HistoryBuffer<string> editHistory;

        void InitFont()
        {
            windowId = ModUtility.GetWindowId();
            uiRect = new Rect(Screen.width - 750, Screen.height - 800, 0f, 0f);
            // load console font
            textStyle = new GUIStyle(GUI.skin.textArea);
            textStyle.wordWrap = false;
            //textStyle.richText = true;
            try
            {
                var text = GameObject.Find(@"_PERSISTENT/Canvas/ConsoleView/ConsoleViewContainer/Content/Scroll View/Viewport/Content/LogText")?.GetComponent<UnityEngine.UI.Text>();
                textStyle.font = text.font;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Load console font failed: " + e);
            }
        }


        IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> obj, int batch)
        {
            int pos = 0;
            var bat = new List<T>();
            foreach (var x in obj)
            {
                bat.Add(x);
                ++pos;
                if (pos >= batch)
                {
                    pos = 0;
                    yield return bat;
                    bat = new List<T>();
                }
            }
            if (bat.Count > 0)
                yield return bat;
        }

        Dictionary<MExtKey, string> KeyTexts = new Dictionary<MExtKey, string>();

        public void OnGUI()
        {
            if (Game.IsSimulating)
                return;

            if (textStyle == null)
                InitFont();

            var mach = Machine.Active();
            if (mach == null || !MachineHandlers.ContainsKey(mach))
                return;

            SelectedCpu = MachineHandlers[mach].GetCpus().Where(x => x.Key.IsModifying).Select(x => x.Value).FirstOrDefault();
            if (SelectedCpu != null)
            {
                if (PrevCpu != SelectedCpu)
                {
                    PrevCpu = SelectedCpu;
                    sourceScript = editedScript = SelectedCpu.Script.Value;
                    lastScriptChangeTime = Time.time;
                    editHistory = new HistoryBuffer<string>(50);
                    editHistory.Add(sourceScript);
                }
                uiRect = GUILayout.Window(windowId, uiRect, GuiFunc, "CPU edit");
            }
            else
            {
                if (KeyTexts.Count > 0)
                    KeyTexts = new Dictionary<MExtKey, string>();
            }
        }

        bool AddPIOLine(MExtKey key)
        {
            GUILayout.Label(key.DisplayName, GUILayout.ExpandWidth(false));
            if (!KeyTexts.ContainsKey(key))
                KeyTexts[key] = key.GenerateText();
            KeyTexts[key] = GUILayout.TextField(KeyTexts[key], GUILayout.Width(80));
            return GUILayout.Button("-", GUILayout.ExpandWidth(false));
        }

        void UpdateScriptStatus(Exception e)
        {
            if (e is Parser.ParserException pe)
                statusText = pe.Message + " " + pe.Pos.ToString();
            else if (e != null)
                statusText = e.Message;
            else
                statusText = "";
        }

        int lastKBFocus = -1;

        int lastEnterPos = -1;
        bool blockingKeys = false;
        void TextEditor(Event current)
        {
            GUI.SetNextControlName("codeInput");
            if (GUI.GetNameOfFocusedControl() != "codeInput")
            {
                if (blockingKeys)
                {
                    blockingKeys = false;
                    StatMaster.StopHotKeys(false);
                }
                return;
            }

            if (!blockingKeys)
            {
                blockingKeys = true;
                StatMaster.StopHotKeys(true);
            }

            //if (lastKBFocus != GUIUtility.keyboardControl)
            //    return;

            if ((current.type == EventType.KeyDown || current.type == EventType.KeyUp) && current.isKey)
            {
                var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                int start, end;
                string text;
                void PrepareForTab()
                {
                    start = Math.Min(te.cursorIndex, te.selectIndex);
                    end = Math.Max(te.cursorIndex, te.selectIndex);
                    text = te.text;
                    while (start >= 0 && text[start] != '\n')
                        --start;
                    ++start;
                    if (end > start)
                        --end;
                    while (end >= 0 && text[end] != '\n')
                        --end;
                    ++end;
                }
                if ((current.keyCode == KeyCode.Tab || current.character == '\t'))
                {
                    if (current.type == EventType.KeyUp)
                    {
                        if (!current.shift)
                        {
                            if (!te.hasSelection)
                            {
                                for (var i = 0; i < 4; i++)
                                    te.Insert(' ');

                                editedScript = te.text;
                            }
                            else
                            {
                                PrepareForTab();
                                var pos = start;
                                while (pos <= end)
                                {
                                    text = text.Insert(pos, "    ");
                                    end += 4;
                                    if (pos == start)
                                    {
                                        if (te.cursorIndex < te.selectIndex)
                                            te.cursorIndex += 4;
                                        else
                                            te.selectIndex += 4;
                                    }
                                    if (te.cursorIndex > te.selectIndex)
                                        te.cursorIndex += 4;
                                    else
                                        te.selectIndex += 4;
                                    while (pos < text.Length && text[pos] != '\n')
                                        ++pos;
                                    ++pos;
                                }
                                editedScript = text;
                            }
                        }
                        else
                        {
                            PrepareForTab();
                            if (te.cursorIndex < te.selectIndex)
                                te.cursorIndex = start;
                            else
                                te.selectIndex = start;
                            var pos = start;
                            while (pos <= end)
                            {
                                int i;
                                for (i = 0; i < 4; ++i)
                                {
                                    if (text[pos] != ' ')
                                        break;
                                    text = text.Remove(pos, 1);
                                }
                                end -= i;
                                if (pos != end)
                                {
                                    if (te.cursorIndex > te.selectIndex)
                                        te.cursorIndex -= i;
                                    else
                                        te.selectIndex -= i;
                                }
                                while (pos < text.Length && text[pos] != '\n')
                                    ++pos;
                                ++pos;
                            }
                            editedScript = text;
                        }
                        editHistory.Add(editedScript);
                        UpdateScriptStatus(SelectedCpu.CheckScript(editedScript));
                    }
                    current.Use();
                }
                else if (current.keyCode == KeyCode.Return)
                {
                    if (current.type == EventType.keyDown)
                    {
                        lastEnterPos = Math.Min(te.cursorIndex, te.selectIndex) - 1;
                    }
                    else if (current.type == EventType.KeyUp && lastEnterPos >= 0)
                    {
                        text = te.text;
                        start = lastEnterPos;
                        lastEnterPos = -1;
                        while (start >= 0 && text[start] != '\n')
                            --start;
                        ++start;
                        int pos = start;
                        while (pos < text.Length && text[pos] == ' ')
                            ++pos;
                        var spaceCount = pos - start;
                        var spacing = text.Substring(start, spaceCount);
                        text = text.Insert(te.cursorIndex, spacing);
                        te.cursorIndex += spacing.Length;
                        te.selectIndex = te.cursorIndex;
                        editedScript = text;
                        editHistory.Add(editedScript);
                        UpdateScriptStatus(SelectedCpu.CheckScript(editedScript));
                    }
                    current.Use();
                }
                else if (current.keyCode == KeyCode.Z && current.control && current.type == EventType.KeyDown)
                {
                    var prev = editHistory.Back();
                    if (prev != null && editedScript != prev)
                    {
                        var len = Math.Min(editedScript.Length, prev.Length);
                        for (int i = 0; i < len; ++i)
                        {
                            if (editedScript[i] != prev[i])
                            {
                                te.selectIndex = te.cursorIndex = i;
                                break;
                            }
                        }
                        editedScript = prev;
                        UpdateScriptStatus(SelectedCpu.CheckScript(editedScript));
                    }
                    current.Use();
                }
                else if (current.keyCode == KeyCode.Y && current.control && current.type == EventType.KeyDown)
                {
                    var prev = editHistory.Forward();
                    if (prev != null)
                    {
                        var len = Math.Min(editedScript.Length, prev.Length);
                        for (int i = 0; i < len; ++i)
                        {
                            if (editedScript[i] != prev[i])
                            {
                                te.selectIndex = te.cursorIndex = i;
                                break;
                            }
                        }
                        editedScript = prev;
                        UpdateScriptStatus(SelectedCpu.CheckScript(editedScript));
                    }
                    current.Use();
                }
            }
        }

        void GuiFunc(int id)
        {
            if (SelectedCpu == null)
                return;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("PIO", GUILayout.ExpandWidth(false));
            bool addPio = GUILayout.Button("+", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (addPio && SelectedCpu.PIO.Count < 100)
                SelectedCpu.AddPIO();

            List<MExtKey> toDelete = new List<MExtKey>();
            foreach (var line in Batch(SelectedCpu.PIO.OrderBy(x => x.Key).Select(x => x.Value), 4))
            {
                GUILayout.BeginHorizontal();
                foreach (var key in line)
                    if (AddPIOLine(key))
                        toDelete.Add(key);
                GUILayout.EndHorizontal();
            }

            foreach (var d in toDelete)
                KeyTexts.Remove(d);
            SelectedCpu.RemovePIO(toDelete);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(700), GUILayout.Height(600));
            // script could have changed because of Undo, so we update it with Script.Value
            if (sourceScript != SelectedCpu.Script.Value)
            {
                sourceScript = editedScript = SelectedCpu.Script.Value;
                editHistory.Add(editedScript);
            }

            var current = Event.current;
            TextEditor(current);
            editedScript = GUILayout.TextArea(editedScript, textStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (GUI.GetNameOfFocusedControl() == "codeInput" && (current.type == EventType.KeyDown || current.type == EventType.KeyUp))
                lastKBFocus = GUIUtility.keyboardControl;

            GUILayout.EndScrollView();
            if (editHistory.Top() != editedScript && lastScriptChangeTime + 0.5f < Time.time)
            {
                editHistory.Add(editedScript);
                lastScriptChangeTime = Time.time;
                var e = SelectedCpu.CheckScript(editedScript);
                UpdateScriptStatus(e);
            }

            if (GUILayout.Button("Save"))
            {
                if (editHistory.Top() != editedScript)
                    editHistory.Add(editedScript);

                lastScriptChangeTime = Time.time;
                foreach (var k in KeyTexts.Keys.ToArray())
                {
                    var textChanged = k.GenerateText() != KeyTexts[k];
                    k.Text_TextChanged(KeyTexts[k]);
                    KeyTexts[k] = k.GenerateText();
                    if (textChanged)
                        SelectedCpu.AfterEdit(k);
                }

                var e = SelectedCpu.ApplyScript(editedScript);
                if (e != null)
                {
                    UpdateScriptStatus(e);
                }
                else
                {
                    statusText = "Saved";
                }
            }
            GUILayout.Label(statusText);
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

    }
}
