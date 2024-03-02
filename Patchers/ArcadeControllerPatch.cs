using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using LSFunctions;

using RTFunctions.Functions;

namespace ArcadiaCustoms.Patchers
{
	[HarmonyPatch(typeof(ArcadeController))]
    public class ArcadeControllerPatch
    {
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix(ArcadeController __instance)
        {
			Debug.LogFormat("{0}Trying to generate new arcade UI...", ArcadePlugin.className);

			if (ArcadePlugin.buttonPrefab == null)
            {
				ArcadePlugin.buttonPrefab = __instance.ic.ButtonPrefab.Duplicate(null);
				UnityEngine.Object.DontDestroyOnLoad(ArcadePlugin.buttonPrefab);
			}

			InputDataManager.inst.playersCanJoin = false;
			ArcadePlugin.MainMenuTester();
			return true;
        }
	}
}
