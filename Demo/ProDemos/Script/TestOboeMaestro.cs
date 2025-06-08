
// Some interesintg link for Oboe
// https://github.com/google/oboe/blob/main/docs/README.md
// https://github.com/google/oboe/wiki/AppsUsingOboe
//
using MidiPlayerTK;
#if UNITY_ANDROID && UNITY_OBOE
using Oboe.Stream;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MPTKDemoEuclidean
{
    // Look also at TestTapMidi.cs script
    // which process MIDI note-on from tap on the UI.

    public class TestOboeMaestro : MonoBehaviour
    {
        public Slider SliderVolume;
        public Slider SliderExtraVolume;
        public Text TxtVolume;

        public Text TxtTittle;
        public Text TxtVersion;
        public Text TxtInfo;
        public Text TxtLog;

        public Text TxtSpeed;
        public Button BtResetSpeed;
        public Slider SliderSpeed;

        public Text TxtPosition;
        public Button BtPrevious;
        public Button BtMidi;
        public Button BtNext;
        public Toggle TogLogEvent;

        public Button BtResetStat;
        public Button BtLogAudioInfo;
        public Button BtFreezeInfo;
        public Button BtHelp;

        public int PresetInstrument;
        PopupListBox popupInstrument;
        public Text TxtSelectedInstrument;

        public PopupListBox TemplateListBox;
        public static PopupListBox PopupListInstrument;

        public Dropdown ComboMidiList;
        public Slider SliderPositionMidi;

        public Dropdown ComboFrameRate;
        public Dropdown ComboBufferSize;
        public Toggle TogApplyFilter;
        public Slider SliderFilter;
        public Toggle TogApplyReverb;
        public Slider SliderReverb;
        public Toggle TogApplyChorus;
        public Slider SliderChorus;
        public Toggle TogApplyInterpol;
        public RectTransform RectTransformLog;
        public ScrollRect scrollRect;

        List<string> frameRate = new List<string> { "Default", "24000", "36000", "48000", "60000", "72000", "84000", "96000" };
        List<string> bufferSize = new List<string> { "64", "128", "256", "512", "1024", "2048" };
        List<string> midiList = new List<string>();

        public LinkedList<string> ListMessage = new LinkedList<string>();
        StringBuilder infoLog;
        bool logUpdated = false;
        bool InfoFreeze = false;

        // MidiFilePlayer prefab is found by script
        private MidiFilePlayer midiFilePlayer;

        public float lineHeight;

        //Called when there is an exception. Warning: UI can be called only from the UI thread
        void LogCallback(string logString, string stackTrace, LogType type)
        {
            if (!string.IsNullOrWhiteSpace(logString))
            {
                ListMessage.AddLast(ListMessage.Count.ToString() + " - " + DateTime.Now.ToString("HH:mm:ss") + " - " + logString);
                if (ListMessage.Count > 100)
                    ListMessage.RemoveFirst();
                logUpdated = true;
            }
        }
        private float CalculateLineHeight(Text text)
        {
            // Base line height with font size and line spacing
            float baseLineHeight = text.fontSize * text.lineSpacing;

            // Adjust for scaling (if any)
            float adjustedHeight = baseLineHeight * text.rectTransform.lossyScale.y;

            return adjustedHeight;
        }
        void Start()
        {
            //MidiSynth.TestAndroidSamples(8);
            //MidiSynth.TestAndroidSamples(56);
            //MidiSynth.TestAndroidSamples(100);
            //MidiSynth.TestAndroidSamples(128);
            //MidiSynth.TestAndroidSamples(256);
            //MidiSynth.TestAndroidSamples(500);
            //return;

            lineHeight = CalculateLineHeight(TxtLog);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
            Application.logMessageReceivedThreaded += LogCallback;
            Input.simulateMouseWithTouches = true;

            SliderVolume.onValueChanged.AddListener((float value) => { SetVolume(); }); // from 0 to 1
            SliderExtraVolume.onValueChanged.AddListener((float value) => { SetVolume(); }); // from 0 to 9
            void SetVolume()
            {
                midiFilePlayer.MPTK_Volume = SliderVolume.value + SliderExtraVolume.value;
            }

            SliderSpeed.onValueChanged.AddListener((float value) => { midiFilePlayer.MPTK_Speed = value; });
            BtResetSpeed.onClick.AddListener(() =>
            {
                SliderSpeed.value = 1f; 
            });

            // Create MIDI list for the MIDI player 
            // ------------------------------------
            MidiPlayerGlobal.MPTK_ListMidi.ForEach(item => midiList.Add(item.Label));
            ComboMidiList.ClearOptions();
            ComboMidiList.AddOptions(midiList);
            ComboMidiList.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    bool isPlaying = midiFilePlayer.MPTK_IsPlaying;
                    midiFilePlayer.MPTK_Stop();
                    midiFilePlayer.MPTK_MidiIndex = iCombo;
                    if (isPlaying) midiFilePlayer.MPTK_Play();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

            // MIDI player current and change position 
            // ---------------------------------------
            SliderPositionMidi.onValueChanged.AddListener((float pos) =>
            {
                if (midiFilePlayer.MPTK_IsPlaying)
                {
                    long lPos = (long)(midiFilePlayer.MPTK_TickLast * pos) / 100;
                    Debug.Log($"{pos} lPos:{lPos} midiFilePlayer.MPTK_TickCurrent:{midiFilePlayer.MPTK_TickCurrent}");
                    if (lPos != midiFilePlayer.MPTK_TickCurrent)
                        midiFilePlayer.MPTK_TickCurrent = lPos;
                }
            });

            // Change synth rate with a combo box
            // ----------------------------------
            ComboFrameRate.ClearOptions();
            ComboFrameRate.AddOptions(frameRate);
            ComboFrameRate.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    midiFilePlayer.MPTK_Stop();
                    // Changing synth rate will reinit the synth
                    Debug.Log($"ComboFrameRate change {iCombo}");
                    midiFilePlayer.MPTK_IndexSynthRate = iCombo - 1; //  -1:default, 0:24000, 1:36000, 2:48000, 3:60000, 4:72000, 5:84000, 6:96000
                    //midiFilePlayer.MPTK_Channels[0].PresetNum = PresetInstrument;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

            // Change buffer size with a combo box
            // -----------------------------------
            ComboBufferSize.ClearOptions();
            ComboBufferSize.AddOptions(bufferSize);
            ComboBufferSize.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    midiFilePlayer.MPTK_Stop();
                    // Changing buff size will reinit the synth
                    midiFilePlayer.MPTK_IndexSynthBuffSize = iCombo;
                    //midiFilePlayer.MPTK_Channels[0].PresetNum = PresetInstrument;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

            // Apply Effects
            TogApplyFilter.onValueChanged.AddListener((bool apply) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.EnableFilter = apply; });
            SliderFilter.onValueChanged.AddListener((float pos) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.FilterFreqOffset = pos; });

            TogApplyReverb.onValueChanged.AddListener((bool apply) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.EnableReverb = apply; });
            SliderReverb.onValueChanged.AddListener((float pos) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.ReverbAmplify = pos; });

            TogApplyChorus.onValueChanged.AddListener((bool apply) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.EnableChorus = apply; });
            SliderChorus.onValueChanged.AddListener((float pos) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.ChorusAmplify = pos; });

            TogApplyInterpol.onValueChanged.AddListener((bool apply) =>
            {
                if (midiFilePlayer.InterpolationMethod == fluid_interp.None)
                    midiFilePlayer.InterpolationMethod = fluid_interp.Linear;
                else
                    midiFilePlayer.InterpolationMethod = fluid_interp.None;
            });

            // Find MidiStreamPlayer (play note in real time or whole MIDI file)
            midiFilePlayer = FindFirstObjectByType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add to the scene with the Maestro menu.");
            else
            {
                // Not works because the MPTK synth is already started
                //midiFilePlayer.OnEventSynthStarted.AddListener((string synthName) =>
                //{
                //    Debug.Log($"OnEventSynthStarted {synthName}");
                //});


                // Popup to select an instrument for the note player
                // -------------------------------------------------
                PopupListInstrument = TemplateListBox.Create("Select an Instrument");
                foreach (MPTKListItem presetItem in MidiPlayerTK.MidiPlayerGlobal.MPTK_ListPreset)
                    PopupListInstrument.AddItem(presetItem);

                popupInstrument = PopupListInstrument;
                PresetInstrument = popupInstrument.FirstIndex();
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = popupInstrument.LabelSelected(PresetInstrument);

                TxtVersion.text = $"Version:{Application.version}    Unity:{Application.unityVersion}";
                TxtTittle.text = Application.productName;

                // MIDI Player
                // -----------
                BtPrevious.onClick.AddListener(() =>
                {
                    midiFilePlayer.MPTK_Previous();
                });

                BtMidi.onClick.AddListener(() =>
                {
                    if (!midiFilePlayer.MPTK_IsPlaying)
                        midiFilePlayer.MPTK_Play();
                    else
                        midiFilePlayer.MPTK_Stop();
                });

                BtNext.onClick.AddListener(() => { midiFilePlayer.MPTK_Next(); });
                BtResetStat.onClick.AddListener(() => { midiFilePlayer.MPTK_ResetStat(); ListMessage.Clear(); logUpdated = true; });
                BtLogAudioInfo.onClick.AddListener(() => { midiFilePlayer.LogInfoAudio(); });
                BtHelp.onClick.AddListener(() => { GotoWeb("https://paxstellar.fr/2024/11/23/deeper-dive-in-mptk/"); });
                TogLogEvent.isOn = midiFilePlayer.MPTK_LogEvents;
                TogLogEvent.onValueChanged.AddListener((bool value) => midiFilePlayer.MPTK_LogEvents = value);
            }

            Debug.Log("Defined synth settings");
            BtFreezeInfo.onClick.AddListener(() => { InfoFreeze = !InfoFreeze; });
            ComboFrameRate.value = 0; // 48000
            ComboBufferSize.value = 3;
            midiFilePlayer.MPTK_InitSynth();

        }

        /// <summary>
        /// call when BtSelectPreset is activated from the UI
        /// </summary>
        public void SelectPreset()
        {
            popupInstrument.OnEventSelect.AddListener((MPTKListItem item) =>
            {
                Debug.Log($"SelectPreset {item.Index} {item.Label}");
                PresetInstrument = item.Index;
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = item.Label;
                midiFilePlayer.MPTK_Channels[0].PresetNum = PresetInstrument;
            });

            popupInstrument.OnEventClose.AddListener(() =>
            {
                Debug.Log($"Close");
                popupInstrument.OnEventSelect.RemoveAllListeners();
                popupInstrument.OnEventClose.RemoveAllListeners();
            });

            popupInstrument.Select(PresetInstrument);
            popupInstrument.gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {

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
            {
                BtMidi.GetComponentInChildren<Text>().text = midiFilePlayer.MPTK_IsPlaying ? "Playing " : "Play Midi";
                if (ComboMidiList.value != midiFilePlayer.MPTK_MidiIndex)
                    ComboMidiList.value = midiFilePlayer.MPTK_MidiIndex;

                // Set current MIDI position if playing
                if (midiFilePlayer.MPTK_IsPlaying && midiFilePlayer.MPTK_TickLast > 0)
                    SliderPositionMidi.SetValueWithoutNotify(((float)midiFilePlayer.MPTK_TickCurrent / midiFilePlayer.MPTK_TickLast) * 100f);

                if (midiFilePlayer.MPTK_EffectSoundFont != null)
                {
                    TogApplyFilter.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableFilter);
                    SliderFilter.SetValueWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.FilterFreqOffset);

                    TogApplyReverb.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableReverb);
                    SliderReverb.SetValueWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.ReverbAmplify);

                    TogApplyChorus.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableChorus);
                    SliderChorus.SetValueWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.ChorusAmplify);
                }

                TogApplyInterpol.SetIsOnWithoutNotify(midiFilePlayer.InterpolationMethod != fluid_interp.None);

                BtFreezeInfo.GetComponentInChildren<Text>().text = InfoFreeze ? "Frozen" : "Freeze";

                TxtVolume.text = $"Volume:{midiFilePlayer.MPTK_Volume:F2}";
                TxtSpeed.text = $"Speed:{midiFilePlayer.MPTK_Speed:F1}";
                TxtPosition.text = $"Tick:{midiFilePlayer.MPTK_TickCurrent}";

                if (!InfoFreeze)
                {
                    // Build synth information
                    // -----------------------
                    StringBuilder infoSynth = midiFilePlayer.synthInfo.MPTK_BuildInfoSynth(midiFilePlayer);

                    // Display synth information
                    TxtInfo.text = infoSynth.ToString();
                }

                if (logUpdated && !InfoFreeze)
                {
                    try
                    {
                        logUpdated = false;
                        // if (infoLog == null)
                        infoLog = new StringBuilder(256);
                        //Debug.LogWarning($"ListMessage:{ListMessage.Count}");
                        foreach (string msg in ListMessage)
                            if (msg != null)
                                infoLog.AppendLine(msg);
                        TxtLog.text = infoLog.ToString();
                        Canvas.ForceUpdateCanvases(); // Ensure UI updates before scrolling
                        scrollRect.verticalNormalizedPosition = 0f; // Scroll to the bottom
                        RectTransformLog.sizeDelta = new Vector3(RectTransformLog.sizeDelta.x, lineHeight * ListMessage.Count);
                    }
                    catch (Exception) { /* possible exception when linked list modified during enumeration ... not an issue */ }
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