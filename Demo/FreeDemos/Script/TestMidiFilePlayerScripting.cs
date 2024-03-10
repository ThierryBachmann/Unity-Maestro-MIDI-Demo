#define MPTK_PRO
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MidiPlayerTK;
using UnityEditor;

namespace DemoMPTK
{
    public class TestMidiFilePlayerScripting : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to play a Midi file from your list of Midi file. This PreFab must be present in your scene.
        /// </summary>
        public MidiFilePlayer midiFilePlayer;

        [Header("[Pro] Delay to ramp up volume at startup or down at stop")]
        [Range(0f, 5000f)]
        public float DelayRampMillisecond;

        [Header("Start position in Midi defined in pourcentaage of the whole druration of the Midi")]
        [Range(0f, 100f)]
        public float StartPositionPct;

        [Header("Stop position in Midi defined in pourcentaage of the whole druration of the Midi")]
        [Range(0f, 100f)]
        public float StopPositionPct;

        [Header("Delay to apply random change")]
        [Range(0f, 10f)]
        public float DelayRandomSecond;
        public bool IsRandomPosition;
        public bool IsRandomSpeed;
        public bool IsRandomTranspose;

        private bool randomPlay = false;
        private bool nextPlay = false;

        /// <summary>@brief
        /// When true the transition between two songs is immediate, but a small crossing can occur
        /// </summary>
        public bool IsWaitNotesOff;

        public int CurrentIndexPlaying;
        public int forceBank;
        public MidiFilePlayer.ModeStopPlay ModeLoop;
        public bool toggleChangeNoteOn;
        public bool toggleDisableChangePreset;
        public bool toggleChangeTempo;

        public bool FoldOutMetronome;
        private int volumeMetronome = 100;
        private int instrumentMetronome = 60;

        public bool FoldOutStartStopRamp = false;
        public bool FoldOutAlterMidi = false;
        public bool FoldOutChannelDisplay = false;
        public bool FoldOutRealTimeChange = false;
        public bool FoldOutSetStartStopPosition = false;
        public bool FoldOutGeneralSettings = false;
        public bool FoldOutEffectSoundFontDisplay = false;
        public bool FoldOutEffectUnityDisplay = false;

        public float widthIndent = 2.5f;

        // Manage skin
        private CustomStyle myStyle;
        private static Color ButtonColor = new Color(.7f, .9f, .7f, 1f);

        private Vector2 scrollerWindow = Vector2.zero;
        private PopupListItem PopMidi;

        private string infoMidi;
        private string infoLyrics;
        private string infoCopyright;
        private string infoSeqTrackName;
        private Vector2 scrollPos1 = Vector2.zero;
        private Vector2 scrollPos2 = Vector2.zero;
        private Vector2 scrollPos3 = Vector2.zero;
        private Vector2 scrollPos4 = Vector2.zero;

        private float LastTimeChange;
#if MPTK_PRO
        private string beatTimeTick = "";
        private string beatMeasure = "";
#endif
        private int countNoteToInsert = 10;
        private long tickPositionToInsert = 0;

        //DateTime localStartTimeMidi;
        TimeSpan realTimeMidi;

        /// <summary>
        /// PreProcessMidi is triggered from an internal thread.
        ///    - Accuracy is garantee (see midiFilePlayer.MPTK_Pulse which is the minimum time in millisecond between two MIDI events).
        ///    - MIDI Events can be modified before processed by the MIDI synth.
        ///    - Direct call to Unity API is not possible.
        ///    - Avoid huge processing in this callback, that could cause irregular musical rhythms.
        ///  set with: midiFilePlayer.OnMidiEvent = PreProcessMidi;
        /// </summary>
        /// <param name="midiEvent">a MIDI event see https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html </param>
        /// <returns>true to playing this event, false to skip since v2.10.0</returns>
        bool MaestroOnMidiEvent(MPTKEvent midiEvent)
        {
            bool playEvent = true;
            if (FoldOutRealTimeChange)
            {
                switch (midiEvent.Command)
                {

                    case MPTKCommand.NoteOn:
                        if (toggleChangeNoteOn)
                        {
                            if (midiEvent.Channel != 9)
                                // transpose one octave depending on the value channel even or odd
                                if (midiEvent.Channel % 2 == 0)
                                    midiEvent.Value += 12;
                                else
                                    midiEvent.Value -= 12;
                            else
                                // Drums are muted
                                playEvent = false;
                        }
                        break;
                    case MPTKCommand.PatchChange:
                        if (toggleDisableChangePreset)
                        {
                            // Transform Patch change event to Meta text event: related channel will played the default preset 0.
                            // TextEvent has no effect on the MIDI synth but is displayed in the demo windows.
                            // It would also been possible de change the preset to another instrument.
                            midiEvent.Command = MPTKCommand.MetaEvent;
                            midiEvent.Meta = MPTKMeta.TextEvent;
                            midiEvent.Info = $"Detected MIDI event Preset Change {midiEvent.Value} removed";
                        }
                        break;
                    case MPTKCommand.MetaEvent:
                        switch (midiEvent.Meta)
                        {
                            case MPTKMeta.SetTempo:
                                if (toggleChangeTempo)
                                {
                                    // Warning: this call back is run out of the main Unity thread, Unity API (like UnityEngine.Random) can't be used.
                                    System.Random rnd = new System.Random();
                                    // Change the tempo with a random value here, because it's too late for the MIDI Sequencer (alredy taken into account).
                                    midiFilePlayer.MPTK_Tempo = rnd.Next(30, 240);
                                    Debug.Log($"Detected MIDI event Set Tempo {midiEvent.Value}, forced to a random value {midiFilePlayer.MPTK_Tempo}");
                                }
                                break;
                        }
                        break;
                }
            }

            // true: plays this event, false to skip
            return playEvent;
        }

