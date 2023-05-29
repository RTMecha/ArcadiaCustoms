using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using LSFunctions;

namespace ArcadiaCustoms
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

		[HarmonyPatch("GenerateSongUI")]
		[HarmonyPrefix]
		public static bool GenerateSongUI(ArcadeController __instance)
		{
			AccessTools.Field(typeof(ArcadeController), "generatedArcadeInfo").SetValue(__instance, true);
			DataManager.inst.UpdateSettingString("currentLevelVersionNumber", SaveManager.inst.ArcadeQueue.MetaData.beatmap.game_version);
			DiscordController.inst.OnStateChange("Selected: " + SaveManager.inst.ArcadeQueue.MetaData.song.title);
			__instance.pauseMusicChange = true;
			__instance.ic.interfaceBranches[17].elements.Clear();
			string data = (SaveManager.inst.ArcadeQueue.MetaData.artist.getUrl() == null) ? "[PLAY]:playsong&&[BACK]:gobacktoarcade&&:&&:&&[SETTINGS]:arcadesettings" : "[PLAY]:playsong&&[BACK]:gobacktoarcade&&:&&[SETTINGS]:arcadesettings&&[GET SONG]:getsong";
			InterfaceController.InterfaceElement interfaceElement = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, data);
			interfaceElement.settings.Add("orientation", "grid");
			interfaceElement.settings.Add("width", "1");
			interfaceElement.settings.Add("grid_h", "5");
			interfaceElement.settings.Add("grid_v", "1");
			interfaceElement.settings.Add("alignment", "center");
			interfaceElement.branch = "arcadeinfo";
			InterfaceController.InterfaceElement item = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, " ", "arcadeinfo");
			InterfaceController.InterfaceElement interfaceElement2 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "-------------------------------------------------------------------------------------------------------------");
			interfaceElement2.settings.Add("alignment", "center");
			interfaceElement2.branch = "arcadeinfo";
			__instance.ic.interfaceBranches[17].elements.Add(item);
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "<b>Level Info</b>", "arcadeinfo"));
			__instance.ic.interfaceBranches[17].elements.Add(interfaceElement2);
			__instance.ic.interfaceBranches[17].elements.Add(item);
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <b><size=200%>{0}", LSText.ClampString(SaveManager.inst.ArcadeQueue.MetaData.song.title.Replace(Environment.NewLine, ""), 40)).Replace(":", "{{colon}}"), "arcadeinfo"));
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <size=125%>By {0}", LSText.ClampString(SaveManager.inst.ArcadeQueue.MetaData.artist.Name.Replace(Environment.NewLine, ""), 35)).Replace(":", "{{colon}}"), "arcadeinfo"));
			__instance.ic.interfaceBranches[17].elements.Add(item);
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <b>Level by:</b> {0}", LSText.ClampString(SaveManager.inst.ArcadeQueue.MetaData.creator.steam_name.Replace(Environment.NewLine, ""), 64)).Replace(":", "{{colon}}"), "arcadeinfo"));
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <b>Difficulty:</b> {0}", string.Format("{1}<size=8> <voffset=-13><size=64><color=#{0}>■", LSColors.ColorToHex(SaveManager.inst.ArcadeQueue.MetaData.song.getDifficultyColor()), SaveManager.inst.ArcadeQueue.MetaData.song.getDifficulty())).Replace(":", "{{colon}}"), "arcadeinfo"));
			int index = 0;
			foreach (var itemA in ArcadeManager.inst.ArcadeList)
            {
				if (SaveManager.inst.ArcadeQueue.AudioFileStr.Contains(itemA.folder))
                {
					index = ArcadeManager.inst.ArcadeList.IndexOf(itemA);
                }
            }
			if (SaveManager.inst.ArcadeSaves.ContainsKey(index))
			{
				var levelRank = DataManager.inst.levelRanks[0];
				for (int i = 0; i < DataManager.inst.levelRanks.Count; i++)
                {
					if (SaveManager.inst.ArcadeSaves[index].Hits.Count > DataManager.inst.levelRanks[i].minHits && SaveManager.inst.ArcadeSaves[index].Hits.Count < DataManager.inst.levelRanks[i].maxHits)
                    {
						levelRank = DataManager.inst.levelRanks[i];
                    }
                }
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <b>Level Rank (WIP):</b> <#{1}>{0}", levelRank.name, LSColors.ColorToHex(levelRank.color)), "arcadeinfo"));
			}
			else
            {
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("                  <b>Level Rank (WIP):</b> <#{1}>{0}", DataManager.inst.levelRanks[0].name, LSColors.ColorToHex(DataManager.inst.levelRanks[0].color)), "arcadeinfo"));
			}
			__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "<b>Description</b>", "arcadeinfo"));
			List<string> list = LSText.WordWrap(SaveManager.inst.ArcadeQueue.MetaData.song.description, 96);
			if (list.Count > 0)
			{
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, list[0].TrimStart(Array.Empty<char>()), "arcadeinfo"));
			}
			else
			{
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "No Description Given.", "arcadeinfo"));
			}
			if (list.Count > 1)
			{
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, list[1].TrimStart(Array.Empty<char>()), "arcadeinfo"));
			}
			else
			{
				__instance.ic.interfaceBranches[17].elements.Add(item);
			}
			if (list.Count > 2)
			{
				__instance.ic.interfaceBranches[17].elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, list[2].TrimStart(Array.Empty<char>()), "arcadeinfo"));
			}
			else
			{
				__instance.ic.interfaceBranches[17].elements.Add(item);
			}
			__instance.ic.interfaceBranches[17].elements.Add(item);
			__instance.ic.interfaceBranches[17].elements.Add(interfaceElement);
			__instance.ic.interfaceBranches[17].elements.Add(item);
			__instance.ic.interfaceBranches[17].elements.Add(interfaceElement2);
			InterfaceController.InterfaceElement interfaceElement3 = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "{{col:#F05355:Project Arrhythmia}} Unified Operating System | Version {{versionNumber}}", "arcadeinfo");
			interfaceElement3.settings.Add("alignment", "right");
			__instance.ic.interfaceBranches[17].elements.Add(interfaceElement3);
			return false;
		}
	}
}
