using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MPTKDemoEuclidean
{
    public class LightSlider : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string Caption;
        public Text Label;
        public Slider lightSlider;
        public int Val;
        public int Min;
        public int Max;
        public float SensibilityDrag = 0.5f;

        //Slider SliderValue;
        //InputField InputValue;
        public int Value
        {
            get { return Val; }
            set
            {
                Val = value;
                SetValue();
            }
        }

        /// <summary>@brief
        /// Define unity event to trigger at end
        /// </summary>
        //[HideInInspector]
        public EventTextSlider OnEventValue;

        // Use this for initialization
        void Start()
        {
            lightSlider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
            lightSlider.minValue = Min;
            lightSlider.maxValue = Max;
            lightSlider.value = Val;
            SetValue();
        }

        // Invoked when the value of the slider changes.
        public void ValueChangeCheck()
        {
            int newval= (int)lightSlider.value;
            if (newval != Val)
            {
                //Debug.Log(lightSlider.value);
                Val = newval;
                SetValue();
            }
        }

        public void SetRange(int min, int max)
        {
            Min = min;
            if (Val < Min)
            {
                Val = Min;
                SetValue();
            }

            Max = max;
            if (Val > Max)
            {
                Val = Max;
                SetValue();
            }
        }

        private void SetValue()
        {
            if (Val < Min) Val = Min;
            if (Val > Max) Val = Max;

            lightSlider.value = Val;
            OnEventValue.Invoke(Val);
            Label.text = Caption + " " + Val.ToString();
        }

        private void SetValue(int newVal)
        {
            if (newVal != Val)
            {
                Val = newVal;
                SetValue();
            }
        }

        public Vector2 posStartDrag;
        public int valStartDrag;
        //bool disablePointerUp;

        public void OnBeginDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Begin: " + eventData.pointerCurrentRaycast.gameObject.name);
            posStartDrag = eventData.position;
            valStartDrag = Val;
            //disablePointerUp = true;
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
            //if (eventData.pointerCurrentRaycast.gameObject != null)
            //{
            //    if (!disablePointerUp)
            //    {
            //        string name = eventData.pointerCurrentRaycast.gameObject.name;
            //        int newVal = Val;

            //        if (name == Minus.name)
            //            newVal--;
            //        else if (name == Plus.name)
            //            newVal++;

            //        SetValue(newVal);
            //    }
            //    disablePointerUp = false;
            //}
        }
    }
}
