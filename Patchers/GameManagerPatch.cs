using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

using LSFunctions;
using DG.Tweening;
using ArcadiaCustoms.Functions;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;

namespace ArcadiaCustoms.Patchers
{

    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
		[HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void SetCameraClipPlanes()
		{
			ArcadePlugin.fromLevel = true;

			ArcadePlugin.timeInLevelOffset = Time.time;
			ArcadePlugin.timeInLevel = 0f;
			LevelManager.finished = false;
		}

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SetAntialiasing()
        {
			if (!LevelManager.finished)
				ArcadePlugin.timeInLevel = Time.time - ArcadePlugin.timeInLevelOffset;
        }
	}
}
