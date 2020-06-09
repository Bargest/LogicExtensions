using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    class ExtAltimeterBlock : AltimeterBlock
    {
        public Logic ModContext;
        MachineHandler machineHandler;

        public ExtAltimeterBlock() { }

        public void LoadFrom(Logic modContext, AltimeterBlock baseObject, GameObject go)
        {
            ModContext = modContext;
            // BasicInfo
            Prefab = baseObject.Prefab;
            infoType = baseObject.infoType;
            Rigidbody = baseObject.Rigidbody;
            noRigidbody = baseObject.noRigidbody;
            MeshRenderer = baseObject.MeshRenderer;
            noRigidbody = baseObject.noRigidbody;
            stripped = baseObject.stripped;
            NetBlock = baseObject.NetBlock;
            _hasParentMachine = baseObject._hasParentMachine;
            _parentMachine = baseObject._parentMachine;
            SimPhysics = baseObject.SimPhysics;
            isSimulating = baseObject.isSimulating;
            IsMagnetic = baseObject.IsMagnetic;
            isDestroyed = baseObject.isDestroyed;
            ShelterAmount = baseObject.ShelterAmount;
            offsetDir = baseObject.offsetDir;

            // SaveableDataHolder

            // BlockBehavior
            VisualController = baseObject.VisualController;
            VisualController.Block = this;
            BreakOnImpact = baseObject.BreakOnImpact;
            BlockHealth = baseObject.BlockHealth;
            myBounds = baseObject.myBounds;
            fireTag = baseObject.fireTag;
            iceTag = baseObject.iceTag;
            blockJoint = baseObject.blockJoint;
            DestroyOnClient = baseObject.DestroyOnClient;
            DestroyOnSimulate = baseObject.DestroyOnSimulate;
            CurrentWindController = baseObject.CurrentWindController;

            var componentsInChildren = baseObject.GetComponentsInChildren<TriggerSetJoint>();
            for (int i = 0; i < componentsInChildren.Length; i++)
                componentsInChildren[i].block = this;

            heightSlider = baseObject.HeightSlider;
            nonAuto = baseObject.NonAuto;
            holdToDetect = baseObject.HoldToDetect;
            inverted = baseObject.Inverted;
            ledColor = baseObject.ledColor;
            hand = baseObject.hand;

            dial = baseObject.dial;
            line = baseObject.line;
            bar = baseObject.bar;
        }

        MExtKey ExtendKey(MKey baseKey, KeyCode defaultValue)
        {
            var newKey = new MExtKey(baseKey.NameLocalisationId, baseKey.Key, defaultValue, this, baseKey.isEmulator);
            //newKey.DeSerialize(baseKey.Serialize());
            return newKey;
        }

        public MExtKey MActivateKey => ActivateKey as MExtKey;
        public MExtKey MEmulateKey => EmulateKey as MExtKey;
        public MSlider MaxHeigthSlider;

        public override MKey AddKey(MKey key)
        {
            key = base.AddKey(ExtendKey(key, key.GetKey(0)));
            var keyIdx = MapperTypes.IndexOf(key);
            return key;
        }

        protected override void Awake()
        {
            ModContext = SingleInstance<Logic>.Instance;
            machineHandler = ModContext.GetMachineHandler(this);
            base.Awake();
            if (MEmulateKey.Text.DisplayName.Length > 8)
                MEmulateKey.Text.DisplayName = "EMU";
            AddText(MActivateKey.Text);
            AddText(MEmulateKey.Text);
            MaxHeigthSlider = AddSliderUnclamped("<" + heightSlider.DisplayName, heightSlider.Key + "_2", 0.0f, 0.0f, 250f);
            heightSlider.DisplayName = ">" + heightSlider.DisplayName;
        }

        public Dictionary<string, object> GetPos()
        {
            var myPos = Rigidbody.position;
            return new Dictionary<string, object>
            {
                { "x", myPos.x },
                { "y", myPos.y },
                { "z", myPos.z }
            };
        }

        // Here goes copy-paste from decompile
        // But we change logic a bit for ExtKeys compability
        bool isDetecting, toggle;
        bool detectedOnceForThisFrame;
        bool activatePressed, emuActivatePressed, activateHeld, emuActivateHeld;
        float ledActive;

        public override void EmulationUpdateBlock()
        {
            // We remove the following code, because emulations via ExtKeys are supposed to look as much 'native' as possible
            /*
              emuActivatePressed = activateKey.EmulationPressed();
              emuActivateHeld = activateKey.EmulationHeld(includePressed: true);
              UpdateIsDetectingState(emuActivatePressed, emuActivateHeld || activateHeld);
            */
        }

        public override void UpdateBlock()
        {
            if (!isSimulating)
            {
                if (BlockMapper.IsOpen && BlockMapper.CurrentInstance.Current == this)
                    ShowVisualisation();
                else
                    HideVisualisation();
                isDetecting = false;
                return;
            }
            if (!SimPhysics || Time.timeScale == 0f)
            {
                detectedOnceForThisFrame = true;
                return;
            }

            activatePressed = MActivateKey.Pressed();
            activateHeld = MActivateKey.Holding();
            UpdateIsDetectingState(activatePressed, activateHeld /*|| emuActivateHeld*/); // we removed emuActivateHeld intentionally

            float targetHeiht, curHeight;
            if (MaxHeigthSlider.Value <= heightSlider.Value)
            {
                // Normal mode
                curHeight = Height;
                targetHeiht = heightSlider.Value;
            }
            else
            {
                // Lerp mode
                curHeight = (Height - heightSlider.Value) * 2;
                targetHeiht = MaxHeigthSlider.Value - heightSlider.Value;
            }

            AnimateHand(curHeight, targetHeiht, isDetecting);
            detectedOnceForThisFrame = false;
        }
        private void UpdateIsDetectingState(bool pressed, bool held)
        {
            if (!nonAuto.IsActive)
            {
                isDetecting = true;
                return;
            }
            if (holdToDetect.IsActive)
            {
                isDetecting = held;
                return;
            }
            if (pressed)
            {
                toggle = !toggle;
            }
            isDetecting = toggle;
        }

        protected void ToggleLED(float active)
        {
            if (ledActive != active)
            {
                MeshRenderer.material.SetColor("_EmissCol", Color.Lerp(Color.black, ledColor, active));
                ledActive = active;
            }
        }

        public void SetEmulation(float v)
        {
            ToggleLED(v);
            MEmulateKey.SetOutValue(this, v); // publish float value instead of old style StartEmulation/StopEmulation
        }

        public override void OnRemoteEmulate(MKey key, bool emulate)
        {
            if (!emulate)
                ToggleLED(0);
            else
                ToggleLED(1);
        }
        public override void SendEmulationUpdateBlock()
        {
            // placeholder to remove parent call
        }

        public override void FixedUpdateBlock()
        {
            if (!SimPhysics || !_parentMachine.isReady || detectedOnceForThisFrame)
            {
                return;
            }
            float outValue;
            if (!isDetecting)
            {
                // Disable
                outValue = 0;
            }
            else if (MaxHeigthSlider.Value <= heightSlider.Value)
            {
                // Normal mode - generate 0 or 1
                outValue = ((!inverted.IsActive) ? (Height > heightSlider.Value) : (Height < heightSlider.Value)) ? 1 : 0;
            }
            else
            {
                // Lerp mode - calculate lerp between sliders
                var heightDiff = Mathf.Clamp01((Height - heightSlider.Value) / (MaxHeigthSlider.Value - heightSlider.Value));
                outValue = (!inverted.IsActive) ? heightDiff : 1.0f - heightDiff;
            }

            SetEmulation(outValue);
            detectedOnceForThisFrame = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetEmulation(0);
        }

        private void Register(KeyInputController input)
        {
            MActivateKey.SetKeycodes(input, machineHandler.IsAnyEmulating);
            MEmulateKey.SetKeycodes(input, machineHandler.IsAnyEmulating);

            machineHandler.AddExtKeyEmulator(MEmulateKey);
            machineHandler.AddKey(input, this, MEmulateKey);
        }

        private void Unregister()
        {
            MActivateKey.ResetKeycodes();
            MEmulateKey.ResetKeycodes();
        }

        public static void Create(Logic logic)
        {
            var prefabId = PrefabMaster.BlockPrefabs.Where(x => x.Value.blockBehaviour is AltimeterBlock).First();
            var originalLogicGate = prefabId.Value.blockBehaviour as AltimeterBlock;

            var blockTemplate = originalLogicGate.gameObject;
            var extLogic = blockTemplate.AddComponent<ExtAltimeterBlock>();
            extLogic.LoadFrom(logic, originalLogicGate, blockTemplate);
            DestroyObject(originalLogicGate);
            PrefabMaster.BlockPrefabs[prefabId.Key].blockBehaviour = extLogic;

            logic.AddKeyRegistrer(typeof(ExtAltimeterBlock), (b, k) => ((ExtAltimeterBlock)b).Register(k), (b) => ((ExtAltimeterBlock)b).Unregister());
        }
    }
}
