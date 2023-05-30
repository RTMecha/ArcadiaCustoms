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

namespace ArcadiaCustoms.Patchers
{

    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
		[HarmonyPatch("getPitch")]
		public static bool getPitch(ref float __result)
		{
			if (EditorManager.inst != null)
			{
				__result = 1f;
				return false;
			}
			__result = new List<float>
			{
				0.5f,
				0.8f,
				1f,
				1.2f,
				1.5f
			}[Mathf.Clamp(0, DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2), 4)];
			return false;
		}

		[HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void SetCameraClipPlanes()
		{
			DOTween.Clear();
			DataManager.inst.gameData = null;
			DataManager.inst.gameData = new DataManager.GameData();
			Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            camera.farClipPlane = 100000;
            camera.nearClipPlane = -100000;

            ArcadePlugin.inst.StartCoroutine(ArcadePlugin.FixTimeline());
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void SetAntialiasing()
        {
            if (GameManager.inst != null && DataManager.inst.gameData.beatmapData != null)
            {
                PostProcessLayer aliasing = GameObject.Find("Main Camera").GetComponent<PostProcessLayer>();
                if (ArcadePlugin.AntiAliasing.Value == true)
                {
                    aliasing.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                }
                else
                {
                    aliasing.antialiasingMode = PostProcessLayer.Antialiasing.None;
                }
            }

            if (GameManager.inst.LiveTheme.objectColors.Count == 9)
            {
                for (int i = 0; i < 9; i++)
                {
                    GameManager.inst.LiveTheme.objectColors.Add(LSColors.pink900);
                }
            }
        }

        [HarmonyPatch("LoadLevelCurrent")]
        [HarmonyPrefix]
        private static bool LevelDecrypter()
        {
            string path = SaveManager.inst.ArcadeQueue.AudioFileStr.Replace("\\level.ogg", "/");
            Debug.LogFormat("{0}Trying to load song.lsen from (" + path + ")", ArcadePlugin.className);
            if (DataManager.inst.GetSettingBool("IsArcade", false) && RTFile.FileExists(path + "song.lsen"))
            {
                Debug.LogFormat("{0}Loaded song.lsen from (" + path + ")", ArcadePlugin.className);
                DiscordController.inst.OnIconChange("arcade");
                DiscordController.inst.OnDetailsChange("Playing Arcade");
                ArcadePlugin.inst.StartCoroutine(ArcadePlugin.PlayDecryptedLevel(path));
                return false;
            }
            return true;
        }

		[HarmonyPatch("PlayLevel")]
		[HarmonyPrefix]
		private static bool PlayLevel(GameManager __instance, AudioClip _song)
		{
			if (ArcadePlugin.DifferentLoad.Value && EditorManager.inst == null)
			{
				//Figure out which of the three below lines of code causes the pause continue bug.
				//-------------------------------------------------------------------------------
				AudioManager.inst.CurrentAudioSource.Pause();
				AudioManager.inst.CurrentAudioSource.clip = _song;
				ObjectManager.inst.StartCoroutine(RTFile.IupdateObjects());
				//-------------------------------------------------------------------------------
				__instance.Camera.GetComponent<Camera>().rect = new Rect(0f, 0f, 1f, 1f);
				__instance.CameraPerspective.GetComponent<Camera>().rect = new Rect(0f, 0f, 1f, 1f);
				__instance.UpdateTimeline();
				__instance.songLength = _song.length;
				__instance.StartCoroutine(playBuffer(__instance, _song));
				return false;
			}
			return true;
		}

		[HarmonyPatch("playBuffer")]
		[HarmonyPrefix]
		private static bool playBufferPrefix(GameManager __instance, ref IEnumerator __result, AudioClip __0)
		{
			if (ArcadePlugin.DifferentLoad.Value && EditorManager.inst == null)
			{
				__result = playBuffer(__instance, __0);
				return false;
			}
			return true;
		}

		private static IEnumerator playBuffer(GameManager __instance, AudioClip _song)
		{
			__instance.gameState = GameManager.State.Playing;

			yield return new WaitForSecondsRealtime(0.2f);
			foreach (DataManager.GameData.BeatmapObject beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
				{
					ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
					gameObjectRef.sequence.all.Goto(-1f, false);
					gameObjectRef.sequence.col.Goto(-1f, false);
				}
			}
			__instance.ResetCheckpoints();
			AudioManager.inst.PlayMusic(null, _song, true, 0.5f, false);
			AudioManager.inst.SetPitch(__instance.getPitch());

			//Dunno how to reference delegates outside of their own class.
			//GameManager.UpdatedAudioPos(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
			__instance.introAnimator.SetTrigger("play");
			__instance.SpawnPlayers(DataManager.inst.gameData.beatmapData.checkpoints[0].pos);
			yield break;
		}

		[HarmonyPatch("EndOfLevel")]
		[HarmonyPrefix]
		private static bool EndOfLevelPatch(GameManager __instance)
		{
			AudioManager.inst.CurrentAudioSource.Pause();
			GameManager.inst.players.SetActive(false);
			InputDataManager.inst.SetAllControllerRumble(0f);
			__instance.gameState = GameManager.State.Paused;
			__instance.timeline.gameObject.SetActive(false);
			__instance.menuUI.GetComponentInChildren<Image>().enabled = true;
			if (DataManager.inst.GetSettingBool("IsArcade", false))
			{
				int workshop_id = __instance.currentArcadeLevel.beatmap.workshop_id;
				int prevHits = SaveManager.inst.ArcadeSaves.ContainsKey(workshop_id) ? SaveManager.inst.ArcadeSaves[workshop_id].Hits.Count : -1;
				if (SaveManager.inst.ArcadeSaves.ContainsKey(workshop_id))
				{
					if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0)
					{
						SaveManager.inst.ArcadeSaves[workshop_id].Deaths = __instance.deaths;
						SaveManager.inst.ArcadeSaves[workshop_id].Hits = __instance.hits;
						SaveManager.inst.ArcadeSaves[workshop_id].Finished = true;
					}
				}
				else if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0)
				{
					SaveManager.inst.ArcadeSaves.Add(workshop_id, new SaveManager.SaveGroup.Save());
					SaveManager.inst.ArcadeSaves[workshop_id].LevelID = workshop_id;
					SaveManager.inst.ArcadeSaves[workshop_id].Deaths = __instance.deaths;
					SaveManager.inst.ArcadeSaves[workshop_id].Hits = __instance.hits;
					SaveManager.inst.ArcadeSaves[workshop_id].Finished = true;
				}
				int index = __instance.menuUI.GetComponent<InterfaceController>().interfaceBranches.FindIndex((InterfaceController.InterfaceBranch x) => x.name == "end_of_level");
				int index2 = __instance.menuUI.GetComponent<InterfaceController>().interfaceBranches.FindIndex((InterfaceController.InterfaceBranch x) => x.name == "getsong");
				int index3 = __instance.menuUI.GetComponent<InterfaceController>().interfaceBranches.FindIndex((InterfaceController.InterfaceBranch x) => x.name == "end_of_level_more_info");
				int num = 5;
				int num2 = 24;
				int num3 = 2;
				int num4 = 11;
				int[] hitsNormalized = new int[num2 + 1];
				foreach (var playerDataPoint in __instance.hits)
				{
					int num5 = (int)RTMath.SuperLerp(0f, AudioManager.inst.CurrentAudioSource.clip.length, 0f, (float)num2, playerDataPoint.time);
					Debug.Log(num5);
					hitsNormalized[num5]++;
				}
				DataManager.LevelRank levelRank = DataManager.inst.levelRanks.Find((DataManager.LevelRank x) => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
				DataManager.LevelRank levelRank2 = DataManager.inst.levelRanks.Find((DataManager.LevelRank x) => prevHits >= x.minHits && prevHits <= x.maxHits);
				if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) == 0)
				{
					levelRank = DataManager.inst.levelRanks.Find((DataManager.LevelRank x) => x.name == "-");
					levelRank2 = null;
				}
				if (levelRank.name == "SS")
				{
					SteamWrapper.inst.achievements.SetAchievement("SS_RANK");
				}
				else if (levelRank.name == "F")
				{
					SteamWrapper.inst.achievements.SetAchievement("F_RANK");
				}
				List<string> list = LSText.WordWrap(levelRank.sayings[UnityEngine.Random.Range(0, levelRank.sayings.Length)], 32);
				string themeColorHex = LSColors.GetThemeColorHex("easy");
				string themeColorHex2 = LSColors.GetThemeColorHex("normal");
				string themeColorHex3 = LSColors.GetThemeColorHex("hard");
				string themeColorHex4 = LSColors.GetThemeColorHex("expert");
				__instance.Pause(false);
				for (int i = 0; i < num4; i++)
				{
					string text = "<b>";
					for (int j = 0; j < num2; j++)
					{
						int num6 = hitsNormalized.Take(j + 1).Sum();
						int num7 = (int)RTMath.SuperLerp(0f, 15f, 0f, (float)num4, (float)num6);
						string str;
						if (num6 == 0)
						{
							str = themeColorHex;
						}
						else if (num6 <= 3)
						{
							str = themeColorHex2;
						}
						else if (num6 <= 9)
						{
							str = themeColorHex3;
						}
						else
						{
							str = themeColorHex4;
						}
						for (int k = 0; k < num3; k++)
						{
							if (num7 == i)
							{
								text = text + "<color=" + str + "ff>▓</color>";
							}
							else if (num7 > i)
							{
								text += "<alpha=#22>▓";
							}
							else if (num7 < i)
							{
								text = text + "<color=" + str + "44>▓</color>";
							}
						}
					}
					text += "</b>";
					if (num == 5)
					{
						text = "<voffset=0.6em>" + text;
						if (prevHits == -1)
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
						}
						else if (prevHits > __instance.hits.Count && levelRank2 != null)
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color><size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%><color=#{2}><b>{3}</b></color>", new object[]
							{
							LSColors.ColorToHex(levelRank2.color),
							levelRank2.name,
							LSColors.ColorToHex(levelRank.color),
							levelRank.name
							});
						}
						else
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
						}
					}
					if (num >= 7 && list.Count > num - 7)
					{
						text = text + "       <alpha=#ff>" + list[num - 7];
					}
					InterfaceController.InterfaceElement interfaceElement = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, text);
					interfaceElement.branch = "end_of_level";
					__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches[index].elements[num] = interfaceElement;
					num++;
				}
				InterfaceController.InterfaceElement interfaceElement2 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("Level Summary - <b>{0}</b> by {1}", __instance.currentArcadeLevel.song.title, __instance.currentArcadeLevel.artist.Name));
				interfaceElement2.branch = "end_of_level";
				__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches[index].elements[2] = interfaceElement2;

				InterfaceController.InterfaceElement interfaceElement3 = null;
				ArcadePlugin.current += 1;
				Debug.LogFormat("{0}Selecting next Arcade level in queue [{1} / {2}]", ArcadePlugin.className, ArcadePlugin.current, (ArcadePlugin.arcadeQueue.Count - 1));
				if (ArcadePlugin.arcadeQueue.Count > 1 && ArcadePlugin.current < ArcadePlugin.arcadeQueue.Count)
				{
					SaveManager.inst.ArcadeQueue = ArcadePlugin.arcadeQueue[ArcadePlugin.current];
					interfaceElement3 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (__instance.currentArcadeLevel.artist.getUrl() != null) ? "[NEXT]:next&&[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info");
				}
				else
				{
					interfaceElement3 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (__instance.currentArcadeLevel.artist.getUrl() != null) ? "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info");
				}
				interfaceElement3.settings.Add("alignment", "center");
				interfaceElement3.settings.Add("orientation", "grid");
				interfaceElement3.settings.Add("width", "1");
				interfaceElement3.settings.Add("grid_h", "5");
				interfaceElement3.settings.Add("grid_v", "1");
				interfaceElement3.branch = "end_of_level";
				__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches[index].elements[17] = interfaceElement3;
				InterfaceController.InterfaceElement interfaceElement4 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "openlink::" + __instance.currentArcadeLevel.artist.getUrl());
				interfaceElement4.branch = "getsong";
				__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches[index2].elements[0] = interfaceElement4;
				__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches[index3].elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "More info to come soon!", "end_of_level_more_info");
				__instance.menuUI.GetComponent<InterfaceController>().SwitchBranch("end_of_level");

				var interfaceBranch = new InterfaceController.InterfaceBranch("next");
				interfaceBranch.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "loadscene::Game::true", "next"));
				__instance.menuUI.GetComponent<InterfaceController>().interfaceBranches.Add(interfaceBranch);

				return false;
			}
			__instance.menuUI.GetComponent<InterfaceController>().SwitchBranch("end_of_level");
			return false;
		}
	}
}
