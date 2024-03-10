using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace MPTKDemoEuclidean
{
    public class TapSwitch : TapPad
    {
        public bool IsOn;
        public bool Locked;
        public Color ColOn;
        public Color ColOff;
        public EventSwitch OnEventSwitchChangeState;
        public EventSwitch OnEventSwitchLockedOn;

        public new void Start()
        {
            ColOn = new Color(0xDB / 255f, 0x64 / 255f, 0x64 / 255f);
            ColOff = new Color(0xD3 /255f, 0xE1 / 255f, 0xCA / 255f);
            base.Start();
            Refresh();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            float rx, ry;
            if (PointerPosition(eventData, out rx, out ry))
            {
                // Lock / Unlock when hit at the 1/3 from the height
                if (ry > 0.66f)
                    Locked = !Locked;

                if (Locked && IsOn)
                    if (OnEventSwitchLockedOn != null) OnEventSwitchLockedOn.Invoke(true);

                if (!IsOn)
                {
                    IsOn = true;
                    if (OnEventSwitchChangeState != null) OnEventSwitchChangeState.Invoke(true);
                }

                Refresh();
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!Locked)
            {
                if (IsOn)
                {
                    IsOn = false;
                    if (OnEventSwitchChangeState != null) OnEventSwitchChangeState.Invoke(false);
                }
                Refresh();
            }
        }

        private void Refresh()
        {
            if (Label != null) Label.text = (Locked ? "Locked" : "") + "\n\nSustain\n\n" + (IsOn ? "On" : "Off");
            if (IsOn)
                ImgBackground.color = ColOn;
            else
                ImgBackground.color = ColOff;

        }
        [System.Serializable]
        public class EventSwitch : UnityEvent<bool>
        {
        }
    }
}
