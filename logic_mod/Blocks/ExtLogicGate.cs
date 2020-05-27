using Localisation;
using Modding;
using Selectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    public class ExtLogicGate : LogicGate
    {
        public Logic ModContext;
        MachineHandler machineHandler;

        public ExtLogicGate()
        {
            
        }

        public void LoadFrom(Logic modContext, LogicGate baseObject, GameObject go)
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

            leaverA = baseObject.leaverA;
            leaverB = baseObject.leaverB;
            ledColor = baseObject.ledColor;
        }

        MExtKey ExtendKey(MKey baseKey, KeyCode defaultValue)
        {
            var newKey = new MExtKey(baseKey.NameLocalisationId, baseKey.Key, defaultValue, this, baseKey.isEmulator);
            //newKey.DeSerialize(baseKey.Serialize());
            return newKey;
        }

        public MExtKey MAKey => AKey as MExtKey;
        public MExtKey MBKey => BKey as MExtKey;
        public MExtKey MEmulateKey => EmulateKey as MExtKey;

        public override MKey AddKey(MKey key)
        {
            key = base.AddKey(ExtendKey(key, key.GetKey(0)));
            return key;
        }

        protected override void Awake()
        {
            ModContext = SingleInstance<Logic>.Instance;
            machineHandler = ModContext.GetMachineHandler(this);
            base.Awake();
            if (MEmulateKey.Text.DisplayName.Length > 8)
                MEmulateKey.Text.DisplayName = "EMU";
            AddText(MAKey.Text);
            AddText(MBKey.Text);
            AddText(MEmulateKey.Text);
        }

        public bool aToggleState, bToggleState;
        float ledActive;

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
            MEmulateKey.SetOutValue(this, v); // instead of StartEmulation/StopEmulation;
        }

        public override void OnRemoteEmulate(MKey key, bool emulate)
        {
            if (!emulate)
                ToggleLED(0);
            else
                ToggleLED(1);
        }

        bool A, B, aToggled, bToggled, aHeld, bHeld, aPressed, bPressed;//, emuAPressed, emuBPressed, emuAHeld, emuBHeld
        private void UpdateState(bool pressedA, bool pressedB, bool heldA, bool heldB)
        {
            A = heldA;
            B = heldB;
            if (ToggledInput.IsActive)
            {
                if (pressedA)
                {
                    aToggled = !aToggled;
                }
                if (pressedB)
                {
                    bToggled = !bToggled;
                }
                A = aToggled;
                B = bToggled;
            }
        }

        public override void EmulationUpdateBlock()
        {
            /*emuAPressed = aKey.EmulationPressed();
            emuBPressed = bKey.EmulationPressed();
            emuAHeld = aKey.EmulationHeld(includePressed: true);
            emuBHeld = bKey.EmulationHeld(includePressed: true);
            UpdateState(emuAPressed, emuBPressed, emuAHeld || aHeld, emuBHeld || bHeld);*/
        }

        public override void SendEmulationUpdateBlock()
        {
            // placeholder to remove parent call
        }

        public override void FixedUpdateBlock()
        {
            if (Time.timeScale == 0f)
                return;

            aPressed = MAKey.Pressed();
            bPressed = MBKey.Pressed();
            aHeld = MAKey.Holding();
            bHeld = MBKey.Holding();
            //UpdateState(aPressed, bPressed, aHeld || emuAHeld, bHeld || emuBHeld);
            UpdateState(aPressed, bPressed, aHeld, bHeld);
            if (A)
                leaverA.localRotation = Quaternion.Euler(0f, 0f, -90f);
            else
                leaverA.localRotation = Quaternion.Euler(0f, 0f, 0f);
            if ((gateType != 0 && B) || (gateType == GateType.NOT && A))
                leaverB.localRotation = Quaternion.Euler(0f, 0f, -90f);
            else
                leaverB.localRotation = Quaternion.Euler(0f, 0f, 0f);

            bool result = false;
            switch (gateType)
            {
                case GateType.NOT:
                    result = (!A);
                    break;
                case GateType.AND:
                    result = (A && B);
                    break;
                case GateType.OR:
                    result = (A || B);
                    break;
                case GateType.NAND:
                    result = (!A || !B);
                    break;
                case GateType.NOR:
                    result = (!A && !B);
                    break;
                case GateType.XOR:
                    result = (A != B);
                    break;
                case GateType.XNOR:
                    result = (A == B);
                    break;
            }
            SetEmulation(result ? 1 : 0);
        }

        public override void UpdateBlock()
        {
            // base.UpdateBlock();   
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetEmulation(0);
        }

        private void RegisterExtLogicGate(KeyInputController input)
        {
            MAKey.SetKeycodes(input, machineHandler.IsAnyEmulating);
            MBKey.SetKeycodes(input, machineHandler.IsAnyEmulating);
            MEmulateKey.SetKeycodes(input, machineHandler.IsAnyEmulating);

            machineHandler.AddExtKeyEmulator(MEmulateKey);
            machineHandler.AddKey(input, this, MEmulateKey);
        }

        private void UnregisterExtLogicGate()
        {
            MAKey.ResetKeycodes();
            MBKey.ResetKeycodes();
            MEmulateKey.ResetKeycodes();
        }

        public static void Create(Logic logic)
        {
            var prefabId = PrefabMaster.BlockPrefabs.Where(x => x.Value.blockBehaviour is LogicGate).First();
            var originalLogicGate = prefabId.Value.blockBehaviour as LogicGate;

            var blockTemplate = originalLogicGate.gameObject;
            var extLogic = blockTemplate.AddComponent<ExtLogicGate>();
            extLogic.LoadFrom(logic, originalLogicGate, blockTemplate);
            DestroyObject(originalLogicGate);
            PrefabMaster.BlockPrefabs[prefabId.Key].blockBehaviour = extLogic;

            logic.AddKeyRegistrer(typeof(ExtLogicGate), (b, k) => ((ExtLogicGate)b).RegisterExtLogicGate(k), (b) => ((ExtLogicGate)b).UnregisterExtLogicGate());
        }
    }
}
