using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class CogScript : AdditionScript
    {
        CogMotorControllerHinge cogMotor;
        float savedSpeed;
        MToggle FPLogic;

        public override void SafeAwake()
        {
            base.SafeAwake();
            cogMotor = BB.GetComponent<CogMotorControllerHinge>();
            FPLogic = cogMotor.AddToggle("FPIO", "accept_fpio", false);
        }

        public override void Reset()
        {
            base.Reset();
            if (BB.SimPhysics)
                savedSpeed = cogMotor.SpeedSlider.Value;
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!FPLogic.IsActive)
                return;

            if (!BB.isSimulating)
                return;

            var forward = machineHandler.ReadValue(cogMotor.ForwardKey);
            var backward = machineHandler.ReadValue(cogMotor.BackwardKey);
            if (forward != backward)
                cogMotor.SpeedSlider.Value = Mathf.Lerp(0, savedSpeed, Math.Abs(forward - backward));
        }
    }
}
