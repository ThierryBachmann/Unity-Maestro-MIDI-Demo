using MidiPlayerTK;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMPTK
{
    /// <summary>@brief
    /// Example of implementation of spatialization by channel or track. This script must be adapted for your need.
    /// Here, the goal is to visually map Midi channels or tracks by a sphere. 
    ///    - The vertical position of the sphere depends on the duration of the note.
    ///    - The radius of the sphere depends on the velocity of the note.
    ///    - If there is no note, the sphere is not visible.
    /// Important notes to be read:
    ///     - This script is attached to the prefab MidiSpatializer on the scene. 
    ///     - At start, prefab MidiSpatializer instanciates MPTK_MaxSpatialSynth MidiFilePlayer with all the hierarchy components attached:
    ///         - a sphere, to represent the channel in the 3D env
    ///         - a text above the sphere
    ///         - a script TestSpatializerFly (this one) to be adapted to your need
    ///         - and a script MidiSynth dedicated to a channel or a track, MPTK_SpatialSynthIndex contains the channel or the track index.
    ///     - the main prefab MidiSpatializer remains enabled, but with this specific role:
    ///         - sphere: disabled
    ///         - text above the sphere: disabled
    ///         - TestSpatializerFly (this one): used only when user interacts with the interface (buttons). See bellow ArrangeRandom() for example.
    ///         - MidiSynth dedicated to reading the current Midi, no playing, MPTK_SpatialSynthIndex is set to -1. The Midi events read are redirected to the dedicated MidiSynth/Channel
    ///     
    ///  
    /// </summary>
    public class TestSpatializerFly : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to read and play a Midi file. Inherits from MidiSynth.
        /// </summary>
        public MidiSpatializer midiSpatializer;

        /// <summary>@brief
        /// Text to display above the sphere, the instrument name.
        /// </summary>
        public TextMesh textPlayer;

        /// <summary>@brief
        /// Text to display at the bottom of the screen: midi title + help
        /// </summary>
        public Text textInfo;


        public Dropdown ComboSelectArrange;

        public Button BtChannelOrTrack;

        ////Curve to adjust speed
        //public AnimationCurve AnimCurve;

        // 
        // C:\Devel\Maestro\MidiFile\Bobby Blue Bland - A Touch Of Blues.mid
        [Header("Path to a external MIDI file")]
        [Tooltip("Let empty for using the MIDI DB.")]
        public string pathToMidiFile;


        //
        [Header("Maximum move speed")]
        public float Speed = 1f;

        //Direction to travel
        public Vector3 Direction = Vector3.zero;


        /// <summary>@brief
        /// The sphere!
        /// </summary>
        public Transform sphere;

        /// <summary>@brief
        /// flag to display more info above the sphere
        /// </summary>
        public bool DisplayDebugInfo;

        /// <summary>@brief
        /// Current vertical position of the sphere
        /// </summary>
        public Vector3 PosSynth;

        /// <summary>@brief
        /// Current scale of the sphere
        /// </summary>
        public float ScaleSynth;
        public Vector3 InitialScaleSynth;

        /// <summary>@brief
        /// Count of notes on this channel since the beginning of the play.
        /// </summary>
        public int CountNote;

        /// <summary>@brief
        /// Current midi name playing
        /// </summary>
        static private string CurrentMidi;

        static private int CountTrack;

        enum Arrangement { Square, Random, Instrument }

        /// <summary>@brief
        /// Flag to arrange each players depending of the current instrument associated to the channel.
        /// It's a static, value is shared will all instanciated TestSpatializer objects.
        /// </summary>
        static private Arrangement ModeArrangement;

        /// <summary>@brief
        // Main MidiPlayer (MIDI reader) - All MIDI events are dispatch from this MidiPlayer to the MidiSpatializer which plays the music 
        /// </summary>
        static private MidiSpatializer MidiMaster;

        /// <summary>@brief
        /// Call when this component is started;
        /// </summary>
        private void Start()
        {

            //Debug.Log($"Start TestSpatializerFly {midiSpatializer.MPTK_SpatialSynthIndex}");
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                //
                // Start Main MidiPlayer (MIDI reader) - Init for the midi reader
                // --------------------------------------------------------------

                if (!string.IsNullOrWhiteSpace(pathToMidiFile))
                    LoadExternalMidiFile();
                // else the MIDI file will be read from the MPTK MIDI repo (from the Unity resources)

                MidiMaster = midiSpatializer;

                // Start of the main TestSpatializerFly, disable the sphere
                sphere.gameObject.SetActive(false);

                // Click on this button invers the selected mode
                BtChannelOrTrack.onClick.AddListener(() =>
                {
                    if (MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel)
                        MidiMaster.MPTK_ModeSpatializer = MidiSynth.ModeSpatializer.Track;
                    else
                        MidiMaster.MPTK_ModeSpatializer = MidiSynth.ModeSpatializer.Channel;
                    if (MidiMaster.MPTK_IsPlaying)
                    {
                        if (!string.IsNullOrWhiteSpace(pathToMidiFile))
                            LoadExternalMidiFile();
                        else
                            MidiMaster.MPTK_RePlay();
                    }
                    ApplyArrangement();

                });

                ComboSelectArrange.onValueChanged.AddListener((int iCombo) =>
                {
                    ModeArrangement = (Arrangement)iCombo;
                    ApplyArrangement();
                });
                ModeArrangement = Arrangement.Square;

            }
            else
            {
                //
                // For all MidiPlayers slaves (MIDI Synth) of the Midi Reader synth
                //

                // Start of each MidiSynth when instatiated, one for each MidiSynth until MPTK_MaxSpatialSynth
                if (MidiFilePlayer.SpatialSynths == null)
                {
                    Debug.LogWarning($"IsMidiSpatializer must be set to true");
                    return;
                }

                // Initial position of the sphere. 
                PosSynth = sphere.position;
                ScaleSynth = 1f;
                InitialScaleSynth = transform.localScale;

                // Set a color
                Renderer Render = sphere.GetComponent<Renderer>();
                // Hue between green and violet
                float hue = Mathf.Lerp(0.38f, 0.80f, (float)midiSpatializer.MPTK_SpatialSynthIndex / (float)midiSpatializer.MPTK_MaxSpatialSynth);
                Render.material.color = Color.HSVToRGB(hue, 0.9f, 0.9f);

                // Event trigger for each group of notes read from midi file. When the main MidiSynth reads a group of notes, 
                // Set event by script, MidiReadEvents will be call for each group of MIDI events in each MidiSynth involved with this rules:
                // if mode = Channel
                //      the MidiSynth associated to the channel reveied events
                // if mode = Track
                midiSpatializer.OnEventNotesMidi.AddListener(MidiReadEvents);
            }

            // Event triggers when a Midi playing is started. This event could be set with Unity Editor.
            // Set for the main MidiSynth and each of the 16 midi synth, MidiStartPlay is called.
            midiSpatializer.OnEventStartPlayMidi.AddListener(MidiStartPlay);
        }

        private void LoadExternalMidiFile()
        {
            // Play the midi file from a byte array (MPTK_MidiName can also be defined but only for information)\n
            midiSpatializer.MPTK_PlayOnStart = false;
            using (System.IO.Stream fsMidi = new System.IO.FileStream(pathToMidiFile, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                byte[] data = new byte[fsMidi.Length];
                fsMidi.Read(data, 0, (int)fsMidi.Length);
                midiSpatializer.MPTK_Play(data);
            }
        }

        private void ApplyArrangement()
        {
            Debug.Log("Select arrangement:" + ModeArrangement);

            // This method is removed from the V2.89.1
            //MidiMaster.MPTK_DisableUnsedSynth(MidiMaster.MPTK_TrackCount);

            foreach (MidiFilePlayer mfp in MidiFilePlayer.SpatialSynths)
            {
                TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();
                tsf.CountNote = 0;
                tsf.ScaleSynth = 1f;
            }

            switch (ModeArrangement)
            {
                case Arrangement.Square:
                    if (MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel)
                        ArrangeByChannel();
                    else
                        ArrangeByTrack();
                    break;
                case Arrangement.Random:
                    ArrangeRandom();
                    break;
                case Arrangement.Instrument:
                    ArrangeByInstrument();
                    break;

            }
        }



        /// <summary>@brief
        /// Call only from the main script.
        /// Set random position of the MidiSynths from the UI. This is call only from the main MidiSynth (the reader). 
        /// The position must be applied to the 16 others MidiSynth (midi players)
        /// </summary>
        public void ArrangeRandom()
        {
            //Debug.Log($"ArrangeRandom {midiFilePlayer.MPTK_SpatialSynthIndex}");
            // for each MidiSynth (one MidiSynth by Channel), calculate a random position
            foreach (MidiFilePlayer mfp in MidiFilePlayer.SpatialSynths)
            {
                float range = 950f;
                int maxTry = 0;
                TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();


                // Find random position with a distance with each others, social distancing ;-)
                while (maxTry < 100) // avoid infinite loop !
                {
                    Vector3 posTry = new Vector3(UnityEngine.Random.Range(-range, range), tsf.PosSynth.y, UnityEngine.Random.Range(0, range));
                    bool posOk = CheckOtherMFPPosition(posTry);

                    if (posOk || maxTry >= 99)
                    {
                        //if (!posOk) Debug.Log($"Force position {midiFilePlayer.MPTK_SpatialSynthIndex}");
                        tsf.PosSynth = posTry;
                        break;
                    }
                    maxTry++;
                }
            }
        }

        /// <summary>@brief
        /// Call only from the main script.
        /// Set random position for a MidiSynth.
        /// </summary>
        private int RandomPosition(MidiFilePlayer mfp)
        {
            float range = 950f;
            int maxTry = 0;
            TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();
            // Find random position with a distance with each others, social distancing ;-)
            while (maxTry < 100) // avoid infinite loop !
            {
                Vector3 posTry = new Vector3(UnityEngine.Random.Range(-range, range), tsf.PosSynth.y, UnityEngine.Random.Range(0, range));
                bool posOk = CheckOtherMFPPosition(posTry);

                if (posOk || maxTry >= 99)
                {
                    //if (!posOk) Debug.Log($"Force position {midiFilePlayer.MPTK_SpatialSynthIndex}");
                    tsf.PosSynth = posTry;
                    break;
                }
                maxTry++;
            }

            return maxTry;
        }

        private static bool CheckOtherMFPPosition(Vector3 posTry)
        {
            bool posOk = true;
            foreach (MidiFilePlayer mfp in MidiFilePlayer.SpatialSynths)
            {
                if (Vector3.Distance(posTry, mfp.transform.position) < 200f)
                {
                    // Position too close another object
                    posOk = false;
                    break;
                }
            }

            return posOk;
        }

        //! [ExampleArrangeByChannel]
        /// <summary>@brief
        /// Arrange the dedicated players in a sqaure of 4 x 4 for each channel.
        /// Call only from the main script.
        /// </summary>
        public void ArrangeByChannel()
        {
            float xdim = 500f; // musical scene x dimension from -xdim to xdim
            float zstart = 0; // first line at the center of the musical scene

            // Exec from the UI, applied to each MidiFilePlayer (MidiSynth)
            for (int iChannel = 0; iChannel < 16; iChannel++)
            {
                MidiFilePlayer mfp = MidiFilePlayer.SpatialSynths[iChannel];

                TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();

                // Line from 1 to 4
                int lineNumber = iChannel % 4 + 1;

                // Next line ?
                if (lineNumber == 1) zstart += 100;

                // Debug.Log($"ArrangeByChannel {mfp.MPTK_SpatialSynthIndex} {lineNumber} { ((float)lineNumber) / 4f}");

                float x = Mathf.Lerp(-xdim, xdim, ((float)lineNumber) / 4f);
                float y = tsf.PosSynth.y; // default position (negative)
                float z = zstart;

                tsf.PosSynth = new Vector3(x, y, z);

            }
        }

        //! [ExampleArrangeByChannel]

        /// <summary>@brief
        /// Arrange the players in a line. 
        /// Call only from the main script.
        /// </summary>
        /// <param name="fromUI"></param>
        public void ArrangeByTrack()
        {
            float xdim = 800f; // musical scene x dimension from -xdim to xdim
            float zstart = -100f; // first line at the center of the musical scene
            int xcount = 7;

            // Exec from the UI, applied to each TestSpatializerFly script instanciated with each MidiSynth
            for (int iTrack = 0; iTrack < CountTrack; iTrack++)
            {
                MidiFilePlayer mfp = MidiFilePlayer.SpatialSynths[iTrack];

                TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();

                // Line from 1 to xcount
                int lineNumber = mfp.MPTK_SpatialSynthIndex % xcount + 1;

                // Next line
                if (lineNumber == 1) zstart += 100;

                // Debug.Log($"ArrangeByTrack {mfp.MPTK_SpatialSynthIndex} {lineNumber} ");

                float x = Mathf.Lerp(-xdim, xdim, ((float)lineNumber) / (float)xcount) - 100;
                float y = 500f;
                float z = zstart;

                tsf.PosSynth = new Vector3(x, y, z);
            }
        }

        /// <summary>@brief
        /// Arrange each players depending of the current instrument associated.
        /// Call only from the main script.
        /// </summary>
        /// <param name="fromUI"></param>
        public void ArrangeByInstrument()
        {
            //Debug.Log($"ArrangeByInstrument {midiFilePlayer.MPTK_SpatialSynthIndex}");
            // Exec from the UI, applied to each MidiFilePlayer (MidiSynth)
            // 16 synths (one by channel) has been created at the start of the main synth (where the Midi is read) and stored in SpatialSynths
            // Below, we calculate a position for each.
            foreach (MidiSpatializer mfp in MidiFilePlayer.SpatialSynths)
            {
                //if (/*MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel && */mfp.MPTK_SpatialSynthIndex >= 16)
                //    continue;
                // Get the synth associate to this channel
                TestSpatializerFly tsf = mfp.gameObject.GetComponent<TestSpatializerFly>();
                //int preset = mfp.MPTK_Channels[mfp.MPTK_SpatialSynthIndex].PresetNum;
                // Appply a 3D position depending the current preset
                tsf.PosSynth = PositionByInstrument(tsf.PosSynth, mfp.MPTK_InstrumentNum, mfp.MPTK_SpatialSynthIndex);
            }
        }

        /// <summary>@brief
        /// Calculate the position based on the GM Instrument Families, see here: http://www.music.mcgill.ca/~ich/classes/mumt306/StandardMIDIfileformat.html
        /// Try to deploy instrument in inverse V
        /// 1-8	    Piano	
        /// 9-16	Chromatic Percussion	
        /// 17-24	Organ	
        /// 25-32	Guitar	
        /// 33-40	Bass	
        /// 41-48	Strings	
        /// 49-56	Ensemble	
        /// 57-64	Brass	
        /// 65-72	Reed
        /// 73-80	Pipe
        /// 81-88	Synth Lead
        /// 89-96	Synth Pad
        /// 97-104	Synth Effects
        /// 105-112	Ethnic
        /// 113-120	Percussive
        /// 121-128	Sound Effects            /// 
        /// </summary>
        /// <param name="preset"></param>
        /// <returns></returns>
        Vector3 PositionByInstrument(Vector3 posSynth, int preset, int channel)
        {
            float range = 950f;
            float x, z;
            if (channel != 9)
            {
                // left to right
                x = Mathf.Lerp(-range, range, (float)preset / 127f);
                // at left:ahead, center:bottom, at right:ahead
                z = preset < 64 ? z = Mathf.Lerp(0, range, (float)preset / 64f) : Mathf.Lerp(range, 0, ((float)preset - 64f) / 64f);
            }
            else
            {
                // Special case for drum: set to center at bottom
                x = 0f;
                z = range;
            }
            return new Vector3(x, posSynth.y, z);
        }

        /// <summary>@brief
        /// Called when a Midi is started. Run for each MidiSynth, so MidiFilePlayer, so TestSpatializeFly
        /// </summary>
        /// <param name="midiname"></param>
        public void MidiStartPlay(string midiname)
        {
            if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
            {
                CurrentMidi = midiname;
                CountTrack = midiSpatializer.MPTK_TrackCount;
                //Debug.Log($"Start Playing '{CurrentMidi}' {CountTrack} Tracks");
                ApplyArrangement();
            }
            PosSynth.y = -150f;
            CountNote = 0;

        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi notes are available (set by Unity Editor in MidiFilePlayer Inspector)
        /// Call only from the Midi reader (MPTK_SpatialSynthIndex == -1)
        /// </summary>
        public void MidiReadEvents(List<MPTKEvent> events)
        {
            foreach (MPTKEvent midievent in events)
            {
                //Debug.Log($"{midievent.ToString()}");

                switch (midievent.Command)
                {
                    case MPTKCommand.NoteOn:
                        // If sphere is not visible, set to a visible position else just increase y position
                        PosSynth.y = PosSynth.y < 0f ? PosSynth.y = 200f : PosSynth.y + 90f * midievent.Duration * 1000f;
                        ScaleSynth = Mathf.Lerp(0.75f, 1.5f, midievent.Velocity / 127f);
                        if (DisplayDebugInfo) Debug.LogFormat($"NoteOn - IdSynth:{midiSpatializer.IdSynth} {midievent.ToString()} Count:{events.Count} Channel:{midievent.Channel} IdSynth:{midiSpatializer.MPTK_SpatialSynthIndex} {InitialScaleSynth} {ScaleSynth}");
                        CountNote++;
                        break;
                    case MPTKCommand.PatchChange:
                        if (DisplayDebugInfo) Debug.LogFormat($"PatchChange - IdSynth:{midiSpatializer.IdSynth} {midievent.ToString()} Channel:{midievent.Channel} IdSynth:{midiSpatializer.MPTK_SpatialSynthIndex}");
                        if (ModeArrangement == Arrangement.Instrument)
                        {
                            // If the patch is changed, the position of the sphere changed also whan arrangement is by instrument
                            PosSynth = PositionByInstrument(PosSynth, midievent.Value, midiSpatializer.MPTK_SpatialSynthIndex);
                        }
                        break;
                    case MPTKCommand.ControlChange:
                        if (DisplayDebugInfo) Debug.LogFormat($"ControlChange - IdSynth:{midiSpatializer.IdSynth} {midievent.ToString()} Channel:{midievent.Channel} IdSynth:{midiSpatializer.MPTK_SpatialSynthIndex}");
                        break;

                    case MPTKCommand.MetaEvent:
                        if (DisplayDebugInfo) Debug.LogFormat($"MetaEvent - IdSynth:{midiSpatializer.IdSynth} {midievent.ToString()} Channel:{midievent.Channel} IdSynth:{midiSpatializer.MPTK_SpatialSynthIndex}");
                        break;
                }
            }
        }

        private float Radius = 900f;
        private Vector3 CenterPosition = new Vector3(0, 0, 0);

        private void SetTarget()
        {
            float y = PosSynth.y;
            PosSynth = UnityEngine.Random.insideUnitSphere * Radius + CenterPosition;
            PosSynth.y = y;

        }

        Vector3 positiontySynthTime = Vector3.zero; // for the smooth movement
        Vector3 scaleSynthTime = Vector3.zero; // for the smooth movement

        // Call from all the script 
        void Update()
        {
            if (midiSpatializer != null)
            {
                if (midiSpatializer.MPTK_SpatialSynthIndex < 0)
                {
                    // For the main MidiPlayer (MIDI reader)
                    // -------------------------------------

                    BtChannelOrTrack.GetComponentInChildren<Text>().text = MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel ? "Channel" : "Track";

                    // The main TestSpatializerFly is used to the UI
                    if (midiSpatializer.MPTK_IsPlaying)
                    {
                        textInfo.text = CurrentMidi;
                        textInfo.text += $"\n{midiSpatializer.MPTK_Position / 1000f:F1} / {midiSpatializer.MPTK_DurationMS / 1000f:F1} second";
                    }
                    else
                        textInfo.text = "No Midi Playing";

                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                        ApplyArrangement();
                }
                else
                {

                    // For all MidiPlayers slaves (MIDI Synth) of the Midi Reader synth
                    // -----------------------------------------------------------------

                    // If mode move random and synth is active
                    if (ModeArrangement == Arrangement.Random && PosSynth.y > 210f)
                        if ((transform.position - PosSynth).sqrMagnitude < 0.1f)
                            SetTarget();

                    // Apply simplified gravity (no acceleration, linear speed)
                    PosSynth.y -= Time.unscaledDeltaTime * 100f;

                    // Limit the vertical position of the sphere
                    if (midiSpatializer.MPTK_SpatialSynthEnabled)
                        if (MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel)
                            PosSynth.y = Mathf.Clamp(PosSynth.y, -150f, 500f);
                        else
                            PosSynth.y = Mathf.Clamp(PosSynth.y, 200f, 500f);
                    else
                        PosSynth.y = -150f;

                    // Smooth movement of the sphere+text
                    transform.position = Vector3.SmoothDamp(transform.position, PosSynth, ref positiontySynthTime, 0.9f, 10000f, Time.unscaledDeltaTime);

                    // Scale the sphere
                    if (ScaleSynth > 0f)
                    {
                        if (ScaleSynth > 0.75f)
                            ScaleSynth -= Time.unscaledDeltaTime / 10f;
                        // Setting a 0 scale seems definitively make the sphere disappear!
                        transform.localScale = Vector3.SmoothDamp(transform.localScale, InitialScaleSynth * ScaleSynth, ref scaleSynthTime, 0.3f, 10000f, Time.unscaledDeltaTime);
                        //if (midiSpatializer.MPTK_SpatialSynthIndex == 0) Debug.Log(transform.localScale);
                    }

                    // Because Test Mesh are always visible, even behind a plane ;-) we need to disable text 
                    // if sphere is under the ground or the channel/track is disabled
                    if (transform.position.y < -20f || !midiSpatializer.MPTK_SpatialSynthEnabled)
                        textPlayer.gameObject.SetActive(false);
                    else
                        textPlayer.gameObject.SetActive(true);

                    // Make the text always facing the camera
                    // --------------------------------------
                    // Calculate projection of camera direction to plan x-z (CamFlightRig / MouseAim contains the initial direction)
                    Vector3 camProj = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
                    // Calculate angle between this projection and the midi spatializer which hold the text (always same direction, // to plan x-z, no need to project)
                    float angle = Vector3.SignedAngle(camProj, this.transform.forward, Vector3.up);
                    // Apply rotation with this angle 
                    textPlayer.transform.rotation = Quaternion.Euler(0, -angle, 0);

                    // other option : rotation 10 turn (360 deg) / minute (10)
                    //textPlayer.transform.Rotate(new Vector3(0f, (Time.unscaledDeltaTime * 360f) / 5f, 0f));

                    // Set the text on each sphere 
                    // ---------------------------

                    if (MidiMaster.MPTK_ModeSpatializer == MidiSynth.ModeSpatializer.Channel)
                    {
                        if (midiSpatializer.MPTK_SpatialSynthIndex == 9)
                            textPlayer.text = "Drums"; // Default preset name is "Standard", not really useful.
                        else if (midiSpatializer.MPTK_SpatialSynthIndex < 16)
                            textPlayer.text = midiSpatializer.MPTK_Channels[midiSpatializer.MPTK_SpatialSynthIndex].PresetName;
                        //else - not working
                        //    textPlayer.text = midiSpatializer.MPTK_InstrumentPlayed;
                    }
                    else
                    {
                        textPlayer.text = midiSpatializer.MPTK_TrackName;
                    }

                    if (DisplayDebugInfo)
                        textPlayer.text += $"\nId{midiSpatializer.MPTK_SpatialSynthIndex + 1} P:{midiSpatializer.MPTK_Channels[midiSpatializer.MPTK_SpatialSynthIndex].PresetName} N:{CountNote}";

                }
            }
        }
    }
}