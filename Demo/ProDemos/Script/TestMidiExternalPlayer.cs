using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMPTK
{
    public class TestMidiExternalPlayer : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to play a Midi file from an external source. This PreFab must be present in your scene.
        /// </summary>
        public MidiExternalPlayer midiExternalPlayer;
        public Button BtPlayRoulette;
        public InputField UrlMidi;
        public Text RunningStatus;
        public Text ErrorStatus;
        public Text EventStatus;
        public Text InfoMidiLyric;
        public Text InfoMidiCopyright;
        public Text InfoMidiTrack;
        float speedRoulette;
        float currentVelocity = 0f;


        private void Start()
        {
            //if (!HelperDemo.CheckSFExists()) return;

            // Warning: when defined by script, this event is not triggered at first load of MPTK 
            // because MidiPlayerGlobal is loaded before any other gamecomponent
            // To be done in Start event (not Awake)
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(EndLoadingSF);

            // Find the Midi external component 
            if (midiExternalPlayer == null)
            {
                //Debug.Log("No midiExternalPlayer defined with the editor inspector, try to find one");
                MidiExternalPlayer fp = FindObjectOfType<MidiExternalPlayer>();
                if (fp == null)
                    Debug.LogWarning("Can't find a MidiExternalPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                else
                {
                    midiExternalPlayer = fp;
                }
            }

            if (midiExternalPlayer != null)
            {
                // There is two methods to trigger event: 
                //      1) in inpector from the Unity editor 
                //      2) by script, see below
                // ------------------------------------------

                // Event trigger when midi file start playing
                // Set event by script
                Debug.Log("OnEventStartPlayMidi defined by script");
                midiExternalPlayer.OnEventStartPlayMidi.AddListener(StartPlay);

                // Set event by script
                Debug.Log("OnEventEndPlayMidi defined by script");
                midiExternalPlayer.OnEventEndPlayMidi.AddListener(EndPlay);

                // Set event by script
                Debug.Log("OnEventNotesMidi defined by script");
                midiExternalPlayer.OnEventNotesMidi.AddListener(ReadNotes);
            }

            RunningStatus.text = "";
            ErrorStatus.text = "";
            EventStatus.text = "";

            BtPlayRoulette.onClick.AddListener(() =>
                {
                    //InfoMidi.text += "T1\nT2\nT3\nT4\nT5\nT6\nT7\nT8\nT9\nT10\nT11\n";
                    InfoMidiLyric.text = "Lyric\n";
                    InfoMidiCopyright.text = "Copyright\n";
                    InfoMidiTrack.text = "Track\n";
                    speedRoulette = 50f;
                    string uri = $"https://www.midiworld.com/download/{UnityEngine.Random.Range(1, 5630)}";
                    // Display url for information
                    UrlMidi.text = uri;
                    Debug.Log("Play from script:" + uri);
                    // Stop current Midi and play the uri
                    midiExternalPlayer.MPTK_Stop();
                    midiExternalPlayer.MPTK_MidiName = uri;
                    midiExternalPlayer.MPTK_Play();
                    StartCoroutine(DoCheck());
                });
        }

        /// <summary>@brief
        /// Check result of loading Midi during 2 seconds. Other method to check: in the update loop or with events.
        /// </summary>
        /// <returns></returns>
        IEnumerator DoCheck()
        {
            // Wait Midi is read or an error is detected
            int maxStep = 20;
            while (midiExternalPlayer.MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined && maxStep > 0)
            {
                yield return new WaitForSeconds(.1f);
                maxStep--;
            }

            // Analyse reading status
            //switch (midiExternalPlayer.MPTK_StatusLastMidiLoaded)
            //{
            //    case 0: break; // Good !
            //    case 1: InfoMidiTrack.text = "<color=red>error, no Midi found</color>"; break;
            //    case 2: InfoMidiTrack.text = "<color=red>error, not a Midi file, too short size</color>"; break;
            //    case 3: InfoMidiTrack.text = "<color=red>error, not a Midi file, signature MThd not found.</color>"; break;
            //    case 4: InfoMidiTrack.text = "<color=red>error, network error or site not found.</color>"; break;
            //    case 5: InfoMidiTrack.text = "<color=red>error, midi file loading.</color>"; break;
            //    default: InfoMidiTrack.text = "<color=red>error unknown.</color>"; break;

            //}
        }

        private void Update()
        {
            if (speedRoulette > 0f)
            {
                BtPlayRoulette.transform.Rotate(Vector3.forward, -speedRoulette);
                speedRoulette = Mathf.SmoothDamp(speedRoulette, 0f, ref currentVelocity, 0.5f);
            }

            if (midiExternalPlayer.MPTK_IsPaused)
                RunningStatus.text = "Paused";
            else if (midiExternalPlayer.MPTK_IsPlaying)
                RunningStatus.text = "Playing";
            else
                RunningStatus.text = "Stop";

            switch (midiExternalPlayer.MPTK_StatusLastMidiLoaded)
            {
                case LoadingStatusMidiEnum.NotYetDefined:
                case LoadingStatusMidiEnum.Success: ErrorStatus.text = ""; break;
                case LoadingStatusMidiEnum.NotFound: ErrorStatus.text = "MIDI not found"; break;
                case LoadingStatusMidiEnum.TooShortSize: ErrorStatus.text = "Not a MIDI file, too short size"; break;
                case LoadingStatusMidiEnum.NoMThdSignature: ErrorStatus.text = "Not a MIDI file, signature MThd not found"; break;
                case LoadingStatusMidiEnum.NetworkError: ErrorStatus.text = "Network error or site not found"; break;
                case LoadingStatusMidiEnum.MidiFileInvalid: ErrorStatus.text = "Error loading MIDI file"; break;
                default: ErrorStatus.text = "Error Unknown"; break;
            }
        }


        /// <summary>@brief
        /// This call can be defined from MidiPlayerGlobal event inspector. Run when SF is loaded.
        /// Warning: not triggered at first load of MPTK because MidiPlayerGlobal id load before any other gamecomponent
        /// </summary>
        public void EndLoadingSF()
        {
            Debug.Log("End loading SF, MPTK is ready to play");
            Debug.Log("Load statistique");
            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        /// <summary>@brief
        /// Event fired by MidiExternalPlayer when a midi is started (set by Unity Editor in MidiFilePlayer Inspector or by script, see above)
        /// </summary>
        public void StartPlay(string midiname)
        {
            Debug.Log("Start Midi " + midiname + " Duration: " + midiExternalPlayer.MPTK_Duration.TotalSeconds + " seconds");
            EventStatus.text = "Event start playing";
            //      midiExternalPlayer.MPTK_Speed = 1f;
            //      midiExternalPlayer.MPTK_Transpose = 0;
        }

        /// <summary>@brief
        /// Event fired by MidiExternalPlayer when a midi is ended, or stop by MPTK_Stop, or Replay with MPTK_Replay, or detected when loading
        /// The parameter reason give the origin of the end
        /// </summary>
        public void EndPlay(string midiname, EventEndMidiEnum reason)
        {
            if (reason == EventEndMidiEnum.MidiErr)
                Debug.LogFormat($"End Play: Error loading midi {midiname}");
            else
                Debug.LogFormat($"End playing midi {midiname} reason:{reason}");
            EventStatus.text = "Event end playing, reason:" + reason + " status:" + midiExternalPlayer.MPTK_StatusLastMidiLoaded;
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when MIDI events notes are available (set by Unity Editor in MidiFilePlayer Inspector or by script, see above)
        /// </summary>
        public void ReadNotes(List<MPTKEvent> events)
        {
            foreach (MPTKEvent midievent in events)
            {
                switch (midievent.Command)
                {
                    case MPTKCommand.NoteOn:
                        Debug.LogFormat($"Midi External Player - Follow MIDI - Note:{midievent.Value} Velocity:{midievent.Velocity} Duration:{midievent.Duration}");
                        break;
                    case MPTKCommand.MetaEvent:
                        switch (midievent.Meta)
                        {
                            case MPTKMeta.Lyric:
                            case MPTKMeta.Marker:
                                // Info from http://gnese.free.fr/Projects/KaraokeTime/Fichiers/karfaq.html and here https://www.mixagesoftware.com/en/midikit/help/HTML/karaoke_formats.html
                                //Debug.Log(midievent.Channel + " " + midievent.Meta + " '" + midievent.Info + "'");
                                string text = midievent.Info.Replace("\\", "\n");
                                text = text.Replace("/", "\n");
                                if (text.StartsWith("@") && text.Length >= 2)
                                {
                                    switch (text[1])
                                    {
                                        case 'K': text = "Type: " + text.Substring(2); break;
                                        case 'L': text = "Language: " + text.Substring(2); break;
                                        case 'T': text = "Title: " + text.Substring(2); break;
                                        case 'V': text = "Version: " + text.Substring(2); break;
                                        default: //I as information, W as copyright, ...
                                            text = text.Substring(2); break;
                                    }
                                    //text += "\n";
                                }
                                InfoMidiLyric.text += text + "\n";
                                break;

                            case MPTKMeta.TextEvent:
                            case MPTKMeta.Copyright:
                                InfoMidiCopyright.text += midievent.Info + "\n";
                                break;

                            case MPTKMeta.SequenceTrackName:
                                InfoMidiTrack.text += "Track: " + midievent.Track + " '" + midievent.Info + "'\n";
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is ended (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void EndPlay()
        {
            Debug.Log("End Midi " + midiExternalPlayer.MPTK_MidiName);
        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }

        /// <summary>@brief
        /// This method is fired from button (with predefined URI) or inputfield in the screen.
        /// See canvas/button.
        /// </summary>
        /// <param name="uri">uri or path to the midi file</param>
        public void Play(string uri)
        {
            RunningStatus.text = "";
            ErrorStatus.text = "";
            EventStatus.text = "";
            UrlMidi.text = uri;
            InfoMidiLyric.text = "Lyric\n";
            InfoMidiCopyright.text = "Copyright\n";
            InfoMidiTrack.text = "Track\n";

            Debug.Log($"Play from script:{uri}");

            // Stop current playing
            midiExternalPlayer.MPTK_Stop();

            if (uri.ToLower().StartsWith("file://") ||
                uri.ToLower().StartsWith("http://") ||
                uri.ToLower().StartsWith("https://"))
            {
                // try to load from an URI (file:// or http://)
                midiExternalPlayer.MPTK_MidiName = uri;
                midiExternalPlayer.MPTK_Play();
            }
            else
            {
                // try to load a byte array and play
                // example with uri= C:\Users\xxx\Midi\DreamOn.mid
                try
                {
                    using (Stream fsMidi = new FileStream(uri, FileMode.Open, FileAccess.Read))
                    {
                        byte[] data = new byte[fsMidi.Length];
                        fsMidi.Read(data, 0, (int)fsMidi.Length);
                        midiExternalPlayer.MPTK_Play(data);
                    }
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                    return;
                }
            }
            StartCoroutine(DoCheck());
        }
    }
}