        /// <summary>
        /// Action is executed at each beat even if there is there no MIDI event on the beat (Pro).
        /// Accuracy is garantee.
        /// Direct call to Unity API is not possible but Debug.Log and Maestro API (example, play a sound at each beat) are allowed.
        /// </summary>
        /// <param name="time">Time in milliseconds since the start of the playing MIDI.</param>
        /// <param name="tick">Current tick.</param>
        /// <param name="measure">Current measure (start from 1).</param>
        /// <param name="beat">current beat (start from 1).</param>
        void OnBeatAction(int time, long tick, int measure, int beat)
        {
            if (FoldOutMetronome)
            {
                Debug.Log($"QuarterAction - time:{time} sec. tick:{tick} tempoMap:{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentSignMap.Index} measure:{measure} beat:{beat}");
                //// for testing only
                //if (measure >= 3)
                //    // Stop the MIDI playing but not sound (not allowed in this action out of the Unity thread)
                //    midiFilePlayer.MPTK_Stop(false);

                midiFilePlayer.MPTK_PlayDirectEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Channel = 9,
                    Value = instrumentMetronome,
                    Velocity = volumeMetronome,
                    Measure = measure,
                    Beat = beat
                });
            }
        }


        void Start()
        {

            // Warning: Avoid defining this event through the script as shown below, as the initial loading may not be triggered
            // if MidiPlayerGlobal is loads before any other game component.
            // It is preferable to set this method from the MidiPlayerGlobal event inspector.
            // This should be done in the Start event (not Awake).
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(MaestroOnEventPresetLoaded);

            PopMidi = new PopupListItem()
            {
                Title = "Select A MIDI File",
                OnSelect = MastroMidiSelected,
                Tag = "NEWMIDI",
                ColCount = 3,
                ColWidth = 250,
            };

            // the prefab MidifIlePlayer must be defined in the inspector. You can associated with midiFilePlayer variable
            // with the inspector or by script
            if (midiFilePlayer == null)
            {
                Debug.Log("No MidiFilePlayer defined with the editor inspector, try to find one");
                MidiFilePlayer fp = FindObjectOfType<MidiFilePlayer>();
                if (fp == null)
                    Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                else
                {
                    midiFilePlayer = fp;
                }
            }

            // useless v2.9.0 MidiLoad midiLoaded = midiFilePlayer.MPTK_Load();
            //if (midiLoaded == null) throw new Exception("Could not load MIDI file");
            //Debug.Log(midiLoaded.MPTK_TrackCount);

            if (midiFilePlayer != null)
            {
#if MPTK_PRO
                // OnMidiEvent (pro) and OnEventNotesMidi are triggered for each notes that the MIDI sequencer read
                // However, the use of these methods depends on the specific requirements of the situation.”:
                //      OnEventNotesMidi is handled from the main Unity thread (in the Update loop) 
                //          - The accuracy of this process is not guaranteed because it depends on the Unity process and the value of Time.deltaTime.
                //            (interval in seconds from the last frame to the current one).
                //          - MIDI events cannot be modified before being processed by the MIDI synth.
                //          - A direct call to the Unity API is not possible.
                //      OnMidiEvent is handled from an internal managed thread 
                //          - The accuracy is garanteed.
                //          - MIDI events can be modified before being processed by the MIDI synth.
                //          - MIDI events are skipped if the return of PreProcessMidi is false (v2.10.0).
                //          - A direct call to the Unity API is not possible (but Debug.log is possible).
                midiFilePlayer.OnMidiEvent = MaestroOnMidiEvent;

                // OnBeatEvent (pro) is triggered by the MIDI sequencer at each beat independantly of MIDI events 
                //    - Evant is executed at each beat even if there is there no MIDI event on the beat.
                //    - Accuracy is garantee.
                //    - Direct call to Unity API is not possible (but you have access to all your script variables).
                // Parameters: 
                //    - time        Time in milliseconds since the start of the playing MIDI.
                //    - tick        Current tick of the beat.
                //    - measure     Current measure (start from 1).
                //    - beat        Current beat (start from 1).
                midiFilePlayer.OnBeatEvent = (int time, long tick, int measure, int beat) =>
                {
                    if (FoldOutMetronome)
                    {
                        beatTimeTick = $"Time:{time} sec. Tick:{tick}";
                        beatMeasure = $"Measure:{measure} Beat:{beat}";

                        Debug.Log($"Quarter - {beatTimeTick} Signature segment:{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentSignMap.Index} {beatMeasure}");
                        midiFilePlayer.MPTK_PlayDirectEvent(new MPTKEvent()
                        {
                            Command = MPTKCommand.NoteOn,
                            Channel = 9,
                            Value = instrumentMetronome,
                            Velocity = volumeMetronome,
                            Measure = measure,
                            Beat = beat
                        });
                    }
                };

#endif

                // There is two methods to trigger event: 
                //      1) in inpector from the Unity editor 
                //      2) by script, see below 
                // ------------------------------------------

                //! [Example OnEventStartPlayMidi]
                // Event trigger each time a MIDI file starts playing
                Debug.Log("OnEventStartPlayMidi defined by script");
                midiFilePlayer.OnEventStartPlayMidi.RemoveAllListeners();
                midiFilePlayer.OnEventStartPlayMidi.AddListener(info =>
                    {
                        MaestroOnEventStartPlayMidi("Event set by script");
                        // It’s a good opportunity to change the channel configuration.”
                        // Example (uncomment to disable channel 0 at start)
                        // midiFilePlayer.MPTK_Channels[0].Enable = false;
                    });

                //! [Example OnEventStartPlayMidi]

                // An event is triggered when the MIDI file has finished playing.
                Debug.Log("OnEventEndPlayMidi defined by script");
                midiFilePlayer.OnEventEndPlayMidi.AddListener(MaestroOnEventEndPlayMidi);

                // An event is triggered for each group of notes that is read from the MIDI file.
                Debug.Log("OnEventNotesMidi defined by script");
                midiFilePlayer.OnEventNotesMidi.AddListener(MaestroOnEventNotesMidi);
            }
        }

        /// <summary>@brief
        /// This method is defined from MidiPlayerGlobal event inspector and run when SoundFont is loaded.
        /// Warning: avoid to define this event by script because the initial loading could be not trigger in the case of MidiPlayerGlobal id load before any other gamecomponent
        /// </summary>
        public void MaestroOnEventPresetLoaded()
        {
            Debug.LogFormat($"End loading SF '{MidiPlayerGlobal.ImSFCurrent.SoundFontName}', MPTK is ready to play");
            Debug.Log("Load statistique");
            Debug.Log($"   Time To Download SF:     {Math.Round(MidiPlayerGlobal.MPTK_TimeToDownloadSoundFont.TotalSeconds, 3)} second");
            Debug.Log($"   Time To Load SoundFont:  {Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3)} second");
            Debug.Log($"   Time To Load Samples:    {Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString()} second");
            Debug.Log($"   Presets Loaded: {MidiPlayerGlobal.MPTK_CountPresetLoaded}");
            Debug.Log($"   Samples Loaded: {MidiPlayerGlobal.MPTK_CountWaveLoaded}");
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is started (set by Unity Editor in MidiFilePlayer Inspector or by script see above)
        /// </summary>
        public void MaestroOnEventStartPlayMidi(string name)
        {
            infoLyrics = "";
            infoCopyright = "";
            infoSeqTrackName = "";
            //localStartTimeMidi = DateTime.Now;
            if (midiFilePlayer != null)
            {
                infoMidi = $"Load time: {midiFilePlayer.MPTK_MidiLoaded.MPTK_LoadTime:F2} milliseconds\n";
                infoMidi += $"Full Duration: {midiFilePlayer.MPTK_Duration} {midiFilePlayer.MPTK_DurationMS / 1000f:F2} seconds {midiFilePlayer.MPTK_TickLast} ticks\n";
                infoMidi += $"First note-on: {TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_PositionFirstNote)} {midiFilePlayer.MPTK_PositionFirstNote / 1000f:F2} seconds {midiFilePlayer.MPTK_TickFirstNote} ticks\n";
                infoMidi += $"Last note-on : {TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_PositionLastNote)} {midiFilePlayer.MPTK_PositionLastNote / 1000f:F2} seconds  {midiFilePlayer.MPTK_TickLastNote} ticks\n";
                infoMidi += $"Track Count  : {midiFilePlayer.MPTK_MidiLoaded.MPTK_TrackCount}\n";
                infoMidi += $"Initial Tempo: {midiFilePlayer.MPTK_MidiLoaded.MPTK_InitialTempo:F2}\n";
                infoMidi += $"Delta Ticks  : {midiFilePlayer.MPTK_MidiLoaded.MPTK_DeltaTicksPerQuarterNote} Ticks Per Quarter\n";
                infoMidi += $"Pulse Length : {midiFilePlayer.MPTK_PulseLenght} milliseconds (MIDI resolution)\n";
                infoMidi += $"Number Beats Measure   : {midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberBeatsMeasure}\n";
                infoMidi += $"Number Quarter Beats   : {midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberQuarterBeat}\n";
                infoMidi += $"Count MIDI Events      : {midiFilePlayer.MPTK_MidiEvents.Count}\n";
                infoMidi += $"Tempo Change\n";
                foreach (MPTKTempo tempo in midiFilePlayer.MPTK_MidiLoaded.MPTK_TempoMap)
                {
                    string sEndTick = tempo.ToTick == long.MaxValue ? "    End" : $"{tempo.ToTick,-7:000000}";
                    infoMidi += $"   Tick:{tempo.FromTick,-7:000000} to {sEndTick}  BPM:{MPTKEvent.QuarterPerMicroSecond2BeatPerMinute(tempo.MicrosecondsPerQuarterNote):F1} \n";
                }

                if (FoldOutSetStartStopPosition && StartPositionPct > 0f)
                    midiFilePlayer.MPTK_TickCurrent = (long)((float)midiFilePlayer.MPTK_TickLast * (StartPositionPct / 100f));
            }
            Debug.Log($"Start Play MIDI '{name}' '{midiFilePlayer.MPTK_MidiName}' Duration: {midiFilePlayer.MPTK_DurationMS / 1000f:F2} seconds  Load time: {midiFilePlayer.MPTK_MidiLoaded.MPTK_LoadTime:F2} milliseconds");
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when midi notes are available. 
        /// Set by Unity Editor in MidiFilePlayer Inspector or by script with OnEventNotesMidi.
        /// </summary>
        public void MaestroOnEventNotesMidi(List<MPTKEvent> midiEvents)
        {
            // Looping in this demo is using percentage. Obviously, absolute tick value can also be used.
            if (FoldOutSetStartStopPosition)
            {
                if (StopPositionPct < StartPositionPct)
                    Debug.LogWarning($"StopPosition ({StopPositionPct} %) is defined before StartPosition ({StartPositionPct} %)");

                if (StartPositionPct > 0f)
                {
                    // Convert percentage start position to tick position
                    long tickStart = (long)(midiFilePlayer.MPTK_TickLast * (StartPositionPct / 100f));
                    if (midiFilePlayer.MPTK_TickCurrent < tickStart)
                        midiFilePlayer.MPTK_TickCurrent = tickStart;
                }
                if (StopPositionPct < 100f)
                {
                    // Convert percentage stop position to tick position
                    long tickStop = (long)(midiFilePlayer.MPTK_TickLast * (StopPositionPct / 100f));
                    if (midiFilePlayer.MPTK_TickCurrent > tickStop)
                    {
                        // restart the same or  random or next 
                        if (!randomPlay && !nextPlay)
                        {
                            midiFilePlayer.MPTK_RePlay();
                        }
                        else if (randomPlay)
                        {
                            midiFilePlayer.MPTK_Stop();
                            int index = UnityEngine.Random.Range(0, MidiPlayerGlobal.MPTK_ListMidi.Count);
                            midiFilePlayer.MPTK_MidiIndex = index;
                            midiFilePlayer.MPTK_Play();
                        }
                        else if (nextPlay)
                            midiFilePlayer.MPTK_Next();
                    }
                }
            }

            foreach (MPTKEvent midiEvent in midiEvents)
            {
                switch (midiEvent.Command)
                {
                    case MPTKCommand.ControlChange:
                        //Debug.LogFormat($"Pan Channel:{midiEvent.Channel} Value:{midiEvent.Value}");
                        break;

                    case MPTKCommand.NoteOn:
                        //Debug.LogFormat($"Note Channel:{midiEvent.Channel} {midiEvent.Value} Velocity:{midiEvent.Velocity} Duration:{midiEvent.Duration}");
                        break;

                    case MPTKCommand.MetaEvent:
                        switch (midiEvent.Meta)
                        {
                            case MPTKMeta.TextEvent:
                                infoLyrics += "TextEvent: " + midiEvent.Info + "\n";
                                break;
                            case MPTKMeta.Lyric:
                            case MPTKMeta.Marker:
                                // Info from http://gnese.free.fr/Projects/KaraokeTime/Fichiers/karfaq.html and here https://www.mixagesoftware.com/en/midikit/help/HTML/karaoke_formats.html
                                //Debug.Log(midievent.Channel + " " + midievent.Meta + " '" + midievent.Info + "'");
                                string text = midiEvent.Info.Replace("\\", "\n");
                                text = text.Replace("/", "\n");
                                if (text.StartsWith("@") && text.Length >= 2)
                                {
                                    switch (text[1])
                                    {
                                        case 'K': text = "Type: " + text.Substring(2); break;
                                        case 'L': text = "Language: " + text.Substring(2); break;
                                        case 'T': text = "Title: " + text.Substring(2); break;
                                        case 'V': text = "Version: " + text.Substring(2); break;
                                        default: //I as information, W as copyright, ...
                                            text = text.Substring(2); break;
                                    }
                                    //text += "\n";
                                }
                                infoLyrics += text + "\n";
                                break;

                            case MPTKMeta.Copyright:
                                infoCopyright += midiEvent.Info + "\n";
                                break;

                            case MPTKMeta.SequenceTrackName:
                                infoSeqTrackName += $"Track:{midiEvent.Track:00} '{midiEvent.Info}'\n";
                                //Debug.LogFormat($"SequenceTrackName Track:{midiEvent.Track} {midiEvent.Value} Name:'{midiEvent.Info}'");
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is ended when reach end or stop by MPTK_Stop or Replay with MPTK_Replay
        /// The parameter reason give the origin of the end
        /// </summary>
        public void MaestroOnEventEndPlayMidi(string name, EventEndMidiEnum reason)
        {
            Debug.LogFormat("End playing midi {0} reason:{1}", name, reason);
        }

        private void MastroMidiSelected(object tag, int midiindex, int indexList)
        {
            Debug.Log("MidiChanged " + midiindex + " for " + tag);
            midiFilePlayer.MPTK_MidiIndex = midiindex;
            midiFilePlayer.MPTK_RePlay();
        }

        //! [Example TheMostSimpleDemoForMidiPlayer]
        /// <summary>@brief
        /// Load a midi file without playing it
        /// </summary>
        private void TheMostSimpleDemoForMidiPlayer()
        {
            MidiFilePlayer midiplayer = FindObjectOfType<MidiFilePlayer>();
            if (midiplayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }

            // Index of the midi from the Midi DB (find it with 'Midi File Setup' from the menu MPTK)
            midiplayer.MPTK_MidiIndex = 10;

            // Open and load the Midi
            if (midiplayer.MPTK_Load() != null)
            {
                // Read midi event to a List<>
                List<MPTKEvent> mptkEvents = midiplayer.MPTK_ReadMidiEvents(1000, 10000);

                // Loop on each Midi events
                foreach (MPTKEvent mptkEvent in mptkEvents)
                {
                    // Log if event is a note on
                    if (mptkEvent.Command == MPTKCommand.NoteOn)
                        Debug.Log($"Note on Time:{mptkEvent.RealTime} millisecond  Note:{mptkEvent.Value}  Duration:{mptkEvent.Duration} millisecond  Velocity:{mptkEvent.Velocity}");

                    // Uncomment to display all Midi events
                    //Debug.Log(mptkEvent.ToString());
                }
            }
        }
        //! [Example TheMostSimpleDemoForMidiPlayer]

        void OnGUI()
        {
            if (myStyle == null) { myStyle = new CustomStyle(); HelperDemo.myStyle = myStyle; }

            if (midiFilePlayer != null)
            {
                scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));

                HelperDemo.GUI_Horizontal(HelperDemo.Zone.INIT);
                HelperDemo.GUI_Vertical(HelperDemo.Zone.INIT);

                // Display popup in first to avoid activate other layout behind
                PopMidi.Draw(MidiPlayerGlobal.MPTK_ListMidi, midiFilePlayer.MPTK_MidiIndex, myStyle);
                MainMenu.Display("Test MIDI File Player Scripting - Demonstrate how to use the MPTK API to Play Midi", myStyle, "https://paxstellar.fr/midi-file-player-detailed-view-2/");
                GUISelectSoundFont.Display(scrollerWindow, myStyle);

                HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN); // for left/right panel

                // Midi action
                OnGUI_LeftPanel();

                // Display information about the MIDI
                OnGUI_RightPanel();

                HelperDemo.GUI_Horizontal(HelperDemo.Zone.END); // for left/right panel

                if (Application.isEditor)
                {
                    HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
                    GUILayout.Label("Go to your Hierarchy, select GameObject MidiFilePlayer: inspector contains a lot of parameters to control the sound.", myStyle.TitleLabel2);
                    HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
                }

                HelperDemo.GUI_Horizontal(HelperDemo.Zone.CLEAN);
                HelperDemo.GUI_Vertical(HelperDemo.Zone.CLEAN);
                GUILayout.EndScrollView();
            }
        }

        private void OnGUI_LeftPanel()
        {
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosMedium, GUILayout.Width(700));

            HelperDemo.DisplayInfoSynth(midiFilePlayer, 600, myStyle);

            // Open the popup to select a midi
            if (GUILayout.Button($"{midiFilePlayer.MPTK_MidiIndex} - '{midiFilePlayer.MPTK_MidiName}'", GUILayout.Height(40)))
                PopMidi.Show = !PopMidi.Show;

            OnGUI_PlayPauseStopMIDI();

            OnGui_MidiTimePosition();

            FoldOutGeneralSettings = GUILayout.Toggle(FoldOutGeneralSettings, "General Settings (volume, speed, ...)");
            if (FoldOutGeneralSettings)
                OnGUI_MidiGeneralSettings();

            FoldOutSetStartStopPosition = GUILayout.Toggle(FoldOutSetStartStopPosition, "Set MIDI Start & Stop Position");
            if (FoldOutSetStartStopPosition)
                OnGUI_MidiStartStopPosition();

            // Channel setting display
            FoldOutChannelDisplay = GUILayout.Toggle(FoldOutChannelDisplay, "Display Channels and Change Properties");
            if (FoldOutChannelDisplay)
                OnGUI_ChannelChange();

            FoldOutStartStopRamp = GUILayout.Toggle(FoldOutStartStopRamp, "Play with Crescendo and Stop with Diminuendo [Pro]");
            if (FoldOutStartStopRamp)
                OnGUI_StartStopCrescendo();

            FoldOutAlterMidi = GUILayout.Toggle(FoldOutAlterMidi, "Modify MIDI and Play [Pro]");
            if (FoldOutAlterMidi)
                OnGUI_ModifyMidiAndPlay();

            FoldOutMetronome = GUILayout.Toggle(FoldOutMetronome, "Enable Metronome [Pro]");
            if (FoldOutMetronome)
                OnGUI_EnableMetronome();

            FoldOutRealTimeChange = GUILayout.Toggle(FoldOutRealTimeChange, "Real-time MIDI Change [Pro]");
            if (FoldOutRealTimeChange)
                OnGUI_RealTimeMIDIChange();

            FoldOutEffectSoundFontDisplay = GUILayout.Toggle(FoldOutEffectSoundFontDisplay, "Enable SoundFont Effects [Pro]");
            if (FoldOutEffectSoundFontDisplay)
#if MPTK_PRO
                HelperDemo.GUI_EffectSoundFont(widthIndent, midiFilePlayer.MPTK_EffectSoundFont);
#else
                HelperDemo.GUI_EffectSoundFont(widthIndent);
#endif

            FoldOutEffectUnityDisplay = GUILayout.Toggle(FoldOutEffectUnityDisplay, "Enable Unity Effects [Pro]");
            if (FoldOutEffectUnityDisplay)
#if MPTK_PRO
                HelperDemo.GUI_EffectUnity(widthIndent, midiFilePlayer.MPTK_EffectUnity);
#else
                HelperDemo.GUI_EffectUnity(widthIndent);
#endif

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
        }

        /// <summary>
        /// Display and change the real time from the MIDI sequencer
        /// </summary>
        private void OnGui_MidiTimePosition()
        {
            // Get the time of the last MIDI event read
            TimeSpan timePosition = TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_Position);

            // For Debug Timing
            //if (midiFilePlayer.MPTK_IsPlaying)
            //    realTimeMidi = TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_RealTime);
            //string realTime = $"Real Time: {realTimeMidi.Hours:00}:{realTimeMidi.Minutes:00}:{realTimeMidi.Seconds:00}:{realTimeMidi.Milliseconds:000} ";
            //// and the delta time with the last MIDI events
            //string deltaTime = $"Delta time with the last MIDI event: {(timePosition - realTimeMidi).TotalSeconds:F3} second";
            //GUILayout.Label(realTime + deltaTime, myStyle.TitleLabel3, GUILayout.Width(500));

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);
            string sPlayTime = $"{timePosition.Hours:00}:{timePosition.Minutes:00}:{timePosition.Seconds:00}:{timePosition.Milliseconds:000}";
            string sRealDuration = $"{midiFilePlayer.MPTK_Duration.Hours:00}:{midiFilePlayer.MPTK_Duration.Minutes:00}:{midiFilePlayer.MPTK_Duration.Seconds:00}:{midiFilePlayer.MPTK_Duration.Milliseconds:000}";
            string sPosition = $"Time: {sPlayTime} / {sRealDuration}";
            double currentPosition = Math.Round(midiFilePlayer.MPTK_Position / 1000d, 2);

            // Change current position with a slider
            double newPosition = Math.Round(HelperDemo.GUI_Slider(sPosition, (float)currentPosition, 0f, (float)midiFilePlayer.MPTK_Duration.TotalSeconds,
                alignCaptionRight: false, enableButton: true, valueButton: 1f, widthCaption: 220, widthSlider: 150, widthLabelValue: 0), 2);
            if (newPosition != currentPosition)
            {
                if (Event.current.type == EventType.Used)
                {
                    //Debug.Log("New position " + currentPosition + " --> " + newPosition + " " + Event.current.type);
                    midiFilePlayer.MPTK_Position = newPosition * 1000d;
                }
            }

            // Change current tick with a slider
            string sTickPosition = $"Tick: {midiFilePlayer.MPTK_TickCurrent} / {midiFilePlayer.MPTK_TickLast}";
            long tick = (long)HelperDemo.GUI_Slider(sTickPosition, (float)midiFilePlayer.MPTK_TickCurrent, 0f, (float)midiFilePlayer.MPTK_TickLast,
                alignCaptionRight: true, enableButton: true, valueButton: 1f, widthCaption: 150, widthSlider: 150, widthLabelValue: 0);
            if (tick != midiFilePlayer.MPTK_TickCurrent)
            {
                if (Event.current.type == EventType.Used)
                {
                    //Debug.Log("New tick " + midiFilePlayer.MPTK_TickCurrent + " --> " + tick + " " + Event.current.type);
                    midiFilePlayer.MPTK_TickCurrent = tick;
                }
            }
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
        }

        //! [Example_GUI_PreloadAndAlterMIDI]
        /// <summary>
        /// Load the selected MIDI, add some notes and play (PRO only)
        /// </summary>
        private void OnGUI_ModifyMidiAndPlay()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);

            GUILayout.Label("When the MIDI is loaded, it's possible to alter the MIDI events brfore playing them. Result not garantee!", myStyle.TitleLabel3);

            countNoteToInsert = (int)HelperDemo.GUI_Slider("Count notes to insert:", (float)countNoteToInsert, 1, 100,
                alignCaptionRight: false, enableButton: true, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            tickPositionToInsert = (long)HelperDemo.GUI_Slider("Tick position to insert:", (long)tickPositionToInsert, 0, (long)midiFilePlayer.MPTK_TickLast,
                alignCaptionRight: false, enableButton: true, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            if (GUILayout.Button("Insert And Play", GUILayout.Width(120)))
            {
#if MPTK_PRO
                // Better to stop the MIDIplaying.
                midiFilePlayer.MPTK_Stop();

                // There is no need of note-off events.
                midiFilePlayer.MPTK_KeepNoteOff = false;
                // MPTK_MidiName must contains the name of the MIDI to load.
                if (midiFilePlayer.MPTK_Load() != null)
                {
                    Debug.Log($"Duration: {midiFilePlayer.MPTK_Duration.TotalSeconds} seconds");
                    Debug.Log($"Count MIDI Events: {midiFilePlayer.MPTK_MidiEvents.Count}");
                    // Insert weird notes in the beautiful MIDI!
                    for (int insertNote = 1; insertNote <= countNoteToInsert; insertNote++)
                        midiFilePlayer.MPTK_MidiEvents.Insert(0,
                            new MPTKEvent()
                            {
                                Channel = 0,
                                Command = MPTKCommand.NoteOn,
                                Value = 60 + insertNote % 12,
                                Duration = midiFilePlayer.MPTK_DeltaTicksPerQuarterNote,
                                Tick = tickPositionToInsert + insertNote * midiFilePlayer.MPTK_DeltaTicksPerQuarterNote
                            });
                    // New event has been inserted, MIDI events list must be sorted by tick.
                    midiFilePlayer.MPTK_SortEvents();

                    // ... then play the modified list of events (any garantee to create the hit of the year!).
                    midiFilePlayer.MPTK_Play(alreadyLoaded: true);
                }


#else
                Debug.LogWarning("MIDI preload and alter MIDI events are available only with the PRO version");
#endif
            }

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

        }
        //! [Example_GUI_PreloadAndAlterMIDI]


        private void OnGUI_MidiGeneralSettings()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);

            // Define the global volume
            midiFilePlayer.MPTK_Volume = HelperDemo.GUI_Slider("Global Volume:", midiFilePlayer.MPTK_Volume, 0f, 1f,
                alignCaptionRight: false, enableButton: true, valueButton: 1f, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            //// Transpose each note
            midiFilePlayer.MPTK_Transpose = (int)HelperDemo.GUI_Slider("Note Transpose:", (float)midiFilePlayer.MPTK_Transpose, -24, 24,
                alignCaptionRight: false, enableButton: true, valueButton: 1f, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            // Change speed
            midiFilePlayer.MPTK_Speed = HelperDemo.GUI_Slider("MIDI Reading Speed:", midiFilePlayer.MPTK_Speed, 0.1f, 10f,
                alignCaptionRight: false, enableButton: true, valueButton: 0.1f, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
        }

        private void OnGUI_StartStopCrescendo()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);

#if MPTK_PRO
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);
            DelayRampMillisecond = (int)HelperDemo.GUI_Slider("Delay (milliseconds)", DelayRampMillisecond, 0, 5000f,
                alignCaptionRight: false, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);
            if (GUILayout.Button($"Play")) midiFilePlayer.MPTK_Play(DelayRampMillisecond);
            if (GUILayout.Button($"Stop")) midiFilePlayer.MPTK_Stop(DelayRampMillisecond);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
