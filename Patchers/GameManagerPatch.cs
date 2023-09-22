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

			DOTween.Clear();
			DataManager.inst.gameData = null;
			DataManager.inst.gameData = new DataManager.GameData();

			ArcadePlugin.timeInLevelOffset = Time.time;
			ArcadePlugin.timeInLevel = 0f;
			finished = false;
			//ArcadePlugin.inst.StartCoroutine(ArcadePlugin.FixTimeline());
		}

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void SetAntialiasing(GameManager __instance)
        {
            if (DataManager.inst.gameData.beatmapData != null)
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

            if (__instance.LiveTheme.objectColors.Count == 9)
            {
                for (int i = 0; i < 9; i++)
                {
					__instance.LiveTheme.objectColors.Add(LSColors.pink900);
                }
            }

			if (!finished)
				ArcadePlugin.timeInLevel = ArcadePlugin.timeInLevelOffset - Time.time;
        }

		public static bool finished = false;

        [HarmonyPatch("LoadLevelCurrent")]
        [HarmonyPrefix]
        static bool LevelDecrypter()
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
		static bool PlayLevel(GameManager __instance, AudioClip _song)
		{
			if (ArcadePlugin.DifferentLoad.Value && EditorManager.inst == null)
			{
				//Figure out which of the three below lines of code causes the pause continue bug.
				//-------------------------------------------------------------------------------
				AudioManager.inst.CurrentAudioSource.Pause();
				AudioManager.inst.CurrentAudioSource.clip = _song;
				ObjectManager.inst.updateObjects();
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
		static bool playBufferPrefix(GameManager __instance, ref IEnumerator __result, AudioClip __0)
		{
			if (SaveManager.inst.ArcadeQueue != null)
			{
				var f = SaveManager.inst.ArcadeQueue.AudioFileStr.Replace("\\", "/").Replace("/level.ogg", "").Replace("/level.wav", "").Replace("/song.lsen", "") + "/LevelStartup.cs";

				Debug.Log($"{ArcadePlugin.className}LevelStartup Path: {f}");

				if (RTFile.FileExists(f))
				{
					var s = RTFile.ReadFromFile(f);

					Debug.Log($"{ArcadePlugin.className}Startup Code: {s}");

					if (!s.Contains("File."))
						__instance.StartCoroutine(RTCode.IEvaluate(s));
				}
			}

			if (ArcadePlugin.DifferentLoad.Value && EditorManager.inst == null)
			{
				__result = playBuffer(__instance, __0);
				return false;
			}
			return true;
		}

		static IEnumerator playBuffer(GameManager __instance, AudioClip _song)
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

			if (__instance.introMain != null)
			{
				__instance.introAnimator.SetTrigger("play");
			}

			__instance.SpawnPlayers(DataManager.inst.gameData.beatmapData.checkpoints[0].pos);
			yield return new WaitForSeconds(0.2f);
            EventManager.inst.updateEvents();

            yield break;
		}

		[HarmonyPatch("EndOfLevel")]
		[HarmonyPrefix]
		static bool EndOfLevelPatch(GameManager __instance)
		{
			finished = true;
			AudioManager.inst.CurrentAudioSource.Pause();
			GameManager.inst.players.SetActive(false);
			InputDataManager.inst.SetAllControllerRumble(0f);

			AudioManager.inst.CurrentAudioSource.time = 0f;
			AudioManager.inst.CurrentAudioSource.Play();

			__instance.gameState = GameManager.State.Paused;
			__instance.timeline.gameObject.SetActive(false);
			__instance.menuUI.GetComponentInChildren<Image>().enabled = true;

			var ic = __instance.menuUI.GetComponent<InterfaceController>();

			if (DataManager.inst.GetSettingBool("IsArcade", false))
			{
				int workshop_id = __instance.currentArcadeLevel.beatmap.workshop_id;
				int prevHits = SaveManager.inst.ArcadeSaves.ContainsKey(workshop_id) ? SaveManager.inst.ArcadeSaves[workshop_id].Hits.Count : -1;

				if (DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0)
                {
					SaveManager.SaveGroup.Save save = null;
					if (SaveManager.inst.ArcadeSaves.ContainsKey(workshop_id))
						save = SaveManager.inst.ArcadeSaves[workshop_id];
					else
					{
						save = new SaveManager.SaveGroup.Save();
						SaveManager.inst.ArcadeSaves.Add(workshop_id, save);
						save.LevelID = workshop_id;
					}

					save.Deaths = __instance.deaths;
					save.Hits = __instance.hits;
					save.Finished = true;
				}

				//More Info
                {
					var moreInfo = ic.interfaceBranches.Find(x => x.name == "end_of_level_more_info");
					moreInfo.elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You died a total of " + __instance.deaths.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[6] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You got hit a total of " + __instance.hits.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[7] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Total song time: " + AudioManager.inst.CurrentAudioSource.clip.length, "end_of_level_more_info");
					moreInfo.elements[8] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Time in level: " + ArcadePlugin.timeInLevel, "end_of_level_more_info");
				}

				int index = ic.interfaceBranches.FindIndex(x => x.name == "end_of_level");
				int index2 = ic.interfaceBranches.FindIndex(x => x.name == "getsong");
				int index3 = ic.interfaceBranches.FindIndex(x => x.name == "end_of_level_more_info");
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
				var levelRank = DataManager.inst.levelRanks.Find((DataManager.LevelRank x) => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
				var levelRank2 = DataManager.inst.levelRanks.Find((DataManager.LevelRank x) => prevHits >= x.minHits && prevHits <= x.maxHits);
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
				List<string> list = LSText.WordWrap(levelRank.sayings[Random.Range(0, levelRank.sayings.Length)], 32);
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
				ic.interfaceBranches[index].elements[2] = interfaceElement2;

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
				ic.interfaceBranches[index2].elements[0] = interfaceElement4;
				//ic.interfaceBranches[index3].elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "More info to come soon!", "end_of_level_more_info");
				ic.SwitchBranch("end_of_level");

				var interfaceBranch = new InterfaceController.InterfaceBranch("next");
				interfaceBranch.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "loadscene::Game::true", "next"));
				ic.interfaceBranches.Add(interfaceBranch);

				return false;
			}
			ic.SwitchBranch("end_of_level");
			return false;
		}

		[HarmonyPatch("LoadLevelFromArcadeQueue")]
		[HarmonyPrefix]
		static bool LoadLevelFromArcadeQueuePrefix(GameManager __instance, ref IEnumerator __result, SaveManager.ArcadeLevel __0)
        {
			__result = LoadLevelFromArcadeQueue(__instance, __0);
			return false;
        }

		public static IEnumerator LoadLevelFromArcadeQueue(GameManager __instance, SaveManager.ArcadeLevel _level)
		{
			string rawJSON = _level.BeatmapJsonStr;
			AudioManager.inst.CurrentAudioSource.Pause();
			AudioManager.inst.CurrentAudioSource.time = 0f;
			__instance.currentArcadeLevel = _level.MetaData;

			if (!string.IsNullOrEmpty(_level.AudioFileStr))
			{
				yield return __instance.StartCoroutine(FileManager.inst.LoadMusicFileRaw(_level.AudioFileStr, false, delegate (AudioClip _song)
				{
					AudioClip song = _song;
					if (_level.BeatmapSong == null)
					{
						rawJSON = __instance.defaultLevel.BeatmapJson.text;
						song = __instance.defaultLevel.BeatmapSong;
					}

					__instance.currentLevelName = _level.MetaData.song.title;
					Debug.Log($"{ArcadePlugin.className}Loaded level from level [{_level.MetaData.song.title}]");
					ParseLevel(__instance, DataManager.inst.gameData.UpdateBeatmap(rawJSON, _level.MetaData.beatmap.game_version), song, _level.MetaData.song.title, _level.MetaData.artist.Name);
				}));
			}
			else if (_level.BeatmapSong != null)
            {
				AudioClip song = _level.BeatmapSong;

				__instance.currentLevelName = _level.MetaData.song.title;
				Debug.Log($"{ArcadePlugin.className}Loaded level from level [{_level.MetaData.song.title}]");
				ParseLevel(__instance, DataManager.inst.gameData.UpdateBeatmap(rawJSON, _level.MetaData.beatmap.game_version), song, _level.MetaData.song.title, _level.MetaData.artist.Name);
			}

			yield break;
		}

		[HarmonyPatch("ParseLevel")]
		[HarmonyPrefix]
		static bool ParseLevel(GameManager __instance, string _rawJSON, AudioClip _song, string _songName, string _artistName)
		{
			__instance.gameState = GameManager.State.Parsing;
			if (!string.IsNullOrEmpty(_rawJSON))
			{
				DataManager.inst.gameData.ParseBeatmap(_rawJSON);
				DiscordController.inst.OnStateChange("Level: " + __instance.currentLevelName);
				__instance.introTitle.text = _songName;
				__instance.introArtist.text = _artistName;
				PlayLevel(__instance, _song);
				return false;
			}
			Debug.LogErrorFormat("{0}Null raw json for level [{1}]", new object[]
			{
				ArcadePlugin.className,
				SaveManager.inst.CurrentStoryLevel.SongName
			});
			return false;
		}
	}
}
