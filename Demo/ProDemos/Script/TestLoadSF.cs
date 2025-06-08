/// Simple script to dynamically SoundFont at startup
/// To be done in next Maestro version:
///     - Try to get ionformation from the SF: MidiPlayerGlobal.ImSFCurrent.SoundFontName is not defined
///     - Create a local cache of dowloaded SF to avoid downloading at each run

using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
//using static UnityEngine.Networking.UnityWebRequest;

namespace DemoMVP
{

    public class TestLoadSF : MonoBehaviour
    {
        [Header("When start, load a soundfont (first external)")]
        public bool LoadSoundFontAtStartup;

        [Header("Applied to synth: 0=All, 1=MidiFilePlayer, 2=MidiStreamPlayer")]
        public int ApplySoundFont;

        // UI and private for loading a soundfont
        // --------------------------------------
        [Serializable]
        public class ExampleSoundFont
        {
            public string Url;
            public string Name;
            public string Description;
        }

        [Header("Soundfont for testing")]
        public List<ExampleSoundFont> ExternalSoundFont;
        public List<ExampleSoundFont> InternalSoundFont;

        // UI for selecting SoundfontSource
        // --------------------------------
        [Header("UI Set Source")]
        public Toggle ToggleSourceExternal;
        public Toggle ToggleSourceInternal;
        public Toggle ToggleSourceDefault;
        public Dropdown ComboChangeSoundfont;

        public InputField InputURLSoundFontAtRun;

        // UI for loading SF
        // -----------------
        [Header("UI Load")]
        public Toggle ToggleLoadFromFontCache;
        public Toggle ToggleSaveToCache;
        public Toggle ToggleApplyMidiPlayer;
        public Toggle ToggleApplyMidiStream;
        public Text TextInfoSFSelected;
        public Button ButtonLoadSoundFont;

        // UI for SF status
        // -----------------
        [Header("UI MPTK Status")]
        public Text TextMPTKSfStatus;

        // UI and private for MidiFilePlayer
        // ---------------------------------
        [Header("UI MidiFilePlayer")]
        public Text TextSoundfontForMidiFilePlayer;
        public MidiFilePlayer midiFilePlayer; // must be set with the inspector
        public Dropdown ComboSelectMidi;
        public Text TextMidiTime;
        public Text TextMidiTick;

        // UI and private for MidiStreamPlayer
        // -----------------------------------
        [Header("UI MidiStreamPlayer")]
        public Text TextSoundfontForMidiStreamPlayer;
        public MidiStreamPlayer midiStreamPlayer;
        public Text TextSelectedNote;
        public Dropdown ComboSelectBank;
        public Dropdown ComboSelectPreset;

        private int selectedNote = 60;
        private MPTKEvent midiStreamEvent; // For playing a MIDI note-on with the MIDI Stream Player
        private Color ColorSalmon = new Color(0.90f, 0.72f, 0.72f);
        private Color ColorWhite = new Color(1, 1, 1);