#else
            GUILayout.Label("Available with Maestro MPTK Pro.", myStyle.TitleLabel3);
#endif

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
        }

        private void OnGUI_PlayPauseStopMIDI()
        {
            // Play/Pause/Stop/Restart actions on midi 
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);

            if (GUILayout.Button("Previous"))
            {
                if (IsWaitNotesOff)
                    StartCoroutine(NextPreviousWithWait(false));
                else
                {
                    midiFilePlayer.MPTK_Previous();
                    CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;
                }
            }

            if (midiFilePlayer.MPTK_IsPlaying && !midiFilePlayer.MPTK_IsPaused)
                GUI.color = ButtonColor;
            if (GUILayout.Button("Play"))
                midiFilePlayer.MPTK_Play();
            GUI.color = Color.white;

            if (GUILayout.Button("Next"))
            {
                if (IsWaitNotesOff)
                    StartCoroutine(NextPreviousWithWait(true));
                else
                {
                    midiFilePlayer.MPTK_Next();
                    CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;
                }
                //Debug.Log("MPTK_Next - CurrentIndexPlaying " + CurrentIndexPlaying);
            }

            if (midiFilePlayer.MPTK_IsPaused)
                GUI.color = ButtonColor;
            if (GUILayout.Button("Pause"))
                if (midiFilePlayer.MPTK_IsPaused)
                    midiFilePlayer.MPTK_UnPause();
                else
                    midiFilePlayer.MPTK_Pause();
            GUI.color = Color.white;

            if (GUILayout.Button("Stop"))
                midiFilePlayer.MPTK_Stop();

            if (GUILayout.Button("Restart"))
                midiFilePlayer.MPTK_RePlay();

            if (GUILayout.Button("Clear"))
            {
                DelayRampMillisecond = 2000;
                StartPositionPct = 0;
                StopPositionPct = 100;
                IsRandomPosition = false;
                IsRandomSpeed = false;
                IsRandomTranspose = false;
                randomPlay = false;
                nextPlay = false;
                FoldOutMetronome = false;
                volumeMetronome = 100;
                instrumentMetronome = 60;
                FoldOutStartStopRamp = false;
                FoldOutAlterMidi = false;
                FoldOutChannelDisplay = false;
                FoldOutRealTimeChange = false;
                FoldOutSetStartStopPosition = false;
                FoldOutGeneralSettings = false;
                FoldOutEffectSoundFontDisplay = false;
                FoldOutEffectUnityDisplay = false;
                widthIndent = 2.5f;
                IsWaitNotesOff = false;
                midiFilePlayer.MPTK_Volume = 0.5f;
                midiFilePlayer.MPTK_Transpose = 0;
                midiFilePlayer.MPTK_Speed = 1;
                midiFilePlayer.MPTK_MidiAutoRestart = false;
                midiFilePlayer.MPTK_StartPlayAtFirstNote = false;
                midiFilePlayer.MPTK_ClearAllSound(true);
            }

            midiFilePlayer.MPTK_StartPlayAtFirstNote = GUILayout.Toggle(midiFilePlayer.MPTK_StartPlayAtFirstNote, "Start First Note");
            midiFilePlayer.MPTK_MidiAutoRestart = GUILayout.Toggle(midiFilePlayer.MPTK_MidiAutoRestart, "Auto Restart");
            IsWaitNotesOff = GUILayout.Toggle(IsWaitNotesOff, "Wait Notes Off");

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
        }

        private void OnGUI_EnableMetronome()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
