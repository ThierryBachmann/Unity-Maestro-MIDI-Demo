using MidiPlayerTK;
using System.Collections.Generic;
using UnityEngine;

namespace DemoMPTK
{
    /// <summary>
    /// Demonstrates MIDI track-based spatialization using the MidiSpatializer prefab.
    /// This example visually maps MIDI tracks to GameObjects in the scene.
    /// 
    /// Key Notes:
    /// - Only the first six tracks are processed. Other tracks will be centered in the scene,
    ///   resulting in poor 3D spatialization.
    /// - Some MIDI files contain only one track; in such cases, spatialization will not be noticeable.
    ///   You can easily modify this demo to enable channel-based spatialization instead.
    /// - Texts above the cylinders are derived from MetaEvent and SequenceTrackName MIDI events,
    ///   if they exist in the MIDI file.
    ///   See: https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
    /// 
    /// Important Notes:
    /// 1) Don't forget to add a Unity spatializer to your project! https://paxstellar.fr/setup-mptk-sound-spatialization/
    ///    I recommend Steam Audio (free ad for a free product!) https://valvesoftware.github.io/steam-audio/downloads.html
    /// 2) Once you have mastered this MVP demo, you will be ready to explore the more advanced "SpatializerFly" demo.
    /// 

    /// Steps to reproduce this demo:
    /// 1) Create a 3D scene with several 3D objects (e.g., the Track__ cylinders in the demo).
    /// 2) Add a MidiSpatializer prefab to your scene.
    /// 3) In the MidiSpatializer prefab:
    ///     3.1) Disable "Channel Spatialization."
    ///     3.2) Enable "Track Spatialization."
    ///     3.3) Set "Max Spatial Synth" to a minimum of 16.
    ///     3.4) Configure other MIDI parameters as usual, using a MIDI File prefab.
    ///     3.5) Attach this script (Spatializer3D.cs) as a component.
    /// 4) Configure the Spatializer3D.cs inspector parameters:
    ///     4.1) Assign a reference to the "Midi Spatializer" (this object).
    ///     4.2) Assign a reference to the "Board".
    ///     4.3) Assign the first six cylinders to "GameObjectsHoldingMidiTrack."
    /// 5) Press Play!
    /// 
    /// </summary>
    public class Spatializer3D : MonoBehaviour
    {
        /// <summary>
        /// MPTK component responsible for reading and playing MIDI files. Inherits from MidiSynth.
        /// Must be assigned in the inspector or retrieved using FindFirstObjectByType<MidiSpatializer>() in Start().
        /// </summary>
        public MidiSpatializer midiSpatializer;

        /// <summary>
        /// The rotating board holding the cylinders. Rotation is handled in Update().
        /// </summary>
        public Transform Board;

        /// <summary>
        /// Array of 3D GameObjects representing the first six MIDI tracks.
        /// These six cylinders are assigned to this array in the inspector.
        /// </summary>
        public Transform[] GameObjectsHoldingMidiTrack;

        // Will contains a reference to the game object (cylinder) which hold the MIDI synth (just for clarity reason).
        private Transform Cylinder;

        /// <summary>
        /// Rotation speed of the board.
        /// </summary>
        [Header("Rotation Speed of the Board")]
        [Range(-100, 100)]
        public float Speed;

        /// <summary>
        /// Global volume of the MIDI player.
        /// </summary>
        [Header("MIDI Player Global Volume")]
        [Range(0, 1)]
        public float Volume;

        /// <summary>
        /// Current rotation angle of the board.
        /// </summary>
        private float angle;

        /// <summary>
        /// Static variable to store volume, ensuring consistency across all instantiated synths.
        /// </summary>
        private static float volume;