        /// <summary>
        /// Classically with a Unity UI (ugui) design, the Start() method is used to initialize the UI
        /// but also to defined all the callback for the UI. So this method is very long and most of the 
        /// process are prepare in this step.
        /// </summary>
        void Start()
        {
            // If there is no soundfont available with MPTK, the MIDI list will not be calculated at start.
            // This call force the creation of an instance of MidiPlayerGlobal ... and build the list.
            MidiPlayerGlobal.BuildMidiList();

            ExternalSoundFont = new List<ExampleSoundFont>
            {
                // Warning: these soundfonts may not be used in your project from the paxstellar site. They may be removed at any time without notice.
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/GSv1471.sf2", Name="GeneralUser GS v1.471 from Christian Collins", Description = "MQ - GS - 269 presets - 312 instruments - 864 samples - Drum kit - 30 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/GeneralUser-GS-v2.0.1.sf2", Name="GeneralUser GS v2.0.1 from Christian Collins" , Description = "MQ - GS - 287 presets - 324 instruments - 920 samples - Drum kit - 31 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/Piano.sf2", Name="Piano only", Description = "LQ - Not GS - 1 preset - 2Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/HQ_Orchestral_Soundfont_Collection_v2.0.sf2", Name="Orchestral instruments", Description = "HQ - GS - 150 presets - 236 instruments - 1552 samples - Drum kit - 421 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/Nice-Steinways-JNv5.8.sf2", Name="Nice Steinway", Description = "HQ - Not GS - 10 presets - 7 instruments - 196 samples - No Drum kit - 385 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/SGM-v2.01-HQ-v3.0.sf2", Name="Shan's GM v2.01 ", Description = "MQ - GS - 282 presets - 283 instruments - 2064 samples - Drum kit - 385 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/20_synths.sf2", Name="Twenty analogic synths", Description = "MQ - Not GS - 20 presets - 20 instruments - 251 samples - Drum kit - 59 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/VintageDreamsWaves-v2.sf2", Name="Tiny SF, Vintage Dreams Waves V2.0", Description = "MQ - Not GS - 136 presets - 238 instruments - 124 samples - Drum kit - 0.3 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/Meowsic_Cat_Soundfont.sf2", Name="Meowsic Cat", Description = "MQ - Not GS - 5 presets - 5 instruments - 113 samples - No Drum kit - 19 Mb" },
                new ExampleSoundFont() { Url = "https://mptkapi.paxstellar.com/Tetris SoundFont.sf2", Name="Tetris", Description = "LQ - Not GS - 10 presets - 10 instruments - 12 samples - No Drum kit - 0.8 Mb" },

            };

            InternalSoundFont = new List<ExampleSoundFont>();
            if (MidiPlayerGlobal.MPTK_ListSoundFont != null)
                foreach (string sfName in MidiPlayerGlobal.MPTK_ListSoundFont)
                    InternalSoundFont.Add(new ExampleSoundFont() { Url = "", Name = sfName, Description = "From MPTK Resource" });

            // Togle which defined the source of the soundfont (internal, external or default MPTK)
            // ------------------------------------------------------------------------------------
            ToggleSourceExternal.onValueChanged.AddListener(delegate
            {
                UpdateSoundfontCombo();
                UpdateSoundfontInfoFromSelectedInUI();
                AdaptButtonLoadSF(true);
            });
            ToggleSourceInternal.onValueChanged.AddListener(delegate
            {
                UpdateSoundfontCombo();
                UpdateSoundfontInfoFromSelectedInUI();
                AdaptButtonLoadSF(true);
            });
            ToggleSourceDefault.onValueChanged.AddListener(delegate
            {
                UpdateSoundfontCombo();
                UpdateSoundfontInfoFromSelectedInUI();
                AdaptButtonLoadSF(true);
            });

            // At start, set the initial value
            UpdateSoundfontCombo();
            UpdateSoundfontInfoFromSelectedInUI();


            // Combo and buttons for selecting Soundfont 
            // ------------------------------------------
            ComboChangeSoundfont.onValueChanged.AddListener(delegate
            {
                UpdateSoundfontInfoFromSelectedInUI();
                AdaptButtonLoadSF(true);
            });

            ToggleApplyMidiPlayer.onValueChanged.AddListener(delegate { AdaptButtonLoadSF(true); });
            ToggleApplyMidiStream.onValueChanged.AddListener(delegate { AdaptButtonLoadSF(true); });

            // Update Combo bank and preset + callback when value selected
            // -----------------------------------------------------------
            ComboSelectBank.onValueChanged.AddListener((int iCombo) =>
            {
                // Get the bank number from the index in the bank list.
                int numberBank = midiStreamPlayer.MPTK_SoundFont.BanksNumber[iCombo];
                //Debug.Log($"Num: {indexBank} Name:{ComboSelectBank.options[iCombo].text}");
                // Select the current bank --> midiStreamPlayer.MPTK_SoundFont.ListPreset will be updated
                midiStreamPlayer.MPTK_SoundFont.SelectBankInstrument(numberBank);

                // Refresh presets list associated to the bank selected
                UpdatePresetCombo();

                // Change the MidiStreamPlayer bank
                PlayBankChange(numberBank);

                // Force preset change for the selected preset (normally, the first in the list updated)
                int numberPreset = midiStreamPlayer.MPTK_SoundFont.PresetsNumber[ComboSelectPreset.value];
                PlayPresetChange(numberPreset);
            });

            // At start, set the initial value
            UpdateBankCombo();

            ComboSelectPreset.onValueChanged.AddListener((int iCombo) =>
            {
                // Get the preset number from the index in the preset list.
                int numberPreset = midiStreamPlayer.MPTK_SoundFont.PresetsNumber[iCombo];
                PlayPresetChange(numberPreset);
            });

            // At start, set the initial value
            UpdatePresetCombo();

            // Check if MPTK prefab MidiStreamPlayer exist in the scene 
            // --------------------------------------------------------
            if (midiStreamPlayer == null)
                Debug.LogWarning("Can't find a MidiStreamPlayer Prefab in the current scene hierarchy. You can add it with the Maestro menu in Unity editor.");
            else
            {
                midiStreamPlayer.MPTK_LogEvents = true;
                TextSelectedNote.text = $"{selectedNote} - {HelperNoteLabel.LabelFromMidi(selectedNote)}";
            }

            // Check if MPTK prefabs MidiSFilePlayer exist in the scene 
            // --------------------------------------------------------
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a midiFilePlayer Prefab in the current scene hierarchy. You can add it with the Maestro menu in Unity editor.");

            // Feed the combo with the list of MIDI available 
            // ----------------------------------------------
            List<String> midiList = new List<string>();
            ComboSelectMidi.ClearOptions();
            if (MidiPlayerGlobal.MPTK_ListMidi != null)
                foreach (MPTKListItem item in MidiPlayerGlobal.MPTK_ListMidi)
                    midiList.Add($"{item.Index} - {item.Label}");
            ComboSelectMidi.AddOptions(midiList);

            ComboSelectMidi.onValueChanged.AddListener((int iCombo) =>
            {
                // A new MIDI has been selected
                if (iCombo >= 0 && iCombo < MidiPlayerGlobal.MPTK_ListMidi.Count)
                {
                    midiFilePlayer.MPTK_Stop();
                    midiFilePlayer.MPTK_MidiIndex = iCombo;
                    midiFilePlayer.MPTK_Play();
                }
            });

            // At start, set the initial value
            ComboSelectMidi.value = midiFilePlayer.MPTK_MidiIndex;
            ComboSelectMidi.RefreshShownValue();

            midiFilePlayer.OnEventStartPlayMidi.AddListener(info =>
            {
                // Callback not used for now
            });


            // Select the default Soundfont 
            Debug.Log($"TestLoadSF - Select the default Soundfont '{ExternalSoundFont[0].Url}' '{ExternalSoundFont[0].Name}' at start.");
            InputURLSoundFontAtRun.text = ExternalSoundFont[0].Url;
            UpdateSoundfontInfoFromSelectedInUI();
            AdaptButtonLoadSF(true);

            ButtonLoadSoundFont.onClick.AddListener(delegate
            {
                // We want to load the SoundFont.
                SoundFontLoadFromTextInfoSF();
            });

            // Prepare the synths (MidiFilePlayer and MidiStreamPlayer in this demo) 
            // with callbacks for download progress and when soundfont is ready.
            // ---------------------------------------------------------------------
            MidiSynth[] MidiSynths = FindObjectsByType<MidiSynth>(FindObjectsSortMode.None);
            foreach (MidiSynth midiSynth in MidiSynths)
            {
                // ProgressCallback is called only for external soundfont when a download is needed.
                midiSynth.MPTK_SoundFont.ProgressCallback = (float progress) =>
                {
                    SoundfontInProgress(progress, midiSynth);
                };


                // This callback will be triggered when the soundfont is ready.
                midiSynth.MPTK_SoundFont.LoadedCallback = (MidiSynth synth) =>
                {
                    SoundfontIsReady(synth);
                };
            }

            // At start, load the selected Soundfont from the URI defined in the UI text field.
            if (LoadSoundFontAtStartup)
                SoundFontLoadFromTextInfoSF();
        }

