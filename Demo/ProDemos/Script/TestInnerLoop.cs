#define MPTK_PRO
using UnityEngine;
using MidiPlayerTK;
using UnityEngine.Scripting;
using MPTK.NAudio.Midi;

namespace DemoMVP
{
    /// <summary>@brief
    /// Load a MIDI and play with inner loop between two ticks positions. 
    /// 
    /// As usual with a MVP demo, focus is on the essentials:
    ///     - no value check, 
    ///     - limited error catch, 
    ///     - no optimization, 
    ///     - limited functions
    ///     - ...
    /// 
    /// </summary>
    public class TestInnerLoop : MonoBehaviour
    {
        #region Define variables visible from the inpector
        [Header("A MidiFilePlayer prefab must exist in the hierarchy")]
        /// <summary>@brief
        /// MPTK component able to play a Midi file from your list of Midi file. This PreFab must be present in your scene.
        /// </summary>
        public MidiFilePlayer midiFilePlayer;

        [Header("Value from MidiFilePlayer, readonly")]
        public long TickPlayer;
        public long TickCurrent;
        public string MeasurePlayer;

        [Header("Value from the inner loop, readonly")]

        public int LoopCount;
        public bool loopEnabled;

        [Header("Set MIDI tick loop values")]

        [Range(0, 40000)]
        public long TickStart;

        [Range(0, 40000)]
        public long TickResume;

        [Range(0, 40000)]
        public long TickEnd;

        [Header("Calculate tick from measure and quarter position")]

        [Range(1, 100)]
        public int Measure;

        [Range(1, 8)]
        public int Quarter;

        public long Tick;

        [Header("Set MIDI loop count")]
        [Range(0, 10)]
        public int LoopMax;

        public bool LoopFinished;

        #endregion


        //! [ExampleMidiInnerLoop]
        // Full source code in TestInnerLoop.cs
        // As usual with a MVP demo, focus is on the essentials:
        //     - no value check, 
        //     - limited error catch, 
        //     - no optimization, 
        //     - limited functions
        //     - ...

        // Contains a reference to the current InnerLoop instance, useful only for clarity in the demo ...
        private MPTKInnerLoop innerLoop;

        // Start is called before the first frame update
        void Start()
        {
            // Find a MidiFilePlayer in the scene hierarchy
            // (Innerloop works also with MidiExternalPlayer, see TestMidiGenerator.cs)
            // ----------------------------------------------

            midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the Maestro menu.");
                return;
            }

            // Preload the MIDI file to be able to set MIDI attributes before playing.
            // In particular MPTK_InnerLoop which is cleared when MIDI is loaded.
            midiFilePlayer.MPTK_Load();

            // Get a shortcut (for clarity) to the inner loop instance (instanciated in the awake phase of the MidiFilePlayer)
            // You can also instanciate or manage your own references and set midiFilePlayer.MPTK_InnerLoop with your MPTKInnerLoop instance.
            innerLoop = midiFilePlayer.MPTK_InnerLoop;

            // No log for this demo, rather we prefer using a callback.
            innerLoop.Log = false;

            // Define C# event of type Func() for each loop phase change: Start --> Resume --> Resume --> ... -> Exit
            // If return is false then looping can be earlier ended.
            // It's also possible to set innerLoop.Finished to True at all places in your script
            // but the loop will finished only when tickPlayer reaches the end of the loop.
            innerLoop.OnEventInnerLoop = (MPTKInnerLoop.InnerLoopPhase mode, long tickPlayer, long tickSeek, int count) =>
            {
                Debug.Log($"Inner Loop {mode} - MPTK_TickPlayer:{tickPlayer} --> TickSeek:{tickSeek} Count:{count}/{innerLoop.Max}");
                if (mode == MPTKInnerLoop.InnerLoopPhase.Exit)
                    // Set the value for the Unity User Interface to be able to reactivate the loop.
                    LoopFinished = true;
                return true;
            };

            innerLoop.Enabled = true;

            midiFilePlayer.MPTK_Play(alreadyLoaded: true);
        }

        // Update is called once per frame
        void Update()
        {
            if (midiFilePlayer != null && midiFilePlayer.MPTK_MidiLoaded != null)
            {
                // Display current real-time tick value of the MIDI sequencer
                TickPlayer = midiFilePlayer.MPTK_MidiLoaded.MPTK_TickPlayer;

                // Display tick value of the last MIDI event read by the MIDI sequencer.
                TickCurrent = midiFilePlayer.MPTK_MidiLoaded.MPTK_TickCurrent;

                // Display current measure and beat value of the last MIDI event read by the MIDI sequencer.
                MeasurePlayer = $"{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentMeasure}.{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentBeat}   -   Last measure: {midiFilePlayer.MPTK_MidiLoaded.MPTK_MeasureLastNote}";

                // These parameters can be changed dynamically with the inspector
                innerLoop.Max = LoopMax;
                innerLoop.Start = TickStart;
                innerLoop.Resume = TickResume;
                innerLoop.End = TickEnd;
                innerLoop.Finished = LoopFinished;

                // These values are read from the inner loop instance and display on the UI.
                loopEnabled = innerLoop.Enabled;
                LoopCount = innerLoop.Count;

                // Calculate tick position of a measure (just for a demo how to calculate tick from bar). 
                // So, it's easy to create loop based on measure.
                Tick = MPTKSignature.MeasureToTick(midiFilePlayer.MPTK_MidiLoaded.MPTK_SignMap, Measure);
                
                // Add quarter. Beat start at the begin of the measure (Beat = 1).
                Tick += (Quarter - 1) * midiFilePlayer.MPTK_DeltaTicksPerQuarterNote;
            }
        }

        //! [ExampleMidiInnerLoop]
    }
}