#if MPTK_PRO
            GUILayout.Label(beatTimeTick + " " + beatMeasure, myStyle.TitleLabel3);
#else
            GUILayout.Label("Available with Maestro MPTK Pro with OnBeatEvent.", myStyle.TitleLabel3);
#endif
            volumeMetronome = (int)HelperDemo.GUI_Slider("Beat Volume", volumeMetronome, 0, 127,
                alignCaptionRight: false, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);
            instrumentMetronome = (int)HelperDemo.GUI_Slider("Instrument from Drum", instrumentMetronome, 0, 127,
                alignCaptionRight: false, widthCaption: 170, widthSlider: 250, widthLabelValue: 50);

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
        }

        private void OnGUI_MidiStartStopPosition()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN);

            GUILayout.Label("Set the start and stop points in the MIDI based on tick values. For ease, sliders show values as a percentage of the entire MIDI.OnEventNotesMidi is used for each MIDI event read from the MIDI. This is where the start and stop positions are handled.", myStyle.TitleLabel3);
            GUILayout.Label("For precise looping in the MIDI, it's better using the Inner Loop features (Pro version).", myStyle.TitleLabel3);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);

            // Calculate tick position from the percentage
            long tickStartPosition = (long)((float)midiFilePlayer.MPTK_TickLast * (StartPositionPct / 100f));
            long tickStopPosition = (long)((float)midiFilePlayer.MPTK_TickLast * (StopPositionPct / 100f));
            string label = $"Start from tick: {tickStartPosition} to {tickStopPosition} {midiFilePlayer.MPTK_TickCurrent} / {midiFilePlayer.MPTK_TickLast}";
            // CHange start and stop position
            StartPositionPct = HelperDemo.GUI_Slider(label, StartPositionPct, 0f, 100f,
                alignCaptionRight: false, enableButton: true, valueButton: 0.1f, widthCaption: 300, widthSlider: 100, widthLabelValue: 30);

            StopPositionPct = HelperDemo.GUI_Slider(null, StopPositionPct, 0f, 100f,
                alignCaptionRight: false, enableButton: true, valueButton: 0.1f, widthCaption: 0, widthSlider: 100, widthLabelValue: 30);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);

