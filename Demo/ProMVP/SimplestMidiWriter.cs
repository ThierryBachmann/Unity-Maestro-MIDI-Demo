using MidiPlayerTK;
using System.IO;
using UnityEngine;

namespace DemoMVP
{
    /// <summary>
    /// Demonstrates creating, saving, and playing a MIDI file using the Maestro API.
    /// 
    /// Features:
    /// - Dynamically generates a simple MIDI file containing a few notes.
    /// - Saves the MIDI file to a temporary location.
    /// - Plays the generated MIDI file using the `MidiExternalPlayer` prefab.
    /// 
    /// Documentation References:
    /// - MIDI Writer: https://paxstellar.fr/class-midifilewriter2/
    /// - MIDI External Player: https://paxstellar.fr/midi-external-player-v2/
    /// - MPTKEvent: https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
    /// 
    /// This example is designed to be the "Hello, World!" equivalent for the MIDI Pro Toolkit (MPTK).
    /// </summary>
    public class SimplestMidiWriter : MonoBehaviour
    {
        private void Start()
        {
            // Generate a temporary file name for the MIDI file
            string pathMidiSource = Path.GetTempFileName() + ".mid";

            // Initialize the MIDI writer class, which handles reading, writing, and playing MIDI files
            MPTKWriter mptkWriter = new MPTKWriter();

            // Track and channel constants
            const int TRACK1 = 1, CHANNEL0 = 0;

            // Number of ticks per quarter note
            int ticksPerQuarterNote = 500;

            // Starting time for MIDI events (in ticks)
            long currentTime = 0;

            // Set the instrument preset (e.g., a music patch) on the specified channel
            mptkWriter.AddChangePreset(TRACK1, currentTime, CHANNEL0, 10);

            // Add MIDI notes with timing
            // Each note is added at a specific time, with a duration and velocity (loudness)

            // Play a D4 note
            currentTime += ticksPerQuarterNote;
            mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 62, 50, ticksPerQuarterNote);

            // Play an E4 note one quarter note later
            currentTime += ticksPerQuarterNote;
            mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 64, 50, ticksPerQuarterNote);

            // Play a G4 note one quarter note later
            currentTime += ticksPerQuarterNote;
            mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 67, 50, ticksPerQuarterNote);

            // Add a silent note (velocity = 0) two quarter notes later
            // This generates only a "Note Off" event
            currentTime += ticksPerQuarterNote * 2;
            mptkWriter.AddNote(TRACK1, currentTime, CHANNEL0, 80, 0, ticksPerQuarterNote);

            // Log all MIDI events for debugging purposes
            mptkWriter.LogWriter();

            // Write the MIDI file to the specified path
            mptkWriter.WriteToFile(pathMidiSource);
            Debug.Log($"MIDI file created at {pathMidiSource}");

            // Play the generated MIDI file
            PlayMidiFromFile(pathMidiSource, mptkWriter);
        }

        private void PlayMidiFromFile(string filePath, MPTKWriter midiWriter)
        {
            // Find the MidiExternalPlayer prefab in the scene
            MidiExternalPlayer midiPlayer = FindFirstObjectByType<MidiExternalPlayer>();
            if (midiPlayer == null)
            {
                Debug.LogWarning("No MidiExternalPlayer Prefab found in the current Scene Hierarchy. Add it via the Maestro menu.");
                return;
            }

            // Configure the MIDI player with the generated MIDI file
            midiPlayer.MPTK_MidiName = "file://" + filePath; // Use the file URI format
            midiPlayer.MPTK_MidiAutoRestart = true;

            // Prepare the MIDI file for playback
            midiWriter.MidiName = filePath;

            // Sort events by absolute time to ensure correct playback order
            midiWriter.StableSortEvents();

            // Calculate timing details for all MIDI events (e.g., time in measures and quarters)
            midiWriter.CalculateTiming(logPerf: true);

            // Start playback using the prepared MIDI file
            midiPlayer.MPTK_Play(mfw2: midiWriter);
        }
    }
}
