using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class FlyingScript : AdditionScript
    {
        FlyingController flyingController;
        float savedSpeed;
        MToggle FPLogic;

        public override void SafeAwake()
        {
            base.SafeAwake();
            flyingController = BB.GetComponent<FlyingController>();
            FPLogic = flyingController.AddToggle("FPIO", "accept_fpio", false);
        }

        public override void Reset()
        {
            base.Reset();
            if (BB.SimPhysics)
                savedSpeed = flyingController.SpeedSlider.Value;
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!FPLogic.IsActive)
                return;

            if (!BB.isSimulating)
                return;

            var floatValue = machineHandler.ReadValue(flyingController.FlyKey);
            if (floatValue != 0)
                flyingController.SpeedSlider.Value = Mathf.Lerp(0, savedSpeed, floatValue);
        }
    }
}