#if UNITY_EDITOR
            // Set looping mode(not apply to inner loop)
            if (GUILayout.Button(MidiFilePlayer.ModeStopPlayLabel[(int)ModeLoop]))
            {
                var dropDownMenu = new GenericMenu();
                foreach (MidiFilePlayer.ModeStopPlay mode in Enum.GetValues(typeof(MidiFilePlayer.ModeStopPlay)))
                    dropDownMenu.AddItem
                        (
                            new GUIContent(MidiFilePlayer.ModeStopPlayLabel[(int)mode], ""),
                            ModeLoop == mode,
                            () => { midiFilePlayer.MPTK_ModeStopVoice = mode; ModeLoop = mode; }
                        );
                dropDownMenu.ShowAsContext();
            }
#endif
            GUILayout.Label("At End:", myStyle.TitleLabel3);


            randomPlay = GUILayout.Toggle(randomPlay, "Random");
            if (randomPlay) nextPlay = false;

            nextPlay = GUILayout.Toggle(nextPlay, "Next");
            if (nextPlay) randomPlay = false;

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

        }

        private void OnGUI_RealTimeMIDIChange()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN);

            GUILayout.Label("OnMidiEvent callback is used before the MIDI event goes to the MIDI Synth. This is where you can modify the MIDI event. Check out the examples below and feel free to get creative!", myStyle.TitleLabel3);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);
            GUILayout.Label("Transpose one octave depending on the channel value and disable drum.", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleChangeNoteOn = GUILayout.Toggle(toggleChangeNoteOn, "");
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);
            GUILayout.Label("Disable preset change MIDI event", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleDisableChangePreset = GUILayout.Toggle(toggleDisableChangePreset, "");
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);
            GUILayout.Label("Random change of MIDI tempo change event", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleChangeTempo = GUILayout.Toggle(toggleChangeTempo, "");
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

        }


        private void OnGUI_ChannelChange()
        {
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN, myStyle.BacgDemosLight);
            HelperDemo.GUI_Indent(widthIndent);
            HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN);

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);

            //! [ExampleUsingChannelAPI_6]

            if (GUILayout.Button("Enable All", GUILayout.Width(100)))
                midiFilePlayer.MPTK_Channels.EnableAll = true;

            if (GUILayout.Button("Disable All", GUILayout.Width(100)))
                midiFilePlayer.MPTK_Channels.EnableAll = false;

            //! [ExampleUsingChannelAPI_6]

            if (GUILayout.Button("Default All", GUILayout.Width(100)))
                midiFilePlayer.MPTK_Channels.ResetExtension();

            if (GUILayout.Button("Random!", GUILayout.Width(100)))
            {
                // For fun xD
                //! [ExampleUsingChannelAPI_1]
                // Force a random preset between 0 and 127 for each channels
                //  midiFilePlayer.MPTK_Channels.ResetExtension(); to return to origin preset
                foreach (MPTKChannel mptkChannel in midiFilePlayer.MPTK_Channels)
                    mptkChannel.ForcedPreset = UnityEngine.Random.Range(0, 127);
                //! [ExampleUsingChannelAPI_1]
            }
            midiFilePlayer.MPTK_Channels.EnableResetChannel = GUILayout.Toggle(midiFilePlayer.MPTK_Channels.EnableResetChannel, "Reset when MIDI start");

            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

            //! [ExampleUsingChannelAPI_Full]

            GUILayout.Label("Channel   Preset Name                                   Preset / Bank                                  Count    Enabled       Volume", myStyle.TitleLabel3);

            for (int channel = 0; channel < midiFilePlayer.MPTK_Channels.Length; channel++)
            {
                HelperDemo.GUI_Horizontal(HelperDemo.Zone.BEGIN);

                // Display channel number and log info
                if (GUILayout.Button($"   {channel:00}", myStyle.TitleLabel3, GUILayout.Width(60)))
                    Debug.Log(midiFilePlayer.MPTK_Channels[channel].ToString());

                //! [ExampleUsingChannelAPI_One]

                // Display preset name
                GUILayout.Label(midiFilePlayer.MPTK_Channels[channel].PresetName ?? "not set", myStyle.TitleLabel3, GUILayout.MaxWidth(140));

                // Display preset and bank index
                int presetNum = midiFilePlayer.MPTK_Channels[channel].PresetNum;
                int bankNum = midiFilePlayer.MPTK_Channels[channel].BankNum;
                int presetForced = midiFilePlayer.MPTK_Channels[channel].ForcedPreset;
                //! [ExampleUsingChannelAPI_One]

                // Check if preset is forced and build a string info
                string sPreset = presetForced == -1 ? $"{presetNum} / {bankNum}" : $"F{presetForced} / {bankNum}";

                // Slider to change the preset on this channel from -1 (disable forced) to 127.
                int forcePreset = (int)HelperDemo.GUI_Slider(sPreset, presetNum, -1f, 127f, alignCaptionRight: true, widthCaption: 120, widthSlider: 80, widthLabelValue: -1);

                if (forcePreset != presetNum)
                {
                    //! [ExampleUsingChannelAPI_2]
                    // Force a preset and a bank whatever the MIDI events from the MIDI file.
                    // set forcePreset to -1 to restore to the last preset and bank value known from the MIDI file.
                    // let forcebank to -1 to not force the bank.
                    // Before v2.10.1 midiFilePlayer.MPTK_ChannelForcedPresetSet(channel, forcePreset, forceBank);
                    midiFilePlayer.MPTK_Channels[channel].ForcedBank = forceBank;
                    midiFilePlayer.MPTK_Channels[channel].ForcedPreset = forcePreset;
                    //! [ExampleUsingChannelAPI_2]

                }

                // Display count note by channel
                GUILayout.Label($"{midiFilePlayer.MPTK_Channels[channel].NoteCount,-5}", myStyle.LabelLeft, GUILayout.Width(30));

                //! [ExampleUsingChannelAPI_7]

                // Toggle to enable or disable a channel
                GUILayout.Label("   ", myStyle.TitleLabel3, GUILayout.Width(20));
                bool state = GUILayout.Toggle(midiFilePlayer.MPTK_Channels[channel].Enable, "", GUILayout.MaxWidth(20));
                if (state != midiFilePlayer.MPTK_Channels[channel].Enable)
                {
                    midiFilePlayer.MPTK_Channels[channel].Enable = state;
                    Debug.LogFormat("Channel {0} state:{1}, preset:{2}", channel, state, midiFilePlayer.MPTK_Channels[channel].PresetName ?? "not set"); /*2.84*/
                }
                //! [ExampleUsingChannelAPI_7]

                //! [ExampleUsingChannelAPI_5]

                // Slider to change volume
                float currentVolume = midiFilePlayer.MPTK_Channels[channel].Volume;
                float volume = HelperDemo.GUI_Slider(null, currentVolume, 0f, 1f, alignCaptionRight: true, enableButton: false, widthCaption: -1, widthSlider: 40, widthLabelValue: 40);
                if (volume != currentVolume)
                    midiFilePlayer.MPTK_Channels[channel].Volume = volume;

                //! [ExampleUsingChannelAPI_5]

                HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);
            }

            //! [ExampleUsingChannelAPI_Full]

            HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            HelperDemo.GUI_Horizontal(HelperDemo.Zone.END);

        }
        /// <summary>
        /// Display information about the MIDI
        /// </summary>
        private void OnGUI_RightPanel()
        {
            if (!string.IsNullOrEmpty(infoMidi) || !string.IsNullOrEmpty(infoLyrics) || !string.IsNullOrEmpty(infoCopyright) || !string.IsNullOrEmpty(infoSeqTrackName))
            {
                //
                // Right Column: midi infomation, lyrics, ...
                // ------------------------------------------
                HelperDemo.GUI_Vertical(HelperDemo.Zone.BEGIN, myStyle.BacgDemosMedium);

                Color savedColor = GUI.color;
                Color savedBackColor = GUI.backgroundColor;
                GUI.color = Color.green;
                GUI.backgroundColor = Color.black;

                if (!string.IsNullOrEmpty(infoMidi))
                {
                    scrollPos1 = GUILayout.BeginScrollView(scrollPos1, false, true);//, GUILayout.Height(heightLyrics));
                    GUILayout.Label("<i>MIDI Info and Tempo Change</i>\n" + infoMidi, myStyle.TextFieldMultiCourier);
                    GUILayout.EndScrollView();
                }
                GUILayout.Space(2);
                if (!string.IsNullOrEmpty(infoLyrics))
                {
                    scrollPos2 = GUILayout.BeginScrollView(scrollPos2, false, true);//, GUILayout.Height(heightLyrics));
                    GUILayout.Label("<i>Lyrics\n</i>" + infoLyrics, myStyle.TextFieldMultiCourier);
                    GUILayout.EndScrollView();
                }
                GUILayout.Space(2);
                if (!string.IsNullOrEmpty(infoCopyright))
                {
                    scrollPos3 = GUILayout.BeginScrollView(scrollPos3, false, true);
                    GUILayout.Label(infoCopyright, myStyle.TextFieldMultiCourier);
                    GUILayout.EndScrollView();
                }
                GUILayout.Space(2);
                if (!string.IsNullOrEmpty(infoSeqTrackName))
                {
                    scrollPos4 = GUILayout.BeginScrollView(scrollPos4, false, true);
                    GUILayout.Label("<i>Track Name</i>\n" + infoSeqTrackName, myStyle.TextFieldMultiCourier);
                    GUILayout.EndScrollView();
                }

                GUI.color = savedColor;
                GUI.backgroundColor = savedBackColor;

                HelperDemo.GUI_Vertical(HelperDemo.Zone.END);
            }
        }


        /// <summary>@brief
        /// Coroutine: stop current midi playing, wait until all samples are off and go next or previous midi
        /// Example call: StartCoroutine(NextPreviousWithWait(false));
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public IEnumerator NextPreviousWithWait(bool next)
        {
            midiFilePlayer.MPTK_Stop();

            yield return midiFilePlayer.MPTK_WaitAllNotesOff(midiFilePlayer.IdSession);
            if (next)
                midiFilePlayer.MPTK_Next();
            else
                midiFilePlayer.MPTK_Previous();
            CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;

            yield return 0;
        }

        void Update()
        {
            if (midiFilePlayer != null && midiFilePlayer.MPTK_IsPlaying)
            {
                //
                // There is no UI for these random change, to be enabled from the inspector
                // 
                float time = Time.realtimeSinceStartup - LastTimeChange;
                if (DelayRandomSecond > 0f && time > DelayRandomSecond)
                {
                    // It's time to apply Random change
                    LastTimeChange = Time.realtimeSinceStartup;

                    // Random position
                    if (IsRandomPosition) midiFilePlayer.MPTK_Position = UnityEngine.Random.Range(0f, (float)midiFilePlayer.MPTK_Duration.TotalMilliseconds);

                    // Random Speed
                    if (IsRandomSpeed) midiFilePlayer.MPTK_Speed = UnityEngine.Random.Range(0.1f, 5f);

                    // Random transpose
                    if (IsRandomTranspose) midiFilePlayer.MPTK_Transpose = UnityEngine.Random.Range(-12, 13);
                }
            }
        }
    }
}