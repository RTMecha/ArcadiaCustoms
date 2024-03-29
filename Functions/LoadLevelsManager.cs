﻿using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using InControl;
using UnityEngine.EventSystems;
using TMPro;

using LSFunctions;

using RTFunctions.Functions.Managers;
using RTFunctions.Functions;

namespace ArcadiaCustoms.Functions
{
    public class LoadLevelsManager : MonoBehaviour
    {
        public static LoadLevelsManager inst;
        public static GameObject menuUI;
        public static float screenScale;
        public static float screenScaleInverse;

        public static GameObject textMeshPro;

        public static Image loadImage;
        public static TextMeshProUGUI loadText;
        public static RectTransform loadingBar;

        public static int totalLevelCount;

        public bool cancelled = false;

        public Color textColor;
        public Color highlightColor;
        public Color textHighlightColor;
        public Color buttonBGColor;

        public static Vector2 ZeroFive => new Vector2(0.5f, 0.5f);
        public static Color ShadeColor => new Color(0f, 0f, 0f, 0.3f);

        void Awake()
        {
            inst = this;

            screenScale = (float)Screen.width / 1920f;
            screenScaleInverse = 1f / screenScale;

            StartCoroutine(CreateDialog());
        }

        void Update()
        {
            screenScale = (float)Screen.width / 1920f;
            screenScaleInverse = 1f / screenScale;

            var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            Camera.main.backgroundColor = LSColors.HexToColor(currentTheme["values"]["bg"]);
            textColor = currentTheme["values"]["text"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text"]);
            highlightColor = currentTheme["values"]["highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["highlight"]);
            textHighlightColor = currentTheme["values"]["text-highlight"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["text-highlight"]);
            buttonBGColor = currentTheme["values"]["buttonbg"] == "transparent" ? ShadeColor : LSColors.HexToColor(currentTheme["values"]["buttonbg"]);

            baseImage?.SetColor(buttonBGColor);
            if (loadText)
                loadText.color = textColor;

            if (InputDataManager.inst.menuActions.Cancel.WasPressed && !LSHelpers.IsUsingInputField())
            {
                cancelled = true;
            }
        }

        public Image baseImage;

        public IEnumerator CreateDialog()
        {
            yield return StartCoroutine(DeleteComponents());

            var findButton = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
                              where x.name == "Text Element"
                              select x).ToList();

            textMeshPro = findButton[0].transform.GetChild(1).gameObject;

            var inter = new GameObject("Loading UI");
            inter.transform.localScale = Vector3.one * screenScale;
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
            canvas.scaleFactor = screenScale;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", ArcadePlugin.className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

            inter.AddComponent<GraphicRaycaster>();

            var openFilePopup = LevelMenuManager.GenerateUIImage("Loading Popup", inter.transform);
            var parent = ((GameObject)openFilePopup["GameObject"]).transform;
            parent.localScale = Vector3.one;

            var openFilePopupRT = (RectTransform)openFilePopup["RectTransform"];
            var zeroFive = new Vector2(0.5f, 0.5f);
            LevelMenuManager.SetRectTransform(openFilePopupRT, Vector2.zero, zeroFive, zeroFive, zeroFive, new Vector2(800f, 600f));

            baseImage = ((Image)openFilePopup["Image"]);

            if (ArcadePlugin.LoadingBackRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(baseImage, ArcadePlugin.LoadingBackRoundness.Value, SpriteManager.RoundedSide.W);
            else
                baseImage.sprite = null;

            var iconBase = new GameObject("icon base");
            iconBase.transform.SetParent(parent);
            iconBase.transform.SetAsFirstSibling();
            iconBase.transform.localScale = Vector3.one;
            iconBase.layer = 5;

            var iconBaseRT = iconBase.AddComponent<RectTransform>();
            iconBaseRT.anchoredPosition = new Vector2(0f, 130f);
            iconBaseRT.sizeDelta = new Vector2(256f, 256f);

            iconBase.AddComponent<CanvasRenderer>();
            var iconBaseImage = iconBase.AddComponent<Image>();

            var loadMask = iconBase.AddComponent<Mask>();
            loadMask.showMaskGraphic = false;

            if (ArcadePlugin.LoadingIconRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(iconBaseImage, ArcadePlugin.LoadingIconRoundness.Value, SpriteManager.RoundedSide.W);
            else
                iconBaseImage.sprite = null;

            var icon = new GameObject("icon");
            icon.transform.SetParent(iconBaseRT);
            icon.transform.localScale = Vector3.one;
            icon.layer = 5;

            var iconRT = icon.AddComponent<RectTransform>();
            icon.AddComponent<CanvasRenderer>();
            loadImage = icon.AddComponent<Image>();

            iconRT.anchoredPosition = new Vector2(0f, 0f);
            iconRT.sizeDelta = new Vector2(256f, 256f);

            loadImage.sprite = SteamWorkshop.inst.defaultSteamImageSprite;

            var title = GenerateUITextMeshPro("Title", parent);
            loadText = (TextMeshProUGUI)title["Text"];
            loadText.fontSize = 30;
            loadText.alignment = TextAlignmentOptions.Center;

            LevelMenuManager.SetRectTransform((RectTransform)title["RectTransform"], new Vector2(0f, -40f), Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(32f, 32f));
            ((GameObject)title["GameObject"]).transform.localScale = Vector3.one;

            var loaderBase = LevelMenuManager.GenerateUIImage("LoaderBase", parent);
            LevelMenuManager.SetRectTransform((RectTransform)loaderBase["RectTransform"], new Vector2(-300f, -140f), zeroFive, zeroFive, new Vector2(0f, 0.5f), new Vector2(600f, 32f));
            ((GameObject)loaderBase["GameObject"]).transform.localScale = Vector3.one;
            ((GameObject)loaderBase["GameObject"]).AddComponent<Mask>();

            if (ArcadePlugin.LoadingBarRoundness.Value != 0)
                SpriteManager.SetRoundedSprite(((Image)loaderBase["Image"]), ArcadePlugin.LoadingBarRoundness.Value, SpriteManager.RoundedSide.W);
            else
                ((Image)loaderBase["Image"]).sprite = null;

            var loader = LevelMenuManager.GenerateUIImage("Loader", ((GameObject)loaderBase["GameObject"]).transform);
            loadingBar = (RectTransform)loader["RectTransform"];
            LevelMenuManager.SetRectTransform(loadingBar, new Vector2(-300f, 0f), zeroFive, zeroFive, new Vector2(0f, 0.5f), new Vector2(0f, 32f));

            loadingBar.localScale = Vector3.one;

            ((Image)loader["Image"]).color = new Color(0.14f, 0.14f, 0.14f, 1f);

            StartCoroutine(ArcadePlugin.GetLevelList());

            yield break;
        }

        public static IEnumerator DeleteComponents()
        {
            Destroy(GameObject.Find("Interface"));
            Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
            Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
            Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
            Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());

            if (LevelMenuManager.inst)
            {
                Destroy(GameObject.Find("VideoPlayer"));
                Destroy(GameObject.Find("folder"));
                Destroy(LevelMenuManager.inst.gameObject);
            }
            
            if (ArcadeMenuManager.inst)
            {
                Destroy(ArcadeMenuManager.inst.menuUI);
                Destroy(ArcadeMenuManager.inst.gameObject);
            }

            yield break;
        }

        public void UpdateInfo(Sprite sprite, string status, int num, bool logError = false)
        {
            float e = (float)num / (float)totalLevelCount;

            //Debug.LogFormat("{0}Loading at {1}% - {2} / {3}", ArcadePlugin.className, e * 100, num, totalLevelCount);
            loadingBar.sizeDelta = new Vector2(600f * e, 32f);

            loadImage.sprite = sprite;
            loadText.text = LSText.ClampString(status, 52);

            if (logError)
                Debug.LogError($"{ArcadePlugin.className}{status}");
        }

        public void UpdateInfo(string name, float percentage)
        {
            loadingBar.sizeDelta = new Vector2(600f * percentage, 32f);

            loadText.text = LSText.ClampString(name, 52);
        }

        public static Dictionary<string, object> GenerateUITextMeshPro(string _name, Transform _parent, bool _noFont = false)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = Instantiate(textMeshPro);
            gameObject.name = _name;
            gameObject.transform.SetParent(_parent);

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.GetComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.GetComponent<CanvasRenderer>());
            var text = gameObject.GetComponent<TextMeshProUGUI>();

            if (_noFont)
            {
                var refer = MaterialReferenceManager.instance;
                var dictionary2 = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

                TMP_FontAsset tmpFont;
                if (dictionary2.ToList().Find(x => x.Value.name == "Arial").Value != null)
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Arial").Value;
                }
                else
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Liberation Sans SDF").Value;
                }

                text.font = tmpFont;
                text.fontSize = 20;
            }

            dictionary.Add("Text", text);

            return dictionary;
        }

        public void End()
        {
            Debug.LogFormat("{0}Loading done!", ArcadePlugin.className);
            ArcadePlugin.inst.StartCoroutine(ArcadePlugin.OnLoadingEnd());
            Destroy(menuUI);
            Destroy(gameObject);
        }
    }
}
