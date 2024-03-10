using MidiPlayerTK;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MPTKDemoEuclidean
{
    public class TapInstrument : TapPad
    {

        int lastNote = -1;

        /// <summary>@brief
        /// Play anite when drag
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (PointerPosition(eventData, out float rx, out float ry))
            {
                int note = BuildNote(rx);
                if (lastNote != note)
                {
                    if (!panelController.SwitchSustain.IsOn)
                        panelController.StopAll();
                    lastNote = note;
                    PlayNote(rx, ry);
                }
            }
        }

        /// <summary>@brief
        /// Play a note on a tap
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (PointerPosition(eventData, out float rx, out float ry))
            {
                PlayNote(rx, ry);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!panelController.SwitchSustain.IsOn)
                panelController.StopAll();
        }

        /// <summary>@brief
        /// Play a note from the panel
        /// </summary>
        /// <param name="rx">set the value of the note</param>
        /// <param name="ry">set the velocity</param>
        /// <returns></returns>
        private void PlayNote(float rx, float ry)
        {
            MPTKEvent mptkEvent;
            int velocity = 20 + (int)(107f * ry);

            if (panelController != null && panelController.midiStream != null)
            {
                switch (panelController.PlayMode)
                {
                    case PanelController.Mode.PlayerDrums:
                        mptkEvent = new MPTKEvent()
                        {
                            Channel = panelController.Channel,
                            Duration = -1,
                            Value = panelController.PresetInstrument, // each note plays a different drum
                            Velocity = velocity,
                        };
                        panelController.PlayFromPlayerIntrument(mptkEvent);
                        break;

                    case PanelController.Mode.PlayerInstrument:
                        //if (panelController.midiStream.MPTK_ChannelPresetGetIndex(panelController.Channel) != panelController.PresetInstrument)
                        //    panelController.midiStream.MPTK_ChannelPresetChange(panelController.Channel, panelController.PresetInstrument, 0);
                        if (panelController.midiStream.MPTK_Channels[panelController.Channel].PresetNum != panelController.PresetInstrument)
                            panelController.midiStream.MPTK_Channels[panelController.Channel].PresetNum = panelController.PresetInstrument;
                        mptkEvent = new MPTKEvent()
                        {
                            Channel = panelController.Channel,
                            Duration = -1,
                            Value = BuildNote(rx),
                            Velocity = velocity,
                        };
                        panelController.PlayFromPlayerIntrument(mptkEvent);
                        break;
                }
            }
        }

        private static int BuildNote(float rx)
        {
            return (int)Mathf.Lerp(50, 72, rx);
        }
    }
}
