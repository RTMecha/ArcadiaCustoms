using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Video;

using SimpleJSON;
using DG.Tweening;
using TMPro;
using InControl;
using LSFunctions;
using Steamworks;

using CielaSpike;

namespace ArcadiaCustoms
{
    [BepInPlugin("com.mecha.arcadiacustoms", "ArcadiaCustoms", " 1.4.0")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ArcadePlugin : BaseUnityPlugin
    {
        //TODO
        //Somehow add the level rank to the Arcade select menu somehow.
        //Implement difficulty modes (Much like what JSaB has)
        //Implement the shine effect when you've SS ranked a level.

        public static ArcadePlugin inst;
        public static string className = "[<color=#F5501B>ArcadiaCustoms</color>] " + PluginInfo.PLUGIN_VERSION + "\n";

        public static InterfaceController ic;
        public static string beatmapsstory = "beatmaps/story";

        public static ConfigEntry<bool> AntiAliasing { get; set; }
        public static ConfigEntry<bool> ReloadArcadeList { get; set; }

        public static ConfigEntry<int> ArcadeGridWidth { get; set; }
        public static ConfigEntry<int> ArcadeGridH { get; set; }
        public static ConfigEntry<int> ArcadeGridV { get; set; }
        public static ConfigEntry<int> ArcadeGridSize { get; set; }

        public static bool LoadEnabled = true;

        public static int current;
        public static List<SaveManager.ArcadeLevel> arcadeQueue = new List<SaveManager.ArcadeLevel>();

        private void Awake()
        {
            inst = this;

            Logger.LogInfo($"Plugin Arcadia Customs is loaded!");
            AntiAliasing = Config.Bind("Antialiasing", "Enabled", false, "If antialiasing is on or not.");
            ReloadArcadeList = Config.Bind("Arcade", "Reload list", true, "If enabled, this will reload the arcade list every time you enter the Specify Simulations screen. Make sure to turn this off if you want to quickly exit the menu.");
            ArcadeGridWidth = Config.Bind("Arcade", "Grid Width", 1, "The width of the arcade select grid.");
            ArcadeGridH = Config.Bind("Arcade", "Grid H", 3, "The horizontal amount of the arcade select grid.");
            ArcadeGridV = Config.Bind("Arcade", "Grid V", 2, "The vertical amount of the arcade select grid.");
            ArcadeGridSize = Config.Bind("Arcade", "Page Size", 21, "The amount of levels per page.");

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

            arcadeGridWidth = ArcadeGridWidth.Value.ToString();
            arcadeGridH = ArcadeGridH.Value.ToString();
            arcadeGridV = ArcadeGridV.Value.ToString();
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
            yield break;
        }

        [HarmonyPatch(typeof(InputSelectManager), "Start")]
        [HarmonyPostfix]
        private static void ResetArcadeInSelection()
        {
            LSHelpers.HideCursor();
            inst.StartCoroutine(GetLevelList());
        }

        [HarmonyPatch(typeof(ArcadeController), "Start")]
        [HarmonyPostfix]
        private static void SetThingBruh(ArcadeController __instance)
        {
            arcadeGridWidth = ArcadeGridWidth.Value.ToString();
            arcadeGridH = ArcadeGridH.Value.ToString();
            arcadeGridV = ArcadeGridV.Value.ToString();

            var field = __instance.GetType().GetField("pageSize", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(__instance, ArcadeGridSize.Value);
        }

        [HarmonyPatch(typeof(ArcadeController), "GenerateUI", typeof(bool))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GenerateUITranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .Start()
                .Advance(31)
                .ThrowIfNotMatch("Is not width", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ArcadePlugin), "arcadeGridWidth")))
                .ThrowIfNotMatch("Is not ldsfld 1", new CodeMatch(OpCodes.Ldsfld))
                .Advance(5)
                .ThrowIfNotMatch("Is not H", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ArcadePlugin), "arcadeGridH")))
                .Advance(5)
                .ThrowIfNotMatch("Is not V", new CodeMatch(OpCodes.Ldstr))
                .SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ArcadePlugin), "arcadeGridV")))
                .InstructionEnumeration();
        }

        public static string arcadeGridWidth = "1";
        public static string arcadeGridH = "3";
        public static string arcadeGridV = "2";

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

        public static void SceneTest()
        {
            UnityEngine.SceneManagement.Scene scene = new UnityEngine.SceneManagement.Scene
            {
                name = "TestScene"
            };
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

        public static void PlayCustomSound(string _sound)
        {
            string text = "beatmaps/soundlibrary/";
            if (RTFile.FileExists(text + _sound))
            {
                inst.StartCoroutine(FileManager.inst.LoadMusicFile(text + _sound, delegate (AudioClip _newSound)
                {
                    _newSound.name = _sound;
                    AudioManager.inst.PlaySound(_newSound);
                }));
            }
        }

        public static void PlayCustomAudio(AudioClip _clip, float _pitch)
        {
            AudioSource audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = _clip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.pitch = _pitch;
            audioSource.volume = AudioManager.inst.sfxVol;
            audioSource.Play();
            inst.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, _clip.length));
            return;
        }

        [HarmonyPatch(typeof(InterfaceController), "Update")]
        [HarmonyPostfix]
        private static void ICUpdate(InterfaceController __instance)
        {
            ApplyMenuTheme(__instance);
        }

        //[HarmonyPatch(typeof(InterfaceController), "Update")]
        //[HarmonyPrefix]
        private static bool ICUpdatePrefix(InterfaceController __instance)
        {
            Cursor.visible = true;

            if (EditorManager.inst == null)
            {
                if (GameObject.Find("EventSystem"))
                {
                    if (GameObject.Find("EventSystem").GetComponent<BaseInput>())
                    {
                        Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
                    }
                    if (GameObject.Find("EventSystem").GetComponent<InControlInputModule>())
                    {
                        Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
                    }
                    if (!GameObject.Find("EventSystem").GetComponent<StandaloneInputModule>())
                    {
                        GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
                    }
                    EventSystem.current.sendNavigationEvents = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) && __instance.screenDone && __instance.currentBranch != "main_menu" && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == InterfaceController.InterfaceBranch.Type.Menu)
            {
                if (__instance.branchChain.Count > 1)
                {
                    if (!string.IsNullOrEmpty(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch))
                    {
                        __instance.SwitchBranch(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch);
                    }
                    else
                    {
                        __instance.SwitchBranch(__instance.branchChain[__instance.branchChain.Count - 2]);
                    }
                }
                else
                {
                    AudioManager.inst.PlaySound("Block");
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) && __instance.screenDone && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == InterfaceController.InterfaceBranch.Type.MainMenu)
            {
                if (!string.IsNullOrEmpty(__instance.interfaceSettings.returnBranch))
                {
                    __instance.SwitchBranch(__instance.interfaceSettings.returnBranch);
                }
                else
                {
                    AudioManager.inst.PlaySound("Block");
                }
            }
            int num = 0;
            foreach (GameObject gameObject in __instance.buttons)
            {
                if (__instance.buttonSettings.Count > num && __instance.buttonSettings[num] != null && gameObject == __instance.currHoveredButton)
                {
                    if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Int)
                    {
                        int num2 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting);
                        if (InputDataManager.inst.menuActions.Left.WasPressed)
                        {
                            num2 -= __instance.buttonSettings[num].value;
                            if (num2 < __instance.buttonSettings[num].min)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num2 = __instance.buttonSettings[num].min;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                Debug.Log(string.Concat(new object[]
                                {
                                "Subtract : ",
                                num2,
                                " : ",
                                __instance.buttonSettings[num].setting
                                }));
                                DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting, num2);
                            }
                        }
                        if (InputDataManager.inst.menuActions.Right.WasPressed)
                        {
                            num2 += __instance.buttonSettings[num].value;
                            if (num2 > __instance.buttonSettings[num].max)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num2 = __instance.buttonSettings[num].max;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                Debug.Log(string.Concat(new object[]
                                {
                                "Add : ",
                                num2,
                                " : ",
                                __instance.buttonSettings[num].setting
                                }));
                                DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting, num2);
                            }
                        }
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Bool)
                    {
                        bool flag = DataManager.inst.GetSettingBool(__instance.buttonSettings[num].setting);
                        if (InputDataManager.inst.menuActions.Left.WasPressed || InputDataManager.inst.menuActions.Right.WasPressed)
                        {
                            flag = !flag;
                            AudioManager.inst.PlaySound("LeftRight");
                            DataManager.inst.UpdateSettingBool(__instance.buttonSettings[num].setting, flag);
                        }
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Vector2)
                    {
                        int num3 = DataManager.inst.GetSettingVector2DIndex(__instance.buttonSettings[num].setting);
                        if (InputDataManager.inst.menuActions.Left.WasPressed)
                        {
                            num3 -= __instance.buttonSettings[num].value;
                            if (num3 < __instance.buttonSettings[num].min)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num3 = __instance.buttonSettings[num].min;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingVector2D(__instance.buttonSettings[num].setting, num3, DataManager.inst.resolutions.ToArray());
                            }
                        }
                        if (InputDataManager.inst.menuActions.Right.WasPressed)
                        {
                            num3 += __instance.buttonSettings[num].value;
                            if (num3 > __instance.buttonSettings[num].max)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num3 = __instance.buttonSettings[num].max;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingVector2D(__instance.buttonSettings[num].setting, num3, DataManager.inst.resolutions.ToArray());
                            }
                        }
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.String)
                    {
                        DataManager.inst.GetSettingEnumName(__instance.buttonSettings[num].setting, 0);
                        int num4 = DataManager.inst.GetSettingEnum(__instance.buttonSettings[num].setting, 0);
                        if (__instance.buttonSettings[num].setting == "Language")
                        {
                            num4 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting + "_i");
                            DataManager.inst.GetSettingString(__instance.buttonSettings[num].setting);
                        }
                        if (InputDataManager.inst.menuActions.Left.WasPressed)
                        {
                            if (__instance.buttonSettings[num].setting == "Language")
                            {
                                num4 -= __instance.buttonSettings[num].value;
                                if (num4 < __instance.buttonSettings[num].min)
                                {
                                    AudioManager.inst.PlaySound("Block");
                                    num4 = __instance.buttonSettings[num].min;
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting + "_i", num4);
                                }
                            }
                            else
                            {
                                num4--;
                                if (num4 < 0)
                                {
                                    AudioManager.inst.PlaySound("Block");
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingEnum(__instance.buttonSettings[num].setting, num4);
                                    string settingEnumFunctionCall = DataManager.inst.GetSettingEnumFunctionCall(__instance.buttonSettings[num].setting, num4);
                                    if (!string.IsNullOrEmpty(settingEnumFunctionCall))
                                    {
                                        var handleEvent = AccessTools.Method(typeof(InterfaceController), "handleEvent");
                                        __instance.StartCoroutine((IEnumerator)AccessTools.Method(typeof(InterfaceController), "handleEvent").Invoke(__instance, new object[] { null, settingEnumFunctionCall, true }));
                                    }
                                }
                            }
                        }
                        if (InputDataManager.inst.menuActions.Right.WasPressed)
                        {
                            if (__instance.buttonSettings[num].setting == "Language")
                            {
                                num4 += __instance.buttonSettings[num].value;
                                if (num4 > __instance.buttonSettings[num].max)
                                {
                                    AudioManager.inst.PlaySound("Block");
                                    num4 = __instance.buttonSettings[num].max;
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting + "_i", num4);
                                }
                            }
                            else
                            {
                                num4++;
                                if (num4 >= DataManager.inst.GetSettingEnumCount(__instance.buttonSettings[num].setting))
                                {
                                    AudioManager.inst.PlaySound("Block");
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingEnum(__instance.buttonSettings[num].setting, num4);
                                    string settingEnumFunctionCall2 = DataManager.inst.GetSettingEnumFunctionCall(__instance.buttonSettings[num].setting, num4);
                                    if (!string.IsNullOrEmpty(settingEnumFunctionCall2))
                                    {
                                        var handleEvent = AccessTools.Method(typeof(InterfaceController), "handleEvent");
                                        __instance.StartCoroutine((IEnumerator)AccessTools.Method(typeof(InterfaceController), "handleEvent").Invoke(__instance, new object[] { null, settingEnumFunctionCall2, true }));
                                    }
                                }
                            }
                        }
                    }
                }
                if (gameObject.GetComponent<ButtonHover>())
                {
                    gameObject.GetComponent<ButtonHover>().colorSelected = __instance.interfaceSettings.borderHighlightColor;
                    gameObject.GetComponent<ButtonHover>().colorDeselected = __instance.interfaceSettings.borderColor;
                }
                if (gameObject == __instance.currHoveredButton)
                {
                    gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
                    if (gameObject.transform.Find("float"))
                    {
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
                    }
                    if (gameObject.transform.Find("bool"))
                    {
                        gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
                    }
                    if (gameObject.transform.Find("vector2"))
                    {
                        gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
                    }
                    if (gameObject.transform.Find("string"))
                    {
                        gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
                    }
                    __instance.currHoveredButton = gameObject;
                }
                else
                {
                    gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
                    if (gameObject.transform.Find("float"))
                    {
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
                    }
                    if (gameObject.transform.Find("bool"))
                    {
                        gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
                    }
                    if (gameObject.transform.Find("vector2"))
                    {
                        gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
                    }
                    if (gameObject.transform.Find("string"))
                    {
                        gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
                    }
                }
                if (!__instance.screenGlitch)
                {
                    if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Int)
                    {
                        int num5 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting);
                        num5 = Mathf.Clamp(num5, 0, 9);
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [         ] >";
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text.Insert(num5 + 3, "■");
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Bool)
                    {
                        bool settingBool = DataManager.inst.GetSettingBool(__instance.buttonSettings[num].setting);
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [ " + (settingBool ? "true" : "false") + " ] >";
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.Vector2)
                    {
                        Vector2 settingVector2D = DataManager.inst.GetSettingVector2D(__instance.buttonSettings[num].setting);
                        gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().text = string.Concat(new object[]
                        {
                        "< [ ",
                        settingVector2D.x,
                        ", ",
                        settingVector2D.y,
                        " ] >"
                        });
                    }
                    else if (__instance.buttonSettings[num].type == InterfaceController.ButtonSetting.Type.String)
                    {
                        string str;
                        if (__instance.buttonSettings[num].setting == "Language")
                        {
                            str = DataManager.inst.GetLanguage(DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting + "_i", 0));
                        }
                        else
                        {
                            str = DataManager.inst.GetSettingEnumName(__instance.buttonSettings[num].setting, 0);
                        }
                        gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [ " + str + " ] >";
                    }
                }
                num++;
            }
            __instance.SpeedUp = Input.GetKey(KeyCode.Space);
            var lastSelectedObj = (GameObject)AccessTools.Field(typeof(InterfaceController), "lastSelectedObj").GetValue(__instance);
            if (__instance.currHoveredButton == null && __instance.buttonsActive)
            {
                __instance.currHoveredButton = lastSelectedObj;
            }
            if (lastSelectedObj != __instance.currHoveredButton && __instance.screenDone)
            {
                AudioManager.inst.PlaySound("UpDown");
            }
            AccessTools.Field(typeof(InterfaceController), "lastSelectedObj").SetValue(__instance, __instance.currHoveredButton);
            return false;
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

        //[HarmonyPatch(typeof(InterfaceController), "AddElement")]
        //[HarmonyPrefix]
        private static bool AddElementPrefix(InterfaceController __instance, IEnumerator __result, InterfaceController.InterfaceElement __0, bool __1)
        {
            inst.StartCoroutine(AddElement(__instance, __0, __1));
            return false;
        }

        //[HarmonyPatch(typeof(InterfaceController), "AddBranch")]
        //[HarmonyPrefix]
        private static bool AddBranchPrefix(InterfaceController __instance, InterfaceController.InterfaceBranch __0)
        {
            inst.StartCoroutine(AddBranch(__instance, __0));
            return false;
        }

        //[HarmonyPatch(typeof(InterfaceController), "Addline")]
        //[HarmonyPrefix]
        private static bool AddlinePrefix(InterfaceController __instance, InterfaceController.InterfaceElement __0)
        {
            inst.StartCoroutine(AddElement(__instance, __0, false));
            return false;
        }

        public static IEnumerator AddBranch(InterfaceController __instance, InterfaceController.InterfaceBranch _branch)
        {
            int num;
            if (_branch.clear_screen && __instance.MainPanel.childCount > 0)
            {
                new List<string>();
                AudioManager.inst.PlaySound("glitch");
                __instance.screenGlitch = true;
                for (int i = 0; i < UnityEngine.Random.Range(2, 4); i = num + 1)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.1f));
                    num = i;
                }
                __instance.screenGlitch = false;
                yield return new WaitForSeconds(0.001f);
                foreach (object obj in __instance.MainPanel.transform)
                {
                    Destroy(((Transform)obj).gameObject);
                }
                __instance.buttons.Clear();
            }
            int count = _branch.elements.Count;
            for (int i = 0; i < count; i = num + 1)
            {
                InterfaceController.InterfaceElement interfaceElement = _branch.elements[i];
                if (interfaceElement != null)
                {
                    yield return inst.StartCoroutine(AddElement(__instance, interfaceElement, false));
                }
                count = _branch.elements.Count;
                num = i;
            }
            if (__instance.currHoveredButton != null)
            {

            }
            __instance.screenDone = true;
            yield break;
        }

        public static IEnumerator AddElement(InterfaceController interfaceController, InterfaceController.InterfaceElement _element, bool _immediate)
        {
            if (_element.branch == interfaceController.currentBranch)
            {
                var scrollBottom = AccessTools.Method(typeof(InterfaceController), "ScrollBottom");
                Debug.Log(scrollBottom);
                interfaceController.StartCoroutine((IEnumerator)scrollBottom.Invoke(interfaceController, new object[] { }));
                float totalTime = 0f;
                totalTime = !interfaceController.SpeedUp ? UnityEngine.Random.Range(interfaceController.interfaceSettings.times.x, interfaceController.interfaceSettings.times.y) : UnityEngine.Random.Range(interfaceController.interfaceSettings.times.x, interfaceController.interfaceSettings.times.y) / interfaceController.FastSpeed;
                if (!_immediate)
                    AudioManager.inst.PlaySound("Click");
                int childCount = interfaceController.MainPanel.childCount;
                string dataText;
                GameObject text;
                switch (_element.type)
                {
                    case InterfaceController.InterfaceElement.Type.Text:
                        {
                            string str1;
                            if (_element.data.Count > 0)
                            {
                                str1 = _element.data[0];
                            }
                            else
                            {
                                str1 = " ";
                                Debug.Log(_element.branch + " - " + childCount);
                            }
                            dataText = _element.data.Count > interfaceController.interfaceSettings.language ? _element.data[interfaceController.interfaceSettings.language] : str1;
                            GameObject gameObject1 = Instantiate(interfaceController.TextPrefab, Vector3.zero, Quaternion.identity);
                            gameObject1.name = "button";
                            gameObject1.transform.SetParent(interfaceController.MainPanel);
                            gameObject1.transform.localScale = Vector3.one;
                            gameObject1.name = string.Format("[{0}] Text", childCount);
                            GameObject gameObject2 = gameObject1.transform.Find("bg").gameObject;
                            text = gameObject1.transform.Find("text").gameObject;
                            if (_element.settings.ContainsKey("bg-color"))
                            {
                                if (_element.settings["bg-color"] == "text-color")
                                    gameObject2.GetComponent<Image>().color = interfaceController.interfaceSettings.textColor;
                                else
                                    gameObject2.GetComponent<Image>().color = LSColors.HexToColor(_element.settings["bg-color"]);
                            }
                            else
                                gameObject2.GetComponent<Image>().color = LSColors.transparent;
                            if (_element.settings.ContainsKey("text-color"))
                            {
                                if (_element.settings["text-color"] == "bg-color")
                                    text.GetComponent<TextMeshProUGUI>().color = interfaceController.interfaceSettings.bgColor;
                                else
                                    text.GetComponent<TextMeshProUGUI>().color = LSColors.HexToColor(_element.settings["text-color"]);
                            }
                            else
                                text.GetComponent<TextMeshProUGUI>().color = interfaceController.interfaceSettings.textColor;
                            if (!string.IsNullOrEmpty(dataText))
                            {
                                if (_element.settings.ContainsKey("alignment"))
                                {
                                    string setting1 = _element.settings["alignment"];
                                    if (!(setting1 == "left"))
                                    {
                                        if (!(setting1 == "center"))
                                        {
                                            if (setting1 == "right")
                                            {
                                                if (!_element.settings.ContainsKey("valignment"))
                                                {
                                                    text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
                                                }
                                                else
                                                {
                                                    string setting2 = _element.settings["valignment"];
                                                    if (!(setting2 == "top"))
                                                    {
                                                        if (!(setting2 == "center"))
                                                        {
                                                            if (setting2 == "bottom")
                                                                text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
                                                        }
                                                        else
                                                            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
                                                    }
                                                    else
                                                        text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopRight;
                                                }
                                            }
                                        }
                                        else if (!_element.settings.ContainsKey("valignment"))
                                        {
                                            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
                                        }
                                        else
                                        {
                                            string setting3 = _element.settings["valignment"];
                                            if (!(setting3 == "top"))
                                            {
                                                if (!(setting3 == "center"))
                                                {
                                                    if (setting3 == "bottom")
                                                        text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Bottom;
                                                }
                                                else
                                                    text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
                                            }
                                            else
                                                text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Top;
                                        }
                                    }
                                    else if (!_element.settings.ContainsKey("valignment"))
                                    {
                                        text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                                    }
                                    else
                                    {
                                        string setting4 = _element.settings["valignment"];
                                        if (!(setting4 == "top"))
                                        {
                                            if (!(setting4 == "center"))
                                            {
                                                if (setting4 == "bottom")
                                                    text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
                                            }
                                            else
                                                text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                                        }
                                        else
                                            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
                                    }
                                }
                                else if (_element.settings.ContainsKey("valignment"))
                                {
                                    string setting = _element.settings["valignment"];
                                    if (!(setting == "top"))
                                    {
                                        if (!(setting == "center"))
                                        {
                                            if (setting == "bottom")
                                                text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
                                        }
                                        else
                                            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                                    }
                                    else
                                        text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
                                }
                                var runTextTransformations = AccessTools.Method(typeof(InterfaceController), "RunTextTransformations");
                                dataText = (string)runTextTransformations.Invoke(interfaceController, new object[] { dataText, childCount });
                                if (dataText.Contains("[[") && dataText.Contains("]]"))
                                {
                                    foreach (Match match in Regex.Matches(dataText, "\\[\\[([^\\]]*)\\]\\]"))
                                    {
                                        Debug.Log(match.Groups[0].Value);
                                        dataText = dataText.Replace(match.Groups[0].Value, LSText.FormatString(match.Groups[1].Value));
                                    }
                                }
                                string[] words = dataText.Split(new string[1]
                                {
                                " "
                                }, StringSplitOptions.RemoveEmptyEntries);
                                string tempText = "";
                                for (int i = 0; i < words.Length; ++i)
                                {
                                    float seconds = totalTime / words.Length;
                                    if (text != null)
                                    {
                                        tempText = tempText + words[i] + " ";
                                        text.GetComponent<TextMeshProUGUI>().text = tempText + (i % 2 == 0 ? "▓▒░" : "▒░░");
                                    }
                                    yield return new WaitForSeconds(seconds);
                                }
                                if (_element.settings.ContainsKey("font-style") && text != null)
                                {
                                    string setting = _element.settings["font-style"];
                                    if (!(setting == "light"))
                                    {
                                        if (!(setting == "normal"))
                                        {
                                            if (setting == "bold")
                                                text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                                        }
                                        else
                                            text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
                                    }
                                    else
                                        text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
                                }
                                else if (text != null)
                                    text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
                                if (text != null)
                                    text.GetComponent<TextMeshProUGUI>().text = dataText;
                                words = null;
                                tempText = null;
                                break;
                            }
                            break;
                        }
                    case InterfaceController.InterfaceElement.Type.Buttons:
                        {
                            GameObject element = Instantiate(interfaceController.ButtonElementPrefab, Vector3.zero, Quaternion.identity);
                            element.name = "button";
                            element.transform.SetParent(interfaceController.MainPanel);
                            element.transform.localScale = Vector3.one;
                            element.name = string.Format("[{0}] Button Holder", childCount);
                            if (_element.settings.ContainsKey("width"))
                            {
                                float result = 0.5f;
                                float.TryParse(_element.settings["width"], out result);
                                element.GetComponent<LayoutElement>().preferredWidth = result * 1792f;
                            }
                            if (_element.settings.ContainsKey("orientation"))
                            {
                                if (_element.settings["orientation"] == "horizontal")
                                    element.GetComponent<VerticalLayoutGroup>().enabled = false;
                                else if (_element.settings["orientation"] == "vertical")
                                    element.GetComponent<HorizontalLayoutGroup>().enabled = false;
                                else if (_element.settings["orientation"] == "grid")
                                {
                                    DestroyImmediate(element.GetComponent<HorizontalLayoutGroup>());
                                    DestroyImmediate(element.GetComponent<VerticalLayoutGroup>());
                                    GridLayoutGroup gridLayoutGroup = element.AddComponent<GridLayoutGroup>();
                                    gridLayoutGroup.spacing = new Vector2(16f, 16f);
                                    float result1 = 1f;
                                    if (_element.settings.ContainsKey("grid_h"))
                                        float.TryParse(_element.settings["grid_h"], out result1);
                                    int result2 = 0;
                                    if (_element.settings.ContainsKey("grid_corner"))
                                        int.TryParse(_element.settings["grid_corner"], out result2);
                                    float result3 = 1f;
                                    if (_element.settings.ContainsKey("grid_v"))
                                        float.TryParse(_element.settings["grid_v"], out result3);
                                    gridLayoutGroup.cellSize = new Vector2((float)(1792.0 - 16.0 * ((double)result1 - 1.0)) / result1, result3 * 54f);
                                    gridLayoutGroup.childAlignment = (TextAnchor)result2;
                                }
                            }
                            else
                                element.GetComponent<HorizontalLayoutGroup>().enabled = false;
                            string[] strArray1 = (_element.data.Count > interfaceController.interfaceSettings.language ? _element.data[interfaceController.interfaceSettings.language] : _element.data[0]).Split(new string[1]
                            {
                            "&&"
                            }, StringSplitOptions.RemoveEmptyEntries);
                            interfaceController.buttonSettings.Clear();
                            if (_element.settings.ContainsKey("buttons"))
                            {
                                string[] strArray2 = _element.settings["buttons"].Split(new string[1]
                                {
                                ":"
                                }, StringSplitOptions.None);
                                int num1 = 0;
                                foreach (string str2 in strArray2)
                                {
                                    if (!string.IsNullOrEmpty(str2))
                                    {
                                        string[] strArray3 = str2.Split(new string[1]
                                        {
                                        "|"
                                        }, StringSplitOptions.None);

                                        var convertStringToButtonType = AccessTools.Method(typeof(InterfaceController), "ConvertStringToButtonType");
                                        InterfaceController.ButtonSetting buttonSetting = new InterfaceController.ButtonSetting((InterfaceController.ButtonSetting.Type)convertStringToButtonType.Invoke(interfaceController, new object[] { strArray3[0] }));
                                        if (buttonSetting.type == InterfaceController.ButtonSetting.Type.Event)
                                        {
                                            int num2 = 0;
                                            foreach (string str3 in strArray3)
                                            {
                                                if (num2 != 0)
                                                    buttonSetting.setting += str3;
                                                if (num2 != 0 && num2 < strArray3.Length - 1)
                                                    buttonSetting.setting += "|";
                                                ++num2;
                                            }
                                        }
                                        else
                                        {
                                            buttonSetting.setting = strArray3[1];
                                            buttonSetting.value = int.Parse(strArray3[2]);
                                            buttonSetting.min = int.Parse(strArray3[3]);
                                            buttonSetting.max = int.Parse(strArray3[4]);
                                        }
                                        interfaceController.buttonSettings.Add(buttonSetting);
                                    }
                                    else if (num1 != 0)
                                        interfaceController.buttonSettings.Add(new InterfaceController.ButtonSetting(InterfaceController.ButtonSetting.Type.Empty));
                                    ++num1;
                                }
                            }
                            else
                            {
                                for (int index = 0; index < strArray1.Length; ++index)
                                    interfaceController.buttonSettings.Add(new InterfaceController.ButtonSetting(InterfaceController.ButtonSetting.Type.Empty));
                            }
                            for (int index = 0; index < strArray1.Length; ++index)
                            {
                                string[] strArray4 = strArray1[index].Split(':');
                                GameObject gameObject3;
                                if (interfaceController.buttonSettings.Count > index && interfaceController.buttonSettings[index].setting != null)
                                {
                                    switch (interfaceController.buttonSettings[index].type)
                                    {
                                        case InterfaceController.ButtonSetting.Type.Int:
                                            gameObject3 = Instantiate(interfaceController.IntButtonPrefab, Vector3.zero, Quaternion.identity);
                                            break;
                                        case InterfaceController.ButtonSetting.Type.Bool:
                                            gameObject3 = Instantiate(interfaceController.BoolButtonPrefab, Vector3.zero, Quaternion.identity);
                                            break;
                                        case InterfaceController.ButtonSetting.Type.String:
                                            gameObject3 = Instantiate(interfaceController.StringButtonPrefab, Vector3.zero, Quaternion.identity);
                                            break;
                                        case InterfaceController.ButtonSetting.Type.Vector2:
                                            gameObject3 = Instantiate(interfaceController.Vector2ButtonPrefab, Vector3.zero, Quaternion.identity);
                                            break;
                                        default:
                                            gameObject3 = Instantiate(interfaceController.ButtonPrefab, Vector3.zero, Quaternion.identity);
                                            break;
                                    }
                                }
                                else
                                {
                                    gameObject3 = Instantiate(interfaceController.ButtonPrefab, Vector3.zero, Quaternion.identity);
                                }
                                gameObject3.transform.SetParent(element.transform);
                                gameObject3.transform.localScale = Vector3.one;
                                gameObject3.AddComponent<ButtonHover>();
                                gameObject3.name = string.Format("[{0}][{1}] Button", childCount, index);
                                if (_element.settings.ContainsKey("alignment"))
                                {
                                    string setting5 = _element.settings["alignment"];
                                    if (!(setting5 == "left"))
                                    {
                                        if (!(setting5 == "center"))
                                        {
                                            if (setting5 == "right")
                                            {
                                                if (!_element.settings.ContainsKey("valignment"))
                                                {
                                                    gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
                                                }
                                                else
                                                {
                                                    string setting6 = _element.settings["valignment"];
                                                    if (!(setting6 == "top"))
                                                    {
                                                        if (!(setting6 == "center"))
                                                        {
                                                            if (setting6 == "bottom")
                                                                gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
                                                        }
                                                        else
                                                            gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
                                                    }
                                                    else
                                                        gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopRight;
                                                }
                                            }
                                        }
                                        else if (!_element.settings.ContainsKey("valignment"))
                                        {
                                            gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
                                        }
                                        else
                                        {
                                            string setting7 = _element.settings["valignment"];
                                            if (!(setting7 == "top"))
                                            {
                                                if (!(setting7 == "center"))
                                                {
                                                    if (setting7 == "bottom")
                                                        gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Bottom;
                                                }
                                                else
                                                    gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
                                            }
                                            else
                                                gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Top;
                                        }
                                    }
                                    else if (!_element.settings.ContainsKey("valignment"))
                                    {
                                        gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                                    }
                                    else
                                    {
                                        string setting8 = _element.settings["valignment"];
                                        if (!(setting8 == "top"))
                                        {
                                            if (!(setting8 == "center"))
                                            {
                                                if (setting8 == "bottom")
                                                    gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
                                            }
                                            else
                                                gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                                        }
                                        else
                                            gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
                                    }
                                }
                                else if (_element.settings.ContainsKey("valignment"))
                                {
                                    string setting = _element.settings["valignment"];
                                    if (!(setting == "top"))
                                    {
                                        if (!(setting == "center"))
                                        {
                                            if (setting == "bottom")
                                                gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
                                        }
                                        else
                                            gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                                    }
                                    else
                                        gameObject3.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
                                }
                                interfaceController.buttons.Add(gameObject3);
                                if (index == 0 && interfaceController.buttonsActive)
                                {
                                    //EventSystem.current.SetSelectedGameObject(gameObject3);
                                    interfaceController.currHoveredButton = gameObject3;
                                    gameObject3.GetComponent<ButtonHover>().Select();
                                }
                                gameObject3.transform.Find("text").GetComponent<TextMeshProUGUI>().text = interfaceController.ParseText(strArray4[0]);
                                if (strArray4[0] == "")
                                {
                                    gameObject3.GetComponent<Button>().navigation = new Navigation()
                                    {
                                        mode = Navigation.Mode.None
                                    };
                                    gameObject3.transform.Find("bg").GetComponent<Image>().enabled = false;
                                }
                                else
                                {
                                    //gameObject3.GetComponent<EventTrigger>().triggers.Add(interfaceController.CreateButtonHoverTrigger(EventTriggerType.PointerEnter, gameObject3));
                                    if (_element.settings.ContainsKey("buttons") && interfaceController.buttonSettings[index].type == InterfaceController.ButtonSetting.Type.Event && gameObject3.GetComponent<ButtonHover>())
                                    {
                                        gameObject3.GetComponent<ButtonHover>().butt = false;
                                        gameObject3.GetComponent<ButtonHover>().branch = _element.branch;
                                        gameObject3.GetComponent<ButtonHover>().data = interfaceController.buttonSettings[index].setting;
                                    }
                                    else if (strArray4.Length == 2 && gameObject3.GetComponent<ButtonHover>())
                                    {
                                        gameObject3.GetComponent<ButtonHover>().butt = true;
                                        gameObject3.GetComponent<ButtonHover>().element = element;
                                        gameObject3.GetComponent<ButtonHover>().link = strArray4[1];
                                    }
                                    else if (strArray4[1] == "setting_str")
                                        DataManager.inst.UpdateSettingString(strArray4[2], strArray4[3]);
                                    if (_element.settings.ContainsKey("default_button") && interfaceController.buttons.Count > int.Parse(_element.settings["default_button"]) && interfaceController.buttonsActive)
                                    {
                                        interfaceController.currHoveredButton = interfaceController.buttons[int.Parse(_element.settings["default_button"])];
                                        if (interfaceController.buttons[int.Parse(_element.settings["default_button"])].GetComponent<ButtonHover>())
                                        {
                                            interfaceController.buttons[int.Parse(_element.settings["default_button"])].GetComponent<ButtonHover>().Select();
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case InterfaceController.InterfaceElement.Type.Event:
                        {
                            foreach (string _data in _element.data)
                            {
                                var handleEvent = AccessTools.Method(typeof(InterfaceController), "handleEvent");
                                if (!string.IsNullOrEmpty(_data))
                                {
                                    Debug.Log(handleEvent);
                                    yield return interfaceController.StartCoroutine((IEnumerator)handleEvent.Invoke(interfaceController, new object[] { _element.branch, _data, false }));
                                }
                                else
                                {
                                    Debug.LogError("Handle Event Error" + _data);
                                }
                            }
                            break;
                        }
                }
                dataText = null;
                text = null;
            }
            yield break;
        }

        //[HarmonyPatch(typeof(ArcadeManager), "Update")]
        //[HarmonyPrefix]
        private static bool ArcadeManagerUpdatePrefix(ArcadeManager __instance)
        {
            if (__instance.ic != null && __instance.ic.currentBranch == "boot" && !DataManager.inst.GetSettingBool("ARCADE_MODE", false) && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)))
            {
                __instance.skippedLoad = true;
                __instance.forcedSkip = true;
                __instance.ArcadeList.Clear();
                __instance.ic.SwitchBranch("main_menu");
                __instance.StopCoroutine("GetFiles");
                __instance.StopCoroutine("LoadMusic");
                __instance.StopCoroutine("BackToMainMenu");
            }
            if (GameManager.inst == null && __instance.ic == null && GameObject.FindGameObjectWithTag("interface") != null)
            {
                __instance.ic = GameObject.FindGameObjectWithTag("interface").GetComponent<InterfaceController>();
            }
            return false;
        }

        //[HarmonyPatch(typeof(InterfaceController), "handleEvent")]
        //[HarmonyPrefix]
        private static bool handleEventPrefix(InterfaceController __instance, string __0, string __1, bool __2)
        {
            inst.StartCoroutine(handleEvent(__instance, __0, __1, __2));
            return false;
        }

        public static IEnumerator handleEvent(InterfaceController interfaceController, string _branch, string _data, bool _override = false)
        {
            if (interfaceController.currentBranch == _branch | _override)
            {
                if (!_data.Contains("::"))
                    _data = _data.Replace("|", "::");
                string[] data = _data.Split(new string[1] { "::" }, 5, StringSplitOptions.None);
                switch (data[0].ToLower())
                {
                    case "apply_level_ui_theme":
                        {
                            if (GameManager.inst != null)
                            {
                                Color col = LSColors.ContrastColor(LSColors.InvertColor(GameManager.inst.LiveTheme.backgroundColor));
                                Color backgroundColor = GameManager.inst.LiveTheme.backgroundColor;
                                interfaceController.interfaceSettings.textHighlightColor = backgroundColor;
                                interfaceController.interfaceSettings.bgColor = new Color(0.0f, 0.0f, 0.0f, 0.3f);
                                interfaceController.interfaceSettings.borderHighlightColor = col;
                                interfaceController.interfaceSettings.textColor = col;
                                interfaceController.interfaceSettings.borderColor = data.Length > 1 && data[1].ToLower() == "true" || data.Length == 1 ? LSColors.fadeColor(col, 0.3f) : LSColors.transparent;
                                break;
                            }
                        }
                        break;
                    case "apply_menu_music":
                        {
                            AudioManager.inst.PlayMusic((string)DataManager.inst.GetSettingEnumValues("MenuMusic", 0), 1f);
                            break;
                        }
                    case "apply_ui_theme":
                        {
                            Color textColor1 = interfaceController.interfaceSettings.textColor;
                            interfaceController.interfaceSettings.textHighlightColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
                            interfaceController.interfaceSettings.bgColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
                            interfaceController.interfaceSettings.borderHighlightColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
                            interfaceController.interfaceSettings.textColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
                            interfaceController.interfaceSettings.borderColor = LSColors.HexToColorAlpha((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
                            interfaceController.cam.GetComponent<Camera>().backgroundColor = interfaceController.interfaceSettings.bgColor;
                            foreach (TextMeshProUGUI componentsInChild in interfaceController.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>())
                            {
                                if (componentsInChild.color == textColor1)
                                    componentsInChild.color = interfaceController.interfaceSettings.textColor;
                            }
                            SaveManager.inst.UpdateSettingsFile(false);
                            break;
                        }
                    case "apply_ui_theme_with_reload":
                        {
                            Color textColor2 = interfaceController.interfaceSettings.textColor;
                            interfaceController.interfaceSettings.textHighlightColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
                            interfaceController.interfaceSettings.bgColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
                            interfaceController.interfaceSettings.borderHighlightColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
                            interfaceController.interfaceSettings.textColor = LSColors.HexToColor((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
                            interfaceController.interfaceSettings.borderColor = LSColors.HexToColorAlpha((string)DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
                            interfaceController.SwitchBranch(interfaceController.currentBranch);
                            interfaceController.cam.GetComponent<Camera>().backgroundColor = interfaceController.interfaceSettings.bgColor;
                            foreach (TextMeshProUGUI componentsInChild in interfaceController.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>())
                            {
                                if (componentsInChild.color == textColor2)
                                    componentsInChild.color = interfaceController.interfaceSettings.textColor;
                            }
                            SaveManager.inst.UpdateSettingsFile(false);
                            break;
                        }
                    case "apply_video_settings":
                        {
                            interfaceController.ApplyVideoSettings();
                            break;
                        }
                    case "apply_video_settings_with_reload":
                        {
                            interfaceController.SwitchBranch(interfaceController.currentBranch);
                            interfaceController.ApplyVideoSettings();
                            SaveManager.inst.UpdateSettingsFile(false);
                            break;
                        }
                    case "branch":
                        {
                            interfaceController.SwitchBranch(data[1]);
                            break;
                        }
                    case "clearplayers":
                        {
                            if (data.Length > 1)
                            {
                                InputDataManager.inst.ClearInputs(data[1] == "true");
                                break;
                            }
                            InputDataManager.inst.ClearInputs();
                            break;
                        }
                    case "deleteline":
                        {
                            if (data.Length > 2)
                            {
                                Destroy(interfaceController.MainPanel.GetChild(interfaceController.MainPanel.childCount - 1 + int.Parse(data[1])).gameObject);
                                break;
                            }
                            Destroy(interfaceController.MainPanel.GetChild(int.Parse(data[1])).gameObject);
                            break;
                        }
                    case "exit":
                        {
                            Application.Quit();
                            break;
                        }
                    case "if":
                        {
                            if (DataManager.inst.GetSettingBool(data[1]))
                            {
                                interfaceController.SwitchBranch(data[2]);
                                break;
                            }
                            break;
                        }
                    case "loadarcadelevels":
                        {
                            interfaceController.StartCoroutine(ArcadeManager.inst.GetFiles());
                            break;
                        }
                    case "loadnextlevel":
                        {
                            SceneManager.inst.LoadNextLevel();
                            break;
                        }
                    case "loadscene":
                        {
                            Debug.Log("Try to load [" + data[1] + "]");
                            if (data.Length >= 3)
                            {
                                Debug.Log("Loading Scene with Loading Display off?");
                                SceneManager.inst.LoadScene(data[1], bool.Parse(data[2]));
                                break;
                            }
                            SceneManager.inst.LoadScene(data[1]);
                            break;
                        }
                    case "openlink":
                        {
                            Application.OpenURL(data[1]);
                            break;
                        }
                    case "pausemusic":
                        {
                            AudioManager.inst.CurrentAudioSource.Pause();
                            break;
                        }
                    case "playmusic":
                        {
                            AudioManager.inst.PlayMusic(data[1], 0.5f);
                            break;
                        }
                    case "playsound":
                        {
                            AudioManager.inst.PlaySound(data[1]);
                            break;
                        }
                    case "playsoundcustom":
                        {
                            PlayCustomSound(data[1]);
                            break;
                        }
                    case "quittoarcade":
                        {
                            if (GameManager.inst != null)
                            {
                                GameManager.inst.QuitToArcade();
                                break;
                            }
                            break;
                        }
                    case "replaceline":
                        {
                            AudioManager.inst.PlaySound("Click");
                            string dataText = data[2];
                            int childCount = data.Length > 3 ? interfaceController.MainPanel.childCount - 1 + int.Parse(data[1]) : int.Parse(data[1]);

                            var runTextTransformations = AccessTools.Method(typeof(InterfaceController), "RunTextTransformations");
                            string str = (string)runTextTransformations.Invoke(interfaceController, new object[] { dataText, childCount });
                            if (data.Length > 3)
                            {
                                Debug.Log(interfaceController.MainPanel.GetChild(interfaceController.MainPanel.childCount - 1 + int.Parse(data[1])));
                                Debug.Log(interfaceController.MainPanel.GetChild(interfaceController.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text);
                                interfaceController.MainPanel.GetChild(interfaceController.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text = str;
                                break;
                            }
                            interfaceController.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text = str;
                            break;
                        }
                    case "replacelineinbranch":
                        {
                            int index = interfaceController.interfaceBranches.FindIndex((Predicate<InterfaceController.InterfaceBranch>)(x => x.name == data[1]));
                            interfaceController.interfaceBranches[index].elements[int.Parse(data[2])].data = new List<string>()
                            {
                                data[3]
                            };
                            break;
                        }
                    case "save_settings":
                        {
                            SaveManager.inst.UpdateSettingsFile(false);
                            break;
                        }
                    case "setbg":
                        {
                            interfaceController.interfaceSettings.bgColor = LSColors.HexToColor(data[1].Replace("#", ""));
                            interfaceController.cam.GetComponent<Camera>().backgroundColor = interfaceController.interfaceSettings.bgColor;
                            break;
                        }
                    case "setbuttonbg":
                        {
                            string hex = data[1].Replace("#", "");
                            Color borderColor = interfaceController.interfaceSettings.borderColor;
                            interfaceController.interfaceSettings.borderColor = !(hex == "none") ? LSColors.HexToColorAlpha(hex) : new Color(0.0f, 0.0f, 0.0f, 0.0f);
                            break;
                        }
                    case "setcurrentlevel":
                        {
                            SaveManager.inst.SetCurrentStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
                            break;
                        }
                    case "sethighlight":
                        {
                            interfaceController.interfaceSettings.borderHighlightColor = LSColors.HexToColor(data[1].Replace("#", ""));
                            break;
                        }
                    case "setmusicvol":
                        {
                            AudioManager.inst.CurrentAudioSource.volume = !(data[1] == "back") ? float.Parse(data[1]) : AudioManager.inst.musicVol;
                            break;
                        }
                    case "setsavedlevel":
                        {
                            Debug.LogFormat("setsavedlevel: {0} - {1}", int.Parse(data[1]), int.Parse(data[2]));
                            SaveManager.inst.SetSaveStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
                            break;
                        }
                    case "settext":
                        {
                            Color textColor3 = interfaceController.interfaceSettings.textColor;
                            interfaceController.interfaceSettings.textColor = LSColors.HexToColor(data[1].Replace("#", ""));
                            foreach (TextMeshProUGUI componentsInChild in interfaceController.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>())
                            {
                                if (componentsInChild.color == textColor3)
                                    componentsInChild.color = interfaceController.interfaceSettings.textColor;
                            }
                            break;
                        }
                    case "setting":
                        {
                            string lower = data[1].ToLower();
                            if (lower == "bool")
                            {
                                DataManager.inst.UpdateSettingBool(data[2], bool.Parse(data[3]));
                                break;
                            }
                            if (lower == "enum")
                            {
                                DataManager.inst.UpdateSettingEnum(data[2], int.Parse(data[3]));
                                break;
                            }
                            if (lower == "string" || lower == "str")
                            {
                                DataManager.inst.UpdateSettingString(data[2], data[3]);
                                break;
                            }
                            if (lower == "achievement" || lower == "achieve")
                            {
                                SteamWrapper.inst.achievements.SetAchievement(data[2]);
                                break;
                            }
                            if (lower == "clearAchievement" || lower == "clearAchieve")
                            {
                                SteamWrapper.inst.achievements.ClearAchievement(data[2]);
                                break;
                            }
                            if (lower == "int")
                            {
                                if (data[3] == "add")
                                {
                                    DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) + 1);
                                    break;
                                }
                                if (data[3] == "sub")
                                {
                                    DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) - 1);
                                    break;
                                }
                                DataManager.inst.UpdateSettingInt(data[2], int.Parse(data[3]));
                                break;
                            }
                            Debug.LogError("Kind not found for setting [" + _data + "]");
                            break;
                        }
                    case "subscribe_official_arcade_levels":
                        {
                            SteamWorkshop.inst.Subscribe(new PublishedFileId_t(2880967129UL));
                            SteamWorkshop.inst.Subscribe(new PublishedFileId_t(2866417555UL));
                            SteamWorkshop.inst.Subscribe(new PublishedFileId_t(2863370412UL));
                            SteamWorkshop.inst.Subscribe(new PublishedFileId_t(2857725081UL));
                            break;
                        }
                    case "unpauselevel":
                        {
                            if (GameManager.inst != null)
                            {
                                GameManager.inst.UnPause();
                                break;
                            }
                            break;
                        }
                    case "wait":
                        {
                            if (data.Length >= 2)
                            {
                                float result = 0.5f;
                                float.TryParse(data[1], out result);
                                if (interfaceController.SpeedUp)
                                {
                                    yield return new WaitForSeconds(result / interfaceController.FastSpeed);
                                    break;
                                }
                                yield return new WaitForSeconds(result);
                                break;
                            }
                            break;
                        }
                }
                yield return null;
            }
        }
    }
}
