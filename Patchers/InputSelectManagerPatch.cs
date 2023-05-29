using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ArcadiaCustoms.Patchers
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
