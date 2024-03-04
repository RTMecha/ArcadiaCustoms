﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;

using LSFunctions;
using DG.Tweening;
using InControl;
using TMPro;

using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;
using RTFunctions.Functions.Animation.Keyframe;
using Ease = RTFunctions.Functions.Animation.Ease;
using SimpleJSON;
using System.IO;
using System.IO.Compression;
using Crosstales.FB;
using System.Windows.Forms;
using Cursor = UnityEngine.Cursor;
using Screen = UnityEngine.Screen;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ArcadiaCustoms.Functions
{
    public class ArcadeMenuManager : MonoBehaviour
    {
        public static ArcadeMenuManager inst;

        #region Base

        public GameObject menuUI;

        public Color textColor;
        public Color highlightColor;
        public Color textHighlightColor;
        public Color buttonBGColor;

        public static Vector2 ZeroFive => new Vector2(0.5f, 0.5f);
        public static Color ShadeColor => new Color(0f, 0f, 0f, 0.3f);

        #endregion

        #region Page

        public static int MaxLevelsPerPage { get; set; } = 20;
        public List<int> CurrentPage { get; set; } = new List<int>
        {
            0,
            0,
            0,
            0,
            0,
            0,
        };

        #endregion

        #region Selection

        public Vector2Int selected;

        public bool SelectedTab => selected.y == 0;

        public List<int> SelectionLimit { get; set; } = new List<int>();

        #endregion

        #region Tabs

        public RectTransform TabContent { get; set; }
        public List<RectTransform> RegularBases { get; set; } = new List<RectTransform>();
        public List<RectTransform> RegularContents { get; set; } = new List<RectTransform>();
        public List<RectTransform> SelectedContents { get; set; } = new List<RectTransform>();

        public List<Tab> Tabs { get; set; } = new List<Tab>();

        public int CurrentTab { get; set; } = 0;

        public List<LocalLevelButton> LocalLevels { get; set; } = new List<LocalLevelButton>();

        #endregion

        #region Settings

        List<List<Tab>> Settings { get; set; } = new List<List<Tab>>
        {
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
            new List<Tab>(),
        };

        #endregion

        #region Open

        public bool OpenedLocalLevel { get; set; }
        public bool OpenedOnlineLevel { get; set; }

        #endregion

        #region Prefabs

        public GameObject localLevelPrefab;

        #endregion

        public float Scroll { get; set; }

        public bool init = false;

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupScene());
        }

        void Update()
        {
            UpdateTheme();

            if (!init || OpenedLocalLevel || OpenedOnlineLevel)
                return;

            UpdateControls();

            for (int i = 0; i < Tabs.Count; i++)
            {
                Tabs[i].Text.color = selected.y == 0 && i == selected.x ? textHighlightColor : textColor;
                Tabs[i].Image.color = selected.y == 0 && i == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            for (int i = 0; i < Settings[CurrentTab].Count; i++)
            {
                var setting = Settings[CurrentTab][i];
                setting.Text.color = selected.y == setting.Position.y && setting.Position.x == selected.x ? textHighlightColor : textColor;
                setting.Image.color = selected.y == setting.Position.y && setting.Position.x == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            try
            {
                if (CurrentTab == 0)
                {
                    localPageField.caretColor = highlightColor;
                    localSearchField.caretColor = highlightColor;

                    var selectOnly = ArcadePlugin.OnlyShowShineOnSelected.Value;
                    var speed = ArcadePlugin.ShineSpeed.Value;
                    var maxDelay = ArcadePlugin.ShineMaxDelay.Value;
                    var minDelay = ArcadePlugin.ShineMinDelay.Value;
                    var color = ArcadePlugin.ShineColor.Value;

                    foreach (var level in LocalLevels)
                    {
                        if (loadingLocalLevels)
                            break;

                        var isSelected = selected.x == level.Position.x && selected.y - 2 == level.Position.y;

                        level.Title.color = isSelected ? textHighlightColor : textColor;
                        level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                        var levelRank = LevelManager.GetLevelRank(level.Level);

                        var shineController = level.ShineController;

                        shineController.speed = speed;
                        shineController.maxDelay = maxDelay;
                        shineController.minDelay = minDelay;
                        shineController.offset = 260f;
                        shineController.offsetOverShoot = 32f;
                        level.shine1?.SetColor(color);
                        level.shine2?.SetColor(color);

                        if ((selectOnly && isSelected || !selectOnly) && levelRank.name == "SS" && shineController.currentLoop == 0)
                        {
                            shineController.LoopAnimation(-1, LSColors.yellow400);
                        }

                        if ((selectOnly && !isSelected || levelRank.name != "SS") && shineController.currentLoop == -1)
                        {
                            shineController.StopAnimation();
                        }

                        if (level.selected != isSelected)
                        {
                            level.selected = isSelected;
                            if (level.selected)
                            {
                                if (level.ExitAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                }

                                level.EnterAnimation = new AnimationManager.Animation("Enter Animation");
                                level.EnterAnimation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                                {
                                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                        new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.EnterAnimation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                };
                                AnimationManager.inst.Play(level.EnterAnimation);
                            }
                            else
                            {
                                if (level.EnterAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                }

                                level.ExitAnimation = new AnimationManager.Animation("Exit Animation");
                                level.ExitAnimation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                                {
                                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                        new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.ExitAnimation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                };
                                AnimationManager.inst.Play(level.ExitAnimation);
                            }
                        }

                        if (isSelected && !LSHelpers.IsUsingInputField() && InputDataManager.inst.menuActions.Submit.WasPressed)
                        {
                            level.Clickable?.onClick?.Invoke(null);
                        }
                    }
                }

                if (CurrentTab == 1)
                {
                    onlinePageField.caretColor = highlightColor;
                    onlineSearchField.caretColor = highlightColor;

                    foreach (var level in OnlineLevels)
                    {
                        if (loadingOnlineLevels)
                            break;

                        var isSelected = selected.x == level.Position.x && selected.y - 3 == level.Position.y;

                        level.TitleText.color = isSelected ? textHighlightColor : textColor;
                        level.BaseImage.color = isSelected ? highlightColor : buttonBGColor;

                        if (isSelected && !LSHelpers.IsUsingInputField() && InputDataManager.inst.menuActions.Submit.WasPressed)
                        {
                            level.Clickable?.onClick?.Invoke(null);
                        }

                        if (level.selected != isSelected)
                        {
                            level.selected = isSelected;
                            if (level.selected)
                            {
                                if (level.ExitAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                }

                                level.EnterAnimation = new AnimationManager.Animation("Enter Animation");
                                level.EnterAnimation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                                {
                                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1.1f, Ease.CircOut),
                                        new FloatKeyframe(0.31f, 1.1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.EnterAnimation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                };
                                AnimationManager.inst.Play(level.EnterAnimation);
                            }
                            else
                            {
                                if (level.EnterAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(level.EnterAnimation.id);
                                }

                                level.ExitAnimation = new AnimationManager.Animation("Exit Animation");
                                level.ExitAnimation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                                {
                                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 1.1f, Ease.Linear),
                                        new FloatKeyframe(0.3f, 1f, Ease.BounceOut),
                                        new FloatKeyframe(0.31f, 1f, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        if (level.RectTransform != null)
                                            level.RectTransform.localScale = new Vector3(x, x, 1f);
                                    }),
                                };
                                level.ExitAnimation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(level.ExitAnimation.id);
                                };
                                AnimationManager.inst.Play(level.ExitAnimation);
                            }
                        }

                    }
                }
            }
            catch
            {

            }
        }

        void UpdateControls()
        {
            if (LSHelpers.IsUsingInputField() || loadingOnlineLevels || loadingLocalLevels)
                return;

            var actions = InputDataManager.inst.menuActions;

            if (actions.Left.WasPressed)
            {
                if (selected.x > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x--;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Right.WasPressed)
            {
                if (selected.x < SelectionLimit[selected.y] - 1)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x++;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Up.WasPressed)
            {
                if (selected.y > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y--;
                    selected.x = Mathf.Clamp(selected.x, 0, SelectionLimit[selected.y] - 1);
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Down.WasPressed)
            {
                if (selected.y < SelectionLimit.Count - 1)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y++;
                    selected.x = Mathf.Clamp(selected.x, 0, SelectionLimit[selected.y] - 1);
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Submit.WasPressed && selected.y == 0)
            {
                AudioManager.inst.PlaySound("blip");
                SelectTab();
            }

            if (actions.Submit.WasPressed && selected.y == 1)
            {
                Settings[CurrentTab][selected.x].Clickable.onClick?.Invoke(null);
            }
            
            if (actions.Submit.WasPressed && selected.y == 2 && CurrentTab == 1)
            {
                Settings[CurrentTab][selected.x].Clickable.onClick?.Invoke(null);
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 1;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha2))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 2;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha3))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 3;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha4))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 4;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 5;
                SelectTab();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha6))
            {
                AudioManager.inst.PlaySound("blip");
                selected.y = 0;
                selected.x = 6;
                SelectTab();
            }
        }

        void UpdateTheme()
        {
            var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            Camera.main.backgroundColor = LSColors.HexToColor(currentTheme["values"]["bg"]);
            textColor = currentTheme["values"]["text"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text"]);
            highlightColor = currentTheme["values"]["highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["highlight"]);
            textHighlightColor = currentTheme["values"]["text-highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text-highlight"]);
            buttonBGColor = currentTheme["values"]["buttonbg"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["buttonbg"]);
        }

        public bool CanSelect => Cursor.visible && !loadingOnlineLevels && !loadingLocalLevels;

        #region Setup

        public IEnumerator DeleteComponents()
        {
            Destroy(GameObject.Find("Interface"));

            var eventSystem = GameObject.Find("EventSystem");
            Destroy(eventSystem.GetComponent<InControlInputModule>());
            Destroy(eventSystem.GetComponent<BaseInput>());
            eventSystem.AddComponent<StandaloneInputModule>();

            var mainCamera = GameObject.Find("Main Camera");
            Destroy(mainCamera.GetComponent<InterfaceLoader>());
            Destroy(mainCamera.GetComponent<ArcadeController>());
            Destroy(mainCamera.GetComponent<FlareLayer>());
            Destroy(mainCamera.GetComponent<GUILayer>());
            yield break;
        }

        public IEnumerator SetupScene()
        {
            LSHelpers.ShowCursor();
            yield return StartCoroutine(DeleteComponents());
            UpdateTheme();

            LevelManager.current = 0;
            LevelManager.ArcadeQueue.Clear();

            var inter = new GameObject("Interface");
            inter.transform.localScale = Vector3.one * RTHelpers.screenScale;
            menuUI = inter;
            inter.AddComponent<CursorManager>();
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = RTHelpers.screenScale;
            canvas.sortingOrder = 10000;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", ArcadePlugin.className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

            inter.AddComponent<GraphicRaycaster>();

            var selectionBase = new GameObject("Selection Base");
            selectionBase.transform.SetParent(inter.transform);
            selectionBase.transform.localScale = Vector3.one;

            var selectionBaseRT = selectionBase.AddComponent<RectTransform>();
            selectionBaseRT.anchoredPosition = Vector2.zero;

            var playLevelMenuBase = new GameObject("Play Level Menu");
            playLevelMenuBase.transform.SetParent(inter.transform);
            playLevelMenuBase.transform.localScale = Vector3.one;

            var playLevelMenuBaseRT = playLevelMenuBase.AddComponent<RectTransform>();
            playLevelMenuBaseRT.anchoredPosition = new Vector2(0f, -1080f);
            playLevelMenuBaseRT.sizeDelta = new Vector2(1920f, 1080f);

            var playLevelMenuImage = playLevelMenuBase.AddComponent<Image>();

            var playLevelMenu = playLevelMenuBase.AddComponent<PlayLevelMenuManager>();
            playLevelMenu.rectTransform = playLevelMenuBaseRT;
            playLevelMenu.background = playLevelMenuImage;

            StartCoroutine(playLevelMenu.SetupPlayLevelMenu());
            
            var downloadLevelMenuBase = new GameObject("Download Level Menu");
            downloadLevelMenuBase.transform.SetParent(inter.transform);
            downloadLevelMenuBase.transform.localScale = Vector3.one;

            var downloadLevelMenuBaseRT = downloadLevelMenuBase.AddComponent<RectTransform>();
            downloadLevelMenuBaseRT.anchoredPosition = new Vector2(0f, -1080f);
            downloadLevelMenuBaseRT.sizeDelta = new Vector2(1920f, 1080f);

            var downloadLevelMenuImage = downloadLevelMenuBase.AddComponent<Image>();

            var downloadLevelMenu = downloadLevelMenuBase.AddComponent<DownloadLevelMenuManager>();
            downloadLevelMenu.rectTransform = downloadLevelMenuBaseRT;
            downloadLevelMenu.background = downloadLevelMenuImage;

            StartCoroutine(downloadLevelMenu.SetupDownloadLevelMenu());

            var topBar = UIManager.GenerateUIImage("Top Bar", selectionBaseRT);

            TabContent = (RectTransform)topBar["RectTransform"];
            UIManager.SetRectTransform(TabContent, new Vector2(0f, 480f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 115f));

            //((Image)topBar["Image"]).color = buttonBGColor;

            topBar.GetObject<Image>().color = buttonBGColor;

            string[] tabNames = new string[]
            {
                "Local",
                "Online",
                "Browser",
                "Download",
                "Queue",
                "Steam",
            };

            SelectionLimit.Add(1);

            var close = GenerateTab();
            close.RectTransform.anchoredPosition = new Vector2(-904f, 0f);
            close.RectTransform.sizeDelta = new Vector2(100f, 100f);
            close.Text.alignment = TextAlignmentOptions.Center;
            close.Text.fontSize = 72;
            close.Text.text = "<scale=1.3><pos=6>X";
            close.Text.color = textColor;
            close.Image.color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            close.Clickable.onClick = delegate (PointerEventData pointerEventData)
            {
                AudioManager.inst.PlaySound("blip");
                SelectTab();
            };
            close.Clickable.onEnter = delegate (PointerEventData pointerEventData)
            {
                if (!CanSelect)
                    return;

                AudioManager.inst.PlaySound("LeftRight");
                selected.y = 0;
                selected.x = 0;
            };

            if (ArcadePlugin.TabsRoundedness.Value != 0)
                SpriteManager.SetRoundedSprite(close.Image, ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
            else
                close.Image.sprite = null;

            for (int i = 0; i < 6; i++)
            {
                int index = i;

                var tab = GenerateTab();

                tab.RectTransform.anchoredPosition = new Vector2(-700f + (i * 300), 0f);
                tab.RectTransform.sizeDelta = new Vector2(290f, 100f);
                tab.Text.alignment = TextAlignmentOptions.Center;
                tab.Text.text = tabNames[Mathf.Clamp(i, 0, tabNames.Length - 1)];
                tab.Text.color = textColor;
                tab.Image.color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                if (ArcadePlugin.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(tab.Image, ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    tab.Image.sprite = null;

                tab.Clickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    AudioManager.inst.PlaySound("blip");
                    SelectTab();
                };
                tab.Clickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!CanSelect)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y = 0;
                    selected.x = index + 1;
                };
                SelectionLimit[0]++;
            }

            // Settings
            SelectionLimit.Add(0);

            // Local
            {
                var local = new GameObject("Local");
                local.transform.SetParent(selectionBaseRT);
                local.transform.localScale = Vector3.one;

                var localRT = local.AddComponent<RectTransform>();
                localRT.anchoredPosition = Vector3.zero;
                localRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(localRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", localRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Reload", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[RELOAD]";

                    Settings[0].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[0].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor)), 0.2f);
                    localPageField = pageField.GetObject<InputField>();
                    localPageField.onValueChanged.ClearAll();
                    localPageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    localPageField.textComponent.fontSize = 30;
                    localPageField.textComponent.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor));
                    localPageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadePlugin.MiscRounded.Value)
                        SpriteManager.SetRoundedSprite(localPageField.image, 1, SpriteManager.RoundedSide.W);
                    else
                        localPageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[0].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(2, 1),
                    });

                    localPageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, LocalPageCount);
                            SetLocalLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        var menu = new GameObject("Load Level System");
                        menu.AddComponent<LoadLevelsManager>();
                    };

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(localPageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                localPageField.text = Mathf.Clamp(p - 1, 0, LocalPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(2, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(localPageField.text, out int p))
                        {
                            if (p < LocalPageCount)
                            {
                                AudioManager.inst.PlaySound("blip");
                                localPageField.text = Mathf.Clamp(p + 1, 0, LocalPageCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", localRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", localRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(localRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                var selectedContent = new GameObject("Selected Content");
                selectedContent.transform.SetParent(localRT);
                selectedContent.transform.localScale = Vector3.one;

                var selectedContentRT = selectedContent.AddComponent<RectTransform>();
                selectedContentRT.anchoredPosition = Vector2.zero;
                selectedContentRT.sizeDelta = Vector3.zero;

                SelectedContents.Add(selectedContentRT);

                // Prefab
                {
                    var level = UIManager.GenerateUIImage("Level", transform);
                    UIManager.SetRectTransform(level.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, new Vector2(300f, 180f));
                    localLevelPrefab = level.GetObject<GameObject>();
                    localLevelPrefab.AddComponent<Mask>();

                    var clickable = localLevelPrefab.AddComponent<Clickable>();

                    var levelDifficulty = UIManager.GenerateUIImage("Difficulty", level.GetObject<RectTransform>());

                    var levelTitle = UIManager.GenerateUITextMeshPro("Title", level.GetObject<RectTransform>());

                    levelTitle.GetObject<TextMeshProUGUI>().text = "BRO";

                    var levelIconBase = UIManager.GenerateUIImage("Icon Base", level.GetObject<RectTransform>());

                    levelIconBase.GetObject<GameObject>().AddComponent<Mask>();

                    var levelIcon = UIManager.GenerateUIImage("Icon", levelIconBase.GetObject<RectTransform>());

                    levelIcon.GetObject<Image>().sprite = SteamWorkshop.inst.defaultSteamImageSprite;

                    var levelRankShadow = UIManager.GenerateUITextMeshPro("Rank Shadow", level.GetObject<RectTransform>());

                    levelRankShadow.GetObject<TextMeshProUGUI>().text = "F";

                    var levelRank = UIManager.GenerateUITextMeshPro("Rank", level.GetObject<RectTransform>());

                    levelRank.GetObject<TextMeshProUGUI>().text = "F";

                    var shine = ArcadePlugin.buttonPrefab.transform.Find("shine").gameObject
                        .Duplicate(level.GetObject<RectTransform>(), "Shine");

                    var shineController = shine.GetComponent<ShineController>();
                    shineController.maxDelay = 1f;
                    shineController.minDelay = 0.2f;
                    shineController.offset = 260f;
                    shineController.offsetOverShoot = 32f;
                    shineController.speed = 0.7f;
                    shine.transform.AsRT().sizeDelta = new Vector2(300f, 24f);
                    shine.transform.GetChild(0).AsRT().sizeDelta = new Vector2(300f, 8f);
                }

                var searchField = UIManager.GenerateUIInputField("Search", localRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(0f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1600f, 60f));

                if (ArcadePlugin.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                localSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                localSearchField = searchField.GetObject<InputField>();
                localSearchField.onValueChanged.ClearAll();
                localSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                localSearchField.textComponent.color = textColor;
                localSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    LocalSearchTerm = _val;
                });
            }

            // Online
            {
                var online = new GameObject("Online");
                online.transform.SetParent(selectionBaseRT);
                online.transform.localScale = Vector3.one;

                var onlineRT = online.AddComponent<RectTransform>();
                onlineRT.anchoredPosition = Vector3.zero;
                onlineRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(onlineRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", onlineRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var prevPage = UIManager.GenerateUIImage("Previous", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPage.GetObject<RectTransform>(), new Vector2(500f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var prevPageClickable = prevPage.GetObject<GameObject>().AddComponent<Clickable>();

                    prevPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(prevPage.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        prevPage.GetObject<Image>().sprite = null;

                    var prevPageText = UIManager.GenerateUITextMeshPro("Text", prevPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(prevPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    prevPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    prevPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    prevPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    prevPageText.GetObject<TextMeshProUGUI>().text = "<";

                    Settings[1].Add(new Tab
                    {
                        GameObject = prevPage.GetObject<GameObject>(),
                        RectTransform = prevPage.GetObject<RectTransform>(),
                        Clickable = prevPageClickable,
                        Image = prevPage.GetObject<Image>(),
                        Text = prevPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor)), 0.2f);
                    onlinePageField = pageField.GetObject<InputField>();
                    onlinePageField.onValueChanged.ClearAll();
                    onlinePageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    onlinePageField.textComponent.fontSize = 30;
                    onlinePageField.textComponent.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor));
                    onlinePageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadePlugin.MiscRounded.Value)
                        SpriteManager.SetRoundedSprite(localPageField.image, 1, SpriteManager.RoundedSide.W);
                    else
                        localPageField.image.sprite = null;

                    var nextPage = UIManager.GenerateUIImage("Next", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPage.GetObject<RectTransform>(), new Vector2(800f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(80f, 64f));

                    var nextPageClickable = nextPage.GetObject<GameObject>().AddComponent<Clickable>();

                    nextPage.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(nextPage.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        nextPage.GetObject<Image>().sprite = null;

                    var nextPageText = UIManager.GenerateUITextMeshPro("Text", nextPage.GetObject<RectTransform>());
                    UIManager.SetRectTransform(nextPageText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    nextPageText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    nextPageText.GetObject<TextMeshProUGUI>().fontSize = 64;
                    nextPageText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    nextPageText.GetObject<TextMeshProUGUI>().text = ">";

                    Settings[1].Add(new Tab
                    {
                        GameObject = nextPage.GetObject<GameObject>(),
                        RectTransform = nextPage.GetObject<RectTransform>(),
                        Clickable = nextPageClickable,
                        Image = nextPage.GetObject<Image>(),
                        Text = nextPageText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(1, 1),
                    });

                    onlinePageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (CanSelect && int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, OnlineLevelCount);
                            SetOnlineLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(onlinePageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                onlinePageField.text = Mathf.Clamp(p - 1, 0, OnlineLevelCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(onlinePageField.text, out int p))
                        {
                            if (p < OnlineLevelCount)
                            {
                                AudioManager.inst.PlaySound("blip");
                                onlinePageField.text = Mathf.Clamp(p + 1, 0, OnlineLevelCount).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };
                }

                var left = UIManager.GenerateUIImage("Left", onlineRT);
                UIManager.SetRectTransform(left.GetObject<RectTransform>(), new Vector2(-880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                left.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var right = UIManager.GenerateUIImage("Right", onlineRT);
                UIManager.SetRectTransform(right.GetObject<RectTransform>(), new Vector2(880f, 300f), ZeroFive, ZeroFive, new Vector2(0.5f, 1f), new Vector2(160f, 838f));
                right.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.04f);

                var regularContent = new GameObject("Regular Content");
                regularContent.transform.SetParent(onlineRT);
                regularContent.transform.localScale = Vector3.one;

                var regularContentRT = regularContent.AddComponent<RectTransform>();
                regularContentRT.anchoredPosition = Vector2.zero;
                regularContentRT.sizeDelta = Vector3.zero;

                RegularContents.Add(regularContentRT);

                var selectedContent = new GameObject("Selected Content");
                selectedContent.transform.SetParent(onlineRT);
                selectedContent.transform.localScale = Vector3.one;

                var selectedContentRT = selectedContent.AddComponent<RectTransform>();
                selectedContentRT.anchoredPosition = Vector2.zero;
                selectedContentRT.sizeDelta = Vector3.zero;

                SelectedContents.Add(selectedContentRT);

                var searchField = UIManager.GenerateUIInputField("Search", onlineRT);

                UIManager.SetRectTransform(searchField.GetObject<RectTransform>(), new Vector2(-100f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1400f, 60f));

                if (ArcadePlugin.MiscRounded.Value)
                    SpriteManager.SetRoundedSprite(searchField.GetObject<Image>(), 1, SpriteManager.RoundedSide.Bottom);
                else
                    searchField.GetObject<Image>().sprite = null;

                onlineSearchFieldImage = searchField.GetObject<Image>();

                searchField.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.black, 0.2f);

                ((Text)searchField["Placeholder"]).alignment = TextAnchor.MiddleLeft;
                ((Text)searchField["Placeholder"]).text = "Search for level...";
                ((Text)searchField["Placeholder"]).color = LSColors.fadeColor(textColor, 0.2f);
                onlineSearchField = searchField.GetObject<InputField>();
                onlineSearchField.onValueChanged.ClearAll();
                onlineSearchField.textComponent.alignment = TextAnchor.MiddleLeft;
                onlineSearchField.textComponent.color = textColor;
                onlineSearchField.onValueChanged.AddListener(delegate (string _val)
                {
                    OnlineSearchTerm = _val;
                });

                var reload = UIManager.GenerateUIImage("Reload", onlineRT);
                UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(700f, 270f), ZeroFive, ZeroFive, ZeroFive, new Vector2(200f, 60f));

                var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                if (ArcadePlugin.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    reload.GetObject<Image>().sprite = null;

                var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                reloadText.GetObject<TextMeshProUGUI>().text = "[SEARCH]";

                reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                {
                    if (!CanSelect)
                        return;

                    AudioManager.inst.PlaySound("LeftRight");
                    selected = new Vector2Int(0, 2);
                };
                reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                {
                    StartCoroutine(SearchOnlineLevels());
                };

                Settings[1].Add(new Tab
                {
                    GameObject = reload.GetObject<GameObject>(),
                    RectTransform = reload.GetObject<RectTransform>(),
                    Clickable = reloadClickable,
                    Image = reload.GetObject<Image>(),
                    Text = reloadText.GetObject<TextMeshProUGUI>(),
                    Position = new Vector2Int(0, 2),
                });

            }

            // Browser
            {
                var browser = new GameObject("Browser");
                browser.transform.SetParent(selectionBaseRT);
                browser.transform.localScale = Vector3.one;

                var browserRT = browser.AddComponent<RectTransform>();
                browserRT.anchoredPosition = Vector3.zero;
                browserRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(browserRT);

                // Settings
                {
                    var localSettingsBar = UIManager.GenerateUIImage("Settings Bar", browserRT);

                    UIManager.SetRectTransform(localSettingsBar.GetObject<RectTransform>(), new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                    localSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);

                    var reload = UIManager.GenerateUIImage("Select", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reload.GetObject<RectTransform>(), new Vector2(-600f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(400f, 64f));

                    var reloadClickable = reload.GetObject<GameObject>().AddComponent<Clickable>();

                    reload.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.03f);

                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(reload.GetObject<Image>(), ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        reload.GetObject<Image>().sprite = null;

                    var reloadText = UIManager.GenerateUITextMeshPro("Text", reload.GetObject<RectTransform>());
                    UIManager.SetRectTransform(reloadText.GetObject<RectTransform>(), Vector2.zero, ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    reloadText.GetObject<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    reloadText.GetObject<TextMeshProUGUI>().fontSize = 32;
                    reloadText.GetObject<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                    reloadText.GetObject<TextMeshProUGUI>().text = "[USE LOCAL BROWSER]";

                    Settings[2].Add(new Tab
                    {
                        GameObject = reload.GetObject<GameObject>(),
                        RectTransform = reload.GetObject<RectTransform>(),
                        Clickable = reloadClickable,
                        Image = reload.GetObject<Image>(),
                        Text = reloadText.GetObject<TextMeshProUGUI>(),
                        Position = new Vector2Int(0, 1),
                    });

                    reloadClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    reloadClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        OpenLocalBrowser();
                    };
                }

            }

            // Download
            {
                var download = new GameObject("Download");
                download.transform.SetParent(selectionBaseRT);
                download.transform.localScale = Vector3.one;

                var downloadRT = download.AddComponent<RectTransform>();
                downloadRT.anchoredPosition = Vector3.zero;
                downloadRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(downloadRT);

                var downloadSettingsBar = UIManager.GenerateUIImage("Settings Bar", downloadRT);

                var downloadSettingsBarRT = downloadSettingsBar.GetObject<RectTransform>();
                UIManager.SetRectTransform(downloadSettingsBarRT, new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                downloadSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            // Queue
            {
                var queue = new GameObject("Queue");
                queue.transform.SetParent(selectionBaseRT);
                queue.transform.localScale = Vector3.one;

                var queueRT = queue.AddComponent<RectTransform>();
                queueRT.anchoredPosition = Vector3.zero;
                queueRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(queueRT);

                var queueSettingsBar = UIManager.GenerateUIImage("Settings Bar", queueRT);

                var queueSettingsBarRT = queueSettingsBar.GetObject<RectTransform>();
                UIManager.SetRectTransform(queueSettingsBarRT, new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                queueSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            // Steam
            {
                var steam = new GameObject("Steam");
                steam.transform.SetParent(selectionBaseRT);
                steam.transform.localScale = Vector3.one;

                var steamRT = steam.AddComponent<RectTransform>();
                steamRT.anchoredPosition = Vector3.zero;
                steamRT.sizeDelta = new Vector2(0f, 0f);

                RegularBases.Add(steamRT);

                var steamSettingsBar = UIManager.GenerateUIImage("Settings Bar", steamRT);

                var steamSettingsBarRT = steamSettingsBar.GetObject<RectTransform>();
                UIManager.SetRectTransform(steamSettingsBarRT, new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                steamSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            selected.x = 1;
            SelectTab();

            init = true;

            yield break;
        }

        #endregion

        public void UpdateMiscRoundness()
        {
            if (ArcadePlugin.PageFieldRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(localPageField.image, ArcadePlugin.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
            else
                localPageField.image.sprite = null;

            if (ArcadePlugin.MiscRounded.Value)
                SpriteManager.SetRoundedSprite(localSearchFieldImage, 1, SpriteManager.RoundedSide.Bottom);
            else
                localSearchFieldImage.sprite = null;
        }

        #region Tabs

        Tab GenerateTab()
        {
            var tabBase = UIManager.GenerateUIImage($"Tab {Tabs.Count}", TabContent);
            var tabText = UIManager.GenerateUITextMeshPro("Text", tabBase.GetObject<RectTransform>());

            var tab = new Tab
            {
                GameObject = tabBase.GetObject<GameObject>(),
                RectTransform = tabBase.GetObject<RectTransform>(),
                Text = tabText.GetObject<TextMeshProUGUI>(),
                Image = tabBase.GetObject<Image>(),
                Clickable = tabBase.GetObject<GameObject>().AddComponent<Clickable>()
            };

            Tabs.Add(tab);
            return tab;
        }

        public void SelectTab()
        {
            Debug.Log($"{ArcadePlugin.className}Selected [X: {selected.x} - Y: {selected.y}]");

            if (selected.x == 0)
                SceneManager.inst.LoadScene("Input Select");
            else
            {
                CurrentTab = selected.x - 1;

                int num = 1;
                foreach (var baseItem in RegularBases)
                {
                    baseItem.gameObject.SetActive(selected.x == num);
                    num++;
                }

                switch (selected.x)
                {
                    case 1:
                        {
                            SelectionLimit[1] = 3;

                            StartCoroutine(RefreshLocalLevels());
                            break;
                        }
                    case 2:
                        {
                            SelectionLimit[1] = 2;

                            var count = SelectionLimit.Count;
                            SelectionLimit.RemoveRange(2, count - 2);

                            SelectionLimit.Add(1);

                            break;
                        }
                    case 3:
                        {
                            SelectionLimit[1] = 1;
                            var count = SelectionLimit.Count;
                            SelectionLimit.RemoveRange(2, count - 2);

                            break;
                        }
                }
            }
        }

        public void UpdateTabRoundness()
        {
            foreach (var tab in Tabs)
            {
                if (ArcadePlugin.TabsRoundedness.Value != 0)
                    SpriteManager.SetRoundedSprite(tab.Image, ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                else
                    tab.Image.sprite = null;
            }

            for (int i = 0; i < Settings.Count; i++)
            {
                foreach (var tab in Settings[i])
                {
                    if (ArcadePlugin.TabsRoundedness.Value != 0)
                        SpriteManager.SetRoundedSprite(tab.Image, ArcadePlugin.TabsRoundedness.Value, SpriteManager.RoundedSide.W);
                    else
                        tab.Image.sprite = null;
                }
            }
        }

        #endregion

        #region Local

        public Image localSearchFieldImage;
        public InputField localSearchField;

        string localSearchTerm;
        public string LocalSearchTerm
        {
            get => localSearchTerm;
            set
            {
                localSearchTerm = value;
                selected = new Vector2Int(0, 2);
                if (localPageField.text != "0")
                    localPageField.text = "0";
                else
                    StartCoroutine(RefreshLocalLevels());
            }
        }

        public int LocalPageCount => ILocalLevels.Count() / MaxLevelsPerPage;

        public IEnumerable<Level> ILocalLevels => LevelManager.Levels.Where(level => string.IsNullOrEmpty(LocalSearchTerm)
                        || level.id == LocalSearchTerm
                        || level.metadata.artist.Name.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(LocalSearchTerm.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(LocalSearchTerm.ToLower()));

        public void SetLocalLevelsPage(int page)
        {
            CurrentPage[0] = page;
            StartCoroutine(RefreshLocalLevels());
        }

        public InputField localPageField;

        Vector2 localLevelsAlignment = new Vector2(-640f, 138f);

        bool loadingLocalLevels;
        public IEnumerator RefreshLocalLevels()
        {
            loadingLocalLevels = true;
            LSHelpers.DeleteChildren(RegularContents[0]);
            LSHelpers.DeleteChildren(SelectedContents[0]);
            LocalLevels.Clear();
            int currentPage = CurrentPage[0] + 1;

            int max = currentPage * MaxLevelsPerPage;

            float top = localLevelsAlignment.y;
            float left = localLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(2, count - 2);

            if (LevelManager.Levels.Count > 0)
            {
                RTHelpers.AddEventTriggerParams(localPageField.gameObject, RTHelpers.ScrollDeltaInt(localPageField, max: LocalPageCount));
            }
            else
            {
                RTHelpers.AddEventTriggerParams(localPageField.gameObject);
            }

            int num = 0;
            foreach (var level in ILocalLevels)
            {
                if (level.id != null && level.id != "0" && num >= max - MaxLevelsPerPage && num < max)
                {
                    var gameObject = localLevelPrefab.Duplicate(RegularContents[0]);

                    int column = (num % MaxLevelsPerPage) % 5;
                    int row = (int)((num % MaxLevelsPerPage) / 5);

                    if (currentRow != row)
                    {
                        currentRow = row;
                        SelectionLimit.Add(1);
                    }
                    else
                    {
                        SelectionLimit[row + 2]++;
                    }

                    float x = left + (column * 320f);
                    float y = top - (row * 190f);

                    gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);

                    var clickable = gameObject.GetComponent<Clickable>();
                    clickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        if (!CanSelect)
                            return;

                        AudioManager.inst.PlaySound("LeftRight");
                        selected.x = column;
                        selected.y = row + 2;
                    };
                    clickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        StartCoroutine(SelectLocalLevel(level));
                    };

                    var image = gameObject.GetComponent<Image>();
                    image.color = buttonBGColor;

                    var difficulty = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                    UIManager.SetRectTransform(difficulty.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                    difficulty.color = RTHelpers.GetDifficulty(level.metadata.song.difficulty).color;

                    if (ArcadePlugin.LocalLevelsRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(image, ArcadePlugin.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        image.sprite = null;

                    var title = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                    UIManager.SetRectTransform(title.rectTransform, new Vector2(0f, -60f), ZeroFive, ZeroFive, ZeroFive, new Vector2(280f, 60f));

                    title.fontSize = 20;
                    title.fontStyle = FontStyles.Bold;
                    title.enableWordWrapping = true;
                    title.overflowMode = TextOverflowModes.Truncate;
                    title.color = textColor;
                    title.text = $"{level.metadata.artist.Name} - {level.metadata.song.title}";

                    var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                    iconBase.rectTransform.anchoredPosition = new Vector2(-90f, 30f);

                    if (ArcadePlugin.LocalLevelsIconRoundness.Value != 0)
                        SpriteManager.SetRoundedSprite(iconBase, ArcadePlugin.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                    else
                        iconBase.sprite = null;

                    var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                    icon.rectTransform.anchoredPosition = Vector2.zero;

                    icon.sprite = level.icon ?? SteamWorkshop.inst.defaultSteamImageSprite;

                    var rank = gameObject.transform.Find("Rank").GetComponent<TextMeshProUGUI>();

                    UIManager.SetRectTransform(rank.rectTransform, new Vector2(90f, 30f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    rank.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                    var levelRank = LevelManager.GetLevelRank(level);
                    rank.fontSize = 64;
                    rank.text = $"<color=#{RTHelpers.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

                    var rankShadow = gameObject.transform.Find("Rank Shadow").GetComponent<TextMeshProUGUI>();

                    UIManager.SetRectTransform(rankShadow.rectTransform, new Vector2(87f, 28f), ZeroFive, ZeroFive, ZeroFive, Vector2.zero);
                    rankShadow.transform.localRotation = Quaternion.Euler(0f, 0f, 356f);

                    rankShadow.fontSize = 68;
                    rankShadow.text = $"<color=#00000035><b>{levelRank.name}</b></color>";

                    var shineController = gameObject.transform.Find("Shine").GetComponent<ShineController>();

                    shineController.maxDelay = 1f;
                    shineController.minDelay = 0.2f;
                    shineController.offset = 260f;
                    shineController.offsetOverShoot = 32f;
                    shineController.speed = 0.7f;

                    LocalLevels.Add(new LocalLevelButton
                    {
                        Position = new Vector2Int(column, row),
                        GameObject = gameObject,
                        Clickable = clickable,
                        RectTransform = gameObject.transform.AsRT(),
                        BaseImage = image,
                        DifficultyImage = difficulty,
                        Title = title,
                        BaseIcon = iconBase,
                        Icon = icon,
                        Level = level,
                        ShineController = shineController,
                        shine1 = gameObject.transform.Find("Shine").GetComponent<Image>(),
                        shine2 = gameObject.transform.Find("Shine/Image").GetComponent<Image>(),
                        Rank = rank,
                    });
                }

                num++;
            }

            loadingLocalLevels = false;
            yield break;
        }

        public void UpdateLocalLevelsRoundness()
        {
            foreach (var level in LocalLevels)
            {
                if (ArcadePlugin.LocalLevelsRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(level.BaseImage, ArcadePlugin.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    level.BaseImage.sprite = null;

                if (ArcadePlugin.LocalLevelsIconRoundness.Value != 0)
                    SpriteManager.SetRoundedSprite(level.BaseIcon, ArcadePlugin.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                else
                    level.BaseIcon.sprite = null;
            }
        }

        public IEnumerator SelectLocalLevel(Level level)
        {
            if (!level.music)
            {
                yield return StartCoroutine(level.LoadAudioClipRoutine(delegate ()
                {
                    AudioManager.inst.StopMusic();
                    PlayLevelMenuManager.inst?.OpenLevel(level);
                    AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
                    AudioManager.inst.SetPitch(RTHelpers.getPitch());
                }));
            }
            else
            {
                AudioManager.inst.StopMusic();
                PlayLevelMenuManager.inst?.OpenLevel(level);
                AudioManager.inst.PlayMusic(level.metadata.song.title, level.music);
                AudioManager.inst.SetPitch(RTHelpers.getPitch());
            }

            yield break;
        }

        #endregion

        #region Online

        public Image onlineSearchFieldImage;
        public InputField onlineSearchField;

        string onlineSearchTerm;
        public string OnlineSearchTerm
        {
            get => onlineSearchTerm;
            set => onlineSearchTerm = value;
        }

        public int OnlineLevelCount { get; set; }

        public void SetOnlineLevelsPage(int page)
        {
            CurrentPage[1] = page;
            StartCoroutine(SearchOnlineLevels());
        }

        public InputField onlinePageField;

        Vector2 onlineLevelsAlignment = new Vector2(-640f, 138f);

        bool loadingOnlineLevels;
        public IEnumerator SearchOnlineLevels()
        {
            var page = CurrentPage[1];
            int currentPage = CurrentPage[1] + 1;

            var search = OnlineSearchTerm;

            string query = string.IsNullOrEmpty(search) && page == 0 ? SearchURL : string.IsNullOrEmpty(search) && page != 0 ? $"{SearchURL}?page={page}" : !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?q={ReplaceSpace(search)}" : !string.IsNullOrEmpty(search) ? $"{SearchURL}?q={ReplaceSpace(search)}&page={page}" : "";

            Debug.Log($"{ArcadePlugin.className}Search query: {query}");

            loadingOnlineLevels = true;
            LSHelpers.DeleteChildren(RegularContents[1]);
            LSHelpers.DeleteChildren(SelectedContents[1]);
            OnlineLevels.Clear();

            if (string.IsNullOrEmpty(query))
            {
                loadingOnlineLevels = false;

                yield break;
            }

            int max = currentPage * MaxLevelsPerPage;

            float top = onlineLevelsAlignment.y;
            float left = onlineLevelsAlignment.x;

            int currentRow = -1;

            var count = SelectionLimit.Count;
            SelectionLimit.RemoveRange(3, count - 3);

            yield return StartCoroutine(AlephNetworkManager.DownloadJSONFile(query, delegate (string j)
            {
                try
                {
                    var jn = JSON.Parse(j);

                    if (jn["items"] != null)
                    {
                        for (int i = 0; i < jn["items"].Count; i++)
                        {
                            var item = jn["items"][i];

                            string id = item["id"];

                            string artist = item["artist"];
                            string title = item["title"];
                            string creator = item["creator"];
                            string description = item["description"];
                            var difficulty = item["difficulty"].AsInt;

                            if (id != null && id != "0")
                            {
                                var gameObject = localLevelPrefab.Duplicate(RegularContents[1]);

                                int column = (i % MaxLevelsPerPage) % 5;
                                int row = (int)((i % MaxLevelsPerPage) / 5);

                                if (currentRow != row)
                                {
                                    currentRow = row;
                                    SelectionLimit.Add(1);
                                }
                                else
                                {
                                    SelectionLimit[row + 3]++;
                                }

                                float x = left + (column * 320f);
                                float y = top - (row * 190f);

                                gameObject.transform.AsRT().anchoredPosition = new Vector2(x, y);

                                var clickable = gameObject.GetComponent<Clickable>();

                                var image = gameObject.GetComponent<Image>();
                                image.color = buttonBGColor;

                                var difficultyImage = gameObject.transform.Find("Difficulty").GetComponent<Image>();
                                UIManager.SetRectTransform(difficultyImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 0f));
                                difficultyImage.color = RTHelpers.GetDifficulty(difficulty).color;

                                if (ArcadePlugin.LocalLevelsRoundness.Value != 0)
                                    SpriteManager.SetRoundedSprite(image, ArcadePlugin.LocalLevelsRoundness.Value, SpriteManager.RoundedSide.W);
                                else
                                    image.sprite = null;

                                var titleText = gameObject.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                                UIManager.SetRectTransform(titleText.rectTransform, new Vector2(0f, -60f), ZeroFive, ZeroFive, ZeroFive, new Vector2(280f, 60f));

                                titleText.fontSize = 20;
                                titleText.fontStyle = FontStyles.Bold;
                                titleText.enableWordWrapping = true;
                                titleText.overflowMode = TextOverflowModes.Truncate;
                                titleText.color = textColor;
                                titleText.text = $"{artist} - {title}";

                                var iconBase = gameObject.transform.Find("Icon Base").GetComponent<Image>();
                                iconBase.rectTransform.anchoredPosition = new Vector2(-90f, 30f);

                                if (ArcadePlugin.LocalLevelsIconRoundness.Value != 0)
                                    SpriteManager.SetRoundedSprite(iconBase, ArcadePlugin.LocalLevelsIconRoundness.Value, SpriteManager.RoundedSide.W);
                                else
                                    iconBase.sprite = null;

                                var icon = gameObject.transform.Find("Icon Base/Icon").GetComponent<Image>();
                                icon.rectTransform.anchoredPosition = Vector2.zero;

                                icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;

                                Destroy(gameObject.transform.Find("Rank").gameObject);
                                Destroy(gameObject.transform.Find("Rank Shadow").gameObject);
                                Destroy(gameObject.transform.Find("Shine").gameObject);

                                int num = -1;

                                int.TryParse(id, out num);

                                if (!OnlineLevelIcons.ContainsKey(id) && num >= 0)
                                {
                                    StartCoroutine(AlephNetworkManager.DownloadBytes($"{CoverURL}{num}", delegate (byte[] bytes)
                                    {
                                        var sprite = SpriteManager.LoadSprite(bytes);
                                        OnlineLevelIcons.Add(id, sprite);
                                        icon.sprite = sprite;
                                    }, delegate (string onError)
                                    {
                                        OnlineLevelIcons.Add(id, SteamWorkshop.inst.defaultSteamImageSprite);
                                        icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    }));
                                }
                                else if (OnlineLevelIcons.ContainsKey(id))
                                {
                                    icon.sprite = OnlineLevelIcons[id];
                                }
                                else
                                {
                                    OnlineLevelIcons.Add(id, SteamWorkshop.inst.defaultSteamImageSprite);
                                    icon.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                }

                                var level = new OnlineLevelButton
                                {
                                    ID = id,
                                    Artist = artist,
                                    Creator = creator,
                                    Description = description,
                                    Difficulty = difficulty,
                                    Title = title,
                                    Position = new Vector2Int(column, row),
                                    GameObject = gameObject,
                                    Clickable = clickable,
                                    RectTransform = gameObject.transform.AsRT(),
                                    BaseImage = image,
                                    DifficultyImage = difficultyImage,
                                    TitleText = titleText,
                                    BaseIcon = iconBase,
                                    Icon = icon,
                                };

                                clickable.onEnter = delegate (PointerEventData pointerEventData)
                                {
                                    if (!CanSelect)
                                        return;

                                    AudioManager.inst.PlaySound("LeftRight");
                                    selected.x = column;
                                    selected.y = row + 3;
                                };
                                clickable.onClick = delegate (PointerEventData pointerEventData)
                                {
                                    AudioManager.inst.PlaySound("blip");
                                    SelectOnlineLevel(level);
                                };

                                OnlineLevels.Add(level);
                            }
                        }
                    }

                    if (jn["count"] != null)
                    {
                        OnlineLevelCount = jn["count"].AsInt;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{ArcadePlugin.className}{ex}");
                }
            }));

            if (OnlineLevels.Count > 0)
            {
                RTHelpers.AddEventTriggerParams(onlinePageField.gameObject, RTHelpers.ScrollDeltaInt(onlinePageField, max: OnlineLevelCount));
            }
            else
            {
                RTHelpers.AddEventTriggerParams(onlinePageField.gameObject);
            }

            loadingOnlineLevels = false;
        }

        public List<OnlineLevelButton> OnlineLevels { get; set; } = new List<OnlineLevelButton>();

        public Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        string ReplaceSpace(string search) => search.ToLower().Replace(" ", "+");

        public void SelectOnlineLevel(OnlineLevelButton onlineLevel)
        {
            DownloadLevelMenuManager.inst?.OpenLevel(onlineLevel);
        }

        #endregion

        #region Browser

        public void OpenLocalBrowser()
        {
            string text = FileBrowser.OpenSingleFile("Select a level to play!", RTFile.ApplicationDirectory, "lsb", "vgd");
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("\\", "/");

                if (!text.Contains("/level.lsb") && !text.Contains("/level.vgd"))
                {
                    Debug.LogError($"{ArcadePlugin.className}Please select an actual level{(text.Contains("/metadata.lsb") ? " and not the metadata!" : ".")}");
                    return;
                }

                var path = text.Replace("/level.lsb", "").Replace("/level.vgd", "");

                if (!RTFile.FileExists($"{path}/metadata.lsb") && !RTFile.FileExists($"{path}/metadata.vgm"))
                {
                    Debug.LogError($"{ArcadePlugin.className}No metadata!");
                    return;
                }

                if (!RTFile.FileExists($"{path}/level.ogg") && !RTFile.FileExists($"{path}/level.wav") && !RTFile.FileExists($"{path}/level.mp3")
                 && !RTFile.FileExists($"{path}/audio.ogg") && !RTFile.FileExists($"{path}/audio.wav") && !RTFile.FileExists($"{path}/audio.mp3"))
                {
                    Debug.LogError($"{ArcadePlugin.className}No song!");
                    return;
                }

                MetaData metadata = RTFile.FileExists($"{path}/metadata.vgm") ? MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.vgm"))) : MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{path}/metadata.lsb")));

                if ((string.IsNullOrEmpty(metadata.serverID) || metadata.serverID == "-1")
                    && (string.IsNullOrEmpty(metadata.LevelBeatmap.beatmap_id) && metadata.LevelBeatmap.beatmap_id == "-1" || metadata.LevelBeatmap.beatmap_id == "0")
                    && (string.IsNullOrEmpty(metadata.arcadeID) || metadata.arcadeID == "-1" || metadata.arcadeID == "0"))
                {
                    metadata.arcadeID = LSText.randomNumString(16);
                    var metadataJN = metadata.ToJSON();
                    RTFile.WriteToFile($"{path}/metadata.lsb", metadataJN.ToString(3));
                }

                var level = new Level(path + "/");

                StartCoroutine(SelectLocalLevel(level));
            }
        }

        #endregion

        public class OnlineLevelButton
        {
            public OnlineLevelButton()
            {

            }

            public string ID { get; set; } = string.Empty;

            public string Artist { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Creator { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;
            public int Difficulty { get; set; }

            public Vector2Int Position { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI TitleText { get; set; }
            public Image BaseImage { get; set; }
            public Image BaseIcon { get; set; }
            public Image Icon { get; set; }
            public Image DifficultyImage { get; set; }

            public Clickable Clickable { get; set; }

            public AnimationManager.Animation EnterAnimation { get; set; }
            public AnimationManager.Animation ExitAnimation { get; set; }

            public bool selected;

            public IEnumerator DownloadLevel()
            {
                var directory = $"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{ID}";

                if (LevelManager.Levels.Has(x => x.id == ID) || RTFile.DirectoryExists(directory))
                {
                    Debug.LogError($"{ArcadePlugin.className}Level already exists! No update system in place yet.");

                    yield break;
                }

                yield return inst.StartCoroutine(AlephNetworkManager.DownloadBytes($"{DownloadURL}{ID}", delegate (byte[] bytes)
                {
                    Directory.CreateDirectory(directory);

                    File.WriteAllBytes($"{directory}.zip", bytes);

                    ZipFile.ExtractToDirectory($"{directory}.zip", $"{directory}");

                    File.Delete($"{directory}.zip");

                    MetaData metadata = RTFile.FileExists($"{directory}/metadata.vgm") ? MetaData.ParseVG(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.vgm"))) : MetaData.Parse(JSON.Parse(RTFile.ReadFromFile($"{directory}/metadata.lsb")));

                    var level = new Level(directory + "/");

                    LevelManager.Levels.Add(level);

                    if (inst.CurrentTab == 0)
                    {
                        inst.StartCoroutine(inst.RefreshLocalLevels());
                    }
                    else if (inst.OpenedOnlineLevel)
                    {
                        DownloadLevelMenuManager.inst.Close(delegate ()
                        {
                            if (ArcadePlugin.OpenOnlineLevelAfterDownload.Value)
                            {
                                inst.StartCoroutine(inst.SelectLocalLevel(level));
                            }
                        });
                    }
                }));

                yield break;
            }
        }

        public class LocalLevelButton
        {
            public LocalLevelButton()
            {

            }

            public Vector2Int Position { get; set; }

            public Level Level { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Title { get; set; }
            public TextMeshProUGUI Rank { get; set; }
            public Image BaseImage { get; set; }
            public Image BaseIcon { get; set; }
            public Image Icon { get; set; }
            public Image DifficultyImage { get; set; }

            public Clickable Clickable { get; set; }

            public ShineController ShineController { get; set; }
            public Image shine1;
            public Image shine2;

            public AnimationManager.Animation EnterAnimation { get; set; }
            public AnimationManager.Animation ExitAnimation { get; set; }

            public bool selected;
        }

        public class Tab
        {
            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Text { get; set; }
            public Image Image { get; set; }
            public Clickable Clickable { get; set; }

            public Vector2Int Position { get; set; }
        }
    }

    public static class ArcadeExtension
    {
        static readonly Dictionary<string, string> namespaceHelpers = new Dictionary<string, string>
        {
            { "UnityEngine.GameObject", "GameObject" },
            { "UnityEngine.RectTransform", "RectTransform" },
            { "UnityEngine.UI.Image", "Image" },
            { "UnityEngine.UI.Text", "Text" },
            { "UnityEngine.UI.Button", "Button" },
            { "UnityEngine.UI.Toggle", "Toggle" },
            { "UnityEngine.UI.InputField", "InputField" },
            { "UnityEngine.UI.Dropdown", "Dropdown" },
            { "TMPro.TextMeshProUGUI", "Text" },
        };

        public static T GetObject<T>(this Dictionary<string, object> dictionary)
            => namespaceHelpers.ContainsKey(typeof(T).ToString()) && dictionary.ContainsKey(namespaceHelpers[typeof(T).ToString()]) ?
            (T)dictionary[namespaceHelpers[typeof(T).ToString()]] : default;
    }
}

#pragma warning restore CS0618 // Type or member is obsolete