using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMPTK
{
    /// <summary>@brief
    /// Minimum Viable Product Focuses on the essentials of a Maestro feature. 
    /// Only a few functions are presented. Links to documentation are provided for further exploration.
    /// Therefore: no optimisations, light error management.
    /// The goal is rather to learn how to use the Maestro API and then progress by building more complex applications.
    /// </summary>
    public class TestMidiExternalPlayer : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK MidiExternalPlayer class for the MidiExternalPlay prefab which is able to play a Midi file from an external source. 
        /// Your scene must contain this PreFab.
        /// 
        /// Global help for the MIDI External player prefab: https://paxstellar.fr/midi-external-player-v2/
        /// 
        /// MidiExternalPlayer API: https://mptkapi.paxstellar.com/d8/dc5/class_midi_player_t_k_1_1_midi_external_player.html
        /// Which inherits from MidiFilePlayer and MidiSynth:
        ///     MidiFilePlayer API: https://mptkapi.paxstellar.com/d7/deb/class_midi_player_t_k_1_1_midi_file_player.html
        ///     MidiSynth API: https://mptkapi.paxstellar.com/d3/d1d/class_midi_player_t_k_1_1_midi_synth.html
        ///     
        /// When a MIDI file is loaded, the MPTK_MidiLoaded attribute contains a lot of useful information:
        ///     MidiLoad class: https://mptkapi.paxstellar.com/d8/d19/class_midi_player_t_k_1_1_midi_load.html
        ///     
        /// Inner loop features:
        ///     When a MIDI file is loaded, the MidiFilePlayer class has an attribute MPTK_InnerLoop to access all the looping functions.
        ///     MPTKInnerLoop class: https://mptkapi.paxstellar.com/d4/d6e/class_midi_player_t_k_1_1_m_p_t_k_inner_loop.html
        ///     
        /// Signature map, essential to convert between tick and measure
        ///     MPTKSignature class: https://mptkapi.paxstellar.com/d9/d5f/class_midi_player_t_k_1_1_m_p_t_k_signature.html#a55c39d229e4d1a9fbbf07d9cc678495f
        ///     
        /// </summary>

        // Reference to the MidiExternalPlat prefab in the scene.
        public MidiExternalPlayer midiExternalPlayer;

        // User Interface components 
        // -------------------------

        // Info and error status
        public Text RunningStatus; // Paused Playing Stop
        public Text ErrorStatus; // Error status from prefab MidiExternalPlayer
        public Text EventStatus;
        public Text InfoMidiLyric;
        public Text InfoMidiCopyright;
        public Text InfoMidiTrack;

        // MIDI UI
        public Button BtPlayRoulette;
        public InputField UrlMidi;
        public Button BtPlay;
        public Button BtReplay;
        public Button BtPause;
        public Button BtStop;
        public Button BtSetPositionBy;
        public Text TxtPlayingPosition;
        public Slider SldPlayingPosition;
        public Toggle TglEnableLog;

        // Inner loop UI
        public Toggle TglEnableInnerLoop;
        public Text TxtCountLoop;
        public Slider SldStartLoopFrom;
        public Slider SldResumeLoopAt;
        public Slider SldEndLoopAt;
        public Text TxtStartLoopFrom;
        public Text TxtResumeLoopAt;
        public Text TxtEndLoopAt;

        // Some internal values
        // --------------------
        float speedRoulette;
        float currentVelocity = 0f;
        int modeSetPosition = 0; // 0 tick, 1 measure/bar


        private void Start()
        {
            // Warning: when defined by script, this event is not triggered at first load of MPTK 
            // because MidiPlayerGlobal is loaded before any other gamecomponent
            // To be done in Start event (not Awake)
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(EndLoadingSF);

            // Find the Midi external component 
            if (midiExternalPlayer == null)
            {
                //Debug.Log("No midiExternalPlayer defined with the editor inspector, try to find one");
                MidiExternalPlayer fp = FindFirstObjectByType<MidiExternalPlayer>();
                if (fp == null)
                    Debug.LogWarning("Can't find a MidiExternalPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                else
                {
                    midiExternalPlayer = fp;
                }
            }

            if (midiExternalPlayer != null)
            {
                // There is two methods to trigger event: 
                //      1) in inpector from the Unity editor 
                //      2) by script (recommended method), see below
                // ------------------------------------------

                // Event triggered when MIDI starts playing
                Debug.Log("OnEventStartPlayMidi defined by script");
                midiExternalPlayer.OnEventStartPlayMidi.AddListener(StartPlay);

                // Event triggered when MIDI ends playing
                Debug.Log("OnEventEndPlayMidi defined by script");
                midiExternalPlayer.OnEventEndPlayMidi.AddListener(EndPlay);

                // Event triggered when a group of MIDI events is ready to be played
                Debug.Log("OnEventNotesMidi defined by script");
                midiExternalPlayer.OnEventNotesMidi.AddListener(ReadNotes);
            }

            RunningStatus.text = "";
            ErrorStatus.text = "";
            EventStatus.text = "";

            BtSetPositionBy.onClick.AddListener(() =>
            {
                // Define the slider unity display: by ticks or by measure/bar
                modeSetPosition++;
                if (modeSetPosition > 1) modeSetPosition = 0;
                switch (modeSetPosition)
                {
                    case 0: BtSetPositionBy.GetComponentInChildren<Text>().text = "Tick"; break;
                    case 1: BtSetPositionBy.GetComponentInChildren<Text>().text = "Measure"; break;
                        // TBD case 2: BtSetPositionBy.GetComponentInChildren<Text>().text = "Quarter"; break;
                }
                SetSliderMaxValue();

            });

            // Play the MIDI
            BtPlay.onClick.AddListener(() =>
            {
                if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
                    midiExternalPlayer.MPTK_Play();
            });

            // Replay the MIDI
            BtReplay.onClick.AddListener(() =>
            {
                if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
                    midiExternalPlayer.MPTK_Play(alreadyLoaded: true);
            });

            // Pause or Unpause
            BtPause.onClick.AddListener(() =>
            {
                if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
                    if (midiExternalPlayer.MPTK_IsPaused)
                        midiExternalPlayer.MPTK_UnPause();
                    else
                        midiExternalPlayer.MPTK_Pause();
            });

            // Stop playing the MIDI
            BtStop.onClick.AddListener(() =>
            {
                if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
                    midiExternalPlayer.MPTK_Stop();
            });

            // Enable inner loop ?
            TglEnableInnerLoop.onValueChanged.AddListener((enable) => midiExternalPlayer.MPTK_InnerLoop.Enabled = enable);


            // Set Start position when MIDI starts for the inner loop feature.
            SldStartLoopFrom.onValueChanged.AddListener((value) =>
            {
                // Display the text associated with this slider (start position)
                TxtStartLoopFrom.text = value.ToString();

                long pos = (long)value;

                // Default mode is slider by ticks
                long tick = pos;

                if (modeSetPosition == 1)
                    // Mode looping by measure/bar, convert bar to tick. InnerLoop works only with tick position.
                    // When a MIDI is loaded, the signature map is created.
                    // Generally there is no signature (default 4/4) or one signature in a MIDI. Calculation 
                    // Obviously, changing the signature along the MIDI will affect the calculation of the tick position from a bar. 
                    // The calculations become complex when there are two or more MIDI signatures, but MPTK is there to help.
                    // Just to remind you, MIDI time positions can only be expressed in ticks. So we need to make this conversion.
                    tick = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)pos);

                // Set the inner loop start attribute
                midiExternalPlayer.MPTK_InnerLoop.Start = tick;

                // Avoid Resume position bellow Start position
                if (pos > SldResumeLoopAt.value)
                    SldResumeLoopAt.value = pos;
            });

            // Set Resume position where the MIDI need to return when position is over the End position
            SldResumeLoopAt.onValueChanged.AddListener((value) =>
            {
                // Display the text associated with this slider
                TxtResumeLoopAt.text = value.ToString();

                long posSlider = (long)value;

                // Default mode is slider by ticks
                long tick = posSlider;

                if (modeSetPosition == 1)
                    // Mode looping by measure/bar, convert bar to tick. InnerLoop works only with tick position.
                    // When a MIDI is loaded, the signature map is created.
                    // Generally there is no signature (default 4/4) or one signature in a MIDI.
                    // Obviously, changing the signature along the MIDI will affect the calculation of the tick position from a bar. 
                    // Just to remind you, MIDI time positions can only be expressed in ticks. So we need to make this conversion.
                    tick = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)posSlider);

                // Resume must be >= to Start position
                if (posSlider >= SldStartLoopFrom.value)
                {
                    // OK, set the inner loop resume attribute
                    midiExternalPlayer.MPTK_InnerLoop.Resume = tick;

                    // Avoid End Loop position before or equal to resume position
                    if (posSlider >= SldEndLoopAt.value)
                    {
                        if (modeSetPosition == 0)
                            SldEndLoopAt.value = posSlider;
                        else
                            SldEndLoopAt.value++;
                    }
                }
                else
                    // Cancel Resume position change
                    SldResumeLoopAt.value = SldStartLoopFrom.value;

            });

            // Set End position where the MIDI need to return to the Resume position
            SldEndLoopAt.onValueChanged.AddListener((value) =>
            {
                // Display the text associated with this slider
                TxtEndLoopAt.text = value.ToString();

                long posSlider = (long)value;

                // Default mode is slider by ticks
                long tick = posSlider;

                if (modeSetPosition == 1)
                    // Mode looping by measure/bar, convert bar to tick. InnerLoop works only with tick position.
                    // When a MIDI is loaded, the signature map is created.
                    // Generally there is no signature (default 4/4) or one signature in a MIDI.
                    // Obviously, changing the signature along the MIDI will affect the calculation of the tick position from a bar. 
                    // Just to remind you, MIDI time positions can only be expressed in ticks. So we need to make this conversion.
                    tick = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)posSlider);

                // Avoid End position before or equal to Resume position
                if (posSlider > SldResumeLoopAt.value)
                    // OK, set the inner loop end attribute
                    midiExternalPlayer.MPTK_InnerLoop.End = tick;
                else
                {
                    // Cancel End position change
                    if (modeSetPosition == 0)
                        SldEndLoopAt.value = SldResumeLoopAt.value;
                    else
                        SldEndLoopAt.value++;
                }
            });

            // Change current playing position
            SldPlayingPosition.onValueChanged.AddListener((position) =>
            {
                // Display the text associated with this slider
                // see in update()

                long posSlider = (long)position;

                // Default mode is slider by ticks
                long tick = posSlider;

                if (modeSetPosition == 1)
                    // Mode looping by measure/bar, convert bar to tick. InnerLoop works only with tick position.
                    // When a MIDI is loaded, the signature map is created.
                    // Generally there is no signature (default 4/4) or one signature in a MIDI.
                    // Obviously, changing the signature along the MIDI will affect the calculation of the tick position from a bar. 
                    // Just to remind you, MIDI time positions can only be expressed in ticks. So we need to make this conversion.
                    tick = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)posSlider);

                //Debug.Log($"SldPlayingPosition.onValueChanged {tick}");
                if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
                    midiExternalPlayer.MPTK_TickCurrent = tick;

            });


            // Make the roulette rotate (just for fun, no really useful for MIDI!). The MIDI is a random MIDI number from midiworld site.
            BtPlayRoulette.onClick.AddListener(() =>
            {
                InfoMidiLyric.text = "Lyric\n";
                InfoMidiCopyright.text = "Copyright\n";
                InfoMidiTrack.text = "Track\n";
                EventStatus.text = "";
                speedRoulette = 50f;
                string uri = $"https://www.midiworld.com/download/{UnityEngine.Random.Range(1, 5172)}";
                // Display url for information
                UrlMidi.text = uri;
                Debug.Log("Play from script:" + uri);
                // Stop current Midi and play the uri
                midiExternalPlayer.MPTK_Stop();
                midiExternalPlayer.MPTK_MidiName = uri;
                midiExternalPlayer.MPTK_Play();

                // Check result of loading Midi during 2 seconds
                StartCoroutine(DoCheck());
            });
        }

        /// <summary>@brief
        /// This call can be defined from MidiPlayerGlobal event inspector. Run when SF is loaded.
        /// Warning: not triggered at first load of MPTK because MidiPlayerGlobal id load before any other gamecomponent
        /// </summary>
        public void EndLoadingSF()
        {
            Debug.Log("End loading SF, MPTK is ready to play");
            Debug.Log("Load statistique");
            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        /// <summary>@brief
        /// Event fired by MidiExternalPlayer when a midi is loaded, just before start it playing.
        /// It's the good moment to define settings that need a MIDI loaded to be applied..
        /// (set by Unity Editor in MidiFilePlayer Inspector or by script, see above)
        /// </summary>
        public void StartPlay(string midiName)
        {
            Debug.Log("Start Midi " + midiName + " Duration: " + midiExternalPlayer.MPTK_Duration.TotalSeconds + " seconds");

            // When a MIDI file is loaded, the maximum value for tick or bar must be updated in the UI slider.
            SetSliderMaxValue();

            // If inner loop is enabled at start, set the MPTK inner loop properties from the UI
            if (TglEnableInnerLoop.isOn)
            {
                midiExternalPlayer.MPTK_InnerLoop.Enabled = true;
                midiExternalPlayer.MPTK_InnerLoop.Log = true;
                if (modeSetPosition == 0)
                {
                    // Slider position in tick
                    midiExternalPlayer.MPTK_InnerLoop.Start = (long)SldStartLoopFrom.value;
                    midiExternalPlayer.MPTK_InnerLoop.Resume = (long)SldResumeLoopAt.value;
                    midiExternalPlayer.MPTK_InnerLoop.End = (long)SldEndLoopAt.value;
                }
                else if (modeSetPosition == 1)
                {
                    // Inner loop is using only tick value, convert from measure/bar to tick
                    // MPTKSignature.MeasureToTic internally uses the signature map created when a MIDI file is loaded and hides the complexity of this calculation.
                    // Usually there is no signature (default 4/4) or one signature in a MIDI file. The difficulty begins when the signature changes during MIDI execution.
                    // Just to remind you, MIDI time positions are only expressed in ticks (MPTK_InnerLoop attributes including). So we need to make this conversion.
                    midiExternalPlayer.MPTK_InnerLoop.Start = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)SldStartLoopFrom.value);
                    midiExternalPlayer.MPTK_InnerLoop.Resume = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)SldResumeLoopAt.value);
                    midiExternalPlayer.MPTK_InnerLoop.End = MPTKSignature.MeasureToTick(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, (int)SldEndLoopAt.value);
                }
            }
            else
                midiExternalPlayer.MPTK_InnerLoop.Enabled = false;


            // Uncomment to test changing other attributes that require a loaded MIDI.
            // midiExternalPlayer.MPTK_Speed = 1.5f;
            // midiExternalPlayer.MPTK_Transpose = 2;
        }

        /// <summary>
        /// When a MIDI file is loaded, the maximum value for tick or bar must be updated in the slider.
        /// See StartPlay() witch is run when a MIDI is loaded.
        /// </summary>
        private void SetSliderMaxValue()
        {
            long max = 0;
            if (modeSetPosition == 0)
            {
                max = midiExternalPlayer.MPTK_TickLast;
            }
            else if (modeSetPosition == 1)
            {
                max = midiExternalPlayer.MPTK_MidiLoaded.MPTK_MeasureLastNote;
            }
            else if (modeSetPosition == 2)
            {
                // TBD
            }

            //Debug.Log($"max: {max}");
            SldPlayingPosition.maxValue = max;
            SldStartLoopFrom.maxValue = max;
            SldResumeLoopAt.maxValue = max;
            SldEndLoopAt.maxValue = max;
        }

        /// <summary>@brief
        /// Event fired by MidiExternalPlayer when a midi is ended, or stop by MPTK_Stop, or Replay with MPTK_Replay, or detected when loading
        /// The reason parameter specifies the reason for the end:  MidiEnd, ApiStop, Replay, Next, Previous, MidiErr, Loop.
        /// </summary>
        public void EndPlay(string midiname, EventEndMidiEnum reason)
        {
            if (reason == EventEndMidiEnum.MidiErr)
                Debug.LogFormat($"End Play: Error loading midi {midiname}");
            else
                Debug.LogFormat($"End playing midi {midiname} reason:{reason}");
            //EventStatus.text = "Event end playing, reason:" + reason + " status:" + midiExternalPlayer.MPTK_StatusLastMidiLoaded;
        }

        /// <summary>@brief
        /// Event fired by the MIDI player when a group of MIDI events are available for playing.
        /// (set by Unity Editor in MidiFilePlayer Inspector or by script, see above)
        /// </summary>
        public void ReadNotes(List<MPTKEvent> events)
        {
            foreach (MPTKEvent midiEvent in events)
            {
                if (TglEnableLog.isOn)
                    // Want to see the event in the Unity log?
                    Debug.Log(midiEvent.ToString());

                if (midiEvent.Command == MPTKCommand.MetaEvent)
                {
                    // This is a good time to display the meta text information in the UI, including the karaoke text.
                    // -----------------------------------------------------------------------------------------------
                    switch (midiEvent.Meta)
                    {
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
                            }
                            InfoMidiLyric.text += text + "\n";
                            break;

                        case MPTKMeta.TextEvent:
                        case MPTKMeta.Copyright:
                            InfoMidiCopyright.text += midiEvent.Info + "\n";
                            break;

                        case MPTKMeta.SequenceTrackName:
                            InfoMidiTrack.text += "Track: " + midiEvent.Track + " '" + midiEvent.Info + "'\n";
                            break;
                    }
                }
            }
        }


        // Unity loop
        private void Update()
        {
            if (speedRoulette > 0f)
            {
                // Make speed roulette going down
                BtPlayRoulette.transform.Rotate(Vector3.forward, -speedRoulette);
                speedRoulette = Mathf.SmoothDamp(speedRoulette, 0f, ref currentVelocity, 0.5f);
            }

            if (midiExternalPlayer != null && midiExternalPlayer.MPTK_MidiLoaded != null)
            {
                TxtCountLoop.text = $"Count: {midiExternalPlayer.MPTK_InnerLoop.Count}";

                // Animate the MIDI position slider
                if (modeSetPosition == 0)
                {
                    // By tick
                    TxtPlayingPosition.text = $"{midiExternalPlayer.MPTK_TickCurrent} / {midiExternalPlayer.MPTK_TickLast}";
                    SldPlayingPosition.SetValueWithoutNotify(midiExternalPlayer.MPTK_TickCurrent);
                }
                else if (modeSetPosition == 1)
                {
                    // By measure/bar. When display by bar, convert current MIDI tick to bar.
                    // When a MIDI is loaded, the signature map is created.
                    // Generally there is no signature (default 4/4) or one signature in a MIDI.
                    // Obviously, changing the signature along the MIDI will affect the calculation of the bar from the tick position. 
                    // Just to remind you, MIDI time positions can only be expressed in ticks. So we need to make this conversion.

                    // Find the MIDI segment related to the current tick.
                    int index = MPTKSignature.FindSegment(midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap, midiExternalPlayer.MPTK_TickCurrent);

                    // Calculate the bar position  from the signature segment and the current tick.
                    int measure = midiExternalPlayer.MPTK_MidiLoaded.MPTK_SignMap[index].TickToMeasure(midiExternalPlayer.MPTK_TickCurrent);

                    // Display in UI.
                    TxtPlayingPosition.text = $"{measure} / {midiExternalPlayer.MPTK_MidiLoaded.MPTK_MeasureLastNote}";
                    SldPlayingPosition.SetValueWithoutNotify(measure);
                }
                else if (modeSetPosition == 2)
                {
                    // TBD
                }


                // Update the text of the UI element RunningStatus, EventStatus, ErrorStatus
                // -------------------------------------------------------------------------

                // Update the text of the UI element RunningStatus
                if (midiExternalPlayer.MPTK_IsPaused)
                    RunningStatus.text = "Paused";
                else if (midiExternalPlayer.MPTK_IsPlaying)
                    RunningStatus.text = "Playing";
                else
                    RunningStatus.text = "Stop";

                // Update the text of the UI element EventStatus
                if (!string.IsNullOrEmpty(midiExternalPlayer.MPTK_WebRequestError))
                {
                    Debug.Log("MPTK_WebRequestError:" + midiExternalPlayer.MPTK_WebRequestError);
                    EventStatus.text = midiExternalPlayer.MPTK_WebRequestError;
                    midiExternalPlayer.MPTK_WebRequestError = null;
                }

                // Update the text of the UI element ErrorStatus
                switch (midiExternalPlayer.MPTK_StatusLastMidiLoaded)
                {
                    case LoadingStatusMidiEnum.NotYetDefined:
                    case LoadingStatusMidiEnum.Success: ErrorStatus.text = "MIDI Loaded"; break;
                    case LoadingStatusMidiEnum.NotFound: ErrorStatus.text = "MIDI not found"; break;
                    case LoadingStatusMidiEnum.TooShortSize: ErrorStatus.text = "Not a MIDI file, too short size"; break;
                    case LoadingStatusMidiEnum.NoMThdSignature: ErrorStatus.text = "Not a MIDI file, signature MThd not found"; break;
                    case LoadingStatusMidiEnum.NetworkError: ErrorStatus.text = "Network error or site not found"; break;
                    case LoadingStatusMidiEnum.MidiFileInvalid: ErrorStatus.text = "Error loading MIDI file"; break;
                    default: ErrorStatus.text = "Error Unknown"; break;
                }
            }
        }

        /// <summary>
        /// Call directly by the UI with an URL as parameters
        /// See in the hierarchy: Canvas/PanelGoToSite.
        /// </summary>
        /// <param name="uri"></param>
        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }

        /// <summary>@brief
        /// This method is fired from button (with predefined URI) or inputfield UI in the screen.
        /// See in the hierarchy: 
        ///     Canvas/PanelV/PanelMidiActionH/PanelRouletteandURLH/PanelButtonPredefined/PanelPredefinedPath/OnClick().
        /// </summary>
        /// <param name="uri">uri or path to the midi file</param>
        public void Play(string uri)
        {
            RunningStatus.text = "";
            ErrorStatus.text = "";
            EventStatus.text = "";
            UrlMidi.text = uri;
            InfoMidiLyric.text = "Lyric\n";
            InfoMidiCopyright.text = "Copyright\n";
            InfoMidiTrack.text = "Track\n";

            Debug.Log($"Play from script:{uri}");

            // Stop current playing
            midiExternalPlayer.MPTK_Stop();

            if (uri.ToLower().StartsWith("file://") ||
                uri.ToLower().StartsWith("http://") ||
                uri.ToLower().StartsWith("https://"))
            {
                // try to load from an URI (file:// or http://)
                midiExternalPlayer.MPTK_MidiName = uri;
                midiExternalPlayer.MPTK_Play();
            }
            else
            {
                // try to load a byte array and play
                // example with uri= C:\Users\xxx\Midi\DreamOn.mid
                try
                {
                    using (Stream fsMidi = new FileStream(uri, FileMode.Open, FileAccess.Read))
                    {
                        byte[] data = new byte[fsMidi.Length];
                        fsMidi.Read(data, 0, (int)fsMidi.Length);
                        midiExternalPlayer.MPTK_Play(data);
                    }
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                    return;
                }
            }
            StartCoroutine(DoCheck());
        }

        /// <summary>@brief
        /// Check result of loading Midi during 2 seconds. Other method to check: in the update loop or with events.
        /// </summary>
        /// <returns></returns>
        IEnumerator DoCheck()
        {
            // Wait Midi is read or an error is detected
            int maxStep = 20;
            while (midiExternalPlayer.MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined && maxStep > 0)
            {
                yield return new WaitForSeconds(.1f);
                maxStep--;
            }
        }
    }
}