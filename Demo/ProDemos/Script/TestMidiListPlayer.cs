using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestMidiListPlayer : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to play a Midi list. This PreFab must be present in your scene.
        /// </summary>
        public MidiListPlayer midiListPlayer;

        public Toggle IsDisplayFulllLog;

        private void Start()
        {
            if (!HelperDemo.CheckSFExists()) return;

            // Checcj if the MidiListPlayer is defined in the inspector
            if (midiListPlayer == null)
            {
                // No, by the way, try to find it 
                Debug.Log("No MidiListPlayer defined with the editor inspector, try to find one");
                MidiListPlayer fp = FindObjectOfType<MidiListPlayer>();
                if (fp == null)
                    Debug.LogWarning("Can't find a MidiListPlayer Prefab in the Hierarchy. Add it with the MPTK menu.");
                else
                {
                    midiListPlayer = fp;
                }
            }
            // v2.11.0
            //if (midiListPlayer.OnEventStartPlayMidi == null) midiListPlayer.OnEventStartPlayMidi = new EventStartMidiClass();
            //if (midiListPlayer.OnEventEndPlayMidi == null) midiListPlayer.OnEventEndPlayMidi = new EventEndMidiClass();
            // Event trigger when midi file start playing
            Debug.Log("OnEventStartPlayMidi defined by script");
            midiListPlayer.OnEventStartPlayMidi.AddListener(StartPlay);

            // Event trigger when midi file end playing
            Debug.Log("OnEventEndPlayMidi defined by script");
            midiListPlayer.OnEventEndPlayMidi.AddListener(EndPlay);
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is started (set by Unity Editor in MidiFilePlayer_1 Inspector)
        /// </summary>
        public void StartPlay(string name)
        {
            Debug.Log("Start Play Midi '" + name);
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer_1 when a midi is ended when reach end or stop by MPTK_Stop or Replay with MPTK_Replay
        /// The parameter reason give the origin of the end
        /// </summary>
        public void EndPlay(string name, EventEndMidiEnum reason)
        {
            Debug.LogFormat("End playing midi {0} reason:{1}", name, reason);
        }

        /// <summary>@brief
        /// This method is fired from UI button: See canvas/button.
        /// </summary>
        public void ClearList()
        {
            midiListPlayer.MPTK_Stop();
            midiListPlayer.MPTK_NewList();
        }

        //! [ExampleCreateListMidiListPlayer]

        /// <summary>@brief
        /// This method is fired from UI button: See canvas/button.
        /// </summary>
        public void CreateList()
        {
            midiListPlayer.MPTK_Stop();
            midiListPlayer.MPTK_NewList();
            midiListPlayer.MPTK_OverlayTimeMS = 1000f;
            midiListPlayer.MPTK_AddMidi("Baez Joan - Plaisir D'Amour", 10000, 20000);
            midiListPlayer.MPTK_AddMidi("Bach - Fugue", 25000, 35000);
            midiListPlayer.MPTK_PlayIndex = 0;
        }

        /// <summary>@brief
        /// This method is fired from UI button: See canvas/button.
        /// </summary>
        public void UpdateList()
        {
            midiListPlayer.MPTK_Stop();
            midiListPlayer.MPTK_RemoveMidi("Baez Joan - Plaisir D'Amour");
            midiListPlayer.MPTK_AddMidi("Louis Armstrong - What A Wonderful World", 25000, 40000);
            midiListPlayer.MPTK_PlayIndex = 0;
        }

        //! [ExampleCreateListMidiListPlayer]

        /// <summary>@brief
        /// This method is fired from UI button: See canvas/button.
        /// </summary>
		public void Quit()
        {
            Application.Quit();
        }

        //! [ExampleLogMidiListPlayer]

        private void Update()
        {
            if (IsDisplayFulllLog != null && IsDisplayFulllLog.isOn)
            {
                MidiListPlayer.MidiListPlayerStatus current;

                current = midiListPlayer.MPTK_GetPlaying;
                if (current != null) DisplayLog("Playing", current);

                current = midiListPlayer.MPTK_GetStarting;
                if (current != null) DisplayLog("Starting", current);

                current = midiListPlayer.MPTK_GetEnding;
                if (current != null) DisplayLog("Ending  ", current);
            }
        }

        private void DisplayLog(string from, MidiListPlayer.MidiListPlayerStatus current)
        {
            Debug.Log(
                $"{from} - Name:{current.MPTK_MidiFilePlayer.name} " +
                $"Status:{current.StatusPlayer} " +
                $"EndAt:{current.EndAt} " +
                $"PlayTime:{current.MPTK_MidiFilePlayer.MPTK_PlayTime} " +
                $"MIDI:{current.MPTK_MidiFilePlayer.MPTK_MidiName} " +
                $"PctVolume:{current.PctVolume:F2} " +
                $"Active Voice:{current.MPTK_MidiFilePlayer.MPTK_StatVoiceCountActive} " +
                $"Audio Read MS:{current.MPTK_MidiFilePlayer.StatAudioFilterReadMS:F2} " +
                $"Dsp Load Pct:{current.MPTK_MidiFilePlayer.StatDspLoadPCT:F2} "
                );
        }
        //! [ExampleLogMidiListPlayer]

    }
}