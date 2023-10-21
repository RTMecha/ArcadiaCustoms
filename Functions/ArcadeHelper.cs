using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers.Networking;

namespace ArcadiaCustoms.Functions
{
    public static class ArcadeHelper
    {
        public static string publishedLevels;

        public static string DailyLevelPath => "beatmaps/showcase level/level.lsb";
        public static string DailyMetadataPath => "beatmaps/showcase level/metadata.lsb";
        public static string DailySongPath => "beatmaps/showcase level/level.ogg";
        public static string DailyCoverPath => "beatmaps/showcase level/level.jpg";
        public static string DailyPlayersPath => "beatmaps/showcase level/players.lsb";

        public static string DriveDirect => "https://drive.google.com/uc?export=download&id=";
        public static string DailyLevel => "17G9LoU28x954v82M_2Hb8qmxxRKk59hq";
        public static string DailyMetadata => "1kBEEWqXhHaAxn1jLFAKkNYVmmYyA0h9G";
        public static string DailySong => "1Nu2j-K_btfoK5AljXLz-6YCg2P8bdIO-";
        public static string DailyCover => "1sxP3JwFlyIAy3Y6pzj7kWa_1h62eun54";
        public static string DailyPlayers => "1xDOT8m52kqM7pNTG0S2Ah9Ujp4FMa9kG";

        public static float BeatmapJSONProgress { get; set; }
        public static float MetadataJSONProgress { get; set; }
        public static float SongProgress { get; set; }
        public static float CoverProgress { get; set; }
        public static float PlayersProgress { get; set; }

        public static IEnumerator GetDailyLevel(Action<SteamWorkshop.SteamItem> levelCallback, Action<Sprite> coverCallback, Action<AudioClip> songCallback)
        {
            string beatmapJSON = "";
            string metadataJSON = "";
            AudioClip audioClip = null;
            Sprite cover = null;

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/showcase level"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/showcase level");

            yield return AlephNetworkManager.inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DriveDirect}{DailyLevel}", delegate (float x)
            {
                BeatmapJSONProgress = x;
                Debug.Log($"BeatmapJSONProgress: {x}");

                if (LoadLevels.inst)
                    LoadLevels.inst.UpdateInfo($"Loading Showcase Level level.lsb... {x * 100}%", x);

            }, delegate (byte[] bytes)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + DailyLevelPath, bytes);
            }, delegate (string onError)
            {

            }));
            
            yield return AlephNetworkManager.inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DriveDirect}{DailyMetadata}", delegate (float x)
            {
                MetadataJSONProgress = x;
                Debug.Log($"MetadataJSONProgress: {x}");

                if (LoadLevels.inst)
                    LoadLevels.inst.UpdateInfo($"Loading Showcase Level metadata.lsb... {x * 100}%", x);

            }, delegate (byte[] bytes)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + DailyMetadataPath, bytes);
            }, delegate (string onError)
            {

            }));
            
            yield return AlephNetworkManager.inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DriveDirect}{DailySong}", delegate (float x)
            {
                SongProgress = x;
                Debug.Log($"SongProgress: {x}");

                if (LoadLevels.inst)
                    LoadLevels.inst.UpdateInfo($"Loading Showcase Level level.ogg... {x * 100}%", x);

            },
            delegate (byte[] bytes)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + DailySongPath, bytes);
            }, delegate (string onError)
            {

            }));
            
            yield return AlephNetworkManager.inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DriveDirect}{DailyCover}", delegate (float x)
            {
                CoverProgress = x;
                Debug.Log($"CoverProgress: {x}");

                if (LoadLevels.inst)
                    LoadLevels.inst.UpdateInfo($"Loading Showcase Level level.jpg... {x * 100}%", x);

            },
            delegate (byte[] bytes)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + DailyCoverPath, bytes);
            }, delegate (string onError)
            {

            }));

            yield return AlephNetworkManager.inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DriveDirect}{DailyPlayers}", delegate (float x)
            {
                PlayersProgress = x;
                Debug.Log($"PlayersProgress: {x}");

                if (LoadLevels.inst)
                    LoadLevels.inst.UpdateInfo($"Loading Showcase Level players.lsb... {x * 100}%", x);
            },
            delegate (byte[] bytes)
            {
                File.WriteAllBytes(RTFile.ApplicationDirectory + DailyPlayersPath, bytes);
            }, delegate (string onError)
            {

            }));

            if (RTFile.FileExists(RTFile.ApplicationDirectory + DailyLevelPath))
                beatmapJSON = FileManager.inst.LoadJSONFile(DailyLevelPath);
            
            if (RTFile.FileExists(RTFile.ApplicationDirectory + DailyMetadataPath))
                metadataJSON = FileManager.inst.LoadJSONFile(DailyMetadataPath);

            if (RTFile.FileExists(RTFile.ApplicationDirectory + DailySongPath))
                yield return FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFile(DailySongPath, delegate (AudioClip ac)
                {
                    audioClip = ac;
                }));

            if (RTFile.FileExists(RTFile.ApplicationDirectory + DailyCoverPath))
                yield return FileManager.inst.StartCoroutine(FileManager.inst.LoadImageFileRaw(RTFile.ApplicationDirectory + DailyCoverPath, delegate (Sprite sprite)
                {
                    cover = sprite;
                }, delegate (string onError)
                {

                }));

            if (string.IsNullOrEmpty(beatmapJSON) || string.IsNullOrEmpty(metadataJSON) || audioClip == null || cover == null)
            {
                Debug.LogError($"{ArcadePlugin.className}Could not get showcase level.");
                yield break;
            }

            var metadata = DataManager.inst.ParseMetadata(metadataJSON, false);

            var id = new Steamworks.PublishedFileId_t((ulong)metadata.beatmap.workshop_id);

            var steamItem = new SteamWorkshop.SteamItem(id);

            steamItem.id = id;
            steamItem.itemID = metadata.beatmap.workshop_id;
            steamItem.metaData = metadata;
            steamItem.folder = RTFile.ApplicationDirectory + "beatmaps/showcase level";

            if (levelCallback != null)
                levelCallback(steamItem);
            if (coverCallback != null)
                coverCallback(cover);
            if (songCallback != null)
                songCallback(audioClip);
        }
    }
}
