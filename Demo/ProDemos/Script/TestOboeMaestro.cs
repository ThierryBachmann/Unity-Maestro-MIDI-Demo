//#define DEBUG_HISTO_DSPSIZE

using MidiPlayerTK;
using System;
using System.Collections.Generic;
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
        public MidiFilePlayer midiFilePlayer;

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

        public string LastErrorMessage = "";
        //Called when there is an exception
        void LogCallback(string logString, string stackTrace, LogType type)
        {
            // if (type != LogType.Log)
            if (!string.IsNullOrWhiteSpace(logString))
                LastErrorMessage = logString;

        }

        void Start()
        {

            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.logMessageReceivedThreaded += LogCallback;
            Input.simulateMouseWithTouches = true;

            MidiPlayerGlobal.MPTK_ListMidi.ForEach(item => midiList.Add(item.Label));
            ComboMidiList.ClearOptions();
            ComboMidiList.AddOptions(midiList);
            ComboMidiList.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    midiFilePlayer.MPTK_Stop();
                    midiFilePlayer.MPTK_MidiIndex = iCombo;
                    midiFilePlayer.MPTK_Play();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

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

            ComboFrameRate.ClearOptions();
            ComboFrameRate.AddOptions(frameRate);
            ComboFrameRate.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    midiFilePlayer.MPTK_Stop();
                    midiFilePlayer.MPTK_IndexSynthRate = iCombo - 1; //  -1:defaul:48000, 0:24000, 1:36000, 2:48000, 3:60000, 4:72000, 5:84000, 6:96000
                    // Before v2.10.1
                    // midiFilePlayer.MPTK_ChannelPresetChange(0, PresetInstrument, 0);
                    // From v2.10.1
                    midiFilePlayer.MPTK_Channels[0].PresetNum = PresetInstrument;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

            ComboBufferSize.ClearOptions();
            ComboBufferSize.AddOptions(bufferSize);
            ComboBufferSize.onValueChanged.AddListener((int iCombo) =>
            {
                try
                {
                    midiFilePlayer.MPTK_Stop();
                    midiFilePlayer.MPTK_IndexSynthBuffSize = iCombo;
                    //#if UNITY_ANDROID && UNITY_OBOE
                    //                    midiFilePlayer.InitOboe();
                    //#else
                    //                    midiFilePlayer.MPTK_IndexSynthBuffSize = iCombo;
                    //#endif

                    // Before v2.10.1
                    // midiFilePlayer.MPTK_ChannelPresetChange(0, PresetInstrument, 0);
                    // From v2.10.1
                    midiFilePlayer.MPTK_Channels[0].PresetNum = PresetInstrument;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            });

            TogApplyFilter.onValueChanged.AddListener((bool apply) => { midiFilePlayer.MPTK_EffectSoundFont.EnableFilter = apply; });
            TogApplyReverb.onValueChanged.AddListener((bool apply) => { midiFilePlayer.MPTK_EffectSoundFont.EnableReverb = apply; });

            // Need MidiStreamPlayer to play note in real time
            midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add to the scene with the Maestro menu.");
            else
            {
                midiFilePlayer.OnEventSynthStarted.AddListener((string synthName) =>
                {
                    Debug.Log($"OnEventSynthStarted {synthName}");
                    ComboFrameRate.value = 2;
                    ComboBufferSize.value = 2;
                });

                midiFilePlayer.MPTK_InitSynth();

                PopupListInstrument = TemplateListBox.Create("Select an Instrument");
                foreach (MPTKListItem presetItem in MidiPlayerTK.MidiPlayerGlobal.MPTK_ListPreset)
                    PopupListInstrument.AddItem(presetItem);

                popupInstrument = PopupListInstrument;
                PresetInstrument = popupInstrument.FirstIndex();
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = popupInstrument.LabelSelected(PresetInstrument);


                TxtVersion.text = $"Version:{Application.version}    Unity:{Application.unityVersion}";
                TxtTittle.text = Application.productName;

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
        }


        public void SelectPreset()
        {
            popupInstrument.OnEventSelect.AddListener((MPTKListItem item) =>
            {
                Debug.Log($"SelectPreset {item.Index} {item.Label}");
                PresetInstrument = item.Index;
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = item.Label;
                // Before v2.10.1
                // midiFilePlayer.MPTK_ChannelPresetChange(0, PresetInstrument, 0);
                // From v2.10.1
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

                if (midiFilePlayer.MPTK_IsPlaying)
                {
                    if (midiFilePlayer.MPTK_TickLast > 0)
                        //if (SliderPositionMidi.value != midiFilePlayer.MPTK_TickCurrent / 100f)
                        SliderPositionMidi.SetValueWithoutNotify(((float)midiFilePlayer.MPTK_TickCurrent / midiFilePlayer.MPTK_TickLast) * 100f);

                    TogApplyFilter.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableFilter);
                    TogApplyReverb.SetIsOnWithoutNotify(midiFilePlayer.MPTK_EffectSoundFont.EnableReverb);
                }

                TxtSpeed.text = $"Speed:{midiFilePlayer.MPTK_Speed:F1}";
                TxtPosition.text = $"Tick:{midiFilePlayer.MPTK_TickCurrent}";
                TxtInfo.text = "";
                TxtInfo.text += $"AudioEngine:{midiFilePlayer.AudioEngine} BufferSizeInFrames:{midiFilePlayer.AudioBufferLenght} FramesPerCallback:{midiFilePlayer.AudioNumBuffers}\n";
                TxtInfo.text += $"OutputRate:{midiFilePlayer.OutputRate} Hz DspBufferSize:{midiFilePlayer.DspBufferSize}\n";
                TxtInfo.text += $"MidiPlayer - Voice:{midiFilePlayer.MPTK_StatVoiceCountActive} Free: {midiFilePlayer.MPTK_StatVoiceCountFree} Played: {midiFilePlayer.MPTK_StatVoicePlayed}\n";
                TxtInfo.text += $"Synth:{midiFilePlayer.IdSynth} StatDspLoadAVG:{midiFilePlayer.StatDspLoadAVG:F1} % StatDeltaAudioFilterReadMS:{midiFilePlayer.StatDeltaAudioFilterReadMS:F2} ms\n";
#if DEBUG_HISTO_DSPSIZE
                TxtInfo.text += "Frame length historic:\n";
                for (int i = 0; i < midiFilePlayer.histoDspSize.Length; i++)
                {
                    TxtInfo.text += $"{midiFilePlayer.histoDspSize[i]:000} ";
                    if (i % 25 == 0 && i != 0) TxtInfo.text += "\n";
                }
#endif
                TxtInfo.text += "\n";
#if UNITY_ANDROID && UNITY_OBOE
                TxtInfo.text += $"Oboe info: SampleRate:{midiFilePlayer.oboeAudioStream.SampleRate} Hz BufferCapacityInFrames: {midiFilePlayer.oboeAudioStream.BufferCapacityInFrames} \n";
                TxtInfo.text += $"BytesPerFrame:{midiFilePlayer.oboeAudioStream.BytesPerFrame} FramesPerBurst:{midiFilePlayer.oboeAudioStream.FramesPerBurst}\n";
                TxtInfo.text += $"FramesRead:{midiFilePlayer.oboeAudioStream.FramesRead} FramesWritten:{midiFilePlayer.oboeAudioStream.FramesWritten} PerformanceMode:{midiFilePlayer.oboeAudioStream.PerformanceMode}\n";
#endif
                if (!string.IsNullOrEmpty(LastErrorMessage))
                    TxtInfo.text += "\n" + LastErrorMessage;
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