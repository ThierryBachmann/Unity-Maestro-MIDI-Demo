using MidiPlayerTK; // Using the Maestro MIDI Pro Toolkit
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMVP
{
    /// <summary>
    /// Demonstrates how to read, modify, and rewrite an existing MIDI file using the Maestro API.
    /// 
    /// Features:
    /// - Reads a MIDI file from a specified path.
    /// - Appends four additional notes to the existing MIDI file.
    /// - Saves the modified MIDI file with a new name.
    /// 
    /// Documentation References:
    /// - MIDI Writer: https://paxstellar.fr/class-midifilewriter2/
    /// - MPTKEvent: https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
    /// 
    /// This example is designed to be a "Hello, World!" equivalent for modifying MIDI files with the MIDI Pro Toolkit (MPTK).
    /// </summary>
    public class SimplestMidiReWriter : MonoBehaviour
    {
        [Header("Enter the full path to the MIDI file.\nIt will be rewritten with '_rewrited.mid' appended to the name.")]
        public string PathMidiSource;

        [Header("Add a UI Button to your canvas and assign it to this variable.")]
        public Button BtLoadMidi;

        private void Start()
        {
            if (BtLoadMidi == null)
            {
                Debug.LogError("No button assigned to 'BtLoadMidi'. Please assign a UI Button in the Inspector.");
                return;
            }

            // Assign the button click action
            BtLoadMidi.onClick.AddListener(ModifyAndRewriteMidiFile);
        }

        private void ModifyAndRewriteMidiFile()
        {
            if (string.IsNullOrEmpty(PathMidiSource))
            {
                Debug.LogError("No MIDI file path provided. Set 'PathMidiSource' in the Inspector.");
                return;
            }

            if (!File.Exists(PathMidiSource))
            {
                Debug.LogError($"The specified MIDI file does not exist: {PathMidiSource}");
                return;
            }

            // Initialize the MIDI writer class
            MPTKWriter mptkWriter = new MPTKWriter();

            // Load the MIDI file
            if (mptkWriter.LoadFromFile(PathMidiSource))
            {
                Debug.Log($"Loaded MIDI file: {PathMidiSource}");

                // Constants for track and channel
                const int TRACK1 = 1;
                const int CHANNEL0 = 0;

                // Display the original MIDI content
                Debug.Log("<b>--- Content after loading ---</b>");
                mptkWriter.LogWriter();

                // Retrieve the delta ticks per quarter note
                int ticksPerQuarterNote = mptkWriter.DeltaTicksPerQuarterNote;

                // Get the time of the last MIDI event
                MPTKEvent lastMidiEvent = mptkWriter.MPTK_LastEvent;
                long currentTime = lastMidiEvent != null ? lastMidiEvent.Tick : 0;
                Debug.Log($"Last MIDI event at tick: {currentTime}, Command: {lastMidiEvent?.Command}");

                // Append new notes
                currentTime += ticksPerQuarterNote; // Start after the last event
                mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 62, 50, ticksPerQuarterNote); // D4

                currentTime += ticksPerQuarterNote;
                mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 64, 50, ticksPerQuarterNote); // E4

                currentTime += ticksPerQuarterNote;
                mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 67, 50, ticksPerQuarterNote); // G4

                currentTime += ticksPerQuarterNote * 2; // Add a silent note with double duration
                mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 80, 0, ticksPerQuarterNote);

                // Display the modified MIDI content
                Debug.Log("<b>--- Content after modification ---</b>");
                mptkWriter.LogWriter();

                // Save the modified MIDI file
                string rewrittenFilePath = Path.Combine(
                    Path.GetDirectoryName(PathMidiSource),
                    Path.GetFileNameWithoutExtension(PathMidiSource) + "_rewrited.mid");

                mptkWriter.WriteToFile(rewrittenFilePath);
                Debug.Log($"MIDI file successfully rewritten: {rewrittenFilePath}");
            }
            else
            {
                Debug.LogWarning($"Failed to load MIDI file: {PathMidiSource}");
            }
        }
    }
}
