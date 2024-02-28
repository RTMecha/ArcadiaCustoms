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
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;

namespace ArcadiaCustoms.Functions
{
    public class ArcadeMenu : MonoBehaviour
    {
        public static ArcadeMenu inst;

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupScene());
        }

        void Update()
        {

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

            LevelManager.current = 0;
            LevelManager.ArcadeQueue.Clear();

            yield break;
        }
    }
}
