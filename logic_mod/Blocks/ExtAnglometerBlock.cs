using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    class ExtAnglometerBlock : AnglometerBlock
    {
        public Logic ModContext;
        MachineHandler machineHandler;

        public ExtAnglometerBlock() { }

        public void LoadFrom(Logic modContext, AnglometerBlock baseObject, GameObject go)
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

            startSlider = baseObject.StartSlider;
            stopSlider = baseObject.StopSlider;
            nonAuto = baseObject.NonAuto;
            holdToDetect = baseObject.HoldToDetect;
            inverted = baseObject.Inverted;
            ledColor = baseObject.ledColor;
            hand = baseObject.hand;

            dial = baseObject.dial;
        }

        MExtKey ExtendKey(MKey baseKey, KeyCode defaultValue)
        {
            var newKey = new MExtKey(baseKey.NameLocalisationId, baseKey.Key, defaultValue, this, baseKey.isEmulator);
            return newKey;
        }

        public MExtKey MActivateKey => activateKey as MExtKey;
        public MExtKey MEmulateKey => EmulateKey as MExtKey;
        public MToggle LerpMode;

        bool isDetecting, toggle;
        bool detectedOnceForThisFrame;
        Vector3 targetDir = Vector3.zero;

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
            LerpMode = AddToggle("Lerp", "lerp_mode", "output as linear interpolation between selected angles", false);

            if (isSimulating)
                targetDir = base.transform.up;
        }

        private float StopFalsePositivesWhenDialAxisAlignsWithTargetDir()
        {
            Vector3 forward = base.transform.forward;
            Vector3 rhs = targetDir;
            float num = Vector3.Dot(forward, rhs);
            num = ((!(num < 0f)) ? num : (0f - num));
            return Mathf.InverseLerp(0.9f, 1f, num);
        }

        private float CorrectedAngle(Vector3 dir)
        {
            float num = ClampAngle(startSlider.Value);
            float num2 = ClampAngle(stopSlider.Value);
            num2 -= num;
            num2 = ClampAngle(num2);
            float num3 = num2 / 2f;
            num3 = ClampAngle(num3 - 180f + num);
            return Mathf.Lerp(ClampAngle(CurrentAngle(dir)), num3, StopFalsePositivesWhenDialAxisAlignsWithTargetDir());
        }

        private float InvLerpAngle(float start, float end, float mid)
        {
            start = ClampAngle(start);
            end = ClampAngle(end);
            mid = ClampAngle(mid);

            return Mathf.InverseLerp(start, end, mid);
        }

        private float ClampAngle(float a)
        {
            return (!(a < 0f)) ? a : (a + 360f);
        }

        public Dictionary<string, object> GetAng()
        {
            Quaternion quaternion = Rigidbody.rotation;
            Vector3 euler = quaternion.eulerAngles;
            Vector3 angV = Rigidbody.angularVelocity;
            return new Dictionary<string, object>
            {
                { "quaternion", BlockUtils.Quat2Dict(quaternion) },
                { "euler", BlockUtils.Vec2Dict(euler) },
                { "velocity", BlockUtils.Vec2Dict(angV) }
            };
        }

        bool activatePressed, emuActivatePressed, activateHeld, emuActivateHeld;
        float ledActive;
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
                if (BlockMapper.IsOpen && BlockMapper.CurrentInstance.Current == this)
                    UpdateVisualisation();
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
            UpdateIsDetectingState(activatePressed, activateHeld /* || emuActivateHeld)*/);
            AnimateHand(targetDir, isDetecting);
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
            MEmulateKey.SetOutValue(this, v);//StartEmulation/StopEmulation;
        }

        public override void OnRemoteEmulate(MKey key, bool emulate)
        {
            if (!emulate)
                ToggleLED(0);
            else
                ToggleLED(1);
        }

        public bool IsBetween(float start, float end, float mid)
        {
            start = ClampAngle(start);
            end = ClampAngle(end);
            mid = ClampAngle(mid);
            if (Mathf.Approximately(start, end))
            {
                return mid > end - 0.5f && mid < end + 0.5f;
            }
            end -= start;
            mid -= start;
            end = ClampAngle(end);
            mid = ClampAngle(mid);
            return mid < end;
        }
        public override void SendEmulationUpdateBlock()
        {
            // placeholder to remove parent call
        }

        public override void FixedUpdateBlock()
        {
            if (!SimPhysics || !_parentMachine.isReady || detectedOnceForThisFrame)
                return;

            float outValue = 0;
            if (isDetecting)
            {
                float mid = CorrectedAngle(targetDir);
                if (!LerpMode.IsActive)
                    outValue = IsBetween(startSlider.Value, stopSlider.Value, mid) ? 1 : 0;
                else
                    outValue = InvLerpAngle(startSlider.Value, stopSlider.Value, mid);
                if (inverted.IsActive)
                    outValue = 1 - outValue;
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
            var prefabId = PrefabMaster.BlockPrefabs.Where(x => x.Value.blockBehaviour is AnglometerBlock).First();
            var originalLogicGate = prefabId.Value.blockBehaviour as AnglometerBlock;

            var blockTemplate = originalLogicGate.gameObject;
            var extLogic = blockTemplate.AddComponent<ExtAnglometerBlock>();
            extLogic.LoadFrom(logic, originalLogicGate, blockTemplate);
            DestroyObject(originalLogicGate);
            PrefabMaster.BlockPrefabs[prefabId.Key].blockBehaviour = extLogic;

            logic.AddKeyRegistrer(typeof(ExtAnglometerBlock), (b, k) => ((ExtAnglometerBlock)b).Register(k), (b) => ((ExtAnglometerBlock)b).Unregister());
        }
    }
}
