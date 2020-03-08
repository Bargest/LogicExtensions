using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class AdditionScript : MonoBehaviour
    {
        public BlockBehaviour BB;
        public Logic ModContext;
        public Rigidbody rigidbody;
        public MachineHandler machineHandler;

        protected void Awake()
        {
            ModContext = SingleInstance<Logic>.Instance;
            BB = GetComponent<BlockBehaviour>();
            machineHandler = ModContext.GetMachineHandler(BB);
            rigidbody = GetComponent<Rigidbody>();
            SafeAwake();
        }

        protected void Update()
        {
            OnUpdate();
        }
        protected void FixedUpdate()
        {
            if (!BB.isSimulating)
                OnBuildingFixedUpdate();
            else
            {
                OnFixedUpdate();
                if (BB.SimPhysics)
                    OnFixedUpdateHost();
                else
                    OnFixedUpdateClient();
            }
        }


        public virtual void Reset()
        {

        }

        public virtual void SafeAwake()
        {

        }
        public virtual void OnUpdate()
        {

        }
        public virtual void OnFixedUpdate()
        {

        }
        public virtual void OnBuildingFixedUpdate()
        {

        }
        public virtual void OnFixedUpdateHost()
        {

        }
        public virtual void OnFixedUpdateClient()
        {

        }
    }
}
