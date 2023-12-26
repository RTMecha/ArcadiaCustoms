using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using LSFunctions;

namespace ArcadiaCustoms.Patchers
{
    [HarmonyPatch(typeof(InputSelectManager))]
    public class InputSelectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void ResetArcadeInSelection()
        {
            LSHelpers.HideCursor();
            //ArcadeManager.inst.skippedLoad = false;
            //ArcadeManager.inst.forcedSkip = false;
            ArcadePlugin.fromLevel = false;
			//DataManager.inst.UpdateSettingBool("IsArcade", true);
        }
	}
}
