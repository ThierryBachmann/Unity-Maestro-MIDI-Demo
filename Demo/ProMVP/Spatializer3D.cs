using MidiPlayerTK;
using UnityEngine;

namespace DemoMPTK
{
    /// <summary>@brief
    /// Example of MVP implementation of spatialization by MIDI tracks. 
    /// Here, the goal is to visually map Midi tracks to gameObject on the scene.
    /// Important notes to be read:
    ///     - Only the first six tracks are processed. 
    ///       Others tracks will be centered on the scene,
    ///       also the 3D spatialization will not be good.
    ///     - Not all MIDI files are organised with tracks, 
    ///       it's possible to play simultaneously sixteen instruments with one MIDI track!!! 
    ///       Obviously in ths case, no 3D Spatialization will be done.
    ///     - MIDI messages holding track name are not mandatory in a MIDI file.
    ///       In this case no name will be displayed. See MetaEvent and SequenceTrackName in class MPTKEvent.
    ///       https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
    /// </summary>
    public class Spatializer3D : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to read and play a Midi file. Inherits from MidiSynth.
        /// Must be defined in the inspector or with FindObjectOfType<MidiSpatializer>() in the Start() function
        /// </summary>
        public MidiSpatializer midiSpatializer;

        /// <summary>@brief
        /// Board wihch hold cylinder wich represents MIDI tracs. Rotation is applied in Update()
        /// </summary>
        public Transform Board;

        /// <summary>@brief
        /// The 3D gameobject visible on the scene which represent the first six MIDI tracks.
        /// The six cylinders on the board are associated with this array in the inspector.
        /// </summary>
        public Transform[] GameObjectsHoldingMidiTrack;

        /// <summary>@brief
        /// Speed rotation of the board
        /// </summary>
        [Range(-100, 100)]
        public float Speed;

        /// <summary>@brief
        /// Global volume
        /// </summary>
        [Range(0, 1)]
        public float Volume;


        /// <summary>@brief
        /// Current angle of the board
        /// </summary>
        private float angle;


        /// <summary>@brief
        /// Need a static variable because the value must be shared with all instanciated synths.
        /// </summary>
        private static float volume;

        private void Start()
        {
            //Debug.Log($"Start TestSpatializerFly {midiSpatializer.MPTK_SpatialSynthIndex}");
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                // Start Main MidiPlayer (MIDI reader) - Init for the midi reader
                // Could be a good place to add User Interface listener.
                // Nothing to do here.
            }
            else
            {
                // Run for all Spatial Midi Synth slaves
                if (midiSpatializer.MPTK_SpatialSynthIndex < GameObjectsHoldingMidiTrack.Length && GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex] != null)
                {
                    // This Spatial Midi Synth becomes a child of the GameObject displayed on the scene (cylinder in this demo).
                    this.transform.SetParent(GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex]);
                    // Position of the Spatial Midi Synth (and most importantly, its AudioSource) will be centered on its parent ... the cylinder.
                    this.transform.localPosition = Vector3.zero;
                }
            }
        }

        /// <summary>@brief
        /// Very important to read!
        /// The Update() like the Start() will be called by every Midi Spatial Synth instanciated (and also by the MIDI reader=.
        /// It's important to make the difference between them.
        /// Reserve the call from the MIDI reader for user interaction 
        /// Reserve the instanciated synths for changind channel or track behaviors (track with this demo).
        /// </summary>
        private void Update()
        {
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                // Here, the MIDI reader update
                // ----------------------------

                // Change in user interface must be applied only for the first Midi Spatializer ... which is the MIDI reader
                angle += Time.deltaTime * Speed;
                Board.transform.rotation = Quaternion.Euler(0, angle, 0);

                // Save the current value set in the inspector in a static.
                // It's a static variable because the value must be shared with all instanciated synth.
                volume = Volume;
            }
            else
            {
                // Here, the updates for all instanciated MIDI synths 
                // --------------------------------------------------

                // Change for the gameObject attached to the synth. This c
                if (midiSpatializer.MPTK_SpatialSynthIndex < GameObjectsHoldingMidiTrack.Length && GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex] != null)
                {
                    // Update track name if exists
                    TextMesh textPlayer = GameObjectsHoldingMidiTrack[midiSpatializer.MPTK_SpatialSynthIndex].GetComponentInChildren<TextMesh>();
                    if (textPlayer != null)
                        textPlayer.text = midiSpatializer.MPTK_TrackName;

                    // Apply the volume read from the inspector for all tracks (instanciated MIDI synths)
                    midiSpatializer.MPTK_Volume = volume;
                }
            }
        }
    }
}