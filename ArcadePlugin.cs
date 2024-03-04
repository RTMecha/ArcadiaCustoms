﻿using System;
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
using UnityEngine.Networking;

using SimpleJSON;
using DG.Tweening;
using TMPro;
using InControl;
using LSFunctions;
using Steamworks;

using ArcadiaCustoms.Functions;
using ArcadiaCustoms.Patchers;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;
using RTFunctions.Functions.Data;

namespace ArcadiaCustoms
{
    [BepInPlugin("com.mecha.arcadiacustoms", "ArcadiaCustoms", "2.0.0")]
    [BepInDependency("com.mecha.rtfunctions")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ArcadePlugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> CurrentLevelMode { get; set; }
        public static ConfigEntry<bool> UseNewArcadeUI { get; set; }

        public static ConfigEntry<int> TabsRoundedness { get; set; }

        public static ConfigEntry<int> PlayLevelMenuButtonsRoundness { get; set; }
        public static ConfigEntry<int> PlayLevelMenuIconRoundness { get; set; }

        public static ConfigEntry<int> LocalLevelsRoundness { get; set; }
        public static ConfigEntry<int> LocalLevelsIconRoundness { get; set; }

        public static ConfigEntry<bool> MiscRounded { get; set; }

        public static ConfigEntry<int> PageFieldRoundness { get; set; }

        public static ConfigEntry<string> LocalLevelsPath { get; set; }
        public static ConfigEntry<bool> OpenOnlineLevelAfterDownload { get; set; }

        #region Sorting

        public static ConfigEntry<bool> LocalLevelAscend { get; set; }
        public static ConfigEntry<LevelSort> LocalLevelOrderby { get; set; }

        public enum LevelSort
        {
            Cover,
            Artist,
            Creator,
            File,
            Title,
            Difficulty,
            DateEdited,
            //DateCreated,
            //DatePublished,
        }

        #endregion

        #region Shine Config

        public static ConfigEntry<bool> OnlyShowShineOnSelected { get; set; }
        public static ConfigEntry<float> ShineSpeed { get; set; }
        public static ConfigEntry<float> ShineMaxDelay { get; set; }
        public static ConfigEntry<float> ShineMinDelay { get; set; }
        public static ConfigEntry<Color> ShineColor { get; set; }

        #endregion

        public static ArcadePlugin inst;
        public static string className = "[<color=#F5501B>ArcadiaCustoms</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        readonly Harmony harmony = new Harmony("Arcade");

        public static bool fromLevel = false;

        public static float timeInLevel = 0f;
        public static float timeInLevelOffset = 0f;

        public static GameObject buttonPrefab;

        void Awake()
        {
            inst = this;

            CurrentLevelMode = Config.Bind("Level", "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb.");
            CurrentLevelMode.SettingChanged += CurrentLevelModeChanged;

            UseNewArcadeUI = Config.Bind("Arcade", "Use New UI", true, "If the arcade should use the new UI or not. The old UI should always be accessible if you want to use it.");

            TabsRoundedness = Config.Bind("Arcade", "Tabs Roundness", 1, new ConfigDescription("How rounded the tabs at the top of the Arcade UI are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            TabsRoundedness.SettingChanged += TabsRoundnessChanged;

            LocalLevelsRoundness = Config.Bind("Arcade", "Local Levels Roundness", 1, new ConfigDescription("How rounded the levels are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            LocalLevelsIconRoundness = Config.Bind("Arcade", "Local Levels Icon Roundness", 0, new ConfigDescription("How rounded the levels' icon are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            LocalLevelsRoundness.SettingChanged += LocalLevelPanelsRoundnessChanged;
            LocalLevelsIconRoundness.SettingChanged += LocalLevelPanelsRoundnessChanged;

            MiscRounded = Config.Bind("Arcade", "Misc Rounded", true, "If the some random elements should be rounded in the UI. (New UI Only)");
            MiscRounded.SettingChanged += MiscRoundedChanged;

            OnlyShowShineOnSelected = Config.Bind("Arcade", "Only Show Shine on Selected", true, "If the SS rank shine should only show on the current selected level with an SS rank or on all levels with an SS rank.");
            ShineSpeed = Config.Bind("Arcade", "SS Rank Shine Speed", 0.7f, new ConfigDescription("How fast the shine goes by.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMaxDelay = Config.Bind("Arcade", "SS Rank Shine Max Delay", 0.6f, new ConfigDescription("The max time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineMinDelay = Config.Bind("Arcade", "SS Rank Shine Min Delay", 0.2f, new ConfigDescription("The min time the shine delays.", new AcceptableValueRange<float>(0.1f, 3f)));
            ShineColor = Config.Bind("Arcade", "SS Rank Shine Color", new Color(1f, 0.933f, 0.345f, 1f), "The color of the shine.");

            PageFieldRoundness = Config.Bind("Arcade", "Page Field Roundness", 1, new ConfigDescription("How rounded the Page Input Field is. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            PageFieldRoundness.SettingChanged += MiscRoundedChanged;

            PlayLevelMenuButtonsRoundness = Config.Bind("Arcade", "Play Level Menu Buttons Roundness", 1, new ConfigDescription("How rounded the Play Menu Buttons are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            PlayLevelMenuButtonsRoundness.SettingChanged += PlayLevelMenuRoundnessChanged;

            PlayLevelMenuIconRoundness = Config.Bind("Arcade", "Play Level Menu Icon Roundness", 2, new ConfigDescription("How rounded the Play Menu Buttons are. (New UI Only)", new AcceptableValueRange<int>(0, 5)));
            PlayLevelMenuIconRoundness.SettingChanged += PlayLevelMenuRoundnessChanged;

            LocalLevelOrderby = Config.Bind("Arcade Sorting", "Local Orderby", LevelSort.Cover, "How the level list is ordered.");
            LocalLevelAscend = Config.Bind("Arcade Sorting", "Local Ascend", true, "If the level order should be up or down.");
            LocalLevelOrderby.SettingChanged += LocalLevelSortChanged;
            LocalLevelAscend.SettingChanged += LocalLevelSortChanged;

            LocalLevelsPath = Config.Bind("Level", "Arcade Path in Beatmaps", "arcade", "The location of your local arcade folder.");
            LocalLevelsPath.SettingChanged += LocalLevelsPathChanged;

            OpenOnlineLevelAfterDownload = Config.Bind("Arcade", "Open After Download", true, "If the Play Level Menu should open once the level has finished downloading.");

            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
            LevelManager.Path = LocalLevelsPath.Value;

            Logger.LogInfo($"Plugin Arcadia Customs is loaded!");

            harmony.PatchAll();

            if (!ModCompatibility.mods.ContainsKey("ArcadiaCustoms"))
            {
                var mod = new ModCompatibility.Mod(this, GetType());
                ModCompatibility.mods.Add("ArcadiaCustoms", mod);
            }
        }

        #region Settings Changed

        void LocalLevelSortChanged(object sender, EventArgs e)
        {
            LevelManager.Sort((int)LocalLevelOrderby.Value, LocalLevelAscend.Value);

            if (LevelMenuManager.inst)
            {
                LevelMenuManager.levelFilter = (int)LocalLevelOrderby.Value;
                LevelMenuManager.levelAscend = LocalLevelAscend.Value;

                var toggleClone = LevelMenuManager.levelList.transform.Find("toggle/toggle").GetComponent<Toggle>();
                toggleClone.onValueChanged.RemoveAllListeners();
                toggleClone.isOn = LevelMenuManager.levelAscend;
                toggleClone.onValueChanged.AddListener(delegate (bool _val)
                {
                    LevelMenuManager.levelAscend = _val;
                    LevelMenuManager.Sort();
                    inst.StartCoroutine(LevelMenuManager.GenerateUIList());
                });

                var dropdownClone = LevelMenuManager.levelList.transform.Find("orderby dropdown").GetComponent<Dropdown>();
                dropdownClone.onValueChanged.RemoveAllListeners();
                dropdownClone.value = LevelMenuManager.levelFilter;
                dropdownClone.onValueChanged.AddListener(delegate (int _val)
                {
                    LevelMenuManager.levelFilter = _val;
                    LevelMenuManager.Sort();
                    inst.StartCoroutine(LevelMenuManager.GenerateUIList());
                });

                StartCoroutine(LevelMenuManager.GenerateUIList());
            }

            if (ArcadeMenuManager.inst)
            {
                ArcadeMenuManager.inst.selected = new Vector2Int(0, 2);
                if (ArcadeMenuManager.inst.localPageField.text != "0")
                    ArcadeMenuManager.inst.localPageField.text = "0";
                else
                    StartCoroutine(ArcadeMenuManager.inst.RefreshLocalLevels());
            }
        }

        void PlayLevelMenuRoundnessChanged(object sender, EventArgs e) => PlayLevelMenuManager.inst?.UpdateRoundness();

        void MiscRoundedChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateMiscRoundness();

        void LocalLevelPanelsRoundnessChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateLocalLevelsRoundness();

        void TabsRoundnessChanged(object sender, EventArgs e) => ArcadeMenuManager.inst?.UpdateTabRoundness();

        void CurrentLevelModeChanged(object sender, EventArgs e)
        {
            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
        }

        void LocalLevelsPathChanged(object sender, EventArgs e)
        {
            LevelManager.Path = LocalLevelsPath.Value;
        }

        #endregion

        public static void ReloadMenu()
        {
            if (fromLevel)
            {
                var menu = new GameObject("Arcade Menu System");
                Component component = UseNewArcadeUI.Value ? menu.AddComponent<ArcadeMenuManager>() : menu.AddComponent<LevelMenuManager>();
            }
            else
            {
                var menu = new GameObject("Load Level System");
                menu.AddComponent<LoadLevelsManager>();
            }
        }

        public static bool currentlyLoading = false;
        public static IEnumerator GetLevelList()
        {
            float delay = 0f;
            if (!currentlyLoading)
            {
                currentlyLoading = true;
                fromLevel = false;
                ArcadeManager.inst.skippedLoad = false;
                ArcadeManager.inst.forcedSkip = false;
                DataManager.inst.UpdateSettingBool("IsArcade", true);

                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + LevelManager.ListPath))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + LevelManager.ListPath);

                var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + LevelManager.ListPath, "*", SearchOption.TopDirectoryOnly);

                if (LoadLevelsManager.inst != null)
                    LoadLevelsManager.totalLevelCount = directories.Length;

                LevelManager.Levels.Clear();
                LevelManager.ArcadeQueue.Clear();
                LevelManager.LoadProgress();

                for (int i = 0; i < directories.Length; i++)
                {
                    var folder = directories[i];

                    if (LoadLevelsManager.inst != null && LoadLevelsManager.inst.cancelled)
                    {
                        SceneManager.inst.LoadScene("Input Select");
                        currentlyLoading = false;
                        yield break;
                    }

                    var path = folder.Replace("\\", "/");
                    var name = Path.GetFileName(path);

                    yield return new WaitForSeconds(delay);

                    MetaData metadata = null;

                    if (RTFile.FileExists($"{path}/metadata.vgm"))
                        metadata = MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.vgm")));
                    else if (RTFile.FileExists($"{path}/metadata.lsb"))
                        metadata = MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.lsb")));

                    if (metadata == null)
                    {
                        if (LoadLevelsManager.inst)
                            LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No metadata in {name}</color>", i, true);

                        yield return new WaitForSeconds(0.5f);

                        continue;
                    }
                    
                    if (!RTFile.FileExists($"{path}/level.ogg") && !RTFile.FileExists($"{path}/level.wav") && !RTFile.FileExists($"{path}/level.mp3")
                        && !RTFile.FileExists($"{path}/audio.ogg") && !RTFile.FileExists($"{path}/audio.wav") && !RTFile.FileExists($"{path}/audio.mp3"))
                    {
                        if (LoadLevelsManager.inst)
                            LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                        yield return new WaitForSeconds(0.5f);

                        continue;
                    }
                    
                    if (!RTFile.FileExists($"{path}/level.lsb") && !RTFile.FileExists($"{path}/level.vgd"))
                    {
                        if (LoadLevelsManager.inst)
                            LoadLevelsManager.inst.UpdateInfo(SteamWorkshop.inst.defaultSteamImageSprite, $"<color=$FF0000>No song in {name}</color>", i, true);

                        yield return new WaitForSeconds(0.01f);

                        continue;
                    }

                    if ((string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "-1")
                        && (string.IsNullOrEmpty(metadata.LevelBeatmap.beatmap_id) && metadata.LevelBeatmap.beatmap_id == "-1" || metadata.LevelBeatmap.beatmap_id == "0")
                        && (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "-1" || metadata.arcadeID == "0"))
                    {
                        metadata.arcadeID = LSText.randomNumString(16);
                        var metadataJN = metadata.ToJSON();
                        RTFile.WriteToFile($"{path}/metadata.lsb", metadataJN.ToString(3));
                    }

                    var level = new Level(path + "/");

                    if (LevelManager.Saves.Has(x => x.ID == level.id))
                        level.playerData = LevelManager.Saves.Find(x => x.ID == level.id);

                    if (LoadLevelsManager.inst)
                        LoadLevelsManager.inst.UpdateInfo(level.icon, $"Loading {name}", i);

                    LevelManager.Levels.Add(level);

                    delay += 0.0001f;
                }

                currentlyLoading = false;
            }

            if (LoadLevelsManager.inst != null)
                LoadLevelsManager.inst.End();

            yield break;
        }

        public static IEnumerator OnLoadingEnd()
        {
            yield return new WaitForSeconds(0.1f);
            AudioManager.inst.PlaySound("loadsound");
            var menu = new GameObject("Arcade Menu System");
            Component component = UseNewArcadeUI.Value ? menu.AddComponent<ArcadeMenuManager>() : menu.AddComponent<LevelMenuManager>();

            yield break;
        }
    }
}