        private void SoundfontInProgress(float progress, MidiSynth midiSynth)
        {
            // Warning - this callback is not running inside the Unity thread.
            // It's not possible to use Unity API. So forget to directly update the UI.
            // But variable can be set and reuse in the Update() ... for example.
            // The progress percentage provided by Unity SendWebRequest seems not a percentage
            // So, I removed the % character ...
            Debug.Log($"<color=yellow>In Progress {midiSynth.name} {progress * 100:F0}</color>");
        }

        /// <summary>
        /// Load the soundfont from the URI defined in the UI.
        /// Activated by the UI.
        /// </summary>
        public void SoundFontLoadFromTextInfoSF()
        {

            List<MidiSynth> midiSynths = new List<MidiSynth>();

            bool downloadOnly = !ToggleApplyMidiPlayer.isOn && !ToggleApplyMidiStream.isOn;
            if (downloadOnly)
                midiSynths.Add(FindObjectsByType<MidiSynth>(FindObjectsSortMode.None)[0]);

            if (ToggleApplyMidiPlayer.isOn)
                midiSynths.AddRange(FindObjectsByType<MidiFilePlayer>(FindObjectsSortMode.None));
            if (ToggleApplyMidiStream.isOn)
                midiSynths.AddRange(FindObjectsByType<MidiStreamPlayer>(FindObjectsSortMode.None));

            foreach (MidiSynth midiSynth in midiSynths)
            {
                // Prepare switching between SoundFonts for each synths selected.
                // Here we load the SoundFont from the URI defined in the UI.
                // Set download options defined in the UI. 
                // -------------------------------------------------------------------
                midiSynth.MPTK_SoundFont.LoadFromCache = ToggleLoadFromFontCache.isOn;
                midiSynth.MPTK_SoundFont.SaveToCache = ToggleSaveToCache.isOn;
                midiSynth.MPTK_SoundFont.DownloadOnly = downloadOnly;

                Debug.Log($"TestLoadSF - Load for '{midiSynth.name}'");
                bool result = midiSynth.MPTK_SoundFont.Load(InputURLSoundFontAtRun.text);
                if (!result)
                    Debug.Log($"TestLoadSF - Download canceled, status:{MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded} URI:{InputURLSoundFontAtRun.text}");
            }
        }

