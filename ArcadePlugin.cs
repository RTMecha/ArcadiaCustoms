using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;

using SimpleJSON;
using DG.Tweening;
using TMPro;
using InControl;
using LSFunctions;
using Steamworks;

using ArcadiaCustoms.Functions;
using ArcadiaCustoms.Patchers;

namespace ArcadiaCustoms
{
    [BepInPlugin("com.mecha.arcadiacustoms", "ArcadiaCustoms", " 1.4.0")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ArcadePlugin : BaseUnityPlugin
    {
        //TODO
        //Somehow add the level rank to the Arcade select menu somehow.
        //Implement difficulty modes (Much like what JSaB has, but allowing for more creativity with different options rather than just "Hardcore mode")
        //Implement the shine effect when you've SS ranked a level.

        public static ArcadePlugin inst;
        public static string className = "[<color=#F5501B>ArcadiaCustoms</color>] " + PluginInfo.PLUGIN_VERSION + "\n";

        public static InterfaceController ic;
        public static string beatmapsstory = "beatmaps/story";

        public static ConfigEntry<bool> AntiAliasing { get; set; }
        public static ConfigEntry<bool> ReloadArcadeList { get; set; }

        public static ConfigEntry<bool> DifferentLoad { get; set; }

        public static bool LoadEnabled = true;

        public static int current;
        public static List<SaveManager.ArcadeLevel> arcadeQueue = new List<SaveManager.ArcadeLevel>();

        private void Awake()
        {
            inst = this;

            Logger.LogInfo($"Plugin Arcadia Customs is loaded!");
            AntiAliasing = Config.Bind("Antialiasing", "Enabled", false, "If antialiasing is on or not.");
            ReloadArcadeList = Config.Bind("Arcade", "Reload list", true, "If enabled, this will reload the arcade list every time you enter the Specify Simulations screen. Make sure to turn this off if you want to quickly exit the menu.");
            DifferentLoad = Config.Bind("Game", "Music loads first", true, "If enabled, the music will be loaded first when entering a level. Having it enabled can fix issues with queue levels not breaking, but might cause the game to take a bit longer to load.");

            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

            Harmony harmony = new Harmony("Arcade");

            harmony.PatchAll(typeof(ArcadePlugin));
            harmony.PatchAll(typeof(GameManagerPatch));
            harmony.PatchAll(typeof(ArcadeControllerPatch));
        }

        private static void UpdateSettings(object sender, EventArgs e)
        {
            if (GameManager.inst != null)
            {
                PostProcessLayer aliasing = GameObject.Find("Main Camera").GetComponent<PostProcessLayer>();
                if (AntiAliasing.Value == true)
                {
                    aliasing.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                }
                else
                {
                    aliasing.antialiasingMode = PostProcessLayer.Antialiasing.None;
                }
            }
        }

        public static IEnumerator SetupScene()
        {
            yield return new WaitForSeconds(20F);
            MainMenuTester();
            yield break;
        }
        public static void MainMenuTester()
        {
            var menu = new GameObject("Main Menu System");
            menu.AddComponent<MainMenuTest>();
        }

        public static IEnumerator FixTimeline()
        {
            yield return new WaitForSeconds(0.2f);
            GameManager.inst.UpdateTimeline();
            if (DifferentLoad.Value)
            {
                inst.StartCoroutine(RTFile.IupdateObjects());
            }
            yield break;
        }

        [HarmonyPatch(typeof(InputSelectManager), "Start")]
        [HarmonyPostfix]
        private static void ResetArcadeInSelection()
        {
            LSHelpers.HideCursor();
            inst.StartCoroutine(GetLevelList());
        }

        public static bool currentlyLoading = false;
        public static IEnumerator GetLevelList()
        {
            float delay = 0f;
            if (ReloadArcadeList.Value == true && !currentlyLoading)
            {
                currentlyLoading = true;
                ArcadeManager.inst.skippedLoad = false;
                ArcadeManager.inst.forcedSkip = false;
                DataManager.inst.UpdateSettingBool("IsArcade", true);

                List<FileManager.LevelFolder> folderList = FileManager.inst.GetFolderList(beatmapsstory);
                List<SteamWorkshop.SteamItem> steamItems = new List<SteamWorkshop.SteamItem>();

                ArcadeManager.inst.ArcadeAudioClips.Clear();
                ArcadeManager.inst.ArcadeImageFiles.Clear();
                ArcadeManager.inst.ArcadeList.Clear();
                arcadeQueue.Clear();

                foreach (var folder in folderList)
                {
                    yield return new WaitForSeconds(delay);
                    int range = folderList.IndexOf(folder);

                    Steamworks.PublishedFileId_t publishedFileId_T = new Steamworks.PublishedFileId_t((ulong)range);

                    SteamWorkshop.SteamItem steamItem = new SteamWorkshop.SteamItem(publishedFileId_T);

                    if (RTFile.FileExists(folder.fullPath + "/level.lsen"))
                    {
                        var js = FileManager.inst.LoadJSONFileRaw(folder.fullPath + "/level.lsen");
                        JSONNode jn = JSON.Parse(js);

                        byte[] llsb = new byte[jn["level.lsb"].Count];
                        byte[] sogg = new byte[jn["level.ogg"].Count];
                        byte[] ljpg = new byte[jn["level.jpg"].Count];
                        byte[] mlsb = new byte[jn["metadata.lsb"].Count];

                        for (int i = 0; i < llsb.Length; i++)
                        {
                            llsb[i] = (byte)jn["level.lsb"][i];
                        }

                        for (int i = 0; i < sogg.Length; i++)
                        {
                            sogg[i] = (byte)jn["level.ogg"][i];
                        }

                        for (int i = 0; i < ljpg.Length; i++)
                        {
                            ljpg[i] = (byte)jn["level.jpg"][i];
                        }

                        for (int i = 0; i < mlsb.Length; i++)
                        {
                            mlsb[i] = (byte)jn["metadata.lsb"][i];
                        }

                        var lvl = System.Text.Encoding.Default.GetString(llsb);

                        string rawJSON = DataManager.inst.gameData.UpdateBeatmap(lvl, DataManager.inst.metaData.beatmap.game_version);

                        gameData.ParseBeatmap(lvl);
                    }

                    if (RTFile.FileExists(folder.fullPath + "/metadata.lsb"))
                    {
                        string metadataStr = FileManager.inst.LoadJSONFileRaw(folder.fullPath + "/metadata.lsb");

                        if (!string.IsNullOrEmpty(metadataStr))
                        {
                            steamItem.metaData = DataManager.inst.ParseMetadata(metadataStr);

                            steamItem.itemID = range;
                            steamItem.id = publishedFileId_T;
                            steamItem.size = metadataStr.Length;
                            steamItem.folder = folder.fullPath;
                            steamItem.musicID = folder.name;

                            //ArcadeManager.inst.ArcadeAudioClips.Add(steamItem.itemID, null);
                            //ArcadeManager.inst.LastAudioClip = null;
                            ArcadeManager.inst.StartCoroutine(FileManager.inst.LoadMusicFileRaw(steamItem.folder + "/level.ogg", true, delegate (AudioClip _song)
                            {
                                _song.name = steamItem.itemID.ToString();
                                ArcadeManager.inst.ArcadeAudioClips.Add(steamItem.itemID, _song);
                                ArcadeManager.inst.LastAudioClip = _song;
                            }));

                            ArcadeManager.inst.StartCoroutine(FileManager.inst.LoadImageFileRaw(steamItem.folder + "/level.jpg", delegate (Sprite _cover)
                            {
                                ArcadeManager.inst.ArcadeImageFiles.Add(steamItem.itemID, _cover);
                            }, delegate (string _error)
                            {
                                ArcadeManager.inst.ArcadeImageFiles.Add(steamItem.itemID, ArcadeManager.inst.defaultImage);
                            }));
                        }

                        ArcadeManager.inst.ArcadeList.Add(steamItem);
                    }

                    delay += 0.0001f;
                }
                if (MainMenuTest.inst != null)
                {
                    MainMenuTest.inst.StartCoroutine(MainMenuTest.GenerateUIList());
                }

                currentlyLoading = false;
            }
            yield break;
        }

        public static List<DataManager.BeatmapTheme> beatmapThemes;
        public static Dictionary<int, int> beatmapThemesIDToIndex;
        public static Dictionary<int, int> beatmapThemesIndexToID;
        public static DataManager.GameData gameData = new DataManager.GameData();

        [HarmonyPatch(typeof(DataManager), "Start")]
        [HarmonyPostfix]
        private static void DataLists()
        {
            if (DataManager.inst.difficulties.Count != 7)
            {
                DataManager.inst.difficulties = new List<DataManager.Difficulty>
                {
                    new DataManager.Difficulty("Easy", LSColors.GetThemeColor("easy")),
                    new DataManager.Difficulty("Normal", LSColors.GetThemeColor("normal")),
                    new DataManager.Difficulty("Hard", LSColors.GetThemeColor("hard")),
                    new DataManager.Difficulty("Expert", LSColors.GetThemeColor("expert")),
                    new DataManager.Difficulty("Expert+", LSColors.GetThemeColor("expert+")),
                    new DataManager.Difficulty("Master", new Color(0.25f, 0.01f, 0.01f)),
                    new DataManager.Difficulty("Animation", LSColors.GetThemeColor("none"))
                };
            }

            if (DataManager.inst.linkTypes[3].name != "YouTube")
            {
                DataManager.inst.linkTypes = new List<DataManager.LinkType>
                {
                    new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
                    new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
                    new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
                    new DataManager.LinkType("Youtube", "https://www.youtube.com/user/{0}"),
                    new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/")
                };
            }

            if (DataManager.inst.AnimationList[1].Animation.keys[1].m_Time != 0.9999f)
            {
                DataManager.inst.AnimationList[1].Animation.keys[1].m_Time = 0.9999f;
                DataManager.inst.AnimationList[1].Animation.keys[1].m_Value = 0f;
            }
        }

        public static byte[] password = LSEncryption.AES_Encrypt(new byte[] { 9, 5, 7, 6, 4, 38, 6, 4, 3, 66, 43, 6, 47, 8, 54, 6 }, new byte[] { 99, 53, 43, 36, 43, 65, 43, 45 });

        public static IEnumerator DecryptLevel(string _filepath, Action<AudioClip> callback)
        {
            Debug.LogFormat("{0}Loading song...", className);
            string songPath = _filepath + "song.lsen";
            var songBytes = File.ReadAllBytes(songPath);

            Debug.LogFormat("{0}Decrypting song...", className);
            var decryptedSong = LSEncryption.AES_Decrypt(songBytes, password);

            File.WriteAllBytes(_filepath + "encryptedsong.ogg", decryptedSong);

            Debug.LogFormat("{0}Writing song to " + _filepath + "encryptedsong.ogg", className);
            AudioClip clipper = new();

            FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFileRaw(_filepath + "encryptedsong.ogg", false, delegate (AudioClip audioClip)
            {
                clipper = audioClip;
            }));

            callback(clipper);

            yield break;
        }

        public static IEnumerator PlayDecryptedLevel(string _path)
        {
            yield return inst.StartCoroutine(DecryptLevel(_path, delegate (AudioClip audioClip)
            {
                SaveManager.inst.ArcadeQueue.AudioFileStr = SaveManager.inst.ArcadeQueue.AudioFileStr.Replace("\\level.ogg", "\\encryptedsong.ogg");
            }));

            Debug.LogFormat("{0}Playing song.lsen from (" + _path + ")", className);

            Debug.LogFormat("{0}ArcadeQueue: \n{1}", className, SaveManager.inst.ArcadeQueue.AudioFileStr);
            var e = AccessTools.Method(typeof(GameManager), "LoadLevelFromArcadeQueue");
            GameManager.inst.StartCoroutine((IEnumerator)e.Invoke(GameManager.inst, new object[] { SaveManager.inst.ArcadeQueue }));

            yield return new WaitForSeconds(1f);

            File.Delete(_path + "encryptedsong.ogg");
        }

        [HarmonyPatch(typeof(InterfaceController), "Update")]
        [HarmonyPostfix]
        private static void ICUpdate(InterfaceController __instance)
        {
            ApplyMenuTheme(__instance);
        }

        public static void ApplyMenuTheme(InterfaceController __instance)
        {
            if (menuTheme.objectColors.Count < 4)
            {
                menuTheme.objectColors.Add(__instance.interfaceSettings.borderColor);
                menuTheme.objectColors.Add(__instance.interfaceSettings.borderHighlightColor);
                menuTheme.objectColors.Add(__instance.interfaceSettings.textColor);
                menuTheme.objectColors.Add(__instance.interfaceSettings.textHighlightColor);
            }
            if (menuTheme.objectColors[0] != __instance.interfaceSettings.borderColor)
            {
                menuTheme.objectColors[0] = __instance.interfaceSettings.borderColor;
            }
            if (menuTheme.objectColors[1] != __instance.interfaceSettings.borderHighlightColor)
            {
                menuTheme.objectColors[1] = __instance.interfaceSettings.borderHighlightColor;
            }
            if (menuTheme.objectColors[2] != __instance.interfaceSettings.textColor)
            {
                menuTheme.objectColors[2] = __instance.interfaceSettings.textColor;
            }
            if (menuTheme.objectColors[3] != __instance.interfaceSettings.textHighlightColor)
            {
                menuTheme.objectColors[3] = __instance.interfaceSettings.textHighlightColor;
            }
            if (menuTheme.backgroundColor != __instance.interfaceSettings.bgColor)
            {
                menuTheme.backgroundColor = __instance.interfaceSettings.bgColor;
            }
        }

        public static DataManager.BeatmapTheme menuTheme = new DataManager.BeatmapTheme();
    }
}
