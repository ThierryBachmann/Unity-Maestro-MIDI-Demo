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
        /// <summary>
        /// If NoteToPlay is < 0, the pitch of the note is defined by the X position on the zone (between 48=C4 and 72=C6)
        /// </summary>
        public int NoteToPlay = -1;
        public float LastTime = 0;

        // For all tap components in the UI
        static public List<MPTKEvent> playerEvents = new List<MPTKEvent>();

        void Start()
        {
            Input.simulateMouseWithTouches = true;
            // Need MidiStreamPlayer to play note in real time
            midiFilePlayer = FindFirstObjectByType<MidiFilePlayer>();
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
            //Debug.Log("OnPointerUp" + eventData.pointerCurrentRaycast.gameObject.name);
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
            else
                Debug.LogWarning("No gameObject found");
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

            // NoteToPlay is defined from the UI, see TapOneNoteCx (defined a not value for NoteToPlay)
            // and TapAndDragPlayer (NoteToPlay = -1)
            // ----------------------------------------------------------------------------------------

            // When NoteToPlay < 0, the velocity is defined by the Y position on the control.
            // Otherwise the velocity is set to 100 (velocity must be between 0 and 127).
            int velocity = NoteToPlay < 0 ? 30 + (int)(107f * ry) : 100;

            // When NoteToPlay < 0, the pitch is defined by the X position on the control, see TapAndDragPlayer inspector.
            // Otherwise the pitch is defined from the value set in the UI, see TapOneNoteC4 inspector.
            int pitch = NoteToPlay < 0 ? (int)Mathf.Lerp(48, 72, rx) : NoteToPlay;

            // Avoid to saturate the MIDI synth ...
            if (Time.fixedTime - LastTime > 0.05f)
            {
                LastTime = Time.fixedTime;
                mptkEvent = new MPTKEvent()
                {
                    Channel = 0,
                    Duration = -1,
                    Value = pitch,
                    Velocity = velocity,
                    // Take time as soon as event has been detected
                    Tag = DateTime.UtcNow.Ticks,
                };
                //Debug.Log($"Play note x:{rx} y:{ry} pitch:{pitch} velocity:{velocity}");
                playerEvents.Add(mptkEvent);
                midiFilePlayer.MPTK_PlayDirectEvent(mptkEvent);
            }
        }

        public void StopAll()
        {
            if (playerEvents.Count > 0)
            {
                //Debug.Log($"MPTK_StopDirectEvent count:{playerEvents.Count}");
                foreach (MPTKEvent ev in playerEvents)
                    midiFilePlayer.MPTK_StopDirectEvent(ev);
                playerEvents.Clear();
            }
        }
    }
}