        /// <summary>
        /// A soundfont is ready for this synth
        /// </summary>
        /// <param name="synth"></param>
        private void SoundfontIsReady(MidiSynth synth)
        {
            Debug.LogFormat($"TestLoadSF - End loading: '{synth.name}' '{synth.MPTK_SoundFont.SoundFontName}' Status: {synth.MPTK_SoundFont.StatusSoundFont}");
            Debug.Log($"   Overall time:            {Math.Round(synth.MPTK_SoundFont.TimeToLoadOverall.TotalSeconds, 3)} second");
            Debug.Log($"   Time To Download SF:     {Math.Round(synth.MPTK_SoundFont.TimeToDownloadSoundFont.TotalSeconds, 3)} second");
            Debug.Log($"   Time To Load SoundFont:  {Math.Round(synth.MPTK_SoundFont.TimeToLoadSoundFont.TotalSeconds, 3)} second");
            Debug.Log($"   Time To Load Samples:    {Math.Round(synth.MPTK_SoundFont.TimeToLoadWave.TotalSeconds, 3).ToString()} second");

            if (synth is MidiStreamPlayer/* && synth.MPTK_SynthState == fluid_synth_status.FLUID_SYNTH_PLAYING*/)
            {
                synth.MPTK_SoundFont.SelectBankInstrument(0);
                UpdateBankCombo();
                UpdatePresetCombo();
            }
            AdaptButtonLoadSF(false);
        }

