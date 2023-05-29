using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

namespace ArcadiaCustoms.Patchers
{

    [HarmonyPatch(typeof(DataManager.BeatmapTheme))]
    public class DataManagerBeatmapThemePatch : MonoBehaviour
    {
        [HarmonyPatch("Lerp")]
        [HarmonyPrefix]
        private static bool Lerp(DataManager.BeatmapTheme __instance, ref DataManager.BeatmapTheme _start, ref DataManager.BeatmapTheme _end, float _val)
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("EditorPlugin") && EditorManager.inst == null)
            {
                __instance.guiColor = Color.Lerp(_start.guiColor, _end.guiColor, _val);
                __instance.backgroundColor = Color.Lerp(_start.backgroundColor, _end.backgroundColor, _val);
                for (int i = 0; i < 4; i++)
                {
                    if (_start.playerColors[i] != null && _end.playerColors[i] != null)
                    {
                        __instance.playerColors[i] = Color.Lerp(_start.GetPlayerColor(i), _end.GetPlayerColor(i), _val);
                    }
                }

                int maxObj = 9;
                if (_start.objectColors.Count > 9 || _end.objectColors.Count > 9)
                {
                    maxObj = 18;
                }

                for (int j = 0; j < maxObj; j++)
                {
                    if (_start.objectColors[j] != null && _end.objectColors[j] != null)
                    {
                        __instance.objectColors[j] = Color.Lerp(_start.GetObjColor(j), _end.GetObjColor(j), _val);
                    }
                }
                for (int k = 0; k < 9; k++)
                {
                    if (_start.backgroundColors[k] != null && _end.backgroundColors[k] != null)
                    {
                        __instance.backgroundColors[k] = Color.Lerp(_start.GetBGColor(k), _end.GetBGColor(k), _val);
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch("Parse")]
        [HarmonyPrefix]
        private static bool ParsePrefix(DataManager.BeatmapTheme __instance, ref DataManager.BeatmapTheme __result, JSONNode __0, bool __1)
        {
            if (!GameObject.Find("BepInEx_Manager").GetComponentByName("EditorPlugin") && EditorManager.inst == null)
            {
                DataManager.BeatmapTheme beatmapTheme = new DataManager.BeatmapTheme();
                beatmapTheme.id = DataManager.inst.AllThemes.Count().ToString();
                if (__0["id"] != null)
                    beatmapTheme.id = __0["id"];
                beatmapTheme.name = "name your themes!";
                if (__0["name"] != null)
                    beatmapTheme.name = __0["name"];
                beatmapTheme.guiColor = LSColors.gray800;
                if (__0["gui"] != null)
                    beatmapTheme.guiColor = LSColors.HexToColorAlpha(__0["gui"]);
                beatmapTheme.backgroundColor = LSColors.gray100;
                if (__0["bg"] != null)
                    beatmapTheme.backgroundColor = LSColors.HexToColor(__0["bg"]);
                if (__0["players"] == null)
                {
                    beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("E57373FF"));
                    beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("64B5F6FF"));
                    beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("81C784FF"));
                    beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha("FFB74DFF"));
                }
                else
                {
                    int num = 0;
                    foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["players"].AsArray)
                    {
                        JSONNode hex = keyValuePair;
                        if (num <= 3)
                        {
                            if (hex != null)
                            {
                                beatmapTheme.playerColors.Add(LSColors.HexToColorAlpha(hex));
                            }
                            else
                                beatmapTheme.playerColors.Add(LSColors.pink500);
                            ++num;
                        }
                        else
                            break;
                    }
                    while (beatmapTheme.playerColors.Count <= 3)
                        beatmapTheme.playerColors.Add(LSColors.pink500);
                }
                if (__0["objs"] == null)
                {
                    beatmapTheme.objectColors.Add(LSColors.pink100);
                    beatmapTheme.objectColors.Add(LSColors.pink200);
                    beatmapTheme.objectColors.Add(LSColors.pink300);
                    beatmapTheme.objectColors.Add(LSColors.pink400);
                    beatmapTheme.objectColors.Add(LSColors.pink500);
                    beatmapTheme.objectColors.Add(LSColors.pink600);
                    beatmapTheme.objectColors.Add(LSColors.pink700);
                    beatmapTheme.objectColors.Add(LSColors.pink800);
                    beatmapTheme.objectColors.Add(LSColors.pink900);
                }
                else
                {
                    int num = 0;
                    Color color = LSColors.pink500;
                    foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["objs"].AsArray)
                    {
                        JSONNode hex = keyValuePair;
                        if (num <= 17)
                        {
                            if (hex != null)
                            {
                                beatmapTheme.objectColors.Add(LSColors.HexToColorAlpha(hex));
                                color = LSColors.HexToColorAlpha(hex);
                            }
                            else
                                beatmapTheme.objectColors.Add(LSColors.pink500);
                            ++num;
                        }
                        else
                            break;
                    }
                    while (beatmapTheme.objectColors.Count <= 17)
                        beatmapTheme.objectColors.Add(color);
                }
                if (__0["bgs"] == null)
                {
                    beatmapTheme.backgroundColors.Add(LSColors.gray100);
                    beatmapTheme.backgroundColors.Add(LSColors.gray200);
                    beatmapTheme.backgroundColors.Add(LSColors.gray300);
                    beatmapTheme.backgroundColors.Add(LSColors.gray400);
                    beatmapTheme.backgroundColors.Add(LSColors.gray500);
                }
                else
                {
                    int num = 0;
                    Color color = LSColors.pink500;
                    foreach (KeyValuePair<string, JSONNode> keyValuePair in __0["bgs"].AsArray)
                    {
                        JSONNode hex = keyValuePair;
                        if (num <= 8)
                        {
                            if (hex != null)
                            {
                                beatmapTheme.backgroundColors.Add(LSColors.HexToColor(hex));
                                color = LSColors.HexToColor(hex);
                            }
                            else
                                beatmapTheme.backgroundColors.Add(LSColors.pink500);
                            ++num;
                        }
                        else
                            break;
                    }
                    while (beatmapTheme.backgroundColors.Count <= 8)
                        beatmapTheme.backgroundColors.Add(color);
                }
                if (__1)
                {
                    DataManager.inst.CustomBeatmapThemes.Add(beatmapTheme);
                    if (DataManager.inst.BeatmapThemeIDToIndex.ContainsKey(int.Parse(beatmapTheme.id)))
                    {
                        if (EditorManager.inst != null)
                            EditorManager.inst.DisplayNotification("Unable to Load theme [" + beatmapTheme.name + "]", 2f, EditorManager.NotificationType.Error);
                    }
                    else
                    {
                        DataManager.inst.BeatmapThemeIndexToID.Add(DataManager.inst.AllThemes.Count() - 1, int.Parse(beatmapTheme.id));
                        DataManager.inst.BeatmapThemeIDToIndex.Add(int.Parse(beatmapTheme.id), DataManager.inst.AllThemes.Count() - 1);
                    }
                }
                __result = beatmapTheme;
                return false;
            }
            return true;
        }
    }
}
