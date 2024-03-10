using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MPTKDemoEuclidean
{
    public class TestEuclideanRhythme : MonoBehaviour
    {
        public const int MaxStep = 32;
        public static float ScrollviewScreenHeight;

        public static bool IsPlaying;

        public Dropdown ComboSelectBank;

        /// <summary>@brief
        /// Tempo defined in BPM (beat per minut)
        /// The ‘beat’ is determined by the time signature of the piece, so 100 BPM in 4/4 equates to 100 quarter notes in one minute.
        ///  60 BPM means 60 beats in each minute, or 1 beat per second.
        /// 120 BPM would be twice as fast: 120 beats in each minute or 2 per second.
        /// </summary>
        public TextSlider SldTempo;
        public TextSlider SldVolume;
        public Text TxtVersion;
        public Text TxtInfo;
        public Button BtNextStat;
        private int currentStat;

        public RectTransform ScrolView;

        /// <summary>@brief
        /// Humanizing: a little random on each hit and volume. Between 0 and 100.
        /// </summary>
        public TextSlider SldHumanize;

        /// <summary>@brief
        /// Level of humanize 0:nothing, 1:full
        /// </summary>
        public static float PctHumanize;
        public static System.Random RndHumanize = new System.Random();

        public RectTransform ContentScroller;
        public RectTransform ContentPopupMessage;
        public RectTransform PanelAbout;
        public Button BtPlay;
        public PanelController templateController;

        public Button BtPrevious;
        public Button BtMidi;
        public Button BtNext;
        public MidiStreamPlayer midiStreamPlayer;
        public MidiFilePlayer midiFilePlayer;

        public PopupListBox TemplateListBox;
        public static PopupListBox PopupListInstrument;
        public static PopupListBox PopupListDrum;
        public static PopupListBox PopupListEffect;

        /// <summary>@brief
        /// ms
        /// </summary>
        public double lastSynthTime;

        /// <summary>@brief
        /// ms
        /// </summary>
        public double timeMidiFromStartPlay;

        /// <summary>@brief
        /// ms
        /// </summary>
        public double timeSinceLastBeat;

        public static float GlobalVolume;

        /// <summary>@brief
        /// Calculated from the BPM: delay in millisecond betweeh two beat (two quarter in 4/4)
        /// </summary>
        public static double CurrentTempo;

        public static int CurrentBeat;

        bool disableSoloControllerchange = false;

        private List<PanelController> Controllers;
        static List<string> Drums;

        private System.Diagnostics.Stopwatch watchOnAudioFilterRead = new System.Diagnostics.Stopwatch();
        Thread midiThread;
        bool threadMode = true;

        void Start()
        {

            Input.simulateMouseWithTouches = true;
            TxtInfo.text = "";

            // Need MidiStreamPlayer to play note in real time
            midiStreamPlayer = FindObjectOfType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
                Debug.LogWarning("Can't find a MidiStreamPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");

            // Need MidiStreamPlayer to play note in real time
            midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");

            PopupListEffect = TemplateListBox.Create("Select an Effect");
            foreach (MPTKListItem effectItem in GenModifier.RealTimeGenerator)
                PopupListEffect.AddItem(effectItem);

            PopupListInstrument = TemplateListBox.Create("Select an Instrument");
            foreach (MPTKListItem presetItem in MidiPlayerTK.MidiPlayerGlobal.MPTK_ListPreset)
                PopupListInstrument.AddItem(presetItem);

            // Unluckily this list is not available in the SoundFont itself, so we are using a default list from the GM/GS standard
            if (Drums == null)
                Drums = new List<string>()
                {
                    "35 - Acoustic Bass Drum",
                    "36 - Bass Drum 1",
                    "37 - Side Stick/Rimshot",
                    "38 - Acoustic Snare",
                    "39 - Hand Clap",
                    "40 - Electric Snare",
                    "41 - Low Floor Tom",
                    "42 - Closed Hi-hat",
                    "43 - High Floor Tom",
                    "44 - Pedal Hi-hat",
                    "45 - Low Tom",
                    "46 - Open Hi-hat",
                    "47 - Low-Mid Tom",
                    "48 - Hi-Mid Tom",
                    "49 - Crash Cymbal 1",
                    "50 - High Tom",
                    "51 - Ride Cymbal 1",
                    "52 - Chinese Cymbal",
                    "53 - Ride Bell",
                    "54 - Tambourine",
                    "55 - Splash Cymbal",
                    "56 - Cowbell",
                    "57 - Crash Cymbal 2",
                    "58 - Vibra Slap",
                    "59 - Ride Cymbal 2",
                    "60 - High Bongo",
                    "61 - Low Bongo",
                    "62 - Mute High Conga",
                    "63 - Open High Conga",
                    "64 - Low Conga",
                    "65 - High Timbale",
                    "66 - Low Timbale",
                    "67 - High Agogô",
                    "68 - Low Agogô",
                    "69 - Cabasa",
                    "70 - Maracas",
                    "71 - Short Whistle",
                    "72 - Long Whistle",
                    "73 - Short Güiro",
                    "74 - Long Güiro",
                    "75 - Claves",
                    "76 - High Wood Block",
                    "77 - Low Wood Block",
                    "78 - Mute Cuíca",
                    "79 - Open Cuíca",
                    "80 - Mute Triangle",
                    "81 - Open Triangle"
                };
            PopupListDrum = TemplateListBox.Create("Select a Drum");
            int startIndex = 35;
            int positionIndex = 0;
            foreach (string label in Drums)
                PopupListDrum.AddItem(new MPTKListItem() { Index = startIndex++, Label = label, Position = positionIndex++ });


            SldTempo.Value = 120; // by default 2 beat per second
            SldVolume.Value = 100;
            TxtVersion.text = "Version 1.4";

            // List of controller to be played. 
            Controllers = new List<PanelController>();

            // The first controller is used as a template for the others, it is disabled and will be never played.m
            templateController.gameObject.SetActive(false);

            BtPlay.onClick.AddListener(() =>
            {
                IsPlaying = !IsPlaying;
                SetLabelBtPlay();
                Play();
            });

            BtNextStat.onClick.AddListener(() =>
            {
                currentStat++;
                if (currentStat >= 3) currentStat = 0;
            });

            BtPrevious.onClick.AddListener(() =>
            {
                midiFilePlayer.MPTK_Previous();
                SetLabelBtMidi();
            });

            BtMidi.onClick.AddListener(() =>
            {
                if (!midiFilePlayer.MPTK_IsPlaying)
                    midiFilePlayer.MPTK_Play();
                else
                    midiFilePlayer.MPTK_Stop();
                SetLabelBtMidi();
            });

            BtNext.onClick.AddListener(() =>
            {
                midiFilePlayer.MPTK_Next();
                SetLabelBtMidi();
            });

            // Build the list of presets associated to the drum bank (need a list of string for Dropdown)
            ComboSelectBank.ClearOptions();
            List<String> patchsDrum = new List<string>();
            foreach (MPTKListItem item in MidiPlayerGlobal.MPTK_ListPresetDrum)
                patchsDrum.Add(item.Label);
            ComboSelectBank.AddOptions(patchsDrum);

            ComboSelectBank.onValueChanged.AddListener((int iCombo) =>
            {
                if (iCombo >= 0 && iCombo < MidiPlayerGlobal.MPTK_ListPresetDrum.Count)
                {
                    // Change preset on channel 9 (channel for drums). We need the index of the preset.

                    // Before v2.10.1
                    // midiStreamPlayer.MPTK_ChannelPresetChange(9, MidiPlayerGlobal.MPTK_ListPresetDrum[iCombo].Index);
                    // From v2.10.1
                    midiFilePlayer.MPTK_Channels[9].PresetNum = MidiPlayerGlobal.MPTK_ListPresetDrum[iCombo].Index;
                }
            });
            ComboSelectBank.value = 0;

            Play();
            Application.wantsToQuit += Application_wantsToQuit;
        }

        private bool Application_wantsToQuit()
        {
            IsPlaying = false;
            return true;
        }

        void SetLabelBtPlay()
        {
            BtPlay.GetComponentInChildren<Text>().text = IsPlaying ? "Stop" : "Play";
        }

        void SetLabelBtMidi()
        {
            BtMidi.GetComponentInChildren<Text>().text = midiFilePlayer.MPTK_IsPlaying ? "Playing " + midiFilePlayer.MPTK_MidiName : "Play Midi";
        }

        /// <summary>@brief
        /// Add a controller from the GUI
        /// </summary>
        /// <param name="smode"></param>
        public void AddController(string smode)
        {
            PanelController.Mode mode = smode != "EuclideDrums" ? PanelController.Mode.PlayerDrums : PanelController.Mode.EuclideDrums;
            switch (smode)
            {
                case "EuclideDrums": mode = PanelController.Mode.EuclideDrums; break;
                case "PlayerDrums": mode = PanelController.Mode.PlayerDrums; break;
                case "PlayerInstr": mode = PanelController.Mode.PlayerInstrument; break;
            }
            PanelController controler = CreateController(mode);
            controler.ToBeRandomsed = true;
            Controllers.Add(controler);
            // Resize the content of the scroller to reflect the position of the scroll bar (100=height of PanelController + space)
            ContentScroller.sizeDelta = new Vector2(ContentScroller.sizeDelta.x, Controllers.Count * ((RectTransform)controler.transform).sizeDelta.y + ContentPopupMessage.sizeDelta.y);
            ContentPopupMessage.SetAsLastSibling();
            ContentScroller.ForceUpdateRectTransforms();
            //Debug.Log($"AddController {mode} {Controlers.Count} { ContentScroller.sizeDelta}");
        }


        public PanelController CreateController(PanelController.Mode mode)
        {
            PanelController controler = Instantiate<PanelController>(templateController);
            controler.IsReady = false;
            controler.PlayMode = mode;
            controler.midiStream = midiStreamPlayer;
            switch (controler.PlayMode)
            {
                case PanelController.Mode.EuclideDrums:
                    controler.PanelTapDrum.SetActive(false);
                    controler.PanelDrumSample.SetActive(true);
                    controler.PanelTapSustain.SetActive(false);
                    controler.PanelRTEffect.SetActive(true);
                    controler.PanelEuclidean.SetActive(true);
                    controler.PanelView.SetActive(true);
                    controler.Channel = 9;
                    break;

                case PanelController.Mode.PlayerDrums:
                    controler.PanelTapDrum.SetActive(true);
                    controler.PanelDrumSample.SetActive(true);
                    controler.PanelTapSustain.SetActive(false);
                    controler.PanelRTEffect.SetActive(true);
                    controler.PanelEuclidean.SetActive(false);
                    controler.PanelView.SetActive(false);
                    controler.Channel = 9;
                    break;
                case PanelController.Mode.PlayerInstrument:
                    controler.PanelTapDrum.SetActive(true);
                    controler.PanelDrumSample.SetActive(true);
                    controler.PanelTapSustain.SetActive(true);
                    controler.PanelRTEffect.SetActive(true);
                    controler.PanelEuclidean.SetActive(false);
                    controler.PanelView.SetActive(false);
                    controler.Channel = 0;
                    break;
            }
            controler.name = $"Controller {Controllers.Count,2:00}";
            controler.transform.position = templateController.transform.position;
            controler.transform.SetParent(templateController.transform.parent);
            // changing parent can affect scale, reset to 1
            controler.transform.localScale = new Vector3(1, 1, 1);
            controler.gameObject.SetActive(true);
            return controler;
        }

        public void UpController(PanelController ctrl)
        {
            MoveController(ctrl, -1);
        }

        public void DownController(PanelController ctrl)
        {
            MoveController(ctrl, 1);
        }
        private void MoveController(PanelController ctrl, int move)
        {
            //Debug.Log($"avant {ctrl.transform.GetSiblingIndex()}");
            if (ctrl.transform.GetSiblingIndex() > 1 || move > 0)
                ctrl.transform.SetSiblingIndex(ctrl.transform.GetSiblingIndex() + move);
            ContentPopupMessage.SetAsLastSibling();
            //Debug.Log($"apres {ctrl.name} {ctrl.transform.GetSiblingIndex()}");
        }

        public void SoloController(PanelController ctrl)
        {
            if (!disableSoloControllerchange)
            {
                disableSoloControllerchange = true;
                //Debug.Log($"from {ctrl.name} mute:{ctrl.Mute.isOn} solo:{ctrl.Solo.isOn}");
                if (ctrl != null)
                {
                    foreach (PanelController controller in Controllers)
                    {
                        if (ctrl.Solo.isOn)
                        {
                            // Mute all but not current
                            if (controller == ctrl)
                                controller.Mute.isOn = false;
                            else
                                controller.Mute.isOn = true;
                        }
                        else
                        {
                            // Remove Solo, unmute all
                            controller.Mute.isOn = false;
                        }

                        // The Toggles Solo are exclusive between controller
                        if (controller != ctrl && controller.Solo.isOn)
                            // This action will trigger a new SoloController call, 
                            // disableSoloControllerchange is used to disble the processing of this event.
                            controller.Solo.isOn = false;
                    }
                }
                disableSoloControllerchange = false;
            }
        }

        /// <summary>@brief
        /// Play or stop playing.
        /// Set the PlayHits function to process midi generated music at each audio frame
        /// </summary>
        /// 
        public void Play()
        {
            lastSynthTime = 0f;
            timeMidiFromStartPlay = 0d;
            timeSinceLastBeat = 999999d; // start with a first beat
            CurrentBeat = -1; // start with a first beat

            if (threadMode && midiThread == null)
            {
                //Debug.Log($"thread start");
                midiThread = new Thread(ThreadPlayHits);
            }

            //Debug.Log($"{IsPlaying}");

            if (IsPlaying)
            {
                if (threadMode)
                {
                    watchOnAudioFilterRead.Reset();
                    watchOnAudioFilterRead.Start();
                    midiThread.Start();
                }
                else
                    midiStreamPlayer.OnAudioFrameStart += SynthPlayHits;
            }
            else
            {
                if (threadMode)
                {
                    watchOnAudioFilterRead.Stop();
                    //midiThread.Stop();
                }
                else
                    midiStreamPlayer.OnAudioFrameStart -= SynthPlayHits;
            }
        }

        /// <summary>@brief
        /// This function will be called at each audio frame.
        /// The frequency depends on the buffer size and the synth rate (see inspector of the MidiStreamPlayer prefab)
        /// Recommended values: Freq=48000 Buffer Size=1024 --> call every 11 ms with a high accuracy.
        /// You can't call Unity API in this function (only Debug.Log) but the most part of MPTK API are available.
        /// For example : MPTK_PlayDirectEvent or MPTK_PlayEvent to play music note from MPTKEvent (see PlayEuclideanRhythme)
        /// </summary>
        /// <param name="synthTimeMS"></param>
        private void SynthPlayHits(double synthTimeMS)
        {
            //Debug.Log($"{synthTimeMS:F0}");

            if (lastSynthTime <= 0d)
            {
                // First call, init the last time
                lastSynthTime = synthTimeMS;
            }

            // Calculate time in millisecond since the last loop
            double deltaTime = synthTimeMS - lastSynthTime;
            lastSynthTime = synthTimeMS;
            timeMidiFromStartPlay += deltaTime;

            // Calculate time since last beat played
            timeSinceLastBeat += deltaTime;

            /// SldTempo in BPM.
            ///  60 BPM means 60 beats in each minute, 1 beat per second, 1000 ms between beat.
            /// 120 BPM would be twice as fast: 120 beats in each minute, 2 per second, 500 ms between beat.
            /// Calculate the delay between two quarter notes in millisecond
            CurrentTempo = (60d / (double)SldTempo.Value) * 1000d;

            // Is it time to play a hit ?
            if (IsPlaying && timeSinceLastBeat >= CurrentTempo)
            {
                //Debug.Log($"{synthMidiMS:F0} {midiStream.StatDeltaAudioFilterReadMS:F2} {deltaTime:F2} {timeSinceLastBeat:F2}");
                timeSinceLastBeat = 0d;

                // could overflow after 273 days (frequency 11 ms) ;-))) 
                if (CurrentBeat >= int.MaxValue - 1) CurrentBeat = 0;

                CurrentBeat++;
                //   lock (this)
                {
                    for (int c = 0; c < Controllers.Count; c++)
                    {
                        if (Controllers[c].PlayMode == PanelController.Mode.EuclideDrums)
                            Controllers[c].PlayEuclideanRhythme();
                    }
                }
            }
        }

        /// <summary>@brief
        /// This function will be called at each audio frame.
        /// The frequency depends on the buffer size and the synth rate (see inspector of the MidiStreamPlayer prefab)
        /// Recommended values: Freq=48000 Buffer Size=1024 --> call every 11 ms with a high accuracy.
        /// You can't call Unity API in this function (only Debug.Log) but the most part of MPTK API are available.
        /// For example : MPTK_PlayDirectEvent or MPTK_PlayEvent to play music note from MPTKEvent (see PlayEuclideanRhythme)
        /// </summary>
        /// <param name="synthTimeMS"></param>
        private void ThreadPlayHits()
        {
            while (IsPlaying)
            {
                double timeMS = ((double)watchOnAudioFilterRead.ElapsedTicks) / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d);
                //Debug.Log($"{timeMS:F0}");

                if (lastSynthTime <= 0d)
                {
                    // First call, init the last time
                    lastSynthTime = timeMS;
                }
                //Debug.Log($"{timeMS:F0}");

                // Calculate time in millisecond since the last loop
                double deltaTime = timeMS - lastSynthTime;
                lastSynthTime = timeMS;
                timeMidiFromStartPlay += deltaTime;

                // Calculate time since last beat played
                timeSinceLastBeat += deltaTime;

                /// SldTempo in BPM.
                ///  60 BPM means 60 beats in each minute, 1 beat per second, 1000 ms between beat.
                /// 120 BPM would be twice as fast: 120 beats in each minute, 2 per second, 500 ms between beat.
                /// Calculate the delay between two quarter notes in millisecond
                CurrentTempo = (60d / (double)SldTempo.Value) * 1000d;

                // Is it time to play a hit ?
                if (IsPlaying && timeSinceLastBeat >= CurrentTempo)
                {
                    //Debug.Log($"{synthMidiMS:F0} {midiStream.StatDeltaAudioFilterReadMS:F2} {deltaTime:F2} {timeSinceLastBeat:F2}");
                    timeSinceLastBeat = 0d;

                    // could overflow after 273 days (frequency 11 ms) ;-))) 
                    if (CurrentBeat >= int.MaxValue - 1) CurrentBeat = 0;

                    CurrentBeat++;
                    //   lock (this)
                    {
                        for (int c = 0; c < Controllers.Count; c++)
                        {
                            if (Controllers[c].PlayMode == PanelController.Mode.EuclideDrums)
                                Controllers[c].PlayEuclideanRhythme();
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void Update()
        {
            // Search for each controller in case of multiple controller must be deleted (quite impossible!)
            // Use a for loop in place a foreach because removing an element in the list change the list and foreach loop don't like this ...
            if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
            {
                Debug.LogWarning(MidiPlayerGlobal.ErrorNoSoundFont);
            }
            else
            //   lock (this)
            {
                PctHumanize = SldHumanize.Value / 100f;
                GlobalVolume = SldVolume.Value;
                //if (midiStream.StatSynthLatencyMIN < float.MaxValue)
                switch (currentStat)
                {
                    case 0:
                        // Stat player
                        TxtInfo.text = $"Controller count: {Controllers.Count}\nSpeed: {CurrentTempo:F0} ms Quarter:{CurrentBeat}";
                        break;

                    case 1:
                        // Stat voice
                        TxtInfo.text = $"Voice: {midiStreamPlayer.MPTK_StatVoiceCountActive}\nFree: {midiStreamPlayer.MPTK_StatVoiceCountFree}\nPlayed: {midiStreamPlayer.MPTK_StatVoicePlayed}";
                        break;
                    case 2:
                        // Stat synth
                        TxtInfo.text = $"Synth {midiStreamPlayer.IdSynth} DSP: {midiStreamPlayer.StatDspLoadAVG:F1} %\nDelta Frame: {midiStreamPlayer.StatDeltaAudioFilterReadMS:F2} ms\nRate: {AudioSettings.outputSampleRate} Hz Buffer: {midiStreamPlayer.AudioBufferLenght}";
                        break;
                }

                /*
                 * TxtInfo.text = $"T:{Controllers.Count} DSP:{midiStream.StatDspLoadAVG:F1} % Speed:{CurrentTempo:F1} ms"
                + $"\nLast:{midiStream.StatSynthLatencyLAST:F2} ms  Avg:{midiStream.StatSynthLatencyAVG:F2} ms"
                + $"\nVoice:{midiStream.MPTK_StatVoiceCountActive} Delta:{midiStream.StatDeltaAudioFilterReadMS:F2} ";
                 + $"\nUI:{midiStream.StatUILatencyLAST:F2} ms";
                 */

                // {midiStream.StatSynthLatencyMIN:00} {midiStream.StatSynthLatencyMAX:00}
                // Useful to hide 3D object as hit (Sphere) and arrow. The scrolview position is left bottom (0,0),
                // so we are testing only the Y position of the 3D object in the screen.
                ScrollviewScreenHeight = ScrolView.GetScreenRect(Camera.main).height;

                for (int indexController = 0; indexController < Controllers.Count;)
                {
                    PanelController controller = Controllers[indexController];

                    // Is controller to be removed from UI ?
                    if (controller.ToBeRemoved)
                    {
                        controller.StopAll();
                        DestroyImmediate(controller.gameObject);
                        Controllers.RemoveAt(indexController);
                    }
                    else
                    {
                        // Is controller to be duplicate from UI ?
                        if (controller.ToBeDuplicated)
                        {
                            controller.ToBeDuplicated = false;
                            PanelController duplicated = CreateController(controller.PlayMode);
                            duplicated.transform.SetSiblingIndex(controller.transform.GetSiblingIndex() + 1);
                            duplicated.DuplicateFrom = controller;
                            Controllers.Insert(indexController, duplicated);
                            // Resize the content of the scroller to reflect the position of the scroll bar (100=height of PanelController + space)
                            ContentScroller.sizeDelta = new Vector2(ContentScroller.sizeDelta.x, Controllers.Count * ((RectTransform)duplicated.transform).sizeDelta.y + ContentPopupMessage.sizeDelta.y);
                            ContentPopupMessage.SetAsLastSibling();
                            ContentScroller.ForceUpdateRectTransforms();
                            //Debug.Log($"AddController {mode} {Controlers.Count} { ContentScroller.sizeDelta}");
                            //DuplicateController(controler, indexController);
                        }

                        // Refresh display controller
                        controller.RefreshPanelView();

                        // Next controller
                        indexController++;
                    }
                }
            }
        }

        void OnDisable()
        {
            //midiStreamPlayer.OnAudioFrameStart -= PlayHits;
        }


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

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}