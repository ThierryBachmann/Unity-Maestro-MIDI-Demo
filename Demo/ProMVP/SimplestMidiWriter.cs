using MidiPlayerTK;                                 // uses MIDI Pro Toolkit
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMVP
{
    /// <summary>
    /// Read and Write to a MIDI file
    /// 
    /// This is intended to be the "Hello, World!" equivalent of the MIDI Pro Toolkit
    /// (MPTK).
    /// 
    /// </summary>
    public class SimplestMidiWriter : MonoBehaviour
    {
        public string PathMidiSource;

        public Button BtLoadMidi;

        // This class is able to read, write MIDI file
        public MPTKWriter mfw;

        // Start is called before the first frame update
        void Start()
        {

            // Button click action
            BtLoadMidi.onClick.AddListener(() =>
            {
                mfw = new MPTKWriter();
                // Load the MIDI file from OS system file 
                if (mfw.LoadFromFile(PathMidiSource))
                {
                    const int TRACK1 = 1;
                    const int CHANNEL0 = 0;
                    long currentTime = 0;

                    // Display each MIDI event (format NAudio)
                    Debug.Log("<b--- Content after loading ---</b>");
                    mfw.LogWriter();

                    // How many ticks for a quarter ?
                    int ticksPerQuarterNote = mfw.DeltaTicksPerQuarterNote;
                    // Search last events
                    MPTKEvent lastMidiEvent = mfw.MPTK_LastEvent;
                    Debug.Log($"lastMidiEvent at:{lastMidiEvent.Tick} code:{lastMidiEvent.Command}");

                    // Time of last event
                    currentTime = lastMidiEvent.Tick;

                    // Next notes will be played a quarter after the last with a duration of a quarter
                    currentTime += ticksPerQuarterNote;

                    // Play a D5 (see class HelperNoteLabel)
                    mfw.AddNote(TRACK1, currentTime, CHANNEL0, 62, 50, ticksPerQuarterNote);

                    // Play a E5 one quarter after
                    currentTime += ticksPerQuarterNote;
                    mfw.AddNote(TRACK1, currentTime, CHANNEL0, 64, 50, ticksPerQuarterNote);

                    // Play a G5 one quarter after
                    currentTime += ticksPerQuarterNote;
                    mfw.AddNote(TRACK1, currentTime, CHANNEL0, 67, 50, ticksPerQuarterNote);

                    // Silent note : velocity=0 (will generate only a noteoff)
                    currentTime += ticksPerQuarterNote * 2;
                    mfw.AddNote(TRACK1, currentTime, CHANNEL0, 80, 0, ticksPerQuarterNote);

                    // Display each MIDI event (format NAudio)
                    Debug.Log("<b>--- Content after modification ---</b>");
                    mfw.LogWriter();


                    // Write the MIDI with changed name
                    string filename = Path.Combine(
                        Path.GetDirectoryName(PathMidiSource),
                        Path.GetFileNameWithoutExtension(PathMidiSource) + "_rewrited.mid");
                    mfw.WriteToFile(filename);

                }
                else
                    Debug.LogWarning($"Error loading MIDI file {PathMidiSource}");

            });
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {

            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
            }
        }
    }
}