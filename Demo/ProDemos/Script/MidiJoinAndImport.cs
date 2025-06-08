using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemoMPTKImport
{
    public class MidiJoinAndImport : MonoBehaviour
    {
        public Text TextMidiTime;
        public Text TextInfoMidi;
        public Slider SliderPosition;
        public InputField InputPosition;
        public Text TextPosition;
        public Toggle MidiPause;

        private PopupListItem PopMidi;
        
        // We need two MidiFilePLayer, one for loading the MIDI for merge source, one for the player of the MIDI merged
        private MidiFilePlayer mfLoader;
        private MidiFilePlayer mfPlayer;
        
        // and a MidiFileWriter to execute merge operation
        private MPTKWriter mfWriter;

        private int indexSelectedMidi;
        private CustomStyle myStyle;

        void Start()
        {
            MidiFilePlayer[] midiFilePlayer = FindObjectsByType<MidiFilePlayer>(sortMode: FindObjectsSortMode.InstanceID);
            if (midiFilePlayer.Length < 2)
            {
                Debug.LogError("This demo needs two MidiFilePlayer prefabs in the hierarchy, one for the loader, one for the player ");
                return;
            }
            mfLoader = midiFilePlayer[0];

            mfPlayer = midiFilePlayer[1];
            mfPlayer.OnEventNotesMidi.AddListener((List<MPTKEvent> events) =>
            {
                /// Called for each MIDI event (or group of MIDI events) ready to be played by the MIDI synth. 
                /// In general, all events in the list are on the same MIDI tick.
                UpdatePosition();
            });

            SliderPosition.onValueChanged.AddListener((float pos) =>
            {
                if (mfPlayer.MPTK_IsPlaying)
                    mfPlayer.MPTK_Position = (int)pos;
            });

            PopMidi = new PopupListItem()
            {
                Title = "Popup MIDI",
                OnSelect = CallbackPopupSelectMidi,
                Tag = "",
                KeepOpen = false,
                ColCount = 2,
                ColWidth = 250,
            };

            MidiPause.onValueChanged.AddListener((bool pause) =>
            {
                if (pause)
                    mfPlayer.MPTK_Pause();
                else
                    mfPlayer.MPTK_UnPause();
            });
        }

        /// <summary>
        /// Call when a MIDI is selected in the popup.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <param name="indexList"></param>
        private void CallbackPopupSelectMidi(object tag, int index, int indexList)
        {
            //Debug.Log("CallbackPopup " + index + " for " + tag);
            indexSelectedMidi = index;
            mfLoader.MPTK_MidiIndex = indexSelectedMidi;
            mfLoader.MPTK_Load();
            TextInfoMidi.text = $"MIDI Selected {mfLoader.MPTK_MidiIndex} - '{mfLoader.MPTK_MidiName}'\nCount Event={mfLoader.MPTK_MidiEvents.Count}    MPTK_DeltaTicksPerQuarterNote={mfLoader.MPTK_DeltaTicksPerQuarterNote}";
        }

        /// <summary>
        /// Display the popup to select a MIDI. Triggered by a button on the UI.
        /// </summary>
        /// <param name="tag"></param>
        public void SelectMIDI(string tag)
        {
            PopMidi.Tag = tag;
            PopMidi.Show = true;
        }
        //! [ExampleMIDIImportAndPlay]

        /// <summary>
        /// Insert the midi selected . Triggered by a button on the UI.
        /// </summary>
        public void InsertMidi()
        {
            if (mfWriter == null)
                Debug.LogWarning("First, create a MidiFileWriter instance");
            else
            {
                // Load the MIDI file selected
                mfLoader.MPTK_MidiIndex = indexSelectedMidi;
                Debug.Log($"MPTK_KeepNoteOff:{mfLoader.MPTK_KeepNoteOff} MPTK_KeepEndTrack:{mfLoader.MPTK_KeepEndTrack} ");
                mfLoader.MPTK_Load();

                // From the UI, get the tick position where to insert
                if (string.IsNullOrWhiteSpace(InputPosition.text)) InputPosition.text = "0";
                long position = Convert.ToInt64(InputPosition.text);
                if (position < 0) { InputPosition.text = "-1"; position = -1; }

                // Insert the MPTK_MidiEvents of the MIDI loaded in the MPTK_MidiEvents of the MIDI Writer. 
                mfWriter.ImportFromEventsList(mfLoader.MPTK_MidiEvents, mfLoader.MPTK_DeltaTicksPerQuarterNote, position: position, name: "MidiJoined", logPerf: true, logDebug: true); ;

                /*  
                    If a MIDI player is playing the MIDI sequence we have to recalculate the duration.
                    The MIDI file player (mfPlayer) share the MIDI event list with the MIDI Writer (mfWriter).
                    Any changes made to the mfWriter.MPTK_MidiEvents will have a direct effect on the Player.
                    But we need to update some properties of the Player to get a correct value of the new duration.
                */

                if (mfPlayer != null && mfPlayer.MPTK_IsPlaying)
                {
                    mfPlayer.MPTK_MidiLoaded.MPTK_EventLastNote = mfPlayer.MPTK_MidiLoaded.MPTK_FindLastNote();
                    mfPlayer.MPTK_MidiLoaded.MPTK_ComputeDuration();
                    Debug.Log($"Last note-on: {(mfPlayer.MPTK_MidiLoaded.MPTK_EventLastNote.ToString() ?? "not found")} Duration: {mfPlayer.MPTK_MidiLoaded.MPTK_DurationMS / 1000f} second");
                }
                Debug.Log($"{mfLoader.MPTK_MidiName} Loaded {mfLoader.MPTK_MidiEvents.Count} events added, total events: {mfWriter.MPTK_MidiEvents.Count}");

                UpdatePosition();
            }
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void PlayMidi()
        {
            if (mfWriter != null && mfWriter.MPTK_MidiEvents != null)
            {
                /*
                    The MIDI file player will play the MIDI event list found in the MIDI Writer.
                    In fact the mfPlayer.MPTK_MidiEvents will directly use the mfWriter.MPTK_MidiEvents to play the MIDI.
                    Any changes made to the mfWriter.MPTK_MidiEvents will have a direct effect on the Player.
                    During playback it's possible to add, modify or insert MIDI events in mfWriter.MPTK_MidiEvents,
                    Changes are automatically taken into account by the player if the change position is after the current tick player (mfPlayer.MPTK_TickPlayer).
                    Insertion before mfPlayer.MPTK_TickPlayer may cause the player to malfunction. 
                */
                mfPlayer.MPTK_Play(mfWriter);
            }
            else
                Debug.LogWarning("No MidiWriter ready for playing");
        }
        //! [ExampleMIDIImportAndPlay]

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void Clear()
        {
            StopMidi();
            if (mfPlayer.MPTK_MidiLoaded != null)
                mfPlayer.MPTK_MidiLoaded.MPTK_Clear();
            mfWriter = new MPTKWriter(deltaTicksPerQuarterNote: 250);
            mfWriter.AddText(0, 0, MPTKMeta.TextEvent, "Init of the sequence");
            Debug.Log("MidiFileWriter2 created");
            //mfWriter.CreateTracksStat();
            mfWriter.LogWriter();
            UpdatePosition();
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void StopMidi()
        {
            mfPlayer.MPTK_Stop();
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void WriteMidiFile()
        {
            if (mfWriter != null && mfWriter.MPTK_MidiEvents != null)
            {
                // Write the MIDI file for using with another player
                string filename = Path.Combine(Application.persistentDataPath, mfWriter.MidiName + ".mid");
                Debug.Log("Write MIDI file:" + filename);
                mfWriter.StableSortEvents();
                mfWriter.WriteToFile(filename);
            }
            else
                Debug.LogWarning("No MidiWriter ready for writing a MIDI file");
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void WriteMidiDB()
        {
            if (mfWriter != null && mfWriter.MPTK_MidiEvents != null)
            {
                // Write the MIDI DB for using with another player
                mfWriter.WriteToMidiDB("@MidiJoin");
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
            else
                Debug.LogWarning("No MidiWriter ready for writing a MIDI file");
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void LogContent()
        {
            if (mfWriter == null)
                Debug.LogWarning("First, create a MidiFileWriter instance");
            else
            {
                mfWriter.CreateTracksStat();
                mfWriter.LogWriter();
            }
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void Quit()
        {
            for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                //Debug.Log(SceneUtility.GetScenePathByBuildIndex(i));
                if (SceneUtility.GetScenePathByBuildIndex(i).Contains("ScenesDemonstration"))
                {
                    SceneManager.LoadScene(i, LoadSceneMode.Single);
                    return;
                }
            }

            Application.Quit();
        }

        /// <summary>
        /// Triggered by a button on the UI.
        /// </summary>
        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }

        /// <summary>
        /// Called for each MIDI event (or group of MIDI events) ready to be played by the MIDI synth. All these events are on same MIDI tick.
        /// </summary>
        public void UpdatePosition()
        {
            if (mfPlayer == null)
                TextPosition.text = "No MIDI Player found, check that a MidiFilePlayer is available into the scene hierarchy";
            else if (mfWriter.MPTK_MidiEvents == null || mfWriter.MPTK_MidiEvents.Count == 0)
            {
                TextPosition.text = $"No MIDI events available in the MidiFileWriter2 - ";
                TextPosition.text += "Click on Play to load the MidiFilePLayer with events";
            }
            else
            {
                if (mfPlayer.MPTK_IsPlaying)
                {
                    int count = mfPlayer.MPTK_MidiEvents.Count;
                    TextPosition.text = $"MIDI Player events: {count} - Time: {mfPlayer.MPTK_Position / 1000d:F2} / {mfPlayer.MPTK_DurationMS / 1000f:F2} second - Ticks:{mfPlayer.MPTK_TickCurrent} / {mfPlayer.MPTK_TickLast}";
                }
                else
                {
                    int count = mfWriter.MPTK_MidiEvents.Count;
                    TextPosition.text = $"Writer - DeltaTicksPerQuarterNote={mfWriter.DeltaTicksPerQuarterNote} -  MIDI events={count}";
                }
            }
            SliderPosition.minValue = 0f;
            SliderPosition.maxValue = mfPlayer.MPTK_DurationMS;
            SliderPosition.SetValueWithoutNotify((float)mfPlayer.MPTK_Position);
        }

        void OnGUI()
        {
            // Set custom Style. Good for background color 3E619800
            if (myStyle == null)
                myStyle = new CustomStyle();

            PopMidi.Draw(MidiPlayerGlobal.MPTK_ListMidi, indexSelectedMidi, myStyle);
        }

        // Update is called once per frame
        void Update()
        {
            if (mfPlayer != null && mfPlayer.MPTK_MidiLoaded != null)
                TextMidiTime.text = $"Real-time MIDI sequencer position: {mfPlayer.MPTK_MidiLoaded.MPTK_TickPlayer}";
        }
    }
}