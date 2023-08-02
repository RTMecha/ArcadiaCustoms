using HarmonyLib;

using UnityEngine;

namespace ArcadiaCustoms.Patchers
{
    [HarmonyPatch(typeof(InputSelectController))]
    public class InputSelectControllerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix()
        {
        }
    }
}
