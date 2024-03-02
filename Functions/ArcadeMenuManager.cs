using System;
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

        public List<ArcadeLevelButton> LocalLevels { get; set; } = new List<ArcadeLevelButton>();

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

        public bool OpenedLevel { get; set; }

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

            if (!init || OpenedLevel)
                return;

            UpdateControls();

            for (int i = 0; i < Tabs.Count; i++)
            {
                Tabs[i].Text.color = selected.y == 0 && i == selected.x ? textHighlightColor : textColor;
                Tabs[i].Image.color = selected.y == 0 && i == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }
            
            for (int i = 0; i < Settings[CurrentTab].Count; i++)
            {
                Settings[CurrentTab][i].Text.color = selected.y == 1 && i == selected.x ? textHighlightColor : textColor;
                Settings[CurrentTab][i].Image.color = selected.y == 1 && i == selected.x ? highlightColor : Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            try
            {
                if (CurrentTab == 0)
                {
                    pageField.caretColor = highlightColor;
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
            }
            catch
            {

            }
        }

        void UpdateControls()
        {
            if (LSHelpers.IsUsingInputField())
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
                    });

                    var pageField = UIManager.GenerateUIInputField("Page", localSettingsBar.GetObject<RectTransform>());
                    UIManager.SetRectTransform(pageField.GetObject<RectTransform>(), new Vector2(650f, 0f), ZeroFive, ZeroFive, ZeroFive, new Vector2(150f, 64f));
                    pageField.GetObject<Image>().color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(Color.Lerp(buttonBGColor, Color.black, 0.2f)));

                    ((Text)pageField["Placeholder"]).alignment = TextAnchor.MiddleCenter;
                    ((Text)pageField["Placeholder"]).text = "Page...";
                    ((Text)pageField["Placeholder"]).color = LSColors.fadeColor(RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor)), 0.2f);
                    this.pageField = pageField.GetObject<InputField>();
                    this.pageField.onValueChanged.ClearAll();
                    this.pageField.textComponent.alignment = TextAnchor.MiddleCenter;
                    this.pageField.textComponent.fontSize = 30;
                    this.pageField.textComponent.color = RTHelpers.InvertColorHue(RTHelpers.InvertColorValue(textColor));
                    this.pageField.text = DataManager.inst.GetSettingInt("CurrentArcadePage", 0).ToString();

                    if (ArcadePlugin.MiscRounded.Value)
                        SpriteManager.SetRoundedSprite(this.pageField.image, 1, SpriteManager.RoundedSide.W);
                    else
                        this.pageField.image.sprite = null;

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
                    });

                    this.pageField.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (int.TryParse(_val, out int p))
                        {
                            p = Mathf.Clamp(p, 0, LevelManager.Levels.Count / MaxLevelsPerPage);
                            SetLocalLevelsPage(p);

                            DataManager.inst.UpdateSettingInt("CurrentArcadePage", p);
                        }
                    });

                    prevPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(0, 1);
                    };
                    prevPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(this.pageField.text, out int p))
                        {
                            if (p > 0)
                            {
                                AudioManager.inst.PlaySound("blip");
                                this.pageField.text = Mathf.Clamp(p - 1, 0, LevelManager.Levels.Count / MaxLevelsPerPage).ToString();
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                        }
                    };

                    nextPageClickable.onEnter = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("LeftRight");
                        selected = new Vector2Int(1, 1);
                    };
                    nextPageClickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        if (int.TryParse(this.pageField.text, out int p))
                        {
                            if (p < LevelManager.Levels.Count / MaxLevelsPerPage)
                            {
                                AudioManager.inst.PlaySound("blip");
                                this.pageField.text = Mathf.Clamp(p + 1, 0, LevelManager.Levels.Count / MaxLevelsPerPage).ToString();
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
                    SearchTerm = _val;
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

                var onlineSettingsBar = UIManager.GenerateUIImage("Settings Bar", onlineRT);

                var onlineSettingsBarRT = onlineSettingsBar.GetObject<RectTransform>();
                UIManager.SetRectTransform(onlineSettingsBarRT, new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                onlineSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
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

                var browserSettingsBar = UIManager.GenerateUIImage("Settings Bar", browserRT);

                var browserSettingsBarRT = browserSettingsBar.GetObject<RectTransform>();
                UIManager.SetRectTransform(browserSettingsBarRT, new Vector2(0f, 360f), ZeroFive, ZeroFive, ZeroFive, new Vector2(1920f, 120f));

                browserSettingsBar.GetObject<Image>().color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
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

        public Image localSearchFieldImage;
        public InputField localSearchField;

        public void UpdateMiscRoundness()
        {
            if (ArcadePlugin.PageFieldRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(pageField.image, ArcadePlugin.PageFieldRoundness.Value, SpriteManager.RoundedSide.W);
            else
                pageField.image.sprite = null;

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
                            SelectionLimit[1] = 2;

                            StartCoroutine(RefreshLocalLevels());
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

        string searchTerm;
        public string SearchTerm
        {
            get => searchTerm;
            set
            {
                searchTerm = value;
                if (pageField.text != "0")
                    pageField.text = "0";
                else
                    StartCoroutine(RefreshLocalLevels());
            }
        }

        public void SetLocalLevelsPage(int page)
        {
            CurrentPage[0] = page;
            StartCoroutine(RefreshLocalLevels());
        }

        InputField pageField;

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
                RTHelpers.AddEventTriggerParams(pageField.gameObject, RTHelpers.ScrollDeltaInt(pageField, max: LevelManager.Levels.Count / MaxLevelsPerPage));
            }

            int num = 0;
            foreach (var level in LevelManager.Levels)
            {
                if (!(string.IsNullOrEmpty(SearchTerm)
                        || level.id == SearchTerm
                        || level.metadata.artist.Name.ToLower().Contains(SearchTerm.ToLower())
                        || level.metadata.creator.steam_name.ToLower().Contains(SearchTerm.ToLower())
                        || level.metadata.song.title.ToLower().Contains(SearchTerm.ToLower())
                        || level.metadata.song.getDifficulty().ToLower().Contains(SearchTerm.ToLower())))
                    continue;

                if (level.id != null && num >= max - MaxLevelsPerPage && num < max)
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
                        AudioManager.inst.PlaySound("LeftRight");
                        selected.x = column;
                        selected.y = row + 2;
                    };
                    clickable.onClick = delegate (PointerEventData pointerEventData)
                    {
                        AudioManager.inst.PlaySound("blip");
                        StartCoroutine(SelectLevel(level));
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

                    LocalLevels.Add(new ArcadeLevelButton
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

        public IEnumerator SelectLevel(Level level)
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

        public class ArcadeLevelButton
        {
            public ArcadeLevelButton()
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