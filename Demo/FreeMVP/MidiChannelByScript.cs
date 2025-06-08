using System.Collections;
using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

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
    /// Functionality Demonstrated:
    /// - Enable or disable channels when a MIDI is playing. 
    /// - Keep only the drum channel playing.
    ///
    /// How to Use:
    /// 1. Add an empty GameObject to your Unity Scene and attach this script to the GameObject.
    /// 2. Add a MidiFilePlayer prefab to your scene (right click on the Hierarchy Tab, menu Maestro)
    /// 3. Run the scene and change the value of OnlyDrum in the inspector
    /// 
    /// Documentation References:
    /// - MIDI File Player: https://paxstellar.fr/midi-file-player-detailed-view-2/
    /// - MPTKChannel: https://mptkapi.paxstellar.com/da/dc1/class_midi_player_t_k_1_1_m_p_t_k_channel.html
    /// 
    /// </summary>

    public class MidiChannelByScript : MonoBehaviour
    {
        // Trigger OnlyDrumChange
        public bool OnlyDrum;

        // Avoid changing channels properties at every frame
        // Keep last value of OnlyDrum, applied only when OnlyDrum is changed
        private bool lastOnlyDrum;

        public MidiFilePlayer midiPlayer;
        private void Awake()
        {
            // Search for an existing MidiFilePlayer prefab in the scene
            midiPlayer = FindFirstObjectByType<MidiFilePlayer>();

            if (midiPlayer == null)
            {
                Debug.Log("No MidiFilePlayer Prefab found in the current Scene Hierarchy.");
            }
        }
        public void Start()
        {
            lastOnlyDrum = OnlyDrum;
        }
        public void Update()
        {
            // Avoid changing channels properties at every frame
            if (lastOnlyDrum != OnlyDrum)
            {
                lastOnlyDrum = OnlyDrum;
                OnlyDrumChange();
            }
        }

        public void OnlyDrumChange()
        {

            // Channel change can be applied only if the MIDI is playing
            if (midiPlayer != null && midiPlayer.MPTK_IsPlaying)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (OnlyDrum == false)
                        // Play all channels
                        midiPlayer.MPTK_Channels[i].Enable = true;
                    else
                    {
                        // Play only drums
                        if (i != 9)
                            midiPlayer.MPTK_Channels[i].Enable = false;
                        else
                            midiPlayer.MPTK_Channels[i].Enable = true;
                    }
                }
            }

        }
    }
}

