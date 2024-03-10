using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MPTKDemoEuclidean
{
    public class TapPad : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image ImgBackground;
        public Text Label;
        public PanelController panelController;
        public Image Pointer;
        Vector2 sizeParentPointer;

        /// <summary>@brief
        /// Define unity event to trigger at end
        /// </summary>
        //[HideInInspector]
        public EventPad OnEventPadHorizontal;
        public EventPad OnEventPadVertical;

        public void Start()
        {
            ImgBackground = GetComponentInChildren<Image>();
            if (Pointer != null)
                sizeParentPointer = ((RectTransform)Pointer.transform.parent).sizeDelta;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Begin: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Ended: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("Clicked: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            //Debug.Log("OnPointerDown: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("Mouse Enter");
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("Mouse Exit");
        }

        protected bool PointerPosition(PointerEventData eventData, out float rx, out float ry)
        {
            bool ret = false;
            rx = ry = 0f;
            GameObject go = eventData.pointerCurrentRaycast.gameObject;
            if (go != null)
            {
                if (go.tag != "TapPad")
                {
                    go = go.transform.parent.gameObject;
                    //Debug.Log("tag " + go.tag);
                    if (go.tag != "TapPad")
                        return false;
                }

                // Is pointer is above the object ?
                if (go != this.gameObject)
                    return false;
                Vector3[] worldCorners = new Vector3[4];
                Vector3[] screenCorners = new Vector3[2];
                // Each corner provides its world space value.The returned array of 4 vertices is clockwise.
                // It starts bottom left and rotates to top left, then top right, and finally bottom right. Note that bottom left, for example, is an (x, y, z) vector with x being left and y being bottom.
                ((RectTransform)go.transform).GetWorldCorners(worldCorners);
                screenCorners[0] = Camera.main.WorldToScreenPoint(worldCorners[0]);
                screenCorners[1] = Camera.main.WorldToScreenPoint(worldCorners[2]);

                float x = eventData.position.x - screenCorners[0].x;
                float y = eventData.position.y - screenCorners[0].y;
                float goWidth = screenCorners[1].x - screenCorners[0].x;
                float goHeight = screenCorners[1].y - screenCorners[0].y;

                rx = NormalizeAndClamp(goWidth, x);
                ry = NormalizeAndClamp(goHeight, y);

                //Debug.Log($"Mouse Down: {go.name} bottom-left :{Camera.main.WorldToScreenPoint(worldCorners[0])} top-right:{Camera.main.WorldToScreenPoint(worldCorners[2])}  Position:{eventData.position} x:{x} y:{y} sx:{sx} sy:{sy}");
                //Debug.Log($"Mouse Down: {go.name} worldCorners0 :{worldCorners[0]} screenCorners0:{screenCorners[1]} Position:{eventData.position} x:{x} y:{y} sx:{goWidth} sy:{goHeight}");
                //if (x < 20)
                //Debug.Log($"Mouse Down: {go.name} x:{x:F2} y:{y:F2} rx:{rx:F2} ry:{ry:F2}");
                //Vector3 screenPositionGameObj = Camera.main.WorldToScreenPoint(go.transform.position);
                //Debug.Log(
                //    $"Mouse Down: {go.name} position:{eventData.position} screenPosition:{eventData.pointerCurrentRaycast.screenPosition} " +
                //    $"{go.transform.position} gameObjectScreenPosition:{screenPositionGameObj} sizeDelta:{((RectTransform)go.transform).sizeDelta}");

                // Set UI pointer position 
                if (Pointer != null && sizeParentPointer != null)
                {
                    ((RectTransform)Pointer.transform).anchoredPosition = new Vector2(rx * sizeParentPointer.x, ry * sizeParentPointer.y);
                    //Debug.Log($"{((RectTransform)Pointer.transform).anchoredPosition} {((RectTransform)Pointer.transform).offsetMin} {((RectTransform)Pointer.transform).offsetMax}");
                }

                ret = true;

            }
            return ret;
        }

        float NormalizeAndClamp(float max, float val)
        {
            //Debug.Log($"{max} {v}");
            if (val > 0f)
                if (val > max)
                    return 1f;
                else
                    return val / max;
            else
                return 0f;
        }

        [System.Serializable]
        public class EventPad : UnityEvent<float, int>
        {
        }

    }
}
