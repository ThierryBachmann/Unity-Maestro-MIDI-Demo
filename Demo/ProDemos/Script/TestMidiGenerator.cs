using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DemoMPTK

{
    /// <summary>
    /// Script for the class MidiFileWriter2. 
    /// </summary>
    public class TestMidiGenerator : MonoBehaviour
    {
        public CustomStyle myStyle;
        DateTime startPlaying;
        Vector2 scrollerWindow = Vector2.zero;

        Func<MPTKWriter> mfwGenerator = null;
        string mfwFilename = "";
        bool midiAutoRestart = false;

        void OnGUI()
        {
            if (!HelperDemo.CheckSFExists()) return;

            Vector3 scale = HelperDemo.GUIScale();

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            MainMenu.Display("Create a MIDI messages by Algo, Write to a MIDI file, Play", myStyle, Screen.width / scale.x, "https://paxstellar.fr/class-midifilewriter2/");

            GUILayout.BeginVertical(myStyle.BacgDemosLight, GUILayout.Width(Screen.width / scale.x), GUILayout.Height(Screen.height / scale.y - 160));
            GUILayout.Label("Write the generated notes to a Midi file and play with a MidiExternalPlay Prefab or play the generated notes with MidiFilePlayer Prefab, no file is created.", myStyle.TitleLabel2Centered, GUILayout.Width(Screen.width / scale.x));
            GUILayout.Label("A low pass effect is enabled with MidiExternalPlay prefab also it sound differently that MidiFilePlayer prefab. See inspector.", myStyle.TitleLabel2Centered, GUILayout.Width(Screen.width / scale.x));

            scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width / scale.x));
            GUIExample(CreateMidiStream_four_notes_milli, "Very simple stream - 4 notes of 500 milliseconds created every 500 milliseconds", "Generated - Milli - Four notes every 500 milliseconds");
            GUIExample(CreateMidiStream_four_notes_ticks, "Very simple stream - 4 consecutives quarters created independantly of the tempo (absolute tick position)", "Generated - Tick - Four consecutive quarters");
            GUIExample(CreateMidiStream_preset_tempo_pitchWheel, "A more complex one - Preset change, Tempo change, Pitch Wheel change, Modulation change, Lyric, Time Signature", "Generated - Tick - Preset, Tempo, Pitch, Modulation, Lyric, Time Signature");
            GUIExample(CreateMidiStream_Chords, "Chords with violin - tonic C major with progression I V IV V - C minor I VIm IIm V - 4 chords with piano from the Maestro library CM DmM7 Gm7b5 FM7", "Generated - Tick - Chords with violin");
            GUIExample(CreateMidiStream_sandbox, "Sandbox - make your trial", "Generated - Sandbox - make your trial");
            GUIExample(CreateMidiStream_silence, "TU - silence at the end", "Generated - Tick - Silence at the end");
            GUIExample(CreateMidiStream_all_notes, "TU - Full Notes from 30 to 127 with velocity from 20 to 127", "Generated - Full Notes");
            GUIExample(CreateMidiStream_full_crescendo, "TU - full velocity crescendo from 0 to 127", "Generated - Tick - Full velocity crescendo from 0 to 127");
            GUIExample(CreateMidiStream_short_crescendo_with_noteoff_tick, "TU - short velocity crescendo millisecond (250 ms, Quarter by 4)", "Generated - Tick - Short velocity crescendo with noteoff");
            GUIExample(CreateMidiStream_short_crescendo_with_noteoff_ms, "TU - short velocity crescendo millisecond (250 milliseconds)", "Generated - Milli - Short velocity crescendo with noteoff");
            GUIExample(CreateMidiStream_midi_merge, "TU - merge MIDI index 0 with MIDI index 1 from the DB", "Generated - Merge MIDI");
            GUIExample(CreateMidiStream_tempochange, "TU - tempo change with 3 measures of 4 quarter", "Generated - Tick - Tempo change");
            GUIExample(CreateMidiStream_tempochange_milli, "TU - tempo change with 3 measures of 4 notes defined by time", "Generated - Milli - Tempo change");
            GUIExample(CreateMidiStream_stable_sort, "TU - stable sort", "Generated - Tick - MIDI Stable sort");
            GUIExample(CreateMultipleTimeSignature, "TU - multiple time signature change", "Generated - Tick - Multiple Time Signature");
            GUIExample(CreateMidiStream_inner_loop_set_with_META, "TU - Arpeggio with two META inner MIDI loops added: @ACTION:INNER_LOOP(500,1000,4) and @ACTION:INNER_LOOP(8500,9000,4)", "Generated - Tick - Inner MIDI loop with META");
            GUIExample(CreateMidiStream_200_drum_hit_at_each_quarter, "TU - Create 200 drum hits. 'Side stick' at each quarter, 'Low floor Tom' at each bar. Useful for accuracy test", "Generated - Tick - Drum hits for accuracy test");
            GUIExample(CreateMidiStream_time_signature_4_1, "TU - Time Signature 4 1 --> 4/2", "Generated - Tick - Time signature 4 1");
            GUIExample(CreateMidiStream_time_signature_4_3, "TU - Time Signature 4 3 --> 4/8", "Generated - Tick - Time signature 4 3");
            GUIExample(LoadMidi_add_four_notes_at_beginning, "Load a MIDI from MIDI DB and add MIDI events", "Generated - Load And Modify MIDI");
            GUIExample(CreateMidi_with_text_information, "Add text information as lyrics", "Generated - Add Text");

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.BeginVertical(myStyle.BacgDemosLight, GUILayout.Width(Screen.width / scale.x), GUILayout.Height(Screen.height / scale.y));

            GUILayout.BeginHorizontal();// myStyle.BacgDemosLight, GUILayout.Width(Screen.width / scale.x));

            float height = 40;
            if (GUILayout.Button(midiAutoRestart ? "MIDI Auto Restart" : "One Shot", GUILayout.Height(height)))
            {
                midiAutoRestart = !midiAutoRestart;
                if (!midiAutoRestart)
                    StopAllPlaying();
            }

            if (GUILayout.Button("Stop Playing", GUILayout.Height(height)))
                StopAllPlaying();

            if (GUILayout.Button("Open Folder MIDI External", GUILayout.Height(height)))
                Application.OpenURL("file://" + Application.persistentDataPath);

            if (GUILayout.Button("Open Folder Maestro MIDI DB", GUILayout.Height(height)))
                Application.OpenURL("file://" + Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile));
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

        }

        private void GUIExample(Func<MPTKWriter> _mfwGenerator, string _title, string _filename)
        {
            float height = 40;
            float width = 100;
            GUILayout.BeginHorizontal(myStyle.BacgDemosMedium);
            GUILayout.Label(_title, myStyle.LabelLeft);
            GUILayout.FlexibleSpace();
            mfwGenerator = _mfwGenerator;
            mfwFilename = _filename;
            if (GUILayout.Button(new GUIContent("Write File\nand Play", "Write To a MIDI file and play with MidiExternalPlay Prefab"), GUILayout.Width(width), GUILayout.Height(height)))
            {
                StopAllPlaying();
                WriteMidiSequenceToFileAndPlay(mfwFilename, mfwGenerator());
            }

            if (GUILayout.Button(new GUIContent("Write To\nthe MIDI DB", ""), GUILayout.Width(width), GUILayout.Height(height)))
            {
                StopAllPlaying();
                WriteMidiToMidiDB(mfwFilename, mfwGenerator());
            }

            if (GUILayout.Button(new GUIContent("Play with\nMidiFilePlayer", ""), GUILayout.Width(width), GUILayout.Height(height)))
            {
                StopAllPlaying();
                PlayDirectlyMidiSequence(mfwFilename, mfwGenerator());
            }

            if (GUILayout.Button(new GUIContent("Log\nMPTK", "Log Maestro MIDI events (MPTKEvent)"), GUILayout.Width(width / 2), GUILayout.Height(height)))
            {
                // Create a MidiFileWriter2 instance with the MIDI events defined for the selected generator.
                MPTKWriter mfw = mfwGenerator();
                mfw.CreateTracksStat();
                mfw.CalculateTiming();
                mfw.LogWriter();
            }
            if (GUILayout.Button(new GUIContent("Log\nMIDI", "Log MIDI events"), GUILayout.Width(width / 2), GUILayout.Height(height)))
                mfwGenerator().LogRaw();

            GUILayout.EndHorizontal();
        }

        /// <summary>@brief
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_four_notes_milli()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory, you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change.
            // By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter();

            mfw.AddText(track0, 0, MPTKMeta.Copyright, "Simple MIDI Generated. 4 quarter ");

            // Playing tempo must be defined at start of the stream. 
            // Defined BPM is mandatory when duration and delay are defined in millisecond in the stream. 
            // The value of the BPM is used to transform duration from milliseconds to internal ticks value.
            // Obviously, duration in millisecànds depends on the BPM selected.
            // With BPM=120, a quarter duration is 500 milliseconds, 240 ticks (default value for a quarter)
            mfw.AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            // With BPM=120, quarter duration is 500ms (60000 / 120). So, notes are played at, 0, 500, 1000, 1500 ms from the start.
            AddNoteMilli(mfw, track1, timeToPlay: 0f, channel0, 60, 50, duration: 500f);
            AddNoteMilli(mfw, track1, timeToPlay: 500f, channel0, 61, 50, duration: 500f);
            AddNoteMilli(mfw, track1, timeToPlay: 1000f, channel0, 62, 50, duration: 500f);
            AddNoteMilli(mfw, track1, timeToPlay: 1500f, channel0, 63, 50, duration: 500f);

            return mfw;
        }

        /// <summary>@brief
        /// Four consecutive quarters played independently of the tempo.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_four_notes_ticks()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            long absoluteTime = 0;


            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter();

            mfw.AddTimeSignature(0, 0, 4, 2);

            // 240 is the default. A classical value for a Midi. define the time precision.
            int ticksPerQuarterNote = mfw.DeltaTicksPerQuarterNote;

            // Some textual information added to the track 0 at time=0
            mfw.AddText(track0, 0, MPTKMeta.Copyright, "Simple MIDI Generated. 4 quarter at 120 BPM");

            // Define Tempo is not mandatory when using time in ticks. The default 120 BPM will be used.
            //mfw.AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            mfw.AddNote(track1, absoluteTime, channel0, 60, 50, ticksPerQuarterNote);

            // Next note will be played one quarter after the previous
            absoluteTime += ticksPerQuarterNote;
            mfw.AddNote(track1, absoluteTime, channel0, 61, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.AddNote(track1, absoluteTime, channel0, 62, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.AddNote(track1, absoluteTime, channel0, 63, 50, ticksPerQuarterNote);

            return mfw;
        }

        //![ExampleFullMidiFileWriter]
        /// <summary>@brief
        /// Midi Generated with MPTK with tempo, preset, pitch wheel change
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_preset_tempo_pitchWheel()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60;

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);


            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Some textual information added to the track 0 at time=0
            mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "MIDI Generated with MPTK with tempo, preset, pitch wheel change");

            // TimeSignatureEvent (not mandatory)   https://paxstellar.fr/2020/09/11/midi-timing/
            //      Numerator(number of beats in a bar, 
            //      Denominator(which is confusingly) in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
            mfw.AddTimeSignature(track0, absoluteTime, 4, 2); // for a 4/4 signature

            // Tempo is defined in beat per minute (not mandatory, by default MIDI are played with a tempo of 120).
            // beatsPerMinute set to 60 at start, it's a slow tempo, one quarter per second.
            // Tempo is global for the whole MIDI independantly of each track and channel.
            mfw.AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Preset for channel 1. Generally 25 is Acoustic Guitar, see https://en.wikipedia.org/wiki/General_MIDI
            // It seems that some reader (as Media Player) refused Midi file if change preset is defined in the track 0, so we set it in track 1.
            mfw.AddChangePreset(track1, absoluteTime, channel0, preset: 25);

            //
            // Build first bar
            // ---------------

            // Creation of the first bar in the partition : 
            //      add four quarter with a tick duration of one quarter (one second with BPM=60)
            //          57 --> A4   
            //          60 --> C5   
            //          62 --> D5  
            //          65 --> F5 

            // Some lyrics added to the track 0
            mfw.AddText(track0, absoluteTime, MPTKMeta.Lyric, "Build first bar");

            mfw.AddNote(track1, absoluteTime, channel0, note: 57, velocity: 50, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            mfw.AddNote(track1, absoluteTime, channel0, note: 60, velocity: 80, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 62, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 65, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            //
            // Build second bar: Same notes but dobble tempo (using the microsecond per quarter change method)
            // -----------------------------------------------------------------------------------------------
            mfw.AddTempoChange(track0, absoluteTime, MPTKEvent.BeatPerMinute2QuarterPerMicroSecond(beatsPerMinute * 2));

            //return mfw;

            mfw.AddNote(track1, absoluteTime, channel0, note: 57, velocity: 50, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            mfw.AddNote(track1, absoluteTime, channel0, note: 60, velocity: 80, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 62, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 65, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            //
            // Build third bar : one note with a pitch change along the bar
            // -------------------------------------------------------------

            mfw.AddChangePreset(track1, absoluteTime, channel0, preset: 50); // synth string

            // Some lyrics added to the track 0
            mfw.AddText(track0, absoluteTime, MPTKMeta.Lyric, "Pitch wheel effect");

            // Play an infinite note A4 (duration = -1) don't forget the noteoff!
            mfw.AddNote(track1, absoluteTime, channel0, note: 57, velocity: 100, length: -1);

            // Apply pitch wheel on the channel 0
            for (float pitch = 0f; pitch <= 2f; pitch += 0.05f) // 40 steps of 0.05
            {
                mfw.AddPitchWheelChange(track1, absoluteTime, channel0, pitch);
                // Advance position 40 steps and for a total duration of 4 quarters
                absoluteTime += (long)((float)ticksPerQuarterNote * 4f / 40f);
            }

            // The noteoff for A4
            mfw.AddOff(track1, absoluteTime, channel0, 57);

            // Reset pitch change to normal value
            mfw.AddPitchWheelChange(track1, absoluteTime, channel0, 0.5f);

            //
            // Build fourth bar : arpeggio of 16 sixteenth along the bar 
            // --------------------------------------------------------

            // Some lyrics added to the track 0
            mfw.AddText(track0, absoluteTime, MPTKMeta.Lyric, "Arpeggio");

            // Dobble the tempo with a variant of AddBPMChange, 
            // change tempo defined in microsecond. Use BeatPerMinute2QuarterPerMicroSecond to convert or use directly AddBPMChange
            //mfw.AddTempoChange(track0, absoluteTime, MidiFileWriter2.BeatPerMinute2QuarterPerMicroSecond(beatsPerMinute));

            // Patch/preset to use for channel 1. Generally 11 is Music Box, see https://en.wikipedia.org/wiki/General_MIDI
            mfw.AddChangePreset(track1, absoluteTime, channel0, preset: 11);

            // Add sixteenth notes (duration = quarter / 4) : need 16 sixteenth to build a bar of 4 quarter
            int note = 57;
            for (int i = 0; i < 16; i++)
            {
                mfw.AddNote(track1, absoluteTime, channel0, note: note, velocity: 100, ticksPerQuarterNote / 4);
                // Advance the position by one sixteenth 
                absoluteTime += ticksPerQuarterNote / 4;
                note += 1;
            }

            //
            // Build fifth bar : one whole note with vibrato
            // ----------------------------------------------

            // Some lyrics added to the track 0
            mfw.AddText(track0, absoluteTime, MPTKMeta.Lyric, "Vibrato");

            // Add a last whole note (4 quarters duration = 1 bar)
            mfw.AddNote(track1, absoluteTime, channel0, note: 85, velocity: 100, length: ticksPerQuarterNote * 4);

            // Apply modulation change, (vibrato)
            mfw.AddControlChange(track1, absoluteTime, channel0, MPTKController.Modulation, 127);

            absoluteTime += ticksPerQuarterNote * 4;

            // Reset modulation change to normal value
            mfw.AddControlChange(track1, absoluteTime, channel0, MPTKController.Modulation, 0);

            //
            // wrap up : add a silence
            // -----------------------------

            mfw.AddText(track0, absoluteTime, MPTKMeta.Lyric, "Silence");
            absoluteTime += ticksPerQuarterNote;

            //
            // optional : build tempo and signature map, measure and beat
            // ----------------------------------------------------------

            // Sort the events by ascending absolute time
            mfw.StableSortEvents();

            // Calculate time, measure and beat for each events
            mfw.CalculateTiming(logDebug: true, logPerf: true);

            return mfw;
        }
        //![ExampleFullMidiFileWriter]



        private MPTKWriter CreateMidiStream_Chords()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60; // One quarter per second

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Patch/preset to use for channel 1. Generally 40 is violin, see https://en.wikipedia.org/wiki/General_MIDI and substract 1 as preset begin at 0 in MPTK
            mfw.AddChangePreset(track1, absoluteTime, channel0, preset: 40);

            mfw.AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Some textual information added to the track 0 at time=0
            mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play chords");

            // Defined a duration of one quarter in millisecond
            long duration = (long)mfw.DurationTickToMilli(ticksPerQuarterNote);


            // From https://apprendre-le-home-studio.fr/bien-demarrer-ta-composition-46-suites-daccords-danthologie-a-tester-absolument-11-idees-de-variations/ (sorry, in french but it's more a note for me !)

            //! [ExampleMidiWriterBuildChordFromRange]

            // Play 4 chords, degree I - V - IV - V 
            // ------------------------------------
            mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I - V - IV - V ");

            // We need degrees in major, so build a major range
            MPTKScaleLib scaleMajor = MPTKScaleLib.CreateScale(MPTKScaleName.MajorHarmonic);

            // Build chord degree 1
            MPTKChordBuilder chordDegreeI = new MPTKChordBuilder()
            {
                // Parameters to build the chord
                Tonic = 60, // play in C
                Count = 3,  // 3 notes to build the chord (between 2 and 20, of course it doesn't make sense more than 7, its only for fun or experiementation ...)
                Degree = 1,
                // Midi Parameters how to play the chord
                Duration = duration, // millisecond, -1 to play indefinitely
                Velocity = 80, // Sound can vary depending on the iQuarter

                // Optionnal MPTK specific parameters
                Arpeggio = 0, // delay in milliseconds between each notes of the chord
                Delay = 0, // delay in milliseconds before playing the chord
            };

            // Build chord degree V
            MPTKChordBuilder chordDegreeV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 5, Duration = duration, Velocity = 80, };

            // Build chord degree IV
            MPTKChordBuilder chordDegreeIV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 4, Duration = duration, Velocity = 80, };

            // Add degrees I - V - IV - V in the MIDI (all in major) 
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeIV); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            //! [ExampleMidiWriterBuildChordFromRange]

            // Add a silent just by moving the time of the next event for one quarter
            absoluteTime += ticksPerQuarterNote;

            // Play 4 others chords, degree  I – VIm – IIm – V
            // -----------------------------------------------
            mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I – VIm – IIm – V");

            // We need 2 degrees in minor, build a minor range
            MPTKScaleLib scaleMinor = MPTKScaleLib.CreateScale(MPTKScaleName.MinorHarmonic);

            // then degree 2 and 6
            MPTKChordBuilder chordDegreeII = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 2, Duration = duration, Velocity = 80, };
            MPTKChordBuilder chordDegreeVI = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 6, Duration = duration, Velocity = 80, };

            // Add degrees I – VIm – IIm – V intp the MidiFileWriter2 MIDI stream
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMinor, chordDegreeVI); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMinor, chordDegreeII); absoluteTime += ticksPerQuarterNote;
            mfw.AddChordFromScale(track1, absoluteTime, channel0, scaleMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            // Add a silent
            absoluteTime += ticksPerQuarterNote;


            // Play 4 chords from library
            // --------------------------
            mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords from library");

            // Piano
            mfw.AddChangePreset(track1, absoluteTime, channel0, preset: 0);

            //! [ExampleMidiWriterBuildChordFromLib]

            MPTKChordBuilder chordLib = new MPTKChordBuilder() { Tonic = 60, Duration = duration, Velocity = 80, };
            mfw.AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.Major, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 62;
            mfw.AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.mM7, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 67;
            mfw.AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.m7b5, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 65;
            mfw.AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.M7, chordLib); absoluteTime += ticksPerQuarterNote;

            //! [ExampleMidiWriterBuildChordFromLib]


            // Return a MidiFileWriter2 object to be played or write
            // see PlayDirectlyMidiSequence() or WriteMidiSequenceToFileAndPlay ()
            return mfw;
        }


        /// <summary>@brief
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_sandbox()
        {
            //In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Track is interesting to structure your Midi. It will be more readable on a sequencer. 
            // Also, track has no effect on the music, must not be confused with channel!
            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            // Track management is done automatically, they are created and ended when needed. There is no real limit, but this class limit the count to 64
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 111;

            int channel0 = 0; // we are using only one channel in this demo
            int channel1 = 1; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter();

            // Some textual information added to the track 0 at time=0
            mfw.AddText(track0, 0, MPTKMeta.SequenceTrackName, "Sandbox");
            mfw.AddChangePreset(track1, 0, channel0, preset: 65); // alto sax
            mfw.AddChangePreset(track1, 0, channel1, preset: 18); // rock organ

            mfw.AddBPMChange(track0, 0, 120);

            mfw.AddText(track0, mfw.ConvertMilliToTick(1000f), MPTKMeta.TextEvent, "Alto Sax, please");
            AddNoteMilli(mfw, track1, 1000f, channel0, 62, 50, -1);
            AddOffMilli(mfw, track1, 4000f, channel0, 62);

            AddNoteMilli(mfw, track1, 10, channel0, 60, 50, -1);
            AddOffMilli(mfw, track1, 3000, channel0, 60);

            mfw.AddText(track0, mfw.ConvertMilliToTick(3000f), MPTKMeta.TextEvent, "Rock Organ, please");
            AddNoteMilli(mfw, track1, 3000f, channel1, 65, 50, 3000);
            AddNoteMilli(mfw, track1, 3500f, channel1, 66, 50, 2500);
            AddNoteMilli(mfw, track1, 4000f, channel1, 67, 50, 2000);


            AddNoteMilli(mfw, track1, 1000f, channel1, 62, 50, -1);
            AddOffMilli(mfw, track1, 4000f, channel1, 62);

            mfw.AddText(track0, mfw.ConvertMilliToTick(6000f), MPTKMeta.TextEvent, "Ending Bip");

            AddNoteMilli(mfw, track1, 6000f, channel0, 80, 50, 100f);
            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_full_crescendo()
        {
            int ticksPerQuarterNote = 500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 240);
            mfw.AddChangePreset(track: 1, absoluteTime, channel: 0, preset: 0);

            for (int velocity = 0; velocity <= 127; velocity += 5)
            {
                // Duration = 1 second for a quarter at BPM 60
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: 60, velocity: velocity, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }

            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_all_notes()
        {
            int ticksPerQuarterNote = 500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 480);
            mfw.AddChangePreset(track: 1, absoluteTime, channel: 0, preset: 0);

            int velocity = 20;
            while (true)
            {
                velocity += 20;
                if (velocity > 127) velocity = 127;
                Debug.Log($"velocity={velocity}");
                for (int note = 0; note <= 127; note += 1)
                {
                    // Duration = 1 second for a quarter at BPM 60
                    mfw.AddNote(track: 1, absoluteTime, channel: 0, note: note, velocity: velocity, length: ticksPerQuarterNote);
                    absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
                }
                if (velocity >= 127)
                    break;
            }
            return mfw;
        }
        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_200_drum_hit_at_each_quarter()
        {
            int ticksPerQuarterNote = 1500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            // Quarter duration = 0.25 second at BPM 240
            mfw.AddBPMChange(track: 0, absoluteTime, 240);

            for (int iQuarter = 0; iQuarter <= 200; iQuarter += 1)
            {
                // Use channel 9 for drum kit, each note is related to a percussion

                // Side stick at each quarter
                mfw.AddNote(track: 1, absoluteTime, channel: 9, note: 37, velocity: 70, length: ticksPerQuarterNote / 2);

                // Low floor Tom at each bar
                if (iQuarter % 4 == 0)
                    mfw.AddNote(track: 1, absoluteTime, channel: 9, note: 41, velocity: 120, length: ticksPerQuarterNote / 2);

                // Next note will be played one quarter after the previous (time signature is 4/4)
                absoluteTime += ticksPerQuarterNote;
            }

            return mfw;
        }
        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_short_crescendo_with_noteoff_tick()
        {
            int ticksPerQuarterNote = 500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 240);
            mfw.AddChangePreset(track: 1, absoluteTime, channel: 0, preset: 0);

            for (int velocity = 40; velocity <= 80; velocity += 5)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: 60, velocity: velocity, length: -1);
                absoluteTime += ticksPerQuarterNote; // Noteoff one quarter after and will be also the next noteon
                mfw.AddOff(track: 1, absoluteTime, channel: 0, note: 60);
            }

            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_short_crescendo_with_noteoff_ms()
        {
            int ticksPerQuarterNote = 500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            float timeToPlay = 0f;

            mfw.AddBPMChange(track: 0, tick: 0, bpm: 240);
            mfw.AddChangePreset(track: 1, tick: 0, channel: 0, preset: 0);

            for (int velocity = 40; velocity <= 80; velocity += 5)
            {
                // Duration = 1 second for a quarter at BPM 60
                AddNoteMilli(mfw, track: 1, timeToPlay: timeToPlay, channel: 0, note: 60, velocity: velocity, duration: -1);
                timeToPlay += 250; // Noteoff 100 milliseconds after and will be also the next noteon
                AddOffMilli(mfw, track: 1, timeToPlay: timeToPlay, channel: 0, note: 60);
            }

            return mfw;
        }


        /// <summary>@brief
        /// Play 3x4 quarters with a tempo change.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_tempochange()
        {
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: 500);
            mfw.AddBPMChange(1, 0 * mfw.DeltaTicksPerQuarterNote, 60);
            mfw.AddNote(1, 0 * mfw.DeltaTicksPerQuarterNote, 0, 60, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 1 * mfw.DeltaTicksPerQuarterNote, 0, 61, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 2 * mfw.DeltaTicksPerQuarterNote, 0, 62, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 3 * mfw.DeltaTicksPerQuarterNote, 0, 63, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddBPMChange(1, 4 * mfw.DeltaTicksPerQuarterNote, 120);
            mfw.AddNote(1, 4 * mfw.DeltaTicksPerQuarterNote, 0, 60, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 5 * mfw.DeltaTicksPerQuarterNote, 0, 61, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 6 * mfw.DeltaTicksPerQuarterNote, 0, 62, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 7 * mfw.DeltaTicksPerQuarterNote, 0, 63, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddBPMChange(1, 8 * mfw.DeltaTicksPerQuarterNote, 240);
            mfw.AddNote(1, 8 * mfw.DeltaTicksPerQuarterNote, 0, 60, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 9 * mfw.DeltaTicksPerQuarterNote, 0, 61, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 10 * mfw.DeltaTicksPerQuarterNote, 0, 62, 60, mfw.DeltaTicksPerQuarterNote);
            mfw.AddNote(1, 11 * mfw.DeltaTicksPerQuarterNote, 0, 63, 60, mfw.DeltaTicksPerQuarterNote);
            return mfw;
        }

        /// <summary>@brief
        /// Play 3x4 quarters with a tempo change.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_tempochange_milli()
        {
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: 500);
            // Initial tempo change
            mfw.AddBPMChange(1, 0, 60);
            AddNoteMilli(mfw, 1, timeToPlay: 0.0f, 0, 60, 60, duration: 1f);
            AddNoteMilli(mfw, 1, timeToPlay: 1.0f, 0, 61, 60, duration: 1f);
            AddNoteMilli(mfw, 1, timeToPlay: 2.0f, 0, 62, 60, duration: 1f);
            AddNoteMilli(mfw, 1, timeToPlay: 3.0f, 0, 63, 60, duration: 1f);

            // simulate tempo change to 120
            AddNoteMilli(mfw, 1, timeToPlay: 4.0f, 0, 60, 60, duration: 0.5f);
            AddNoteMilli(mfw, 1, timeToPlay: 4.5f, 0, 61, 60, duration: 0.5f);
            AddNoteMilli(mfw, 1, timeToPlay: 5.0f, 0, 62, 60, duration: 0.5f);
            AddNoteMilli(mfw, 1, timeToPlay: 5.5f, 0, 63, 60, duration: 0.5f);

            // simulate tempo change to 240
            AddNoteMilli(mfw, 1, timeToPlay: 6.0f, 0, 60, 60, duration: 0.25f);
            AddNoteMilli(mfw, 1, timeToPlay: 6.25f, 0, 61, 60, duration: 0.25f);
            AddNoteMilli(mfw, 1, timeToPlay: 6.50f, 0, 62, 60, duration: 0.25f);
            AddNoteMilli(mfw, 1, timeToPlay: 6.75f, 0, 63, 60, duration: 0.25f);
            return mfw;
        }
        /// <summary>@brief
        /// Create some note, meta event not in order and check stable sort
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_stable_sort()
        {
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: 500);
            mfw.AddText(0, tick: 0, MPTKMeta.Lyric, "some text");
            mfw.AddNote(1, tick: 500, 0, 61, 100, 500);
            mfw.AddNote(1, tick: 0, 0, 60, 100, 500);
            mfw.AddChangePreset(1, tick: 0, 0, preset: 100);
            mfw.AddChangePreset(1, tick: 10, 0, preset: 100);
            mfw.AddText(0, tick: 0, MPTKMeta.Lyric, "other text");
            mfw.StableSortEvents(logPerf: true);
            return mfw;
        }

        //! [ExampleMIDIImport]
        /// <summary>@brief
        /// Join two MIDI from the MidiDB
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_midi_merge()
        {
            // Join two MIDI from the MidiDB
            //     - create an empty MIDI writer
            //     - Import a first one (MIDI index 0 from the MIDI DB)
            //     - Import a second one (MIDI index 1 from the MIDI DB)
            MPTKWriter mfw = null;
            try
            {
                // Create a Midi File Writer instance
                // -----------------------------------
                mfw = new MPTKWriter();

                // A MIDI loader is useful to load all MIDI events from a MIDI file.
                MidiFilePlayer mfLoader = FindObjectOfType<MidiFilePlayer>();
                if (mfLoader == null)
                {
                    Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the Maestro menu.");
                    return null;
                }

                // No, with v2.10.0 - It's mandatory to keep noteoff when loading MIDI events for merging
                // mfLoader.MPTK_KeepNoteOff = true;
                // it's recommended to not keep end track
                mfLoader.MPTK_KeepEndTrack = false;

                // Load the initial MIDI index 0 from the MidiDB
                // ---------------------------------------------
                mfLoader.MPTK_MidiIndex = 0;
                mfLoader.MPTK_Load();
                // All merge operation will be done with the ticksPerQuarterNote of the first MIDI
                mfw.ImportFromEventsList(mfLoader.MPTK_MidiEvents, mfLoader.MPTK_DeltaTicksPerQuarterNote, name: mfLoader.MPTK_MidiName, logPerf: true);
                Debug.Log($"{mfLoader.MPTK_MidiName} Events loaded: {mfLoader.MPTK_MidiEvents.Count} DeltaTicksPerQuarterNote:{mfw.DeltaTicksPerQuarterNote}");

                // Load the MIDI index 1 from the MidiDB 
                // -------------------------------------
                mfLoader.MPTK_MidiIndex = 1;
                mfLoader.MPTK_Load();
                // All MIDI events loaded will be added to the MidiFileWriter2.
                // Position and Duration will be converted according the ticksPerQuarterNote initial and ticksPerQuarterNote from the MIDI to be inserted.
                mfw.ImportFromEventsList(mfLoader.MPTK_MidiEvents, mfLoader.MPTK_DeltaTicksPerQuarterNote, name: "MidiMerged", logPerf: true);
                Debug.Log($"{mfLoader.MPTK_MidiName} Events loaded: {mfLoader.MPTK_MidiEvents.Count} DeltaTicksPerQuarterNote:{mfw.DeltaTicksPerQuarterNote}");

                // Add a silence of a 4 Beat Notes after the last event.
                // It's optionnal but recommended if you want to automatic restart on the generated MIDI with a silence before looping.
                long absoluteTime = mfw.MPTK_MidiEvents.Last().Tick + mfw.MPTK_MidiEvents.Last().Length;
                Debug.Log($"Add a silence at {mfw.MPTK_MidiEvents.Last().Tick} + {mfw.MPTK_MidiEvents.Last().Length} = {absoluteTime} ");
                mfw.AddSilence(track: 1, absoluteTime, channel: 0, length: mfw.DeltaTicksPerQuarterNote * 4);
            }
            catch (Exception ex) { Debug.LogException(ex); }

            // 
            return mfw;
        }
        //! [ExampleMIDIImport]

        //! [ExampleMidiWileWriterLoadMidi]
        /// <summary>@brief
        /// Join two MIDI from the MidiDB
        /// </summary>
        /// <returns></returns>
        private MPTKWriter LoadMidi_add_four_notes_at_beginning()
        {
            // Join two MIDI from the MidiDB
            //     - create an empty MIDI writer
            //     - Import a first one (MIDI index 0 from the MIDI DB)
            //     - Import a second one (MIDI index 1 from the MIDI DB)
            MPTKWriter mfw = null;
            try
            {
                // Create a Midi File Writer instance
                // -----------------------------------
                mfw = new MPTKWriter();

                // Load MIDI from DB at index 2 (for testing with just a drum line so in channel 9)
                mfw.LoadFromMidiDB(2);

                // Select preset 10 for channel 0
                mfw.AddChangePreset(track: 1, tick: 0, channel: 0, 10);

                // Add four notes (here, we consider Delta Ticks Per Beat Note = 1024)
                mfw.AddNote(track: 1, tick: 0, channel: 0, note: 60, velocity: 100, length: 512);
                mfw.AddNote(track: 1, tick: 512, channel: 0, note: 60, velocity: 100, length: 512);
                mfw.AddNote(track: 1, tick: 1024, channel: 0, note: 60, velocity: 100, length: 512);
                mfw.AddNote(track: 1, tick: 1536, channel: 0, note: 60, velocity: 100, length: 512);

                // MPTK_Addxxxx methods add event at the end of the MIDI events list
                // We need to sort the events by ascending absolute time
                mfw.StableSortEvents();

                // Optional if you want just playing the MIDI
                // Calculate real time, measure and quarter for each events
                // Without this call, you will get the warning "No tempo map detected".
                // @li Calculate #MPTK_TempoMap with #MPTKTempo.MPTK_CalculateMap
                // @li Calculate #MPTK_SignMap with  #MPTKSignature.MPTK_CalculateMap
                // @li Calculate time and duration of each events from the tick value and from the tempo map.
                // @li Calculate measure and quarter position taking into account time signature.
                ////// mfw.CalculateTiming();

                // Find a MidiFilePlayer in the hierarchy
                // MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();

                // And play!
                // midiPlayer.MPTK_Play(mfw2: mfw);
            }
            catch (Exception ex) { Debug.LogException(ex); }

            // 
            return mfw;
        }
        private MPTKWriter CreateMidi_with_text_information()
        {
            MPTKWriter mfw = null;
            int ticksPerQuarterNote = 500;
            long absoluteTime = 0;
            try
            {
                // Create a Midi File Writer instance
                // -----------------------------------
                mfw = new MPTKWriter(deltaTicksPerQuarterNote: ticksPerQuarterNote);
                mfw.ExtendedText = true;

                // Some textual information added to the track 0 at time=0
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 60, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "only ASCII");
                absoluteTime += ticksPerQuarterNote;

                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 62, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "リリックテキスト");
                absoluteTime += ticksPerQuarterNote;

                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 64, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "抒情文字");
                absoluteTime += ticksPerQuarterNote;

                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 66, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "가사 텍스트");
                absoluteTime += ticksPerQuarterNote;

                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 68, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "français éôè");
                absoluteTime += ticksPerQuarterNote;

                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 68, velocity: 100, length: ticksPerQuarterNote);
                mfw.AddText(track: 1, tick: absoluteTime, typeMeta: MPTKMeta.Lyric, text: "Norsk språk");
                absoluteTime += ticksPerQuarterNote;


                //mfw.AddText(0, 0, MPTKMeta.SequenceTrackName, "It's a sequence track name");
                //mfw.AddText(0, 0, MPTKMeta.Lyric, "It's a lyric");
            }
            catch (Exception ex) { Debug.LogException(ex); }

            // 
            return mfw;
        }


        //! [ExampleMidiWileWriterLoadMidi]

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_silence()
        {
            int ticksPerQuarterNote = 500;

            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 60);
            mfw.AddChangePreset(track: 1, absoluteTime, channel: 0, preset: 21);

            // Duration = 1 second for a quarter at BPM 60
            mfw.AddNote(track: 1, absoluteTime, channel: 0, note: 57, velocity: 60, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            // wrap up : add a silence of 2 seconds
            mfw.AddSilence(track: 1, absoluteTime, channel: 0, length: ticksPerQuarterNote * 4);

            return mfw;
        }

        private MPTKWriter CreateFourOnTheFloor()
        {
            int track0 = 0;
            int track1 = 1;
            int channel0 = 9; // we are using only one channel in this demo
            int beatsPerMinute = 120;
            int ticksPerQuarterNote = 1500;
            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);


            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Tempo is defined in beat per minute (not mandatory, by default MIDI are played with a tempo of 120).
            // beatsPerMinute set to 60 at start, it's a slow tempo, one quarter per second.
            // Tempo is global for the whole MIDI independantly of each track and channel.
            mfw.AddBPMChange(track0, absoluteTime, beatsPerMinute);


            mfw.AddNote(track1, absoluteTime, channel0, note: 40, velocity: 100, length: -1);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 40, velocity: 100, length: -1);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 40, velocity: 100, length: -1);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 36, velocity: 100, length: -1);
            absoluteTime += ticksPerQuarterNote;

            mfw.AddNote(track1, absoluteTime, channel0, note: 36, velocity: 1, length: -1);
            return mfw;
        }



        private MPTKWriter CreateMultipleTimeSignature()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 1500;

            // Create a Midi file of type 1 (recommended)
            MPTKWriter mfw = new MPTKWriter(ticksPerQuarterNote, 1);


            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Some textual information added to the track 0 at time=0
            //mfw.AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "MIDI Generated with MPTK with tempo, preset, pitch wheel change");

            //// TimeSignatureEvent (not mandatory)   https://paxstellar.fr/2020/09/11/midi-timing/
            ////      Numerator(number of beats in a bar, 
            ////      Denominator(which is confusingly) in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 4, denominator: 2); // for a 4/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 4/2");


            // Tempo is defined in beat per minute (not mandatory, by default MIDI are played with a tempo of 120).
            // beatsPerMinute set to 60 at start, it's a slow tempo, one quarter per second.
            // Tempo is global for the whole MIDI independantly of each track and channel.
            mfw.AddBPMChange(track: 0, tick: absoluteTime, bpm: 120);

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 60 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }

            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 2, denominator: 2); // for a 2/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 2/2");

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 55 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }

            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 1, denominator: 2); // for a 2/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 1/2");

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 50 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }

            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 4, denominator: 2); // for a 2/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 4/2");

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 60 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }


            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 2, denominator: 2); // for a 2/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 2/2");

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 55 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }

            mfw.AddTimeSignature(track: 0, tick: absoluteTime, numerator: 1, denominator: 2); // for a 2/4 signature
            mfw.AddText(track: 0, absoluteTime, MPTKMeta.TextEvent, "TimeSignature 1/2");

            // Build bar
            for (int i = 0; i < 4; i++)
            {
                mfw.AddNote(track: 1, tick: absoluteTime, channel: 0, note: 50 + i, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }
            return mfw;
        }

        //![ExampleCreateMeta]

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_inner_loop_set_with_META()
        {
            int ticksPerQuarterNote = 500;

            // A querter each DTPQN
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 120);
            mfw.AddChangePreset(track: 1, absoluteTime, channel: 0, preset: 0);

            Debug.Log("Create first loop @ACTION:INNER_LOOP(500,1000,4)");
            mfw.AddText(track: 1, absoluteTime, MPTKMeta.TextEvent, "@ACTION:INNER_LOOP(500,1000,4)");

            // Generate 4 measures of 4 quarters
            for (int note = 0; note < 16; note++)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: note + 50, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote;
            }

            // absoluteTime = 8000 at this step
            Debug.Log($"Create second loop at position {absoluteTime} @ACTION:INNER_LOOP(8500,9000,4)");
            mfw.AddText(track: 1, absoluteTime, MPTKMeta.TextEvent, "@ACTION:INNER_LOOP(8500,9000,4)");

            // Generate 4 measures of 4 quarters
            for (int note = 16; note < 32; note++)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: note + 50, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote;
            }

            return mfw;
        }
        //![ExampleCreateMeta]

        /// <summary>@brief
        /// Midi Generated with time signature
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_time_signature_4_1()
        {
            int ticksPerQuarterNote = 500;

            // A querter each DTPQN
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 120);
            mfw.AddTimeSignature(track: 1, absoluteTime, numerator: 4, denominator: 1);
            // Generate 16 notes
            for (int note = 0; note < 16; note++)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: note + 50, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote;
            }

            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with time signature
        /// </summary>
        /// <returns></returns>
        private MPTKWriter CreateMidiStream_time_signature_4_3()
        {
            int ticksPerQuarterNote = 500;

            // A querter each DTPQN
            MPTKWriter mfw = new MPTKWriter(deltaTicksPerQuarterNote: ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.AddBPMChange(track: 0, absoluteTime, 120);
            mfw.AddTimeSignature(track: 1, absoluteTime, numerator: 4, denominator: 3);
            // Generate 16 notes
            for (int note = 0; note < 16; note++)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.AddNote(track: 1, absoluteTime, channel: 0, note: note + 50, velocity: 100, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote;
            }

            return mfw;
        }

        public MPTKEvent AddOffMilli(MPTKWriter mfw, int track, float timeToPlay, int channel, int note)
        {
            long tick = mfw.ConvertMilliToTick(timeToPlay);
            return mfw.AddOff(track, tick, channel, note);
        }

        public MPTKEvent AddNoteMilli(MPTKWriter mfw, int track, float timeToPlay, int channel, int note, int velocity, float duration)
        {
            long tick = mfw.ConvertMilliToTick(timeToPlay);
            int length = duration < 0 ? -1 : (int)mfw.DurationMilliToTick(duration);
            return mfw.AddNote(track, tick, channel, note, velocity, length);
        }

        //! [ExampleMIDIPlayFromWriter]
        private void PlayDirectlyMidiSequence(string name, MPTKWriter mfw)
        {
            // Play MIDI with the MidiExternalPlay prefab without saving MIDI in a file
            MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }

            midiPlayer.MPTK_Stop();
            mfw.MidiName = name;

            midiPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventStartPlayMidi.AddListener((string midiname) =>
            {
                startPlaying = DateTime.Now;
                Debug.Log($"Start playing '{midiname}'");
            });

            midiPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) =>
            {
                Debug.Log($"End playing '{midiname}' {reason} Real Duration={(DateTime.Now - startPlaying).TotalSeconds:F3} seconds");
            });

            midiPlayer.OnEventNotesMidi.RemoveAllListeners();
            midiPlayer.OnEventNotesMidi.AddListener((List<MPTKEvent> events) =>
            {
                foreach (MPTKEvent midievent in events)
                    Debug.Log($"At {midievent.RealTime:F1} \t\t{midievent}");
            });

            // In case of an inner loop has been defined in a Meta
            midiPlayer.MPTK_InnerLoop.OnEventInnerLoop = (MPTKInnerLoop.InnerLoopPhase mode, long tickPlayer, long tickSeek, int count) =>
            {
                Debug.Log($"Inner Loop {mode} - MPTK_TickPlayer:{tickPlayer} --> TickSeek:{tickSeek} Count:{count}/{midiPlayer.MPTK_InnerLoop.Max}");
                return true;
            };

            // Sort the events by ascending absolute time
            mfw.StableSortEvents();

            // Calculate time, measure and quarter for each events
            mfw.CalculateTiming(logPerf: true);

            midiPlayer.MPTK_MidiAutoRestart = midiAutoRestart;

            midiPlayer.MPTK_Play(mfw2: mfw);
        }
        //! [ExampleMIDIPlayFromWriter]

        //! [ExampleMIDIWriteAndPlay]
        private void WriteMidiSequenceToFileAndPlay(string name, MPTKWriter mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write MIDI file:" + filename);

            //! [ExampleCalculateMaps]
            // A MidiFileWriter2 (mfw) has been created with new MidiFileWriter2() With a set of MIDI events.

            // Sort the events by ascending absolute time (optional)
            mfw.StableSortEvents();

            // Calculate time, measure and beat for each events
            mfw.CalculateTiming(logDebug: true, logPerf: true);
            mfw.LogWriter();

            //! [ExampleCalculateMaps]

            // Write the MIDI file
            mfw.WriteToFile(filename);

            // Need an external player to play MIDI from a file from a folder
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiExternalPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }
            midiExternalPlayer.MPTK_Stop();

            // this prefab is able to load a MIDI file from the device or from an url (http)
            // -----------------------------------------------------------------------------
            midiExternalPlayer.MPTK_MidiName = "file://" + filename;
            midiExternalPlayer.MPTK_ExtendedText = true;
            midiExternalPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventStartPlayMidi.AddListener((string midiname) =>
            {
                Debug.Log($"Start playing {midiname}");
            });

            midiExternalPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) =>
            {
                if (reason == EventEndMidiEnum.MidiErr)
                    Debug.LogWarning($"Error  {midiExternalPlayer.MPTK_StatusLastMidiLoaded} when loading '{midiname}'");
                else
                    Debug.Log($"End playing {midiname} {reason}");
            });

            midiExternalPlayer.MPTK_MidiAutoRestart = midiAutoRestart;

            midiExternalPlayer.MPTK_Play();
        }
        //! [ExampleMIDIWriteAndPlay]

        //! [ExampleMIDIWriteToDB]
        private void WriteMidiToMidiDB(string name, MPTKWriter mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write MIDI file:" + filename);

            // Sort the events by ascending absolute time (optional)
            mfw.StableSortEvents();

            // Calculate time, measure and beat for each events
            mfw.CalculateTiming(logDebug: true, logPerf: true);
            mfw.LogWriter();

            // Write the MIDI file
            mfw.WriteToMidiDB(filename);
            //AssetDatabase.Refresh();

            //// Can't play immediately a MIDI file added to the MIDI DB. It's a Unity resource
            //// The MIDI is not yet available at this time.
        }

        //! [ExampleMIDIWriteAndPlay]
        private static void StopAllPlaying()
        {
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer != null)
                midiExternalPlayer.MPTK_Stop();
            MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer != null)
                midiFilePlayer.MPTK_Stop();
        }
    }
}

