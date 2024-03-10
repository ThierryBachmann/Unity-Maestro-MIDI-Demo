
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TinyMidiSequencer : MonoBehaviour
    {
        // MPTK component able to play a stream of note like midi note generated
        public MidiStreamPlayer midiStreamPlayer;

        // GUI State of a key on the keyboard
        class KeyboardState
        {
            public int Key; // key number, 60=C5
            public float Time; // noteon time (realtimeSinceStartup)
            public Rect Zone; // GUI zone of the key
            public MPTKEvent Note; // Note currently playing, else null
        }

        /// <summary>@brief
        /// Current time for writing Midi events. Increased in Update(). Paused when no activity.
        /// </summary>
        public float CurrentTimeMs;
        public float MaxNoactivitySec = 3f;

        /// <summary>@brief
        /// Time of the last mouse down or up
        /// </summary>
        public float LastTimeActivity;

        /// <summary>@brief
        /// Increase CurrentTimeMs when true. Set to false if no activity
        /// </summary>
        public bool SeqTimeUpdate = false;

        /// <summary>@brief
        /// This class store the sequence of midi event. No player but the sequence can be writed to a midi file or played with MidiFilePlayer class
        /// </summary>
        MPTKWriter midiFileWriter;

        // Information on key GUI Key
        KeyboardState[] KeysState;

        // Create a popup able to select preset/patch for the sequencer
        PopupListItem PopPatch;
        public int CurrentPatch = 0;
        public bool TestFootPrint;

        int spaceH = 30;

        // Manage skin
        public CustomStyle myStyle;
        Vector2 scrollerWindow = Vector2.zero;
        static private Texture buttonIconFolder;

        private List<MPTKEvent> SequenceNotes;
        private bool waitingNoteOff = false;

        private MPTKFootPrint libFootPrint;
        private string MidiFound="";

        void Awake()
        {
            // Create the popup able to select a patch
            PopPatch = new PopupListItem()
            {
                Title = "Select A Patch",
                OnSelect = PopupPatchChange,
                Tag = "NEWPATCH",
                ColCount = 5,
                ColWidth = 200,
            };

            // State of each key of the keyboard
            KeysState = new KeyboardState[127];
            for (int key = 0; key < KeysState.Length; key++)
                KeysState[key] = new KeyboardState() { Key = key, Note = null };

            libFootPrint = gameObject.AddComponent<MPTKFootPrint>();
        }

        private void Start()
        {
            if (TestFootPrint)
            {
                libFootPrint.Verbose = true;
                libFootPrint.MPTK_AddMultiple();
                libFootPrint.MPTK_AddOne("_simple-48");
                libFootPrint.MPTK_AddOne("_simple-49");
                libFootPrint.MPTK_AddOne("_simple-50");
            }

            // Instanciate MidiFileWriter2 class able to create midi stream for writing
            midiFileWriter = new MPTKWriter();

            // Need MidiStreamPlayer to play note in real time
            midiStreamPlayer = FindObjectOfType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
                Debug.LogWarning("Can't find a MidiStreamPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
            else
                midiStreamPlayer.OnEventSynthStarted.AddListener((string synthName) =>
                {
                    // Init a new sequence when synth is ready
                    NewSequence();
                });


        }
        /// <summary>@brief
        /// Trigger when a patch is selected in the popup
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="patch"></param>
        /// <param name="indexList"></param>
        private void PopupPatchChange(object tag, int patch, int indexList)
        {
            PatchChange(patch);
        }

        void OnGUI()
        {
            if (!HelperDemo.CheckSFExists()) return;

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));

            // Display popup in first to avoid activate other layout behind
            PopPatch.Draw(MidiPlayerGlobal.MPTK_ListPreset, CurrentPatch, myStyle);

            MainMenu.Display("Tiny Sequencer Demonstration", myStyle, "https://paxstellar.fr/class-midifilewriter2/");

            GUILayout.BeginVertical(myStyle.BacgDemosLight);

            // Detect mouse event on the UI keyboard
            CheckKeyboardEvent();

            GUILayout.BeginHorizontal();
            string infoRecording = $"Recording position {CurrentTimeMs / 1000f:F1} second";
            if (!SeqTimeUpdate)
                infoRecording = $"Pause since {(Time.realtimeSinceStartup - LastTimeActivity - MaxNoactivitySec):F1} second";
            GUILayout.Label(infoRecording, myStyle.LabelLeft, GUILayout.Width(500), GUILayout.Height(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            int ambitus = 2;

            // Create the UI keyboard
            for (int key = 48; key < 48 + ambitus * 12 + 1; key++)
            {
                // Create a key white or black
                GUILayout.Button(
                    HelperNoteLabel.LabelFromMidi(key) + "\n\n" + key.ToString() + "\n" + key.ToString("X"),
                    HelperNoteLabel.IsSharp(key) ? myStyle.KeyBlack : myStyle.KeyWhite,
                    GUILayout.Width(40),
                    GUILayout.Height(HelperNoteLabel.IsSharp(key) ? 100 : 120));

                // Get last key position
                Event e = Event.current;
                if (e.type == EventType.Repaint)
                    // Store the rectangle of the key on the GUI for detecting mouse event
                    KeysState[key].Zone = GUILayoutUtility.GetLastRect();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);

            // Reset sequence of notes
            if (GUILayout.Button("New Sequence", GUILayout.Width(100), GUILayout.Height(30)))
                NewSequence();

            GUILayout.Space(spaceH);

            // Select an instrument
            if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null)
            {
                string name = MidiPlayerGlobal.MPTK_PresetName(CurrentPatch);
                if (string.IsNullOrEmpty(name))
                    name = "Preset not defined";

                if (GUILayout.Button(name, GUILayout.Width(200), GUILayout.Height(30)))
                    PopPatch.Show = !PopPatch.Show;
                PopPatch.PositionWithScroll(ref scrollerWindow);
            }

            GUILayout.Space(spaceH);

            // Info current sequence
            if (SequenceNotes != null && SequenceNotes.Count > 0)
            {
                string infoSequence = $"Note Count: {SequenceNotes.Count}";
                if (SequenceNotes.Last().Command == MPTKCommand.NoteOn)
                {
                    infoSequence += $"  -  Last Note: {HelperNoteLabel.LabelFromMidi(SequenceNotes.Last().Value)} {SequenceNotes.Last().Duration} ms";
                    infoSequence += " -  " + MidiFound;
                }
                GUILayout.Label(infoSequence, myStyle.LabelLeft, GUILayout.Width(600), GUILayout.Height(30));
            }
            GUILayout.Space(spaceH);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(spaceH);

            // Write the sequence as a midi file and play 
            if (GUILayout.Button("Write the sequence of notes to a Midi file and play", GUILayout.Width(350), GUILayout.Height(30)))
                WriteMidiSequenceToFileAndPlay("TinySequencer", midiFileWriter);

            GUILayout.Space(spaceH);

            // Play directly the sequence without writing a midi file
            if (GUILayout.Button("Play the sequence directly with MidiFilePlayer Prefab", GUILayout.Width(350), GUILayout.Height(30)))
                PlayDirectlyMidiSequence("TinySequencer", midiFileWriter);

            GUILayout.Space(spaceH);

            if (GUILayout.Button("Stop playing", GUILayout.Width(100), GUILayout.Height(30)))
                StopAllPlaying();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // Open the result directory
            GUILayout.Space(spaceH);
            GUILayout.BeginHorizontal(myStyle.BacgDemosLight);
            GUILayout.Label("Click here to open the folder where MIDI files are created:", myStyle.TitleLabel2, GUILayout.Width(450), GUILayout.Height(48));
            GUILayout.Space(spaceH);
            if (buttonIconFolder == null)
                buttonIconFolder = Resources.Load<Texture2D>("Textures/computer");
            if (GUILayout.Button(new GUIContent(buttonIconFolder, "Open the directory"), GUILayout.Width(48), GUILayout.Height(48)))
                Application.OpenURL("file://" + Application.persistentDataPath);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void NewSequence()
        {
            // Remove all midi events
            midiFileWriter.Clear();
            SequenceNotes = new List<MPTKEvent>();
            CurrentTimeMs = 0;
            waitingNoteOff = false;
            // Apply selected preset
            PatchChange(CurrentPatch);
        }

        /// <summary>@brief
        /// Check keyboard and mouse to create noteon noteoff
        /// </summary>
        private void CheckKeyboardEvent()
        {
            Event e = Event.current;
            //if (e.type != EventType.Layout && e.type != EventType.Repaint) Debug.Log(e.type + " " + e.mousePosition + " isMouse:" + e.isMouse + " isKey:" + e.isKey + " keyCode:" + e.keyCode + " modifiers:" + e.modifiers + " displayIndex:" + e.displayIndex);

            // Check keyboard
            // --------------
            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
            {
                //Debug.Log($"{e.keyCode} {(int)e.keyCode}");
                if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
                {
                    KeyboardState ks = KeysState[e.keyCode - KeyCode.Keypad0 + 48];
                    if (e.type == EventType.KeyDown)
                    {
                        if (ks.Note == null)
                            // Create a new note
                            NewNote(ks);
                    }
                    else if (e.type == EventType.KeyUp)
                        StopNote(ks);
                }
                e.Use();
            }

            // Check mouse
            // -----------
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            {
                bool foundKey = false;
                foreach (KeyboardState ks in KeysState)
                {
                    if (ks != null)
                    {
                        if (ks.Zone.Contains(e.mousePosition))
                        {
                            foundKey = true;
                            if (e.type == EventType.MouseDown)
                                NewNote(ks);
                            else if (e.type == EventType.MouseUp)
                            {
                                if (ks.Note != null)
                                    // Mouse Up inside button note with an existing noteon
                                    StopNote(ks);
                                else
                                    // Mouse Up inside button note but without noteon
                                    StopAllNotes();
                            }
                            break;
                        }
                    }
                }
                // Mouse Up outside all button note
                if (!foundKey && e.type == EventType.MouseUp)
                    StopAllNotes();
            }
        }

        private void PatchChange(int patch)
        {
            LastTimeActivity = Time.realtimeSinceStartup;
            SeqTimeUpdate = true;
            CurrentPatch = patch;

            // Play the change directly 
            midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent { Command = MPTKCommand.PatchChange, Value = patch, Channel = 1 });

            // Add midi event "Preset Change" to the midifilewriter object
            long tickFromTime = midiFileWriter.ConvertMilliToTick(CurrentTimeMs);
            midiFileWriter.AddChannelAfterTouch(1, tickFromTime, 1, patch);
        }

        /// <summary>@brief
        /// Create a new note and play
        /// </summary>
        /// <param name="ks"></param>
        private void NewNote(KeyboardState ks)
        {
            waitingNoteOff = true;
            LastTimeActivity = Time.realtimeSinceStartup;
            SeqTimeUpdate = true;
            //Debug.Log("NewNote TimeNoteOn: " +  ks.Key);
            ks.Time = Time.realtimeSinceStartup;
            ks.Note = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = ks.Key,
                Channel = 1,
                Duration = -1, // real duration will be set when StopNote will be called
                Velocity = 100
            };

            // Play the note directly when keyboard is hit (could have bad interact if sequence is playing in the same time !)
            midiStreamPlayer.MPTK_PlayEvent(ks.Note);

            // Add midi event "Note On" to the midifilewriter object
            midiFileWriter.AddNote(1,midiFileWriter.ConvertMilliToTick(CurrentTimeMs), 1, ks.Key, 100, -1);
        }


        /// <summary>@brief
        /// Stop note
        /// </summary>
        /// <param name="ks"></param>
        private void StopNote(KeyboardState ks)
        {
            if (ks.Note != null)
            {
                waitingNoteOff = false;

                LastTimeActivity = Time.realtimeSinceStartup;
                SeqTimeUpdate = true;

                // Stop the note directly when keyboard is hit 
                midiStreamPlayer.MPTK_StopEvent(ks.Note);

                // Add midi event "Note Off" to the midifilewriter object
                midiFileWriter.AddOff(1, midiFileWriter.ConvertMilliToTick(CurrentTimeMs), 1, ks.Key);

                SequenceNotes.Add(new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Value = ks.Note.Value,
                    Duration = (long)((LastTimeActivity - ks.Time) * 1000f),
                });

                if (TestFootPrint)
                {
                    List<MPTKFootPrint.FootPrint> listMidi = libFootPrint.MPTK_Search(SequenceNotes);
                    if (listMidi.Count > 0)
                    {
                        MidiFound = $"MIDI Identified: {listMidi[0].Name} Score:{listMidi[0].ScoreTempo:F2}";
                        foreach (MPTKFootPrint.FootPrint midiFP in listMidi)
                            Debug.Log(midiFP.ToString());
                    }
                    else
                    {
                        MidiFound = "No MIDI found";
                        Debug.Log(MidiFound);
                    }
                }

                //midiFootprint.Decode(footprint, midiFootprint.NoteCount);

                ks.Note = null;
            }
        }

        private void StopAllNotes()
        {
            foreach (KeyboardState ks in KeysState)
                if (ks != null)
                    StopNote(ks);
        }

        private static void PlayDirectlyMidiSequence(string name, MPTKWriter mfw)
        {
            // Play midi with the MidiExternalPlay prefab without saving midi in a file
            MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }

            midiPlayer.MPTK_Stop();
            mfw.MidiName = name;

            // Set optional event handler
            midiPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventStartPlayMidi.AddListener((string midiname) => { Debug.Log($"Start playing {midiname}"); });

            // Set optional event handler
            midiPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) => { Debug.Log($"End playing {midiname} {reason}"); });

            // Play the MidiFileWriter object
            midiPlayer.MPTK_Play(mfw);
        }

        private static void WriteMidiSequenceToFileAndPlay(string name, MPTKWriter mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write MIDI file:" + filename);

            // Wite the midi file
            mfw.WriteToFile(filename);

            // Need an external player to play midi from a file from a folder
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiExternalPlayer Prefab in the current Scene Hierarchy. Add it with the Maestro menu.");
                return;
            }
            midiExternalPlayer.MPTK_Stop();
            midiExternalPlayer.MPTK_MidiName = "file://" + filename;

            // Set optional event handler
            midiExternalPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventStartPlayMidi.AddListener((string midiname) => { Debug.Log($"Start playing {midiname}"); });

            // Set optional event handler
            midiExternalPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) => { Debug.Log($"End playing {midiname} {reason}"); });

            // Play the external file created above
            midiExternalPlayer.MPTK_Play();
        }

        private static void StopAllPlaying()
        {
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer != null) midiExternalPlayer.MPTK_Stop();
            MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer != null) midiFilePlayer.MPTK_Stop();
        }
        private void Update()
        {
            // Pause sequence time if there is no activity since one second
            if (!waitingNoteOff && Time.realtimeSinceStartup - LastTimeActivity > MaxNoactivitySec)
                SeqTimeUpdate = false;

            // Increase sequence time if there is activity
            if (SeqTimeUpdate)
                CurrentTimeMs += Time.deltaTime * 1000f;
        }
    }
}