        /// <summary>
        /// Important details on how the MidiSpatializer prefab functions:
        /// - At runtime, the MidiSpatializer prefab instantiates multiple MIDI synths based on this GameObject.
        /// - Update() and Start() will be called for every instantiated MIDI synth, as well as the MIDI reader.
        /// - It is crucial to differentiate between the two:
        ///   - The MIDI reader handles user interactions.
        ///   - Instantiated synths modify channel or track behaviors (track-based in this demo).
        /// </summary>
        private void Start()
        {
            Debug.Log($"Start TestSpatializerFly {midiSpatializer.MPTK_SpatialSynthIndex}");

            // Each MPTK_SpatialSynthIndex corresponds to a specific spatial synth instance.
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                // Main MIDI Player (MIDI Reader) initialization - a good place to add UI listeners.
                // No action needed here.
            }
            else
            {
                // Process all Spatial MIDI synths instances to be associated to GameObjects.
                if (midiSpatializer.MPTK_SpatialSynthIndex < GameObjectsHoldingMidiTrack.Length &&
                    GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex] != null)
                {
                    // Set a reference to the game object (cylinder) which hold the MIDI synth (just for clarity reason).
                    Cylinder = GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex];

                    // This is where the magic happens!
                    // By moving the synth in the 3D space, its sound will originate from that position.
                    // The spatial MIDI synth is assigned as a child of the corresponding cylinder in the scene.
                    this.transform.SetParent(Cylinder);

                    // Note: at runtime, these MIDI synths will be moved under the GameObject cylinder in your project.

                    // Center the Spatial MIDI Synth (and, by the way its AudioSource) on the parent cylinder.
                    this.transform.localPosition = Vector3.zero;

                    // There is no need of OnEventNotesMidi for 3D spatialization,
                    // just to have a visual effect on the cylinders when notes
                    // are received on each MPTK Synth associated to each gameobject.
                    midiSpatializer.OnEventNotesMidi.AddListener((list) =>
                    {
                        //Debug.Log($"{midiSpatializer.MPTK_SpatialSynthIndex} {list.Count}");

                        // Change height of the cylinder which holds the MIDI synth
                        Vector3 scale = Cylinder.localScale;
                        scale.y = scale.y + list.Count * 1.8f;
                        Cylinder.localScale = scale;
                    });
                }
            }
        }

        /// <summary>
        /// Important details on how the MidiSpatializer prefab functions:
        /// - At runtime, the MidiSpatializer prefab instantiates multiple MIDI synths based on this GameObject.
        /// - Update() and Start() will be called for every instantiated MIDI synth, as well as the MIDI reader.
        /// - It is crucial to differentiate between the two:
        ///   - The MIDI reader handles user interactions.
        ///   - Instantiated synths modify channel or track behaviors (track-based in this demo).
        /// </summary>
        private void Update()
        {
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                // MIDI Reader Updates
                // -------------------

                // UI interactions are applied only to the first MidiSpatializer, which acts as the MIDI reader.
                angle += Time.deltaTime * Speed;
                Board.transform.rotation = Quaternion.Euler(0, angle, 0);

                // Store the current volume setting in a static variable to share it with all synth instances.
                volume = Volume;
            }
            else
            {
                // Updates for all instantiated MIDI synths.
                // This also the good place to change GameObject aspect like dimension, color, ...
                // -------------------------------------------------------------------------------

                // Modify the corresponding GameObject attached to each synth.
                if (midiSpatializer.MPTK_SpatialSynthIndex < GameObjectsHoldingMidiTrack.Length && Cylinder != null)
                {
                    // Update the track name if available.
                    TextMesh textPlayer = Cylinder.GetComponentInChildren<TextMesh>();
                    if (textPlayer != null)
                        textPlayer.text = midiSpatializer.MPTK_TrackName;

                    // Apply the global volume setting to all instantiated MIDI synths.
                    midiSpatializer.MPTK_Volume = volume;

                    if (Cylinder != null)
                    {
                        // Smoothly returns to the initial value (no need for 3D spatialization, just to have a visual effect)
                        Vector3 scale = Cylinder.localScale;
                        scale.y = Mathf.Lerp(Cylinder.localScale.y, 6, Time.deltaTime);
                        Cylinder.localScale = scale;
                    }
                }
            }
        }
    }
}
