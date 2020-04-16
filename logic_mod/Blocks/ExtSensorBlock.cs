using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    class ExtSensorBlock : SensorBlock
    {
        public Logic ModContext;
        MachineHandler machineHandler;

        public ExtSensorBlock() { }

        public void LoadFrom(Logic modContext, SensorBlock baseObject, GameObject go)
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

            nonAuto = baseObject.NonAuto;
            holdToDetect = baseObject.HoldToDetect;
            inverted = baseObject.Inverted;
            ledColor = baseObject.ledColor;

            sensorMask = baseObject.sensorMask;
            sensorPos = baseObject.sensorPos;
            sphereTop = baseObject.sphereTop;
            sphereBottom = baseObject.sphereBottom;
            cylinder = baseObject.cylinder;
        }

        MExtKey ExtendKey(MKey baseKey, KeyCode defaultValue)
        {
            var newKey = new MExtKey(baseKey.NameLocalisationId, baseKey.Key, defaultValue, this, baseKey.isEmulator);
            //newKey.DeSerialize(baseKey.Serialize());
            return newKey;
        }

        public MExtKey MActivateKey => activateKey as MExtKey;
        public MExtKey MEmulateKey => EmulateKey as MExtKey;

        public MToggle LerpMode;

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
            LerpMode = AddToggle("Lerp", "lerp_mode", "output as linear interpolation to closest object", false);
        }

        bool isDetecting, toggle;
        bool detectedOnceForThisFrame;
        int overlapCount;
        float closestDistance = 0;
        Collider closestCollider = null;
        Dictionary<string, object> detectedObjectInfo = null;

        bool NeedCollider(Collider collider)
        {
            BlockBehaviour componentInParent = collider.transform.GetComponentInParent<BlockBehaviour>();
            if (collider.transform.GetComponentInParent<BlockBehaviour>() == this)
                return false;
            
            if (ignoreStatic.IsActive)
            {
                Rigidbody attachedRigidbody = collider.attachedRigidbody;
                if (!attachedRigidbody || attachedRigidbody.isKinematic)
                    return false;
            }
            if (collider.isTrigger)
            {
                if (collider.transform.root != SingleInstanceFindOnly<AddPiece>.Instance.PhysicsGoalObject.root && collider.transform.root != ReferenceMaster.physicsGoalInstance)
                    return false;
                if (StatMaster.isMP)
                {
                    if ((bool)collider.gameObject.GetComponent<InsigniaTriggerObject>())
                        return false;
                }
                else if ((bool)collider.gameObject.GetComponentInParent<FinishLine>())
                    return false;
            }
            Transform transform = collider.transform;
            if (!_parentMachine.finishedPhysics)
            {
                if (componentInParent != null)
                {
                    if (componentInParent == this || (ignoreStatic.IsActive && componentInParent.Rigidbody.isKinematic))
                        return false;
                    for (int j = 0; j < componentInParent.DestroyOnSimulate.Length; j++)
                    {
                        GameObject gameObject = componentInParent.DestroyOnSimulate[j];
                        if (gameObject != null && gameObject.transform == transform)
                            return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            else 
            {
                if (transform == base.transform)
                    return false;
                return true;
            }
            return false;
        }

        void CheckPoint(Collider coll, Vector3 point)
        {
            // we sacrifice the real (0;0;0) point because it's probability is near zero
            // but all FA points go to this coordinate
            if (point == Vector3.zero)
                return;

            var closeDist = Vector3.Distance(sensorPos.position, point);
            if (closeDist < closestDistance)
            {
                closestDistance = closeDist;
                closestCollider = coll;
            }
            
            if (ModContext.DrawSensorDebug)
                DebugDraw.DrawSphere(point, 0.3f, Color.red);
        }
        protected void NewEvaluateSensor()
        {
            closestCollider = null;
            detectedObjectInfo = null;
            float radius = radiusSlider.Value;
            float totalHeight = distanceSlider.Value;
            float height = totalHeight - radius * 2f;
            if (height < 0f)
                height = 0f;

            Vector3 point = sensorPos.position + forward * radius;
            Vector3 point2 = sensorPos.position + forward * (height + radius);
            IEnumerable<RaycastHit> cast = Physics.SphereCastAll(point, radius, forward, height, sensorMask);

            string reverserComment0 = "Hello, my dear reverser! The following code looks strange, so let me explain it.";
            string reverserComment1 = "Here I want to get distance to the closest object in sensor's range. BUT:";
            string reverserComment2 = "Besiege uses old unity engine, and it turns out there is just no way to get distance to collider (NOT it's bounging box)";
            string reverserComment3 = "Also SphereCastAll returns no collision points when collider is overlapped by sphere in initial position";
            string reverserComment4 = "Casting sphere from behind sensor ends up with false alarm on objects behind it and not detecting the same object if it is in front of sensor too (e.g. big stone arc mesh in sandbox)";
            string reverserComment5 = "This is why the following code bruteforces maximum non-colliding sphere in case we run into this stupid unity logic";
            string reverserComment6 = "Fortunately it doesn't happen too often. The main trigger is having a very big radiusSlider.Value";
            string reverserComment7 = "Also in rare cases spherecast just ignores ground, so we explicitly make raycast to get at least something";
            // crutch for ground
            bool haveRay = Physics.Raycast(sensorPos.position, forward, out RaycastHit directRaycast, totalHeight, sensorMask);
            if (haveRay)
                cast = cast.Concat(new[] { directRaycast });
            closestDistance = float.MaxValue;
            HashSet<Collider> foundColliders = new HashSet<Collider>();
            foreach (var colliderPoints in cast.GroupBy(x => x.collider).ToDictionary(x => x.Key, x => new HashSet<RaycastHit>(x)))
            {
                if (colliderPoints.Value.Count == 0)
                    continue;
                var collider = colliderPoints.Key;
                if (NeedCollider(collider))
                {
                    foundColliders.Add(collider);
                    foreach (var hit in colliderPoints.Value)
                    {
                        if (hit.point == Vector3.zero && hit.normal == -forward)
                        {
                            float step = radius / 2;
                            float lastNonOverlapRadius = 0;
                            float newRadius = radius - step;
                            while (step > 0.01)
                            {
                                step /= 2;
                                if (Physics.OverlapSphere(sensorPos.position + forward * newRadius, newRadius, sensorMask).Contains(collider))
                                {
                                    newRadius -= step;
                                }
                                else
                                {
                                    lastNonOverlapRadius = newRadius;
                                    newRadius += step;
                                }
                            }
                            // now lastOverlapRadius has approx biggest sphere, NOT touching this collider
                            // cast sphere towards original direction
                            if (lastNonOverlapRadius > 0)
                            {
                                var resultPoint = sensorPos.position + forward * lastNonOverlapRadius;
                                var newCasts = Physics.SphereCastAll(resultPoint, lastNonOverlapRadius, forward, radius, sensorMask)
                                    .Where(x => x.collider == collider).ToList();
                                if (newCasts.Count > 0)
                                    CheckPoint(collider, newCasts[0].point);
                                //else
                                //    Debug.Log($"{step} {collider}");
                            }
                        }
                        else
                        {
                            CheckPoint(collider, hit.point);
                        }
                    }
                }
            }
            overlapCount = foundColliders.Count;
        }

        public Dictionary<string, object> GetTargetObject()
        {
            if (closestCollider == null)
                return null;

            if (detectedObjectInfo != null)
                return detectedObjectInfo;

            // Static objects have rigidBody.isKinematic
            var rigidBody = closestCollider.attachedRigidbody;
            // Mesh has no rigidbody
            var isStaic = (rigidBody == null || rigidBody.isKinematic);
            var transform = closestCollider.transform;
            // AI has EntityAI
            var parentAI = transform.GetComponentInParent<EntityAI>();
            var simpleAI = transform.GetComponentInParent<EnemyAISimple>();
            // Blocks have BlockBehavior
            var parentBlock = transform.GetComponentInParent<BlockBehaviour>();
            var objectInfo = rigidBody?.GetComponentInParent<SimBehaviour>();
            var bombInfo = rigidBody?.GetComponentInParent<ExplodeOnCollide>();
            var killable = rigidBody?.GetComponentInParent<InjuryController>();
            var projectile = rigidBody?.GetComponentInParent<ProjectileScript>();
            var isDead = parentAI != null && parentAI.isDead || simpleAI != null && simpleAI.isDead;
            var canBreak = objectInfo is IExplosionEffect
                || rigidBody?.GetComponent<PhysNodeTile>() != null
                || rigidBody?.GetComponent<StructuralPhysTile>() != null;
            var objectType = (bombInfo != null || (parentBlock is ExplodeOnCollideBlock) ? "bomb"
                                : parentBlock != null ? "block"
                                : projectile != null ? "projectile"
                                : isDead ? "dead"
                                : parentAI != null || simpleAI != null ? "unit"
                                : killable != null ? "creature"
                                : canBreak ? "breakable"
                                : objectInfo != null ? "entity"
                                : "other");
            detectedObjectInfo = new Dictionary<string, object>
            {
                { "static", isStaic ? (long)1 : 0 },
                { "type", objectType }
            };

            return detectedObjectInfo;
        }

        float ledActive;

        bool activatePressed, emuActivatePressed, activateHeld, emuActivateHeld;
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
                    ShowSensorArea();
                else
                    HideSensorArea();
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
            //UpdateIsDetectingState(activatePressed, activateHeld || emuActivateHeld);
            UpdateIsDetectingState(activatePressed, activateHeld);
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

        public override void FixedUpdateBlock()
        {
            if (!_parentMachine.isReady || detectedOnceForThisFrame)
                return;

            float outValue = 0;
            if (isDetecting)
            {
                NewEvaluateSensor();
                if (!LerpMode.IsActive)
                    outValue = overlapCount > 0 ? 1 : 0;
                else
                    outValue = 1 - Mathf.InverseLerp(0, distanceSlider.Value, closestDistance);
                if (Inverted.IsActive)
                    outValue = 1 - outValue;
            }
            SetEmulation(outValue);
            detectedOnceForThisFrame = true;

        }

        public override void SendEmulationUpdateBlock()
        {
            // placeholder to remove parent call
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
            var prefabId = PrefabMaster.BlockPrefabs.Where(x => x.Value.blockBehaviour is SensorBlock).First();
            var originalLogicGate = prefabId.Value.blockBehaviour as SensorBlock;

            var blockTemplate = originalLogicGate.gameObject;
            var extLogic = blockTemplate.AddComponent<ExtSensorBlock>();
            extLogic.LoadFrom(logic, originalLogicGate, blockTemplate);
            DestroyObject(originalLogicGate);
            PrefabMaster.BlockPrefabs[prefabId.Key].blockBehaviour = extLogic;

            logic.AddKeyRegistrer(typeof(ExtSensorBlock), (b, k) => ((ExtSensorBlock)b).Register(k), (b) => ((ExtSensorBlock)b).Unregister());
        }
    }
}
