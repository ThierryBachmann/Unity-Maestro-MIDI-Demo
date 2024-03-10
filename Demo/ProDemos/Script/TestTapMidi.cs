using MidiPlayerTK;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MPTKDemoEuclidean
{
    public class TestTapMidi : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public MidiFilePlayer midiFilePlayer;
        public int NoteToPlay = -1;
        public int LastPitch = 0;
        public int LastVelocity = 0;

        // For all tap components in the UI
        static public List<MPTKEvent> playerEvents = new List<MPTKEvent>();

        void Start()
        {
            Input.simulateMouseWithTouches = true;
            // Need MidiStreamPlayer to play note in real time
            midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiStreamPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Begin: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (NoteToPlay < 0 && PointerPosition(eventData, out float rx, out float ry))
                PlayNote(rx, ry);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            //Debug.Log("Drag Ended: " + eventData.pointerCurrentRaycast.gameObject.name);
            StopAll();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("Clicked: " + eventData.pointerCurrentRaycast.gameObject.name);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            //Debug.Log("OnPointerDown: " + eventData.pointerCurrentRaycast.gameObject.name);
            if (PointerPosition(eventData, out float rx, out float ry))
                PlayNote(rx, ry);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            //Debug.Log("OnPointerUp");
            StopAll();
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


        private void PlayNote(float rx, float ry)
        {
            MPTKEvent mptkEvent;

            int velocity = NoteToPlay < 0 ? 20 + (int)(107f * ry) : 100;
            int pitch = NoteToPlay < 0 ? (int)Mathf.Lerp(50, 72, rx) : NoteToPlay;

            if (LastPitch != pitch && LastVelocity != velocity)
            {
                LastPitch = pitch;
                LastVelocity = velocity;
                mptkEvent = new MPTKEvent()
                {
                    Channel = 0,
                    Duration = -1,
                    Value = pitch,
                    Velocity = velocity,
                    // Take time as soon as event has been detected
                    Tag = DateTime.UtcNow.Ticks,
                };
                Debug.Log($"Play note pitch:{pitch} velocity:{velocity}");
                playerEvents.Add(mptkEvent);
                midiFilePlayer.MPTK_PlayDirectEvent(mptkEvent);
            }
        }

        //private int BuildPitch(float v) { return (int)Mathf.Lerp(50, 72, v); }
        //private int BuildVelocity(float v) { return 20 + (int)(107f * v); }
        public void StopAll()
        {
            if (playerEvents.Count > 0)
            {
                Debug.Log($"MPTK_StopDirectEvent count:{playerEvents.Count}");
                foreach (MPTKEvent ev in playerEvents)
                    midiFilePlayer.MPTK_StopDirectEvent(ev);
                playerEvents.Clear();
                LastPitch = LastVelocity = 0;
            }
            // Clear all remaining sound
            //Debug.Log($"MPTK_ClearAllSound");
            //midiFilePlayer.MPTK_ClearAllSound();
        }
    }
}