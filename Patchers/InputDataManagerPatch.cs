using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

namespace ArcadiaCustoms.Patchers
{
	[HarmonyPatch(typeof(InputDataManager))]
    public class InputDataManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instruction)
        {
            return new CodeMatcher(instruction)
                .Start()
                .Advance(6)
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_8))
                .InstructionEnumeration();
        }
    }
}
