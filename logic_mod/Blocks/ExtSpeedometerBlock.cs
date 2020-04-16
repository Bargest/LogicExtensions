using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    class ExtSpeedometerBlock : SpeedometerBlock
    {
        public Logic ModContext;
        MachineHandler machineHandler;

        public ExtSpeedometerBlock() { }

        public void LoadFrom(Logic modContext, SpeedometerBlock baseObject, GameObject go)
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

            speedSlider = baseObject.HeightSlider; // really?
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

        public MExtKey MActivateKey => activateKey as MExtKey;
        public MExtKey MEmulateKey => EmulateKey as MExtKey;
        public MSlider maxSpeedSlider;

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
            maxSpeedSlider = AddSliderUnclamped("<" + speedSlider.DisplayName, speedSlider.Key + "_2", 0.0f, 0.0f, 250f);
            speedSlider.DisplayName = ">" + speedSlider.DisplayName;
        }

        bool isDetecting, toggle;
        bool activatePressed, emuActivatePressed, activateHeld, emuActivateHeld;
        bool detectedOnceForThisFrame;
        float ledActive;
        Vector3 lastPosition, smoothVel1, smoothVel2, velocity;

        public float ESqrSpeed => (!noRigidbody) ? Rigidbody.velocity.sqrMagnitude : velocity.sqrMagnitude;

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

        public override void EmulationUpdateBlock()
        {
            //emuActivatePressed = activateKey.EmulationPressed();
            //emuActivateHeld = activateKey.EmulationHeld(includePressed: true);
            //UpdateIsDetectingState(emuActivatePressed, emuActivateHeld || activateHeld);
        }

        public override void UpdateBlock()
        {
            if (!isSimulating)
            {
                isDetecting = false;
                return;
            }
            if (Time.timeScale == 0f)
            {
                detectedOnceForThisFrame = true;
                return;
            }
            activatePressed = MActivateKey.Pressed();
            activateHeld = MActivateKey.Holding();
            //UpdateIsDetectingState(activatePressed, activateHeld || emuActivateHeld);
            UpdateIsDetectingState(activatePressed, activateHeld);
            if (noRigidbody)
            {
                Vector3 position = VisualController.MeshFilter.transform.position;
                Vector3 a = (position - lastPosition) / Time.deltaTime;
                velocity = a * 0.333333343f + smoothVel1 * 0.333333343f + smoothVel2 * 0.333333343f;
                smoothVel2 = smoothVel1;
                smoothVel1 = velocity;
                lastPosition = position;
            }
            float targetSpeed, curSpeed = ESqrSpeed;
            if (maxSpeedSlider.Value <= speedSlider.Value)
                targetSpeed = speedSlider.Value;
            else
            {
                curSpeed = (float)(Math.Sqrt(ESqrSpeed) - speedSlider.Value);
                curSpeed = curSpeed < 0 ? 0 : curSpeed * curSpeed * 2;
                targetSpeed = maxSpeedSlider.Value - speedSlider.Value;
            }

            AnimateHand(curSpeed, targetSpeed * targetSpeed, isDetecting);
            detectedOnceForThisFrame = false;
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
            MEmulateKey.SetOutValue(this, v);//StartEmulation/StopEmulation;
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
            if (!isDetecting)
            {
                StopEmulation();
                return;
            }
            float outValue = 0;
            if (!isDetecting)
                outValue = 0;
            else if (maxSpeedSlider.Value <= speedSlider.Value)
                outValue = ((!inverted.IsActive) ? (ESqrSpeed > speedSlider.Value * speedSlider.Value) : (ESqrSpeed < speedSlider.Value * speedSlider.Value)) ? 1 : 0;
            else
            {
                var curSpeed = (float)(Math.Sqrt(ESqrSpeed) - speedSlider.Value);
                var heightDiff = Mathf.Clamp01(curSpeed / (maxSpeedSlider.Value - speedSlider.Value));
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
            var prefabId = PrefabMaster.BlockPrefabs.Where(x => x.Value.blockBehaviour is SpeedometerBlock).First();
            var originalLogicGate = prefabId.Value.blockBehaviour as SpeedometerBlock;

            var blockTemplate = originalLogicGate.gameObject;
            var extLogic = blockTemplate.AddComponent<ExtSpeedometerBlock>();
            extLogic.LoadFrom(logic, originalLogicGate, blockTemplate);
            DestroyObject(originalLogicGate);
            PrefabMaster.BlockPrefabs[prefabId.Key].blockBehaviour = extLogic;

            logic.AddKeyRegistrer(typeof(ExtSpeedometerBlock), (b, k) => ((ExtSpeedometerBlock)b).Register(k), (b) => ((ExtSpeedometerBlock)b).Unregister());
        }
    }
}
