using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DemoMPTK
{
    public class InputInt : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
      IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler,
      IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InputField Input;
        public Text Label;
        public string Caption;
        public int Init;
        public int Min;
        public int Max;

        public float SensibilityDrag = 0.5f;

        private int val;
        private Vector2 posStartDrag;
        private int valStartDrag;
        private bool disablePointerUp;

        public int Value
        {
            get { return val; }
            set
            {
                val = value;
                SetValue();
            }
        }

        /// <summary>@brief
        /// Define unity event to trigger at end
        /// </summary>
        //[HideInInspector]
        public EventIntSlider OnEventValue;

        // Use this for initialization
        void Start()
        {
            Label.text = Caption;
            val = Init;
            SetValue();
            Input.onEndEdit.AddListener((string inp) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(Input.text))
                        Input.text = Min.ToString();

                    SetValue(Convert.ToInt32(Input.text));
                }
                catch (Exception)
                {
                    Debug.LogWarning("Channel incorrect, use Channel 0 by default");
                }
            });
        }

        private void SetValue()
        {
            if (val < Min) val = Min;
            if (val > Max) val = Max;

            OnEventValue.Invoke(val);
            Input.text = val.ToString();
        }

        private void SetValue(int newVal)
        {
            if (newVal != val)
            {
                val = newVal;
                SetValue();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Begin: " + eventData.pointerCurrentRaycast.gameObject.name);
            posStartDrag = eventData.position;
            valStartDrag = val;
            disablePointerUp = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float x = eventData.position.x - posStartDrag.x;
            //Debug.Log("Dragging: " + eventData.pointerCurrentRaycast.gameObject.name + " " + x + " " + (int)(x * SensibilityDrag));
            int newVal = valStartDrag + (int)(x * SensibilityDrag);
            SetValue(newVal);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Ended: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("Clicked: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //Debug.Log("Mouse Down: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("Mouse Enter");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("Mouse Exit");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //Debug.Log("Mouse Up: " + eventData.pointerCurrentRaycast.gameObject.name);
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                if (!disablePointerUp)
                {
                    string name = eventData.pointerCurrentRaycast.gameObject.name;
                    int newVal = val;

                    //if (name == Minus.name)
                    //    newVal--;
                    //else if (name == Plus.name)
                    //    newVal++;

                    SetValue(newVal);
                }
                disablePointerUp = false;
            }
        }
    }



    [System.Serializable]
    public class EventIntSlider : UnityEvent<int>
    {
    }
}