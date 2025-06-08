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

        [Header("Readonly values from MidiFilePlayer")]
        public long TickPlayer;
        public long TickCurrent;
        public string MeasurePlayer;

        [Header("Setting the tick values of the inner loop")]

        [Range(0, 40000)]
        public long TickStart;

        [Range(0, 40000)]
        public long TickResume;

        [Range(0, 40000)]
        public long TickEnd;

        [Header("Readonly values from the inner loop")]
        public int LoopCount;
        public bool loopEnabled;

        [Header("Condition to stop looping")]
        [Range(0, 10)]
        public int LoopMax;
        public bool LoopFinished;

        [Header("Calculator: get tick value from measure and quarter value")]

        [Range(1, 100)]
        public int Measure;

        [Range(1, 8)]
        public int Quarter;

        public long Tick;

        #endregion

        //! [ExampleMidiInnerLoop]

        // Full source code in TestInnerLoop.cs
        // As usual with a MVP demo, focus is on the essentials:
        //     - no value check, 
        //     - limited error catch, 
        //     - no optimization, 
        //     - limited functions
        //     - ...

        // Start is called before the first frame update
        void Start()
        {
            // Find a MidiFilePlayer in the scene hierarchy
            // Innerloop works also with MidiExternalPlayer
            // ----------------------------------------------

            midiFilePlayer = FindFirstObjectByType<MidiFilePlayer>();
            if (midiFilePlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the Maestro menu.");
                return;
            }

            // The MPTK_InnerLoop attributes are cleared when the MIDI is loaded.
            // To define the start condition, you need to define a callback function (here StartPlay)
            // that will set the inner loop attributes when the MIDI is started.
            // Note: with this demo the MPTK_InnerLoop attributes are defined in the Unity Update().
            //       So, defining the initial condition is not useful ... just for the demo!
            midiFilePlayer.OnEventStartPlayMidi.AddListener(StartPlay);

            midiFilePlayer.MPTK_Play();
        }

        // Event fired by MidiFilePlayer or MidiExternalPlayer instance when a midi is started.
        // Useful when MPTK properties are cleared when the MIDI is loaded ... for example for MPTK_InnerLoop.
        public void StartPlay(string midiname)
        {
            Debug.Log("Start Midi " + midiname + " Duration: " + midiFilePlayer.MPTK_Duration.TotalSeconds + " seconds");

            // midiFilePlayer.MPTK_InnerLoop is instantiated during the awake phase of the MidiFilePlayer.
            // You can also instantiated or manage your own references and set midiFilePlayer.MPTK_InnerLoop with your MPTKInnerLoop instance.
            midiFilePlayer.MPTK_InnerLoop.Enabled = true;

            // No log from MPTK for this demo, rather we prefer to use a callback to define our own.
            midiFilePlayer.MPTK_InnerLoop.Log = false;

            // Define C# event of type Func() for each loop phase change: Start --> Resume --> ... --> Resume --> Exit
            // If return is false then looping can be exited earlier..
            // It's also possible to set innerLoop.Finished to True anywhere in your script
            // but the loop will not be finished until tickPlayer reaches the end of the loop.
            midiFilePlayer.MPTK_InnerLoop.OnEventInnerLoop = (MPTKInnerLoop.InnerLoopPhase mode, long tickPlayer, long tickSeek, int count) =>
            {
                Debug.Log($"Inner Loop {mode} - MPTK_TickPlayer:{tickPlayer} --> TickSeek:{tickSeek} Count:{count}/{midiFilePlayer.MPTK_InnerLoop.Max}");
                if (mode == MPTKInnerLoop.InnerLoopPhase.Exit)
                    // Set the value for the Unity User Interface to be able to reactivate the loop.
                    LoopFinished = true;
                return true;
            };

            // Set initial inner loop attributes
            SetInnerLoopParameters();

        }

        /// <summary>
        /// Set the Inner loop attributes from the inspector's values.
        /// </summary>
        private void SetInnerLoopParameters()
        {
            // These parameters can be changed dynamically with the inspector
            midiFilePlayer.MPTK_InnerLoop.Max = LoopMax;
            midiFilePlayer.MPTK_InnerLoop.Start = TickStart;
            midiFilePlayer.MPTK_InnerLoop.Resume = TickResume;
            midiFilePlayer.MPTK_InnerLoop.End = TickEnd;
            midiFilePlayer.MPTK_InnerLoop.Finished = LoopFinished;
        }

        // Update is called once per frame
        void Update()
        {
            if (midiFilePlayer != null && midiFilePlayer.MPTK_MidiLoaded != null)
            {
                // Display current real-time tick value of the MIDI sequencer.
                TickPlayer = midiFilePlayer.MPTK_MidiLoaded.MPTK_TickPlayer;

                // Display tick value of the last MIDI event read by the MIDI sequencer.
                TickCurrent = midiFilePlayer.MPTK_MidiLoaded.MPTK_TickCurrent;

                // Display current measure and beat value of the last MIDI event read by the MIDI sequencer.
                MeasurePlayer = $"{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentMeasure}.{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentBeat}   -   Last measure: {midiFilePlayer.MPTK_MidiLoaded.MPTK_MeasureLastNote}";

                // Set inner loop attributes from the inspector's values.
                SetInnerLoopParameters();

                // These values are read from the inner loop instance and display on the UI.
                loopEnabled = midiFilePlayer.MPTK_InnerLoop.Enabled;
                LoopCount = midiFilePlayer.MPTK_InnerLoop.Count;

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