        /// <summary>
        /// Update UI with the list of SoundFont available.
        /// </summary>
        private void UpdateSoundfontCombo()
        {
            ComboChangeSoundfont.ClearOptions();
            if (!ToggleSourceDefault.isOn)
            {
                List<ExampleSoundFont> listSf = ToggleSourceExternal.isOn ? ExternalSoundFont : InternalSoundFont;
                foreach (ExampleSoundFont sf in listSf)
                {
                    ComboChangeSoundfont.options.Add(new Dropdown.OptionData() { text = sf.Name });
                }
                ComboChangeSoundfont.value = 0;
                ComboChangeSoundfont.RefreshShownValue();
            }
            else
            {
                ComboChangeSoundfont.ClearOptions();
            }
        }

        /// <summary>
        /// Update label selected soundfont and input for loading the soundfont
        /// </summary>
        private void UpdateSoundfontInfoFromSelectedInUI()
        {
            TextInfoSFSelected.text = "<b>Selected SoundFont:</b> ";
            if (!ToggleSourceDefault.isOn)
            {
                if (ToggleSourceExternal.isOn)
                {
                    ExampleSoundFont sf = ExternalSoundFont[(int)ComboChangeSoundfont.value];
                    TextInfoSFSelected.text += sf.Name + " - " + sf.Description;
                    InputURLSoundFontAtRun.text = sf.Url;
                }
                else
                {
                    // Check if internal (MPTK) soundfont exists
                    if (InternalSoundFont.Count > 0)
                    {
                        ExampleSoundFont sf = InternalSoundFont[(int)ComboChangeSoundfont.value];
                        TextInfoSFSelected.text += sf.Name + " - " + sf.Description;
                        InputURLSoundFontAtRun.text = sf.Name;
                    }
                }
            }
            else
            {
                TextInfoSFSelected.text += "Default SoundFont from MPTK";
                InputURLSoundFontAtRun.text = "";
            }
        }

        private void UpdateBankCombo()
        {
            ComboSelectBank.ClearOptions();
            // Banks list can be null if any soundfont is available.
            if (midiStreamPlayer.MPTK_SoundFont.BanksName != null)
                ComboSelectBank.AddOptions(midiStreamPlayer.MPTK_SoundFont.BanksName);
        }

        private void UpdatePresetCombo()
        {
            ComboSelectPreset.ClearOptions();
            // Presets list can be null if any soundfont is available.
            if (midiStreamPlayer.MPTK_SoundFont.PresetsName != null)
                ComboSelectPreset.AddOptions(midiStreamPlayer.MPTK_SoundFont.PresetsName);
        }

        // Open the folder in the file explorer or Finder
        public void ShowCacheFolder()
        {
            string folderPath = MidiPlayerGlobal.MPTK_PathSoundFontCache;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Application.OpenURL("file://" + folderPath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
#endif
        }
        public void ShowWebSite(string url)
        {
            Application.OpenURL(url);
        }

        public void PlayNextNote()
        {
            if (selectedNote >= 127)
                selectedNote = 0;
            else
                selectedNote++;
            PlayNoteOn();
        }

        public void PlayPreviousNote()
        {
            if (selectedNote <= 0)
                selectedNote = 127;
            else
                selectedNote--;
            PlayNoteOn();
        }

        /// <summary>
        /// Play one note with the MidiStreamPlayer, fixed duration of 2 s, fixed channel 0, fixed velocity 100.\n
        /// </summary>
        public void PlayNoteOn()
        {
            if (!midiStreamPlayer.MPTK_SoundFont.IsReady)
                Debug.Log("SoundFont is not loaded");
            else
            {
                TextSelectedNote.text = $"{selectedNote} - {HelperNoteLabel.LabelFromMidi(selectedNote)}";
                // playing a NoteOn
                midiStreamEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Value = selectedNote,
                    Channel = 0,
                    Velocity = 0x64, // Sound can vary depending on the velocity
                    Delay = 0,
                    Duration = 2000
                };

                midiStreamPlayer.MPTK_PlayEvent(midiStreamEvent);
            }
        }

        public void StopAllSounds()
        {
            if (midiStreamEvent != null)
                midiStreamPlayer.MPTK_StopEvent(midiStreamEvent);
        }

