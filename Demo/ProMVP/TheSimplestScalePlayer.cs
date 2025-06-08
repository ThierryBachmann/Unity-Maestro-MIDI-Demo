using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using MPTK.NAudio.Midi;

namespace DemoMVP
{
    /// <summary>@brief
    /// Minimum Viable Product focus on the essentials of a Maestro function. 
    /// Only a few functions are presented. Links to the documentation are provided for further exploration.
    /// Therefore, error tests are absent, the user interface is almost non-existent and manipulations in Unity are reduced. 
    /// 
    /// The goal is rather to learn how to use the Maestro API and then progress by building more complex applications.
    /// Maestro is based on the use of prefabs (MidiFilePlayer, MidiStreamPlayer, …) which must be added in the Unity editor in the hierarchy of your project.
    /// In these demos, we prefer to create the prefabs by script to avoid manipulations in the editor. 
    /// It is rather recommended to create Prefabs in Unity Editor to take advantage of the Inspectors and its many directly accessible parameters.
    /// 
    /// Demonstration:      Play two notes from the scale "Major melodic" (C5 and E5) when the space key is pressed, stop the notes when the key is released.
    ///                     Play with "Minor melodic" is Shift+Space is pressed.
    /// Implementation:     Add an empty gameobject in your Unity Scene then add this script to this gameobject.
    /// Running and using:  Play and stroke the space key.
    /// 
    /// </summary>
    public class TheSimplestScalePlayer : MonoBehaviour
    {
        // This class is able to play MIDI event: play note, play chord, patch change, apply effect, ... see doc!
        // https://mptkapi.paxstellar.com/d9/d1e/class_midi_player_t_k_1_1_midi_stream_player.html
        private MidiStreamPlayer midiStreamPlayer;

        // Description of a MIDI event. MPTKEvent are an input for the MidiStreamPlayer
        // https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
        private MPTKEvent mptkEvent1;
        private MPTKEvent mptkEvent2;

        // Holds a scale from the scale lib (not related to a tonic, contains only intervals)
        // https://mptkapi.paxstellar.com/da/dde/class_midi_player_t_k_1_1_m_p_t_k_range_lib.html
        private MPTKScaleLib mptkScaleMajor;
        private MPTKScaleLib mptkScaleMinor;

        private void Awake()
        {
            // Search for an existing prefab in the scene
            midiStreamPlayer = FindFirstObjectByType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
            {
                // All lines bellow are useless if the prefab is found on the Unity Scene.
                Debug.Log("Any MidiStreamPlayer Prefab found in the current Scene Hierarchy.");
                Debug.Log("It will be created by script. For a more serious project, add it to the scene!");

                // Create an empty gameobject in the scene
                GameObject go = new GameObject("HoldsMaestroPrefab");

                // MidiPlayerGlobal load the SoundFont. It is a singleton, only one instance will be created. 
                go.AddComponent<MidiPlayerGlobal>();

                // Add a MidiStreamPlayer prefab.
                midiStreamPlayer = go.AddComponent<MidiStreamPlayer>();

                // *** Set essential parameters ***

                // Core player is using internal thread for a good musical rendering
                midiStreamPlayer.MPTK_CorePlayer = true;

                // Display log about the MIDI events played.
                // Enable Monospace font in the Unity log window for better display.
                midiStreamPlayer.MPTK_LogEvents = true;

                // If disabled, nothing is send to the MIDI synth!
                midiStreamPlayer.MPTK_DirectSendToPlayer = true;
            }

            // Create a scale from the scale "Major melodic". Log enabled to display the content of the scale.
            mptkScaleMajor = MPTKScaleLib.CreateScale(MPTKScaleName.MajorMelodic, log: true);

            // Create a scale from the scale "Minor melodic". Log enabled to display the content of the scale.
            mptkScaleMinor = MPTKScaleLib.CreateScale(MPTKScaleName.MinorMelodic, log: true);

            Debug.Log("Use Space key to play the tonic and the 2nd note.");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Create a MIDI note-on for the tonic of the scale
                mptkEvent1 = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn, // midi command for starting playing a note
                    Value = 60, // 60 for C5
                };

                // Create a second MIDI note-on for the third of the scale
                int interval; 
                if (Input.GetKey(KeyCode.LeftShift))
                    interval = mptkScaleMajor[2]; // -> 3
                else
                    interval = mptkScaleMinor[2]; // -> 4

                Debug.Log($"interval: {interval}");

                mptkEvent2 = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn, // midi command for starting playing a note
                    Value = 60 + interval, // C5 + third 
                };

                // Play the MIDI events
                midiStreamPlayer.MPTK_PlayEvent(mptkEvent1);
                midiStreamPlayer.MPTK_PlayEvent(mptkEvent2);
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                // Stop playing our "Hello, Scale!"
                midiStreamPlayer.MPTK_StopEvent(mptkEvent1);
                midiStreamPlayer.MPTK_StopEvent(mptkEvent2);
            }
        }
    }
}