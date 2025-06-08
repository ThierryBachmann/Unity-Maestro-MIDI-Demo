using UnityEngine;
using MidiPlayerTK;

namespace DemoMVP
{
    /// <summary>
    /// Functionality Demonstrated:
    /// - how to change MIDI events at runtime.
    /// - how to dynamically create a MidiFIlePlayer, there is no need to add MPTK prefab to the scene.
    /// 
    /// Steps:
    /// 1. Attach this script to a GameObject in Unity.
    /// 2. Run the scene.
    /// 3. Change option in the GameObject inspector
    /// 
    /// Documentation References:
    /// - MIDI File Player: https://paxstellar.fr/midi-file-player-detailed-view-2/
    /// - MPTKEvent: https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
    /// 
    /// A Minimum Viable Product (MVP) that focuses on the essentials of the Maestro API functionality. 
    /// This script demonstrates only a few basic functions to help users get started with the API.
    /// - Error handling is minimal, the user interface is basic, and manipulations in Unity are minimized.
    /// - Prefabs like `MidiFilePlayer` and `MidiStreamPlayer` are essential components of Maestro. 
    ///   While this demo creates these prefabs via script, it is recommended to add them in the Unity editor 
    ///   for real projects to take full advantage of the Inspector's parameters.
    /// </summary>
    public class HackYourMidiPlayer : MonoBehaviour
    {
        [Header("Enable Arpeggio Effect")]
        public bool toggleAddArpeggio = true;

        [Header("MIDI Channel for Arpeggio (-1 for all channels)")]
        public int channelArpeggio = 0;

        [Header("Scale for Arpeggio")]
        public MPTKScaleName scaleArpeggio = MPTKScaleName.MajorMelodic;

        [Header("Number of Arpeggio Notes")]
        public int countArpeggio = 2;

        [Header("Skip Preset Change Events")]
        public bool toggleEnableChangePreset = false;

        [Header("Randomize Tempo Events")]
        public bool toggleEnableChangeTempo = false;

        [Header("Log MIDI Events in Console")]
        public bool toggleLogMidiEvent = false;

        private MPTKScaleLib scaleForArpeggio; // Holds intervals for the arpeggio.
        private MPTKScaleName currentScale = MPTKScaleName.MajorMelodic; // Checks changes from Inspector.
        private MidiFilePlayer midiFilePlayer; // Reference to a dynamically added MidiFilePlayer.

        private void Awake()
        {
            Debug.Log($"Awake: Adding MidiFilePlayer component to '{gameObject.name}'");

            // Add the MidiFilePlayer component to this GameObject and configure it.
            midiFilePlayer = gameObject.AddComponent<MidiFilePlayer>();
            midiFilePlayer.MPTK_CorePlayer = true;
            midiFilePlayer.MPTK_DirectSendToPlayer = true;
            midiFilePlayer.MPTK_EnableChangeTempo = true;

            // Ensure the global MIDI manager is available.
            if (MidiPlayerGlobal.Instance == null)
                gameObject.AddComponent<MidiPlayerGlobal>();
        }

        public void Start()
        {
            // Create a scale (list of intervals) based on the selected scale.
            // Note: This must be done on the main Unity thread.
            scaleForArpeggio = MPTKScaleLib.CreateScale(index: currentScale, log: false);

            Debug.Log("Start: Selecting and playing the first MIDI file in the database.");

            midiFilePlayer.MPTK_LogEvents = true;
            midiFilePlayer.MPTK_MidiIndex = 0; // Select the first MIDI file in the database.
            midiFilePlayer.OnMidiEvent = MaestroOnMidiEvent; // Set the MIDI event callback.
            midiFilePlayer.MPTK_Play(); // Start playing the MIDI file.
        }

