using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using LSFunctions;

namespace ArcadiaCustoms.Patchers
{
	[HarmonyPatch(typeof(ArcadeController))]
    public class ArcadeControllerPatch
    {
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static bool StartPrefix()
        {
			Debug.LogFormat("{0}Trying to generate new arcade UI...", ArcadePlugin.className);
			ArcadePlugin.MainMenuTester();
			return true;
        }
	}
}
