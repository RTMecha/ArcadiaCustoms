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
		[HarmonyPrefix]
		static void StartPrefix() =>InputDataManager.inst.ClearInputs();

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

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ISUpdateTranspiler(IEnumerable<CodeInstruction> instruction)
        {
            return new CodeMatcher(instruction)
                .Start()
                .Advance(4)
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_8))
                .InstructionEnumeration();
        }


		[HarmonyPatch("loadStrings")]
		[HarmonyPrefix]
		static bool loadStringsPrefix(InputSelectManager __instance, ref int ___randomLength)
		{
			__instance.randomStrings.Clear();
			for (int i = 0; i < 8; i++)
			{
				__instance.randomStrings.Add(string.Concat(new string[]
				{
					"<color=",
					LSText.randomHex("666666"),
					">",
					LSText.randomString(___randomLength),
					"</color>"
				}));
				__instance.randomStrings2.Add(LSText.randomHex("666666"));
			}
			return false;
		}

		[HarmonyPatch("canChange")]
		[HarmonyPrefix]
		static bool canChangePrefix(InputSelectManager __instance, ref IEnumerator __result, ref int ___randomLength)
        {
			__result = canChange(__instance, ___randomLength);
			return false;
        }


		static IEnumerator canChange(InputSelectManager __instance, int ___randomLength)
		{
			for (int i = 0; i < 8; i++)
			{
				if (Random.value < 0.5f)
				{
					__instance.randomStrings[i] = string.Concat(new string[]
					{
					"<color=",
					LSText.randomHex("666666"),
					">",
					LSText.randomString(___randomLength),
					"</color>"
					});
					__instance.randomStrings2[i] = LSText.randomHex("666666");
				}
			}
			yield return new WaitForSeconds(Random.Range(0f, 0.4f));
			__instance.StartCoroutine(canChange(__instance, ___randomLength));
			yield break;
		}
	}
}