        /// <summary>
        /// Callback triggered for each MIDI event just before it is sent to the synthesizer.
        /// Use this to modify, add, or skip MIDI events.
        /// - Avoid heavy processing here as it could disrupt musical timing.
        /// - Unity APIs (except Debug.Log) cannot be used because this runs outside the Unity main thread.
        /// </summary>
        /// <param name="midiEvent">The MIDI event being processed.</param>
        /// <returns>True to play the event, false to skip it.</returns>
        bool MaestroOnMidiEvent(MPTKEvent midiEvent)
        {
            // By default, the MIDI event will be sent to the synthesizer.
            bool keepEventToPlay = true;

            switch (midiEvent.Command)
            {
                case MPTKCommand.NoteOn:
                    if (toggleAddArpeggio)
                    {
                        // Calculate delay (in milliseconds) between arpeggio notes based on the tempo.
                        //  - 1/16 tick delay, a quarter divides by four: MPTK_DeltaTicksPerQuarterNote / 4
                        //  - transform tick value to milliseconds with MPTK_Pulse
                        // The current tempo (MPTK_Pulse, millisecond of a tick) is used for this calculation,
                        // so we need to update the value at each call in case of a tempo change has been done.
                        long arpeggioDelay = (long)(midiFilePlayer.MPTK_DeltaTicksPerQuarterNote / 4 * midiFilePlayer.MPTK_Pulse);

                        if (channelArpeggio == -1 || channelArpeggio == midiEvent.Channel)
                        {
                            if (scaleForArpeggio != null)
                            {
                                // Add additional notes to create the arpeggio.
                                for (int interval = 0; interval < scaleForArpeggio.Count && interval < countArpeggio; interval++)
                                {
                                    // Add a note same channel, duration, velocity, but add an interval to the value.
                                    // If delay is not set (or defined to 0), a chord will be played.
                                    MPTKEvent noteArpeggio = new MPTKEvent()
                                    {
                                        Command = MPTKCommand.NoteOn, // midi command
                                        Value = midiEvent.Value + scaleForArpeggio[interval],
                                        Channel = midiEvent.Channel,
                                        Duration = midiEvent.Duration,
                                        Velocity = midiEvent.Velocity,
                                        Delay = interval * arpeggioDelay, // delay in millisecond before playing the arpeggio note.
                                    };
                                    // Add immediately this note to the MIDI synth for immediate playing (with the delay defined in the event)
                                    midiFilePlayer.MPTK_PlayDirectEvent(noteArpeggio);
                                }
                            }
                        }
                    }
                    break;

                case MPTKCommand.PatchChange:
                    if (toggleEnableChangePreset)
                    {
                        // Transform Patch change event to Meta text event: related channel will played the default preset 0.
                        // TextEvent has no effect on the MIDI synth but is displayed in the demo windows.
                        // It would also been possible de change the preset to another instrument.
                        midiEvent.Command = MPTKCommand.MetaEvent;
                        midiEvent.Meta = MPTKMeta.TextEvent;
                        midiEvent.Info = $"Skipping Preset Change: {midiEvent.Value}";
                        Debug.Log(midiEvent.Info);
                    }
                    break;

                case MPTKCommand.MetaEvent:
                    if (midiEvent.Meta == MPTKMeta.SetTempo && toggleEnableChangeTempo)
                    {
                        // Randomize tempo (in BPM).
                        // Warning: this callback is run out of the main Unity thread, Unity API (like UnityEngine.Random) can't be used.
                        System.Random rnd = new System.Random();
                        midiFilePlayer.MPTK_Tempo = rnd.Next(30, 240);
                        // Value contains Microseconds Per Beat Note, convert to BPM for display.
                        Debug.Log($"Tempo changed from {MPTKEvent.QuarterPerMicroSecond2BeatPerMinute(midiEvent.Value):F0} to {midiFilePlayer.MPTK_Tempo} BPM");
                    }
                    break;
            }

            return keepEventToPlay;
        }

        private void Update()
        {
            // Update the scale if it has been changed in the Inspector.
            if (currentScale != scaleArpeggio)
            {
                // Create a scale (a list of intervals) related to the selected scale.
                // Be aware that this method must be call from the main Unity thread not from the OnMidiEvent callback.
                currentScale = scaleArpeggio;
                scaleForArpeggio = MPTKScaleLib.CreateScale(index: currentScale, log: false);
            }

            // Enable or disable MIDI event logging in real time.
            midiFilePlayer.MPTK_LogEvents = toggleLogMidiEvent;
        }
    }
}
