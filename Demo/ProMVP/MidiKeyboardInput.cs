using UnityEngine;
using System;
using MidiPlayerTK;

namespace DemoMPTK
{
    /// <summary>@brief
    /// Example of MVP implementation for reading MIDI event from a keyboard. 
    /// See here for detailed API doc:
    ///       https://mptkapi.paxstellar.com/da/d70/class_midi_player_t_k_1_1_midi_keyboard.html
    /// For testing:
    ///     - create a new scene
    ///     - add an empty gameObject
    ///     - add this script to the gameObject
    ///     - connect your MIDI keyboard and run!
    ///     - Each MIDI event read from the keyboard are log in the console (not playing)
    /// </summary>
    public class MidiKeyboardInput : MonoBehaviour
    {
        private bool midiKeyboardReady = false;

        private void Start()
        {
            // Midi Keyboard need to be initialized at start
            if (MidiKeyboard.MPTK_Init())
            {
                midiKeyboardReady = true;

                // Log version of the Midi plugins
                Debug.Log(MidiKeyboard.MPTK_Version());

                // Open or refresh all input MIDI devices able to send MIDI message
                MidiKeyboard.MPTK_OpenAllInp();
            }
        }

        private void OnApplicationQuit()
        {
            if (midiKeyboardReady)
                // Mandatory to avoid Unity crash!
                MidiKeyboard.MPTK_CloseAllInp();
        }

        void Update()
        {
            if (midiKeyboardReady)
            {
                int count = 0;
                try
                {
                    MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                    if (status != MidiKeyboard.PluginError.OK)
                        Debug.LogWarning($"MIDI Keyboard error, status: {status}");


                    // Read message available in the queue
                    // Limit the count of read messages to avoid locking Unity
                    while (count < 100)
                    {
                        count++;

                        // Read a MIDI event if available
                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        if (midievent == null)
                            break;

                        // ... and log 
                        Debug.Log($"[{DateTime.UtcNow.Millisecond:00000}] {midievent}");
                    }
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
        }
    }
}