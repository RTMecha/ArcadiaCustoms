using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ArcadiaCustoms
{
    public class ButtonHover : MonoBehaviour, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public Color colorSelected = ArcadeManager.inst.ic.interfaceSettings.borderHighlightColor;
        public Color colorDeselected = ArcadeManager.inst.ic.interfaceSettings.borderColor;
        public string branch;
        public string data;
        public bool butt;
        public GameObject element;
        public string link;
        public bool hovered;

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            Select();
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            Deselect();
        }

        public void Select()
        {
            hovered = true;
            ArcadeManager.inst.ic.currHoveredButton = gameObject;
            transform.DOScale(new Vector3(1.1f, 1.1f, 1f), 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
            transform.Find("bg").GetComponent<Image>().DOColor(colorSelected, 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
            Debug.Log(ArcadeManager.inst.ic.currHoveredButton);
        }

        public void Deselect()
        {
            hovered = false;
            transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
            transform.Find("bg").GetComponent<Image>().DOColor(colorDeselected, 0.3f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
            Debug.Log(ArcadeManager.inst.ic.currHoveredButton);
        }

        public void Activate()
        {
            if (!butt)
            {
                if (ArcadeManager.inst.ic.screenDone && !ArcadeManager.inst.ic.screenGlitch)
                {
                    var handleEvent = AccessTools.Method(typeof(InterfaceController), "handleEvent");
                    if (!string.IsNullOrEmpty(data))
                    {
                        Debug.Log(handleEvent);
                        ArcadeManager.inst.ic.StartCoroutine((IEnumerator)handleEvent.Invoke(ArcadeManager.inst.ic, new object[] { branch, data, false }));
                    }
                    else
                    {
                        Debug.LogError("Handle Event Error" + data);
                    }
                }
            }
            else
            {
                if (!(link != "branch_name") || !ArcadeManager.inst.ic.screenDone || ArcadeManager.inst.ic.screenGlitch || !ArcadeManager.inst.ic.buttonsActive)
                {
                    AudioManager.inst.PlaySound("Block");
                    return;
                }
                if (!string.IsNullOrEmpty(link) && link != " ")
                {
                    foreach (object obj in element.transform)
                    {
                        Transform transform = (Transform)obj;
                        transform.GetComponent<Button>().interactable = false;
                        EventSystem.current.SetSelectedGameObject(null);
                        Destroy(transform.GetComponent<EventTrigger>());
                    }
                    AudioManager.inst.PlaySound("blip");
                    ArcadeManager.inst.ic.SwitchBranch(link);
                    return;
                }
                AudioManager.inst.PlaySound("Block");
            }
        }

        private void Update()
        {
            if (ArcadeManager.inst.ic.currHoveredButton != gameObject && hovered == true)
            {
                Deselect();
            }
            if (ArcadeManager.inst.ic.currHoveredButton == gameObject && hovered == true && Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                //Activate();
            }
        }

        public void OnPointerDown(PointerEventData pointerEventData)
        {
            Activate();
        }
    }
}
