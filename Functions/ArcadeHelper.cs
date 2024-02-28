using System.Collections.Generic;
using System.Linq;

using LSFunctions;

using UnityEngine;
using UnityEngine.UI;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace ArcadiaCustoms.Functions
{
    public static class ArcadeHelper
    {

		public static void EndOfLevel()
		{
			var __instance = GameManager.inst;
			GameManager.inst.players.SetActive(false);
			InputDataManager.inst.SetAllControllerRumble(0f);

			__instance.gameState = GameManager.State.Paused;
			__instance.timeline.gameObject.SetActive(false);
			__instance.menuUI.GetComponentInChildren<Image>().enabled = true;

			var ic = __instance.menuUI.GetComponent<InterfaceController>();

			var metadata = LevelManager.CurrentLevel.metadata;

			if (DataManager.inst.GetSettingBool("IsArcade", false))
			{
				Debug.Log($"{__instance.className}Setting Player Data");
				int prevHits = LevelManager.CurrentLevel.playerData != null ? LevelManager.CurrentLevel.playerData.Hits : -1;

				if (!PlayerManager.IsZenMode)
				{
					if (LevelManager.CurrentLevel.playerData == null)
					{
						LevelManager.CurrentLevel.playerData = new LevelManager.PlayerData
						{
							ID = LevelManager.CurrentLevel.id,
						};
					}

					LevelManager.CurrentLevel.playerData.Deaths = __instance.deaths.Count;
					LevelManager.CurrentLevel.playerData.Hits = __instance.hits.Count;
					LevelManager.CurrentLevel.playerData.Completed = true;
					LevelManager.CurrentLevel.playerData.Boosts = LevelManager.BoostCount;

					if (LevelManager.Saves.Has(x => x.ID == LevelManager.CurrentLevel.id))
					{
						var saveIndex = LevelManager.Saves.FindIndex(x => x.ID == LevelManager.CurrentLevel.id);
						LevelManager.Saves[saveIndex] = LevelManager.CurrentLevel.playerData;
					}
					else
						LevelManager.Saves.Add(LevelManager.CurrentLevel.playerData);

					LevelManager.SaveProgress();
				}

				Debug.Log($"{__instance.className}Setting More Info");
				//More Info
				{
					var moreInfo = ic.interfaceBranches.Find(x => x.name == "end_of_level_more_info");
					moreInfo.elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You died a total of " + __instance.deaths.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[6] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You got hit a total of " + __instance.hits.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[7] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You boosted a total of " + LevelManager.BoostCount + " times.", "end_of_level_more_info");
					moreInfo.elements[8] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Total song time: " + AudioManager.inst.CurrentAudioSource.clip.length, "end_of_level_more_info");
					moreInfo.elements[9] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Time in level: " + ArcadePlugin.timeInLevel, "end_of_level_more_info");
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
					hitsNormalized[num5]++;
				}

				Debug.Log($"{__instance.className}Setting Level Ranks");
				var levelRank = DataManager.inst.levelRanks.Find(x => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
				var newLevelRank = DataManager.inst.levelRanks.Find(x => prevHits >= x.minHits && prevHits <= x.maxHits);

				if (PlayerManager.IsZenMode)
				{
					levelRank = DataManager.inst.levelRanks.Find(x => x.name == "-");
					newLevelRank = null;
				}

				Debug.Log($"{__instance.className}Setting Achievements");
				if (levelRank.name == "SS")
					SteamWrapper.inst.achievements.SetAchievement("SS_RANK");
				else if (levelRank.name == "F")
					SteamWrapper.inst.achievements.SetAchievement("F_RANK");

				Debug.Log($"{__instance.className}Setting End UI");
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
						else if (prevHits > __instance.hits.Count && newLevelRank != null)
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color><size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%><color=#{2}><b>{3}</b></color>", new object[]
							{
								LSColors.ColorToHex(newLevelRank.color),
								newLevelRank.name,
								LSColors.ColorToHex(levelRank.color),
								levelRank.name
							});
						}
						else
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
						}
					}
					if (num == 7)
					{
						text = "<voffset=0.6em>" + text;

						text += $"       <voffset=0em><size=300%><color=#{LSColors.ColorToHex(levelRank.color)}><b>{LevelManager.CalculateAccuracy(__instance.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}%</b></color>";
					}
					if (num >= 9 && list.Count > num - 9)
					{
						text = text + "       <alpha=#ff>" + list[num - 9];
					}

					var interfaceElement = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, text);
					interfaceElement.branch = "end_of_level";
					ic.interfaceBranches[index].elements[num] = interfaceElement;
					num++;
				}
				var interfaceElement2 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("Level Summary - <b>{0}</b> by {1}", metadata.song.title, metadata.artist.Name));
				interfaceElement2.branch = "end_of_level";
				ic.interfaceBranches[index].elements[2] = interfaceElement2;

				InterfaceController.InterfaceElement interfaceElement3 = null;
				LevelManager.current++;

				Debug.LogFormat("{0}Selecting next Arcade level in queue [{1} / {2}]", ArcadePlugin.className, LevelManager.current, LevelManager.ArcadeQueue.Count - 1);
				if (LevelManager.ArcadeQueue.Count > 1 && LevelManager.current < LevelManager.ArcadeQueue.Count)
				{
					LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.current];
					interfaceElement3 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[NEXT]:next&&[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info");
				}
				else
				{
					interfaceElement3 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[MORE INFO]:end_of_level_more_info");
				}

				interfaceElement3.settings.Add("alignment", "center");
				interfaceElement3.settings.Add("orientation", "grid");
				interfaceElement3.settings.Add("width", "1");
				interfaceElement3.settings.Add("grid_h", "5");
				interfaceElement3.settings.Add("grid_v", "1");
				interfaceElement3.branch = "end_of_level";
				ic.interfaceBranches[index].elements[17] = interfaceElement3;
				var interfaceElement4 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "openlink::" + metadata.artist.getUrl());
				interfaceElement4.branch = "getsong";
				ic.interfaceBranches[index2].elements[0] = interfaceElement4;

				var interfaceBranch = new InterfaceController.InterfaceBranch("next");
				interfaceBranch.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "loadscene::Game::true", "next"));
				ic.interfaceBranches.Add(interfaceBranch);
			}
			ic.SwitchBranch("end_of_level");
		}
	}
}