        public void PlayBankChange(int bank)
        {
            // playing a bank change
            midiStreamEvent = new MPTKEvent()
            {
                Command = MPTKCommand.ControlChange,
                Controller = MPTKController.BankSelectMsb,
                Value = bank,
                Channel = 0,
            };

            midiStreamPlayer.MPTK_PlayEvent(midiStreamEvent);
        }
        public void PlayPresetChange(int preset)
        {
            // playing a preset change
            midiStreamEvent = new MPTKEvent()
            {
                Command = MPTKCommand.PatchChange,
                Value = preset,
                Channel = 0,
            };

            midiStreamPlayer.MPTK_PlayEvent(midiStreamEvent);
        }

        public void PlayMidi()
        {
            if (!midiFilePlayer.MPTK_SoundFont.IsReady)
                Debug.Log("SoundFont is not loaded");
            else
                midiFilePlayer.MPTK_Play();
        }

        void Update()
        {
            if (midiFilePlayer != null && midiFilePlayer.MPTK_IsPlaying)
            {
                // Animate the MidiPlayer
                TimeSpan times = TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_Position);
                TextMidiTime.text = $"{times.Minutes:00}:{times.Seconds:00}";
                TextMidiTick.text = midiFilePlayer.MPTK_TickCurrent.ToString();
            }

            // Display information about the default MPTK soundfont
            if (MidiPlayerGlobal.MPTK_SoundFontIsReady)
                TextMPTKSfStatus.text = $"Default MPTK Soundfont -  {MidiPlayerGlobal.MPTK_SoundFontName} - <color=green>Ready</color>";
            else
                TextMPTKSfStatus.text = "Default MPTK Soundfont - <color=red>Not Ready</color>";

            // Display information about the soundfont applied to the MidiFilePlayer prefabs
            TextSoundfontForMidiFilePlayer.text = "MidiFilePlayer - " + statusSoundFont(midiFilePlayer);

            // Display information about the soundfont applied to the MidiStreamPlayer prefabs
            TextSoundfontForMidiStreamPlayer.text = "MidiStreamPlayer - " + statusSoundFont(midiStreamPlayer);
        }

        /// <summary>
        /// Build a string with the status of the soundfont applied to the synth.
        /// </summary>
        /// <param name="synth"></param>
        /// <returns></returns>
        string statusSoundFont(MidiSynth synth)
        {
            string status = "";
            if (synth != null)
            {
                // Search source of the soundfont
                string source = "";
                if (synth.MPTK_SoundFont.IsInternal)
                    if (synth.MPTK_SoundFont.IsDefault)
                        source = "Default MPTK";
                    else
                        source = "Internal MPTK";
                else
                    source = "External";
                status = $"{synth.MPTK_SoundFont.SoundFontName} [{source}]";
            }

            // Search soundfont status
            switch (synth.MPTK_SoundFont.StatusSoundFont)
            {
                case LoadingStatusSoundFontEnum.InProgress:
                    status += $"<color=yellow> - In Progress {synth.MPTK_SoundFont.ProgressValue * 100:F0}";
                    break;
                case LoadingStatusSoundFontEnum.Success:
                    status += "<color=green> - Success";
                    break;
                default:
                    status += $"<color=red> - {synth.MPTK_SoundFont.StatusSoundFont}";
                    break;
            }
            status += "</color>";

            // Add time to download (if any)
            status += $" - Time: {Math.Round(synth.MPTK_SoundFont.TimeToDownloadSoundFont.TotalSeconds, 3)}";

            return status;
        }

        /// <summary>
        /// Change the display of the button Load SoundFont (backcolor + text) depending of UI action (when a soundfont is selected or not).
        /// </summary>
        /// <param name="needToLoadTheSoundFont"></param>
        private void AdaptButtonLoadSF(bool needToLoadTheSoundFont)
        {
            if (needToLoadTheSoundFont)
            {
                ButtonLoadSoundFont.GetComponentInChildren<Text>().text = "To be loaded";
                ButtonLoadSoundFont.GetComponent<Image>().color = ColorSalmon;
            }
            else
            {
                ButtonLoadSoundFont.GetComponentInChildren<Text>().text = "Loaded";
                ButtonLoadSoundFont.GetComponent<Image>().color = ColorWhite;
            }
        }
    }
}