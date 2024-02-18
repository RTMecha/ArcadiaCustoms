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

namespace ArcadiaCustoms
{
    [BepInPlugin("com.mecha.arcadiacustoms", "ArcadiaCustoms", " 1.6.3")]
    [BepInDependency("com.mecha.rtfunctions")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ArcadePlugin : BaseUnityPlugin
    {
        //TODO
        //Implement the shine effect when you've SS ranked a level.

        //Update list

        public static ConfigEntry<int> CurrentLevelMode { get; set; }

        public static ArcadePlugin inst;
        public static string className = "[<color=#F5501B>ArcadiaCustoms</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        readonly Harmony harmony = new Harmony("Arcade");

        public static bool fromLevel = false;

        public static float timeInLevel = 0f;
        public static float timeInLevelOffset = 0f;

        void Awake()
        {
            inst = this;

            CurrentLevelMode = Config.Bind("Level", "Level Mode", 0, "If a modes.lsms exists in the arcade level folder that you're loading, it will list other level modes (think easy mode, cutscene mode, hard mode, etc). The value in this config is for choosing which mode gets loaded. 0 is the default level.lsb.");
            CurrentLevelMode.SettingChanged += CurrentLevelModeChanged;
            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;

            Logger.LogInfo($"Plugin Arcadia Customs is loaded!");

            harmony.PatchAll();

            try
            {
                if (!ModCompatibility.mods.ContainsKey("ArcadiaCustoms"))
                {
                    var mod = new ModCompatibility.Mod(this, GetType());
                    ModCompatibility.mods.Add("ArcadiaCustoms", mod);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Mod Error" + ex.ToString());
            }
        }

        void CurrentLevelModeChanged(object sender, EventArgs e)
        {
            LevelManager.CurrentLevelMode = CurrentLevelMode.Value;
        }

        public static void MainMenuTester()
        {
            if (fromLevel)
            {
                var menu = new GameObject("Main Menu System");
                menu.AddComponent<ArcadeMenuManager>();
            }
            else
            {
                var menu = new GameObject("Load Level System");
                menu.AddComponent<LoadLevels>();
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
                {
                    SceneManager.inst.LoadScene("Input Select");
                    currentlyLoading = false;
                    System.Windows.Forms.MessageBox.Show("Arcade directory does not exist!");
                    yield break;
                }

                var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + LevelManager.ListPath, "*", SearchOption.TopDirectoryOnly);

                if (directories.Length < 1)
                {
                    SceneManager.inst.LoadScene("Input Select");
                    currentlyLoading = false;
                    System.Windows.Forms.MessageBox.Show("No levels to load!");
                    yield break;
                }

                if (LoadLevels.inst != null)
                    LoadLevels.totalLevelCount = directories.Length;

                LevelManager.Levels.Clear();
                LevelManager.ArcadeQueue.Clear();
                LevelManager.LoadProgress();

                int num = 0;
                foreach (var folder in directories)
                {
                    if (LoadLevels.inst != null && LoadLevels.inst.cancelled)
                    {
                        SceneManager.inst.LoadScene("Input Select");
                        currentlyLoading = false;
                        yield break;
                    }

                    var path = folder.Replace("\\", "/");
                    var name = Path.GetFileName(path);

                    yield return new WaitForSeconds(delay);

                    if (RTFile.FileExists(path + "/metadata.lsb") || RTFile.FileExists(path + "/metadata.vgm"))
                    {
                        var level = new Level(path + "/");

                        if (level.metadata && level.metadata.beatmap.workshop_id == -1)
                            level.metadata.beatmap.workshop_id = UnityEngine.Random.Range(0, int.MaxValue);

                        if (LevelManager.Saves.Has(x => x.ID == level.id))
                            level.playerData = LevelManager.Saves.Find(x => x.ID == level.id);

                        if (LoadLevels.inst)
                            LoadLevels.inst.UpdateInfo(level.icon, name, num);

                        LevelManager.Levels.Add(level);
                    }

                    delay += 0.0001f;
                    num++;
                }

                currentlyLoading = false;
            }

            if (LoadLevels.inst != null)
                LoadLevels.inst.End();

            yield break;
        }

        public static IEnumerator OnLoadingEnd()
        {
            yield return new WaitForSeconds(0.1f);
            AudioManager.inst.PlaySound("loadsound");
            var menu = new GameObject("Main Menu System");
            menu.AddComponent<ArcadeMenuManager>();
            yield break;
        }
    }
}
