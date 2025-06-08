using UnityEngine;
using MidiPlayerTK;
using System.Threading;
using System.Collections.Concurrent;

namespace DemoMPTK
{
    /// <summary>
    /// Demonstrates an example of a minimal viable product (MVP) implementation for reading MIDI events 
    /// from a MIDI keyboard and playing them using the MPTK framework.
    /// 
    /// This implementation reduces latency by avoiding the Unity main thread for MIDI event reading and playback. 
    /// The remaining latency factors include:
    /// - The MIDI input device (especially with USB MIDI interfaces).
    /// - FMOD audio settings (consider using a smaller buffer size for lower latency).
    /// 
    /// Documentation References:
    /// - MIDI Keyboard API: https://mptkapi.paxstellar.com/da/d70/class_midi_player_t_k_1_1_midi_keyboard.html
    /// - MIDI Stream Player API: https://mptkapi.paxstellar.com/d9/d1e/class_midi_player_t_k_1_1_midi_stream_player.html
    /// 
    /// To test this script use the provided scene (MidiKeyboardThread) or:
    /// 1. Download and install the MIDI Keyboard tool from https://paxstellar.fr/class-midikeyboard/ (if not already installed).
    /// 2. Add a GameObject (can be empty) to your Unity scene and attach this script to the GameObject.
    /// 3. Add a MidiStreamPlayer prefab to your scene (right click on the Hierarchy Tab, menu Maestro)
    /// 4. Connect a MIDI keyboard to your computer and run the Unity scene.
    /// 5. All MIDI events received from the keyboard will be played through the MPTK MIDI Synth.
    /// 6. Add your custom logic in Update() method for visualization, game interaction, or other Unity behaviors (optional).
    /// </summary>
    public class MidiKeyboardThread : MonoBehaviour
    {
        // Indicates whether the MIDI keyboard is ready to use
        private bool midiKeyboardReady = false;

        // Thread for reading and processing MIDI events without blocking the Unity main thread
        private Thread midiThread;

        // Handles MIDI event playback, such as notes, chords, patch changes, and effects
        private MidiStreamPlayer midiStreamPlayer;

        // Queue for storing MIDI events, allowing integration with custom Unity behaviors in the Update() method
        private ConcurrentQueue<MPTKEvent> midiQueue = new ConcurrentQueue<MPTKEvent>();

        private void Awake()
        {
            // Look for an existing MidiStreamPlayer prefab in the scene
            midiStreamPlayer = FindFirstObjectByType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
            {
                Debug.LogWarning("No MidiStreamPlayer Prefab found in the current Scene Hierarchy. Add one via the 'Maestro / Add Prefab' menu.");
            }
        }

        private void Start()
        {
            // Initialize the MIDI keyboard at the start
            if (midiStreamPlayer != null && MidiKeyboard.MPTK_Init())
            {
                midiKeyboardReady = true;

                // Log the MIDI Keyboard version
                Debug.Log(MidiKeyboard.MPTK_Version());

                // Open or refresh all MIDI input devices that can send MIDI messages
                MidiKeyboard.MPTK_OpenAllInp();

                // Start a dedicated thread for reading MIDI events and playing them
                midiThread = new Thread(ThreadMidiPlayer);
                midiThread.Start();
            }
        }

        /// <summary>
        /// Thread for continuously reading MIDI events from the keyboard and sending them to the MPTK MIDI Synth.
        /// This thread avoids Unity's main thread for reduced latency.
        /// </summary>
        private void ThreadMidiPlayer()
        {
            while (midiKeyboardReady)
            {
                try
                {
                    // Check for errors in the MIDI plugin
                    MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                    if (status != MidiKeyboard.PluginError.OK)
                    {
                        Debug.LogWarning($"MIDI Keyboard error, status: {status}");
                    }

                    // Read a MIDI event if available
                    MPTKEvent midiEvent = MidiKeyboard.MPTK_Read();

                    if (midiEvent != null)
                    {
                        // Add the event to the queue for custom processing in Unity's Update() method
                        midiQueue.Enqueue(midiEvent);

                        // Immediately play the MIDI event
                        midiStreamPlayer.MPTK_PlayEvent(midiEvent);
                    }

                    // Add a short delay to prevent overloading the CPU
                    Thread.Sleep(1);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"ThreadMidiPlayer - {ex}");
                    break;
                }
            }
        }

        private void Update()
        {
            if (midiKeyboardReady && !midiQueue.IsEmpty)
            {
                if (midiQueue.TryDequeue(out MPTKEvent midiEvent))
                {
                    // Add your custom logic here for visualization, game interaction, or other Unity behaviors
                    // The music played on the MIDI keyboard will continue in the background
                    Debug.Log(midiEvent.ToString());
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (midiKeyboardReady)
            {
                // Close all MIDI input devices to prevent Unity crashes
                MidiKeyboard.MPTK_CloseAllInp();
            }
            midiKeyboardReady = false;
        }
    }
}
