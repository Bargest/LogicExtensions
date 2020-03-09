using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class WaterCannonScript : AdditionScript
    {
        WaterCannonController waterCannonController;
        float savedSpeed;
        MToggle FPLogic;
        bool valueChanged;
        float lastParticleChange;

        public override void SafeAwake()
        {
            base.SafeAwake();
            waterCannonController = BB.GetComponent<WaterCannonController>();
            FPLogic = waterCannonController.AddToggle("FPIO", "accept_fpio", false);
        }

        public override void Reset()
        {
            base.Reset();
            if (BB.SimPhysics)
            {
                savedSpeed = waterCannonController.StrengthSlider.Value;
                valueChanged = false;
            }
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!FPLogic.IsActive || !BB.isSimulating)
                return;

            var floatValue = machineHandler.ReadValue(waterCannonController.ShootKey);
            float newValue;
            if (savedSpeed >= 0)
                newValue = Mathf.Lerp(0, savedSpeed, floatValue);
            else
                newValue = Mathf.Lerp(savedSpeed, 0, floatValue);

            if (!Mathf.Approximately(waterCannonController.StrengthSlider.Value, newValue))
            {
                valueChanged = true;
                waterCannonController.StrengthSlider.Value = newValue;
                waterCannonController.ParentMachine.RegisterFixedUpdate(waterCannonController, false);
            }
            if (valueChanged && Time.time - lastParticleChange > 0.1)
            {
                if (!Mathf.Approximately(waterCannonController.StrengthSlider.Value, 0))
                {
                    waterCannonController.prevActiveState = false;
                    waterCannonController.prevBoilingState = false;
                }
                valueChanged = false;
                lastParticleChange = Time.time;
            }
        }
    }
}
