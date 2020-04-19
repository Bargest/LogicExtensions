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

        Vector3 CalculateForce(float flySpeed)
        {
            var reverseMultiplier = ((!flyingController.ReverseToggle.IsActive) ? 1f : (-1f));
            var magnitude = 100f * flySpeed * reverseMultiplier;
            var force = flyingController.transform.forward * magnitude;
            return force - flyingController.Rigidbody.velocity * flyingController.dragScaler;
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!BB.isSimulating)
                return;

            if (!FPLogic.IsActive)
                return;

            var floatValue = machineHandler.ReadValue(flyingController.FlyKey);
            if (floatValue != 0)
            {
                floatValue = Mathf.Lerp(0, savedSpeed, floatValue);
                // It seems that after last update FlyingController stopped using speedSlider.Value directly.
                // And of course new field used - 'magnitude' - is private.
                // Because we need to replace the original force, calculated from savedSpeed, by our new finalForce,
                // we reimplement force calculations the same way as in game, subtract this force and pray
                // this calculation will not change.
                var finalForce = CalculateForce(floatValue);
                var originalForce = CalculateForce(savedSpeed);
                flyingController.Rigidbody.AddForce(-originalForce + finalForce);
            }
        }
    }
}
