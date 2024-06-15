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
            {
                var diff = forward - backward;
                var direction = diff >= 0 ? 1 : -1;
                // apply sign switch from game to prevent it resetting our negatives
                var targetMax = savedSpeed * direction;
                if (cogMotor.Input < 0)
                    targetMax /= cogMotor.Input;

                cogMotor.SpeedSlider.Value = Mathf.Lerp(0, targetMax, Math.Abs(diff));
            }
        }
    }
}
