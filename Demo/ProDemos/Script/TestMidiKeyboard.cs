using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestMidiKeyboard : MonoBehaviour
    {
        // UI related
        public InputInt InputIndexDevice;
        public InputInt InputChannel;
        public InputInt InputPreset;
        public InputInt InputNote;
        public Toggle ToggleMidiRead;
        public Toggle ToggleRealTimeRead;
        public Toggle ToggleMsgSystem;
        public Text TextSendNote;
        public Text TextAlertRT;
        public Text TextCountEventQueue;
        public Text TextMidiPlayed;

        public float DelayToRefreshDeviceMilliSeconds = 1000f;

        float TimeForRefresh;

        // Maestro prefab for playing MIDI event coming from a connected MIDI keyboard
        // They are defined in the UI
        public MidiStreamPlayer midiStreamPlayer;
        public MidiFilePlayer midiFilePlayer;

        private void Start()
        {
            // Midi Keyboard need to be initialized at start
            MidiKeyboard.MPTK_Init();

            // Log version of the Midi plugins
            Debug.Log(MidiKeyboard.MPTK_Version());

            TextAlertRT.enabled = false;

            // UI Toggle for reading MIDI keyboard events. Open or close Midi Input Devices.
            ToggleMidiRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                    MidiKeyboard.MPTK_OpenAllInp();
                else
                    MidiKeyboard.MPTK_CloseAllInp();
                CheckStatus($"Open/close all input");

            });

            // UI Toggle for real time read (MIDI message processing )
            ToggleRealTimeRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                {
                    // MIDI message are processed when available without waiting a Unity Update()
                    // There is some risq of crash when app in Unity editor is stopped (Unity is not able to unload external module loaded).
                    TextAlertRT.enabled = true;
                    MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                else
                {
                    // MIDI message are processed from the Unity Update()
                    // So, there is latency related to the current Unity FPS.
                    TextAlertRT.enabled = false;
                    MidiKeyboard.OnActionInputMidi -= ProcessEvent;
                    MidiKeyboard.MPTK_UnsetRealTimeRead();
                }
            });

            // UI toggle for reading or not system message (not sysex)
            ToggleMsgSystem.onValueChanged.AddListener((bool state) =>
            {
                MidiKeyboard.MPTK_ExcludeSystemMessage(state);
            });

            // UI button for playing one note
            InputNote.OnEventValue.AddListener((int val) =>
            {
                TextSendNote.text = "Send Note " + HelperNoteLabel.LabelFromMidi(val);
            });

            // UI button for Read preset value and send a midi message to change preset on the device 'index"
            InputPreset.OnEventValue.AddListener((int val) =>
            {
                int index = InputIndexDevice.Value;

                // Send a patch change when button is clicked
                MPTKEvent midiEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.PatchChange,
                    Value = InputPreset.Value,
                    Channel = InputChannel.Value,
                    Delay = 0,
                };
                MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
                CheckStatus($"Play PatchChange on device: {index}");
            });

            TextMidiPlayed.text = "";

            // Setting the MidiFilePlayer prefab. Need to be defined from the UI.

            // Don't use the Maestro internal synth, but send MIDI events to the external MIDI Synth
            midiFilePlayer.MPTK_DirectSendToPlayer = false;

            // MPTK_LogEvents = true is not able to display MIDI events in the log
            // because they are generated from a system thread and HandleLog is not able to receive these logs.
            midiFilePlayer.MPTK_LogEvents = false;

            // Select the first MIDI available
            midiFilePlayer.MPTK_MidiIndex=0;

            // Event triggered when MIDI start playing, display the MIDI name in the UI
            midiFilePlayer.OnEventStartPlayMidi.AddListener(info => TextMidiPlayed.text = info);

            // Event triggered when a group of MIDI event is available for playing
            midiFilePlayer.OnEventNotesMidi.AddListener((List<MPTKEvent> events) =>
            {
                // Called for each MIDI event (or group of MIDI events) ready to be played by the MIDI synth.
                // All these events are on same MIDI tick.
                foreach (MPTKEvent midiEvent in events)
                {
                    // We log the event here for watching the MIDI event in the UI Log.
                    Debug.Log(midiEvent);
                    MidiKeyboard.MPTK_PlayEvent(midiEvent, InputIndexDevice.Value);
                }
            });
        }

        private void OnApplicationQuit()
        {
            Debug.Log("OnApplicationQuit " + Time.time + " seconds");
            MidiKeyboard.MPTK_UnsetRealTimeRead();
            MidiKeyboard.MPTK_CloseAllInp();
            CheckStatus($"Close all input");
        }

        /// <summary>@brief
        /// Log input and output midi device
        /// </summary>
        public void RefreshDevices()
        {
            Debug.Log($"Midi Input: {MidiKeyboard.MPTK_CountInp()} count of device available");
            for (int i = 0; i < MidiKeyboard.MPTK_CountInp(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetInpName(i)}");

            Debug.Log($"Midi Output: {MidiKeyboard.MPTK_CountOut()} count of device available");
            for (int i = 0; i < MidiKeyboard.MPTK_CountOut(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetOutName(i)}");
            Debug.Log("---------------------------------------------------");
        }

        /// <summary>@brief
        /// Open a device for output. The index is the same read with MPTK_GetOutName
        /// </summary>
        public void OpenDevice()
        {
            try
            {
                int index = InputIndexDevice.Value;
                MidiKeyboard.MPTK_OpenOut(index);
                CheckStatus($"Open Device on device: {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        // Set from the UI
        public void PlayRandomNote()
        {
            PlayOneNote(UnityEngine.Random.Range(-12, +12));
        }

        // Also set from the UI
        public void PlayOneNote(int random)
        {
            MPTKEvent midiEvent;

            int index = InputIndexDevice.Value;

            // playing a NoteOn
            midiEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0x64, // Sound can vary depending on the velocity
                Delay = 0,
            };
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            CheckStatus($"Play NoteOn on device: {index}");

            // Send Notoff with a delay of 2000 milliseconds
            midiEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOff,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0,
                Delay = 2000,
            };
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            // When event is delayed, last status is sent when event is send, so after the delay!
        }

        /// <summary>
        /// Linked to UI button for sending MIDI timing message
        /// </summary>
        /// <param name="select"></param>
        public void SendSystemRealTimeMessage(int select)
        {
            MPTKEvent midiEvent = null;

            int index = InputIndexDevice.Value;

            // playing a NoteOn
            switch (select)
            {
                case 0: midiEvent = new MPTKEvent() { Command = MPTKCommand.TimingClock }; break;
                case 1: midiEvent = new MPTKEvent() { Command = MPTKCommand.StartSequence }; break;
                case 2: midiEvent = new MPTKEvent() { Command = MPTKCommand.ContinueSequence }; break;
                case 3: midiEvent = new MPTKEvent() { Command = MPTKCommand.StopSequence }; break;
                case 4: midiEvent = new MPTKEvent() { Command = MPTKCommand.AutoSensing }; break;
            }
            if (midiEvent != null)
            {
                MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
                CheckStatus($"Send {midiEvent.Command} on device: {index}");
            }
        }

        // Set from the UI
        public void CloseDevice()
        {
            int index = 0;
            try
            {
                index = InputIndexDevice.Value;

                MidiKeyboard.MPTK_CloseOut(index);
                CheckStatus($"Close Device on device: {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        private static bool CheckStatus(string message)
        {
            MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
            if (status == MidiKeyboard.PluginError.OK)
            {
                Debug.Log(message + " ok");
                return true;
            }
            else
            {
                Debug.Log(message + $" not ok - {status}");
                return false;
            }
        }

        private void Update()
        {
            try
            {
                // Count of avialable MIDI events from the external MIDI keyboard
                TextCountEventQueue.text = $"Read queue: {MidiKeyboard.MPTK_SizeReadQueue()}";

                if (ToggleMidiRead.isOn && !ToggleRealTimeRead.isOn)
                {
                    // Check every TimeForRefresh millisecond if a new device is connected or is disconnected
                    if (Time.fixedUnscaledTime > TimeForRefresh)
                    {
                        TimeForRefresh = Time.fixedUnscaledTime + DelayToRefreshDeviceMilliSeconds / 1000f;
                        // Open or refresh midi input 
                        MidiKeyboard.MPTK_OpenAllInp();
                        MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                        if (status != MidiKeyboard.PluginError.OK)
                            Debug.LogWarning($"Midi Keyboard error, status: {status}");
                    }

                    // Read each events available from the MIDI keyboard message queue by max 100 to avoid locking Unity
                    // See other method to read MIDI events from a callback with MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    int count = 0;
                    while (count++ < 100)
                    {
                        // MIDI is read from the queue and transformed to a MPTKEvent message.
                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        if (midievent == null)
                            break;

                        // Play a MIDI event with the Maestro prefab
                        ProcessEvent(midievent);
                    }
                }
            }
            catch (System.Exception ex)
            {
                //MidiPlayerGlobal.ErrorDetail(ex);
                Debug.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Play a MIDI event with the Maestro prefab
        /// </summary>
        /// <param name="midievent"></param>
        private void ProcessEvent(MPTKEvent midievent)
        {
            midiStreamPlayer.MPTK_PlayDirectEvent(midievent);
            Debug.Log(midievent);
        }

        // Set from the UI
        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}