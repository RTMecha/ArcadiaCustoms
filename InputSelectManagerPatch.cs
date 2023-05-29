using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BepInEx.Configuration;
using UnityEngine.Rendering.PostProcessing;
using SimpleJSON;
using DG.Tweening;
using LSFunctions;

namespace ArcadiaCustoms
{
    [HarmonyPatch(typeof(InputSelectManager))]
    public class InputSelectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ISUpdateTranspiler(IEnumerable<CodeInstruction> instruction)
        {
            return new CodeMatcher(instruction)
                .Start()
                .Advance(4)
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_8))
                .InstructionEnumeration();
        }
    }
}
