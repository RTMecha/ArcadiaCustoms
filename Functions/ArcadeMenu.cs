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
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ArcadiaCustoms.Functions
{
    public class ArcadeMenu : MonoBehaviour
    {
        public static ArcadeMenu inst;

        public GameObject menuUI;

        public Color textColor;
        public Color highlightColor;
        public Color textHighlightColor;
        public Color buttonBGColor;

        public static int MaxLevelsPerPage { get; set; } = 30;

        public static Vector2 ZeroFive => new Vector2(0.5f, 0.5f);
        public static Color ShadeColor => new Color(0f, 0f, 0f, 0.3f);

        public float Scroll { get; set; }
        public Vector2Int Selected { get; set; }
        public bool SelectedTab { get; set; }

        public RectTransform TabContent { get; set; }
        public Transform RegularContent { get; set; }
        public Transform SelectedContent { get; set; }

        public List<Tab> Tabs { get; set; } = new List<Tab>();

        public List<ArcadeLevelButton> Levels = new List<ArcadeLevelButton>();

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupScene());
        }

        void Update()
        {
            UpdateTheme();
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
            inter.AddComponent<SpriteManager>();
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

            var selectionBaseRT = selectionBase.AddComponent<RectTransform>();
            selectionBaseRT.anchoredPosition = Vector2.zero;

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

            for (int i = 0; i < 6; i++)
            {
                var tab = GenerateTab();

                tab.RectTransform.anchoredPosition = new Vector2(-700f + (i * 300), 0f);
                tab.RectTransform.sizeDelta = new Vector2(290f, 100f);
                tab.Text.alignment = TextAlignmentOptions.Center;
                tab.Text.text = tabNames[Mathf.Clamp(i, 0, tabNames.Length - 1)];
                tab.Text.color = textColor;
                tab.Image.color = Color.Lerp(buttonBGColor, Color.white, 0.01f);
            }

            yield break;
        }

        Tab GenerateTab()
        {
            var tabBase = UIManager.GenerateUIImage($"Tab {Tabs.Count}", TabContent);
            var tabText = UIManager.GenerateUITextMeshPro("Text", tabBase.GetObject<RectTransform>());

            var tab = new Tab
            {
                GameObject = tabBase.GetObject<GameObject>(),
                RectTransform = tabBase.GetObject<RectTransform>(),
                Text = (TextMeshProUGUI)tabText["Text"],
                Image = tabBase.GetObject<Image>(),
            };

            Tabs.Add(tab);
            return tab;
        }

        public class ArcadeLevelButton
        {
            public ArcadeLevelButton()
            {

            }

            public Level Level { get; set; }

            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Title { get; set; }
            public Image BaseImage { get; set; }
            public Image DifficultyImage { get; set; }

            public Button Button { get; set; }

            public AnimationManager.Animation Animation { get; set; }
        }

        public class Tab
        {
            public GameObject GameObject { get; set; }
            public RectTransform RectTransform { get; set; }
            public TextMeshProUGUI Text { get; set; }
            public Image Image { get; set; }
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
        };

        public static T GetObject<T>(this Dictionary<string, object> dictionary)
        {
            if (namespaceHelpers.ContainsKey(typeof(T).ToString()) && dictionary.ContainsKey(namespaceHelpers[typeof(T).ToString()]))
            {
                return (T)dictionary[namespaceHelpers[typeof(T).ToString()]];
            }

            return default;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete