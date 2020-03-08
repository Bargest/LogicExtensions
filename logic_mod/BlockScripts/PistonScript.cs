using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.BlockScripts
{
    class PistonScript : AdditionScript
    {
        SliderCompress sliderCompress;
        ConfigurableJoint joint;
        MToggle FPLogic;
        float savedSpeed;
        public override void SafeAwake()
        {
            base.SafeAwake();
            sliderCompress = BB.GetComponent<SliderCompress>();
            joint = GetComponent<ConfigurableJoint>();
            FPLogic = sliderCompress.AddToggle("FPIO", "accept_fpio", false);
        }

        public override void Reset()
        {
            base.Reset();
            if (BB.SimPhysics)
                savedSpeed = sliderCompress.SpeedSlider.Value;
        }

        public override void OnFixedUpdateHost()
        {
            base.OnFixedUpdateHost();
            if (!FPLogic.IsActive)
                return;

            if (!joint || joint.connectedBody == null || sliderCompress.ToggleModeToggle.IsActive)
                return;

            var keyPos = machineHandler.ReadValue(sliderCompress.ExtendKey);
            sliderCompress.posToBe = Mathf.Lerp(sliderCompress.startLimit, sliderCompress.newLimit, keyPos);
            if (joint.targetPosition.x == sliderCompress.posToBe)
                return;

            if (rigidbody.IsSleeping())
                rigidbody.WakeUp();
            if (joint.connectedBody.IsSleeping())
                joint.connectedBody.WakeUp();
            sliderCompress.SpeedSlider.Value = 0;
            var x = Mathf.MoveTowards(joint.targetPosition.x, sliderCompress.posToBe, savedSpeed * Time.deltaTime * 6f);
            joint.targetPosition = new Vector3(x, joint.targetPosition.y, joint.targetPosition.z);
        }
    }
}
