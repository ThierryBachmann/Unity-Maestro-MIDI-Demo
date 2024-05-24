//#define DEBUG_HISTO_DSPSIZE
// remove   STEAMAUDIO_ENABLED
// add      UNITY_OBOE
// add      DEBUG_HISTO_DSPSIZE

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
    public class TestOboeMaestro : MonoBehaviour
    {
        public Text TxtTittle;
        public Text TxtVersion;
        public Text TxtInfo;
        public Text TxtSpeed;
        public Text TxtPosition;
        public Toggle TogApplyFilter;
        public Toggle TogApplyReverb;
        public Button BtPrevious;
        public Button BtMidi;
        public Button BtNext;

        public int PresetInstrument;
        PopupListBox popupInstrument;
        public Text TxtSelectedInstrument;

        public PopupListBox TemplateListBox;
        public static PopupListBox PopupListInstrument;

        public Dropdown ComboMidiList;
        public Dropdown ComboFrameRate;
        public Dropdown ComboBufferSize;
        public Slider SliderPositionMidi;

        List<string> frameRate = new List<string> { "Default", "24000", "36000", "48000", "60000", "72000", "84000", "96000" };
        List<string> bufferSize = new List<string> { "64", "128", "256", "512", "1024", "2048" };
        List<string> midiList = new List<string>();

        public LinkedList<string> ListMessage = new LinkedList<string>();

        // MidiFilePlayer prefab is found by script
        private MidiFilePlayer midiFilePlayer;

        //Called when there is an exception
        void LogCallback(string logString, string stackTrace, LogType type)
        {
            // if (type != LogType.Log)
            if (!string.IsNullOrWhiteSpace(logString))
            {
                ListMessage.AddLast(logString);
                // Keep only the last 3 messages
                if (ListMessage.Count > 3)
                    ListMessage.RemoveFirst();
            }
        }

        void Start()
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.logMessageReceivedThreaded += LogCallback;
            Input.simulateMouseWithTouches = true;

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
                    midiFilePlayer.MPTK_IndexSynthRate = iCombo - 1; //  -1:defaul:48000, 0:24000, 1:36000, 2:48000, 3:60000, 4:72000, 5:84000, 6:96000
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
            TogApplyReverb.onValueChanged.AddListener((bool apply) => { if (midiFilePlayer.MPTK_EffectSoundFont != null) midiFilePlayer.MPTK_EffectSoundFont.EnableReverb = apply; });

            // Find MidiStreamPlayer (play note in real time or whole MIDI file)
            midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add to the scene with the Maestro menu.");
            else
            {
                // Did'nt works because the MPTK synth is already started
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

                BtNext.onClick.AddListener(() =>
                {
                    midiFilePlayer.MPTK_Next();
                });
            }

            Debug.Log("Defined synth settings");
            ComboFrameRate.value = 3; // 48000
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
                    TogApplyReverb.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableReverb);
                }

                TxtSpeed.text = $"Speed:{midiFilePlayer.MPTK_Speed:F1}";
                TxtPosition.text = $"Tick:{midiFilePlayer.MPTK_TickCurrent}";

                // Build synth information
                // -----------------------


                StringBuilder infoSynth = new StringBuilder();
                infoSynth.Append($"AudioEngine: {midiFilePlayer.AudioEngine} BufferSizeInFrames: {midiFilePlayer.AudioBufferLenght} FramesPerCallback: {midiFilePlayer.AudioNumBuffers}");
                infoSynth.AppendLine($" OutputRate: {midiFilePlayer.OutputRate} Hz DspBufferSize: {midiFilePlayer.DspBufferSize}");
                infoSynth.Append($"Voice:{midiFilePlayer.MPTK_StatVoiceCountActive} Free: {midiFilePlayer.MPTK_StatVoiceCountFree} Played: {midiFilePlayer.MPTK_StatVoicePlayed}");
                infoSynth.AppendLine($" IdSynth: {midiFilePlayer.IdSynth} StatDspLoad: {midiFilePlayer.StatDspLoadPCT:F1} Average: {midiFilePlayer.StatDspLoadAVG:F1} % AudioLatency: {midiFilePlayer.StatDeltaAudioFilterReadMS:F2} ms");
#if DEBUG_HISTO_DSPSIZE
                int count_inf_64 = 0;
                int count_not_64 = 0;
                int min_size = 99999;
                int max_size = 0;
                for (int i = 0; i < midiFilePlayer.histoDspSize.Length; i++)
                {
                    if (midiFilePlayer.histoDspSize[i] < 64 ) count_inf_64++;
                    if (midiFilePlayer.histoDspSize[i] % 64 != 0) count_not_64++;
                    if (midiFilePlayer.histoDspSize[i] < min_size) min_size = midiFilePlayer.histoDspSize[i];
                    if (midiFilePlayer.histoDspSize[i] > max_size) max_size = midiFilePlayer.histoDspSize[i];
                }

                infoSynth.AppendLine($"Frame length historic: size between:{min_size,-4} and {max_size,-4} LowerThan64:{count_inf_64,-2} NotModulo64:{count_not_64,-2}");
                for (int i = 0; i < midiFilePlayer.histoDspSize.Length; i++)
                {
                    infoSynth.Append($"{midiFilePlayer.histoDspSize[i]:000} ");
                    if (i % 25 == 0 && i != 0) infoSynth.AppendLine("");
                }
#endif
                infoSynth.AppendLine("");

#if UNITY_ANDROID && UNITY_OBOE
                ResultWithValue<double> latency = midiFilePlayer.oboeAudioStream.CalculateLatencyMillis();
                infoSynth.AppendLine($"Oboe info: SampleRate:{midiFilePlayer.oboeAudioStream.SampleRate} Hz BufferCapacityInFrames: {midiFilePlayer.oboeAudioStream.BufferCapacityInFrames}  Oboe Latency: {latency.Value:F2} ms");
                infoSynth.AppendLine($"BytesPerFrame:{midiFilePlayer.oboeAudioStream.BytesPerFrame} FramesPerBurst:{midiFilePlayer.oboeAudioStream.FramesPerBurst} BufferSizeInFrames: {midiFilePlayer.oboeAudioStream.BufferSizeInFrames}");
                infoSynth.AppendLine($"FramesRead:{midiFilePlayer.oboeAudioStream.FramesRead} FramesWritten:{midiFilePlayer.oboeAudioStream.FramesWritten} PerformanceMode:{midiFilePlayer.oboeAudioStream.PerformanceMode}");
                
#endif
                try
                {
                    foreach (string msg in ListMessage)
                        infoSynth.AppendLine(msg);
                }
                catch (Exception) { /* possible exception when linked list modified during enumeration ... not an issue */ }

                // Display synth information
                TxtInfo.text = infoSynth.ToString();
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