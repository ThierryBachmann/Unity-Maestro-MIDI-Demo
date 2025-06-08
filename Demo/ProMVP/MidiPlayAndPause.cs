using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

namespace DemoMVP
{

    /// <summary>
    /// Demonstrates a technique to partially pause a MIDI player by disabling all channels except the drum channel. 
    /// Unfortunately, it's not possible to directly pause a subset of channels in the MIDI player.
    /// 
    /// To achieve this, two MidiFilePlayers are used:
    /// - **MidiFilePlayerMain**: Starts playing from the beginning.
    /// - **MidiFilePlayerDrum**: Paused at the start, with only channel 9 (the drum channel) enabled.
    /// 
    /// The process is as follows:
    /// 1. At a specified position (PausePosition), the first player is paused, and the second player is resumed.
    /// 2. After a defined duration (PauseDuration), the second player is paused, and the first player resumes from its paused position.
    /// 
    /// To test this script, use the provided scene (MidiPlayAndPause) or:
    /// 1. Add two MidiFilePlayer to your scene
    /// 2. Add a GameObject (can be empty) to your Unity scene.
    /// 3. Attach this script to the GameObject.
    /// 4. Defined the MIDI to play in the first MidiFilePlayer, the second will use the same.
    /// 5. Assign the two MidiFilePlayer in the GameObject inspector in the corresponding field.
    /// 
    /// This is a minimal viable product (MVP) demo focusing on the core functionality:
    /// - Limited value validation
    /// - Limited error handling
    /// - No performance optimizations
    /// </summary>
    public class MidiPlayAndPause : MonoBehaviour
    {
        [Header("Assign two MidiFilePlayers in the scene to this script")]
        public MidiFilePlayer MidiFilePlayerMain;
        public MidiFilePlayer MidiFilePlayerDrum;

        [Header("Position (in ticks) to switch from MidiFilePlayerMain to MidiFilePlayerDrum")]
        public long PausePosition;

        [Header("Duration (in ticks) before returning to MidiFilePlayerMain")]
        public long PauseDuration;

        [Header("Flag to prevent repeated switching")]
        public bool SwitchDone;

        [Header("Check to restore the initial state")]
        public bool ResetLoop;

        void Awake()
        {
            // Ensure both MidiFilePlayers are defined in the Inspector
            if (MidiFilePlayerMain == null)
            {
                Debug.LogWarning("MidiFilePlayerMain is not defined in the Inspector");
                return;
            }
            MidiFilePlayerMain.MPTK_PlayOnStart = true;

            if (MidiFilePlayerDrum == null)
            {
                Debug.LogWarning("MidiFilePlayerDrum is not defined in the Inspector");
                return;
            }
            MidiFilePlayerDrum.MPTK_PlayOnStart = true;

            // Set default values for PausePosition and PauseDuration if not defined
            if (PausePosition == 0) PausePosition = 2000;
            if (PauseDuration == 0) PauseDuration = 1500;
        }

        void Start()
        {
            if (MidiFilePlayerMain != null && MidiFilePlayerDrum != null)
            {
                // Ensure both players use the same MIDI file
                MidiFilePlayerDrum.MPTK_MidiIndex = MidiFilePlayerMain.MPTK_MidiIndex;

                // Configure the drum player channels once it starts
                MidiFilePlayerDrum.OnEventStartPlayMidi.AddListener(info =>
                {
                    // Disable all channels except the drum channel (channel 9)
                    MidiFilePlayerDrum.Channels.EnableAll = false;
                    MidiFilePlayerDrum.Channels[9].Enable = true;

                    Debug.Log("MidiFilePlayerDrum is paused at the start");

                    // Warning: never use MPTK_Start() because the channels setting will be reset to default (enable all)
                    MidiFilePlayerDrum.MPTK_Pause();
                });

                // Log beat events for debugging
                MidiFilePlayerMain.OnBeatEvent = (int time, long tick, int measure, int beat) =>
                {
                    Debug.Log($"MidiFilePlayerMain beat event: Tick={tick}, Beat={beat}/{measure}, SwitchDone={SwitchDone}");
                };
                MidiFilePlayerDrum.OnBeatEvent = (int time, long tick, int measure, int beat) =>
                {
                    Debug.Log($"MidiFilePlayerDrum beat event: Tick={tick}, Beat={beat}/{measure}, SwitchDone={SwitchDone}");
                };
            }
        }

        void Update()
        {
            if (MidiFilePlayerMain != null && MidiFilePlayerDrum != null)
            {
                if (!SwitchDone)
                {
                    // Switch from main player to drum player when reaching the specified tick
                    // Warning: never use MPTK_Start() because the channels setting will be reset to default (enable all)

                    if (!MidiFilePlayerMain.MPTK_IsPaused && MidiFilePlayerMain.MPTK_TickCurrent > PausePosition)
                    {
                        Debug.Log("Pausing main player and resuming drum player");
                        MidiFilePlayerMain.MPTK_Pause();
                        MidiFilePlayerDrum.MPTK_UnPause();
                        MidiFilePlayerDrum.MPTK_TickCurrent = PausePosition;
                    }

                    // Switch back to main player after the pause duration
                    if (!MidiFilePlayerDrum.MPTK_IsPaused && MidiFilePlayerDrum.MPTK_TickCurrent > PausePosition + PauseDuration)
                    {
                        Debug.Log("Pausing drum player and resuming main player");
                        MidiFilePlayerDrum.MPTK_Pause();
                        MidiFilePlayerMain.MPTK_UnPause();
                        MidiFilePlayerMain.MPTK_TickCurrent = PausePosition;
                        SwitchDone = true;
                    }
                }

                // Restore initial conditions when ResetLoop is checked
                if (ResetLoop)
                {
                    Debug.Log("Resetting to initial state");
                    ResetLoop = false;
                    SwitchDone = false;

                    // Reset main player
                    MidiFilePlayerMain.MPTK_TickCurrent = 0;
                    // Update internal tick, not updated when the player is paused
                    MidiFilePlayerMain.MPTK_MidiLoaded.MPTK_TickCurrent = 0;
                    MidiFilePlayerMain.MPTK_UnPause();

                    // Reset drum player
                    MidiFilePlayerDrum.MPTK_TickCurrent = PausePosition;
                    // Update internal tick, not updated when the player is paused
                    MidiFilePlayerDrum.MPTK_MidiLoaded.MPTK_TickCurrent = PausePosition;
                    MidiFilePlayerDrum.MPTK_Pause();
                }
            }
        }
    }
}