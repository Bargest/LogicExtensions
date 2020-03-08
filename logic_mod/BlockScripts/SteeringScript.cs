using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class SteeringScript : AdditionScript
    {
        float savedSpeed;
        MKey[] UsedKeys;
        MToggle FPLogic;
        SteeringWheel steeringWheel;
        Vector3 jointEulerRotation = Vector3.zero;
        public override void SafeAwake()
        {
            base.SafeAwake();
            steeringWheel = BB.GetComponent<SteeringWheel>();
            FPLogic = steeringWheel.AddToggle("FPIO", "accept_fpio", false);
            UsedKeys = steeringWheel.MapperTypes.Where(x => x is MKey).Select(x => x as MKey).ToArray();
        }

        public override void Reset()
        {
            base.Reset();
            if (BB.SimPhysics)
                savedSpeed = steeringWheel.SpeedSlider.Value;
        }

        private void HandleHinge()
        {
            var leftValue = machineHandler.ReadValue(UsedKeys[0]);
            var rightValue = machineHandler.ReadValue(UsedKeys[1]);
            rigidbody.WakeUp();
            if (leftValue == rightValue)
            {
                RotateToAngle(0, savedSpeed);
                return;
            }
            
            rigidbody.WakeUp();
            float num4, num5;
            if (steeringWheel.Flipped)
            {
                num4 = 0f - steeringWheel.LimitsSlider.Min;
                num5 = steeringWheel.LimitsSlider.Max;
                float tmp = leftValue;
                leftValue = rightValue;
                rightValue = tmp;
            }
            else
            {
                num4 = 0f - steeringWheel.LimitsSlider.Max;
                num5 = steeringWheel.LimitsSlider.Min;
            }

            //float targetAngle = ((steeringWheel.AngleToBe < num4) ? num4 : ((!(steeringWheel.AngleToBe > num5)) ? steeringWheel.AngleToBe : num5));
            float targetAngle;
            if (leftValue > rightValue)
                targetAngle = num5 * (leftValue - rightValue);
            else
                targetAngle = num4 * (rightValue - leftValue);

            RotateToAngle(targetAngle, savedSpeed);
        }

        private void HandleWheel()
        {
            var leftValue = machineHandler.ReadValue(UsedKeys[0]);
            var rightValue = machineHandler.ReadValue(UsedKeys[1]);
            if (leftValue == rightValue)
            {
                steeringWheel.SpeedSlider.Value = savedSpeed;
                return;
            }
            var targetSpeed = leftValue - rightValue;
            // steeringWheel.SpeedSlider.Value = Mathf.Lerp(0, savedSpeed, Math.Abs(targetSpeed));
            var targetAngle = targetSpeed > 0 ? steeringWheel.AngleToBe + 360 : steeringWheel.AngleToBe - 360;
            RotateToAngle(targetAngle, Mathf.Lerp(0, savedSpeed, Math.Abs(targetSpeed)));
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!FPLogic.IsActive || !steeringWheel.blockJoint)
                return;

            if (steeringWheel.allowLimits && steeringWheel.LimitsSlider.IsActive)
            {
                // don't apply any logic to steering hinge without ReturnToCenter
                if (!steeringWheel.ReturnToCenterToggle.IsActive)
                    return;
                HandleHinge();
            }
            else
            {
                HandleWheel();
            }
        }

        public void RotateToAngle(float targetAngle, float speed)
        {
            if (steeringWheel == null)
                return;

            float maxDelta = Time.deltaTime * 100f * speed;
            var num = Mathf.MoveTowards(steeringWheel.AngleToBe, targetAngle, maxDelta);
            steeringWheel.AngleToBe = num;
            steeringWheel.SpeedSlider.Value = 0;

            jointEulerRotation.x = steeringWheel.axis.x * num;
            jointEulerRotation.y = steeringWheel.axis.y * num;
            jointEulerRotation.z = steeringWheel.axis.z * num;
            (steeringWheel.blockJoint as ConfigurableJoint).targetRotation = Quaternion.Euler(jointEulerRotation);
        }
    }
}
