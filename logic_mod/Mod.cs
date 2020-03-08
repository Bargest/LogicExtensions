using System;
using Modding;
using UnityEngine;
using static Modding.ModNetworking.CallbacksWrapper;

namespace Logic
{
	public class Mod : ModEntryPoint
	{
        private Logic mod;

        public override void OnLoad()
        {
            this.mod = SingleInstance<Logic>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(this.mod);
        }
    }
}
