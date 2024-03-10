#define MPTK_PRO
//#define DEBUG_STATUS_STAT // also in MidiSynth.cs

using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DemoMPTK
{
    public class HelperDemo
    {
        // Manage skin
        static public CustomStyle myStyle;
        public int Midi;
        public string Label;

        static List<HelperDemo> ListNote;
        static List<HelperDemo> ListEcart;

        static private StringBuilder logSynthInfo = new StringBuilder(256);
        static public void DisplayInfoSynth(MidiSynth synth, int width, CustomStyle myStyle)
        {
            logSynthInfo.Clear();
            string modeSynth = synth.MPTK_CorePlayer ? "Core" : "AudioSource";
            logSynthInfo.Append("Mode:");
            logSynthInfo.Append($"\t{modeSynth}");
            logSynthInfo.Append($"\tRate:{synth.OutputRate}");
            logSynthInfo.Append($"\tBuffer:{synth.DspBufferSize}");
            logSynthInfo.Append($"\tTime:{Math.Round(synth.StatDeltaAudioFilterReadMS, 2),-5:F2} ms\n");

            logSynthInfo.Append("Voice:");
            logSynthInfo.Append($"\tPlayed:{synth.MPTK_StatVoicePlayed,-3}");
            logSynthInfo.Append($"\tActive:{synth.MPTK_StatVoiceCountActive,-4}");
            logSynthInfo.Append($"\tReused:{synth.MPTK_StatVoiceCountReused,-3} {Mathf.RoundToInt(synth.MPTK_StatVoiceRatioReused)}%");
            logSynthInfo.Append($"\tIn Cache:{synth.MPTK_StatVoiceCountFree,-4}\n");

#if DEBUG_STATUS_STAT
            if (synth.StatusStat != null && synth.StatusStat.Length >= (int)fluid_voice_status.FLUID_VOICE_OFF + 2)
            {
                logSynthInfo.Append("Status:");
                logSynthInfo.Append($"\tOn:{synth.StatusStat[(int)fluid_voice_status.FLUID_VOICE_ON],-4}");
                logSynthInfo.Append($"\tSustain:{synth.StatusStat[(int)fluid_voice_status.FLUID_VOICE_SUSTAINED],-4}");
                logSynthInfo.Append($"\tRelease:{synth.StatusStat[(int)fluid_voice_status.FLUID_VOICE_OFF + 1],-4}\n");
            }
#endif
            if (synth.StatAudioFilterReadMA != null)
            {
                logSynthInfo.Append("Synth (ms):");
                logSynthInfo.Append($"\tSample:{Math.Round(synth.StatAudioFilterReadMS, 2),-5:F2}");
                logSynthInfo.Append($"\tMini:{(synth.StatAudioFilterReadMIN < double.MaxValue ? Math.Round(synth.StatAudioFilterReadMIN, 2) : 0)}");
                logSynthInfo.Append($"\tMaxi:{Math.Round(synth.StatAudioFilterReadMAX, 2),-5:F2}");
                logSynthInfo.Append($"\tAvg:{Math.Round(synth.StatAudioFilterReadAVG, 2),-5:F2}\n");
            }

            if (synth.StatDspLoadMAX != 0f)
            {
                logSynthInfo.Append("DSP (%):");
                logSynthInfo.Append($"\tLoad:{Math.Round(synth.StatDspLoadPCT, 2)}");
                logSynthInfo.Append($"\tMini:{Math.Round(synth.StatDspLoadMIN, 2),-5:F2}");
                logSynthInfo.Append($"\tMaxi:{Math.Round(synth.StatDspLoadMAX, 2),-5:F2}");
                logSynthInfo.Append($"\tAvg:{Math.Round(synth.StatDspLoadAVG, 2),-5:F2}");
            }
            else
                logSynthInfo.Append($"\tDSP Load:{Math.Round(synth.StatDspLoadPCT, 1)}");

            if (synth.StatDspLoadPCT >= 100f)
                logSynthInfo.Append($"\t<color=red>\tDSP Load over 100%</color>");
            else if (synth.StatDspLoadPCT >= synth.MaxDspLoad)
                logSynthInfo.Append($"\t<color=orange>\tDSP Load over {synth.MaxDspLoad}%</color>");
            //else logSynthInfo.Append("\n");

            // Available only when a file Midi reader is enabled
            if (synth.StatDeltaThreadMidiMA != null && synth.StatDeltaThreadMidiMIN < double.MaxValue)
            {
                logSynthInfo.Append($"\nMIDI (ms):");
                logSynthInfo.Append($"\tDeltaTick:{Math.Round(synth.StatDeltaThreadMidiMS, 2),-5:F2}");
                logSynthInfo.Append($"\tMini:{(synth.StatDeltaThreadMidiMIN < double.MaxValue ? Math.Round(synth.StatDeltaThreadMidiMIN, 2) : 0),-5:F2}");
                logSynthInfo.Append($"\tMaxi:{Math.Round(synth.StatDeltaThreadMidiMAX, 2),-5:F2}");
                logSynthInfo.Append($"\tAvg:{Math.Round(synth.StatDeltaThreadMidiAVG, 2),-5:F2}\n");

                logSynthInfo.Append($"\tRead:{Math.Round(synth.StatReadMidiMS, 2),-5:F2}");
                logSynthInfo.Append($"\tTreat:{Math.Round(synth.StatProcessMidiMS, 2),-5:F2}");
                logSynthInfo.Append($"\tMaxi:{Math.Round(synth.StatProcessMidiMAX, 2),-5:F2}");
                logSynthInfo.Append($"\tDeltaMidi:{Math.Round(synth.StatDeltaTimeMidi, 2),-5:F2}");
            }

            //#if !UNITY_ANDROID
            //#endif
            Color savedColor = GUI.color;
            Color savedBackColor = GUI.backgroundColor;
            GUI.color = Color.green;
            GUI.backgroundColor = Color.black;
            GUILayout.Label(logSynthInfo.ToString(), myStyle.TextFieldMultiCourier);
            Rect rectLabel = GUILayoutUtility.GetLastRect();
            GUI.color = savedColor;
            GUI.backgroundColor = savedBackColor;
            if (GUI.Button(new Rect(rectLabel.width - 68, rectLabel.y+1, 75, 20), "Reset Stat"))
                synth.MPTK_ResetStat();
        }


        static public bool CheckSFExists()
        {
            if (MidiPlayerGlobal.ImSFCurrent == null || !MidiPlayerGlobal.MPTK_SoundFontLoaded)
            {
                //Debug.Log(MidiPlayerGlobal.ErrorNoSoundFont);
                return false;
            }
            return true;
        }

        static public string LabelFromMidi(int midi)
        {
            if (midi < 0 || midi >= ListNote.Count)
                return "xx";
            else
                return ListNote[midi].Label;
        }

        static public string LabelFromEcart(int ecart)
        {
            if (ecart < 0 || ecart >= 12)
                return "xx";
            else
                return ListEcart[ecart].Label;
        }
        static public void InitNote()
        {
            ListEcart = new List<HelperDemo>();
            ListEcart.Add(new HelperDemo() { Label = "C", Midi = 0, });
            ListEcart.Add(new HelperDemo() { Label = "C#", Midi = 1, });
            ListEcart.Add(new HelperDemo() { Label = "D", Midi = 2, });
            ListEcart.Add(new HelperDemo() { Label = "D#", Midi = 3, });
            ListEcart.Add(new HelperDemo() { Label = "E", Midi = 4, });
            ListEcart.Add(new HelperDemo() { Label = "F", Midi = 5, });
            ListEcart.Add(new HelperDemo() { Label = "F#", Midi = 6, });
            ListEcart.Add(new HelperDemo() { Label = "G", Midi = 7, });
            ListEcart.Add(new HelperDemo() { Label = "G#", Midi = 8, });
            ListEcart.Add(new HelperDemo() { Label = "A", Midi = 9, });
            ListEcart.Add(new HelperDemo() { Label = "A#", Midi = 10, });
            ListEcart.Add(new HelperDemo() { Label = "B", Midi = 11, });

            ListNote = new List<HelperDemo>();
            ListNote.Add(new HelperDemo() { Label = "C0", Midi = 0, });
            ListNote.Add(new HelperDemo() { Label = "C0#", Midi = 1, });
            ListNote.Add(new HelperDemo() { Label = "D0", Midi = 2, });
            ListNote.Add(new HelperDemo() { Label = "D0#", Midi = 3, });
            ListNote.Add(new HelperDemo() { Label = "E0", Midi = 4, });
            ListNote.Add(new HelperDemo() { Label = "F0", Midi = 5, });
            ListNote.Add(new HelperDemo() { Label = "F0#", Midi = 6, });
            ListNote.Add(new HelperDemo() { Label = "G0", Midi = 7, });
            ListNote.Add(new HelperDemo() { Label = "G0#", Midi = 8, });
            ListNote.Add(new HelperDemo() { Label = "A0", Midi = 9, });
            ListNote.Add(new HelperDemo() { Label = "A0#", Midi = 10, });
            ListNote.Add(new HelperDemo() { Label = "B0", Midi = 11, });
            ListNote.Add(new HelperDemo() { Label = "C1", Midi = 12, });
            ListNote.Add(new HelperDemo() { Label = "C1#", Midi = 13, });
            ListNote.Add(new HelperDemo() { Label = "D1", Midi = 14, });
            ListNote.Add(new HelperDemo() { Label = "D1#", Midi = 15, });
            ListNote.Add(new HelperDemo() { Label = "E1", Midi = 16, });
            ListNote.Add(new HelperDemo() { Label = "F1", Midi = 17, });
            ListNote.Add(new HelperDemo() { Label = "F1#", Midi = 18, });
            ListNote.Add(new HelperDemo() { Label = "G1", Midi = 19, });
            ListNote.Add(new HelperDemo() { Label = "G1#", Midi = 20, });
            ListNote.Add(new HelperDemo() { Label = "A1", Midi = 21, });
            ListNote.Add(new HelperDemo() { Label = "A1#", Midi = 22, });
            ListNote.Add(new HelperDemo() { Label = "B1", Midi = 23, });
            ListNote.Add(new HelperDemo() { Label = "C2", Midi = 24, });
            ListNote.Add(new HelperDemo() { Label = "C2#", Midi = 25, });
            ListNote.Add(new HelperDemo() { Label = "D2", Midi = 26, });
            ListNote.Add(new HelperDemo() { Label = "D2#", Midi = 27, });
            ListNote.Add(new HelperDemo() { Label = "E2", Midi = 28, });
            ListNote.Add(new HelperDemo() { Label = "F2", Midi = 29, });
            ListNote.Add(new HelperDemo() { Label = "F2#", Midi = 30, });
            ListNote.Add(new HelperDemo() { Label = "G2", Midi = 31, });
            ListNote.Add(new HelperDemo() { Label = "G2#", Midi = 32, });
            ListNote.Add(new HelperDemo() { Label = "A2", Midi = 33, });
            ListNote.Add(new HelperDemo() { Label = "A2#", Midi = 34, });
            ListNote.Add(new HelperDemo() { Label = "B2", Midi = 35, });
            ListNote.Add(new HelperDemo() { Label = "C3", Midi = 36, });
            ListNote.Add(new HelperDemo() { Label = "C3#", Midi = 37, });
            ListNote.Add(new HelperDemo() { Label = "D3", Midi = 38, });
            ListNote.Add(new HelperDemo() { Label = "D3#", Midi = 39, });
            ListNote.Add(new HelperDemo() { Label = "E3", Midi = 40, });
            ListNote.Add(new HelperDemo() { Label = "F3", Midi = 41, });
            ListNote.Add(new HelperDemo() { Label = "F3#", Midi = 42, });
            ListNote.Add(new HelperDemo() { Label = "G3", Midi = 43, });
            ListNote.Add(new HelperDemo() { Label = "G3#", Midi = 44, });
            ListNote.Add(new HelperDemo() { Label = "A3", Midi = 45, });
            ListNote.Add(new HelperDemo() { Label = "A3#", Midi = 46, });
            ListNote.Add(new HelperDemo() { Label = "B3", Midi = 47, });
            ListNote.Add(new HelperDemo() { Label = "C4", Midi = 48, });
            ListNote.Add(new HelperDemo() { Label = "C4#", Midi = 49, });
            ListNote.Add(new HelperDemo() { Label = "D4", Midi = 50, });
            ListNote.Add(new HelperDemo() { Label = "D4#", Midi = 51, });
            ListNote.Add(new HelperDemo() { Label = "E4", Midi = 52, });
            ListNote.Add(new HelperDemo() { Label = "F4", Midi = 53, });
            ListNote.Add(new HelperDemo() { Label = "F4#", Midi = 54, });
            ListNote.Add(new HelperDemo() { Label = "G4", Midi = 55, });
            ListNote.Add(new HelperDemo() { Label = "G4#", Midi = 56, });
            ListNote.Add(new HelperDemo() { Label = "A4", Midi = 57, });
            ListNote.Add(new HelperDemo() { Label = "A4#", Midi = 58, });
            ListNote.Add(new HelperDemo() { Label = "B4", Midi = 59, });
            ListNote.Add(new HelperDemo() { Label = "C5", Midi = 60, });
            ListNote.Add(new HelperDemo() { Label = "C5#", Midi = 61, });
            ListNote.Add(new HelperDemo() { Label = "D5", Midi = 62, });
            ListNote.Add(new HelperDemo() { Label = "D5#", Midi = 63, });
            ListNote.Add(new HelperDemo() { Label = "E5", Midi = 64, });
            ListNote.Add(new HelperDemo() { Label = "F5", Midi = 65, });
            ListNote.Add(new HelperDemo() { Label = "F5#", Midi = 66, });
            ListNote.Add(new HelperDemo() { Label = "G5", Midi = 67, });
            ListNote.Add(new HelperDemo() { Label = "G5#", Midi = 68, });
            ListNote.Add(new HelperDemo() { Label = "A5", Midi = 69, });
            ListNote.Add(new HelperDemo() { Label = "A5#", Midi = 70, });
            ListNote.Add(new HelperDemo() { Label = "B5", Midi = 71, });
            ListNote.Add(new HelperDemo() { Label = "C6", Midi = 72, });
            ListNote.Add(new HelperDemo() { Label = "C6#", Midi = 73, });
            ListNote.Add(new HelperDemo() { Label = "D6", Midi = 74, });
            ListNote.Add(new HelperDemo() { Label = "D6#", Midi = 75, });
            ListNote.Add(new HelperDemo() { Label = "E6", Midi = 76, });
            ListNote.Add(new HelperDemo() { Label = "F6", Midi = 77, });
            ListNote.Add(new HelperDemo() { Label = "F6#", Midi = 78, });
            ListNote.Add(new HelperDemo() { Label = "G6", Midi = 79, });
            ListNote.Add(new HelperDemo() { Label = "G6#", Midi = 80, });
            ListNote.Add(new HelperDemo() { Label = "A6", Midi = 81, });
            ListNote.Add(new HelperDemo() { Label = "A6#", Midi = 82, });
            ListNote.Add(new HelperDemo() { Label = "B6", Midi = 83, });
            ListNote.Add(new HelperDemo() { Label = "C7", Midi = 84, });
            ListNote.Add(new HelperDemo() { Label = "C7#", Midi = 85, });
            ListNote.Add(new HelperDemo() { Label = "D7", Midi = 86, });
            ListNote.Add(new HelperDemo() { Label = "D7#", Midi = 87, });
            ListNote.Add(new HelperDemo() { Label = "E7", Midi = 88, });
            ListNote.Add(new HelperDemo() { Label = "F7", Midi = 89, });
            ListNote.Add(new HelperDemo() { Label = "F7#", Midi = 90, });
            ListNote.Add(new HelperDemo() { Label = "G7", Midi = 91, });
            ListNote.Add(new HelperDemo() { Label = "G7#", Midi = 92, });
            ListNote.Add(new HelperDemo() { Label = "A7", Midi = 93, });
            ListNote.Add(new HelperDemo() { Label = "A7#", Midi = 94, });
            ListNote.Add(new HelperDemo() { Label = "B7", Midi = 95, });
            ListNote.Add(new HelperDemo() { Label = "C8", Midi = 96, });
            ListNote.Add(new HelperDemo() { Label = "C8#", Midi = 97, });
            ListNote.Add(new HelperDemo() { Label = "D8", Midi = 98, });
            ListNote.Add(new HelperDemo() { Label = "D8#", Midi = 99, });
            ListNote.Add(new HelperDemo() { Label = "E8", Midi = 100, });
            ListNote.Add(new HelperDemo() { Label = "F8", Midi = 101, });
            ListNote.Add(new HelperDemo() { Label = "F8#", Midi = 102, });
            ListNote.Add(new HelperDemo() { Label = "G8", Midi = 103, });
            ListNote.Add(new HelperDemo() { Label = "G8#", Midi = 104, });
            ListNote.Add(new HelperDemo() { Label = "A8", Midi = 105, });
            ListNote.Add(new HelperDemo() { Label = "A8#", Midi = 106, });
            ListNote.Add(new HelperDemo() { Label = "B8", Midi = 107, });
            ListNote.Add(new HelperDemo() { Label = "C9", Midi = 108, });
            ListNote.Add(new HelperDemo() { Label = "C9#", Midi = 109, });
            ListNote.Add(new HelperDemo() { Label = "D9", Midi = 110, });
            ListNote.Add(new HelperDemo() { Label = "D9#", Midi = 111, });
            ListNote.Add(new HelperDemo() { Label = "E9", Midi = 112, });
            ListNote.Add(new HelperDemo() { Label = "F9", Midi = 113, });
            ListNote.Add(new HelperDemo() { Label = "F9#", Midi = 114, });
            ListNote.Add(new HelperDemo() { Label = "G9", Midi = 115, });
            ListNote.Add(new HelperDemo() { Label = "G9#", Midi = 116, });
            ListNote.Add(new HelperDemo() { Label = "A9", Midi = 117, });
            ListNote.Add(new HelperDemo() { Label = "A9#", Midi = 118, });
            ListNote.Add(new HelperDemo() { Label = "B9", Midi = 119, });
            ListNote.Add(new HelperDemo() { Label = "C10", Midi = 120, });
            ListNote.Add(new HelperDemo() { Label = "C10#", Midi = 121, });
            ListNote.Add(new HelperDemo() { Label = "D10", Midi = 122, });
            ListNote.Add(new HelperDemo() { Label = "D10#", Midi = 123, });
            ListNote.Add(new HelperDemo() { Label = "E10", Midi = 124, });
            ListNote.Add(new HelperDemo() { Label = "F10", Midi = 125, });
            ListNote.Add(new HelperDemo() { Label = "F10#", Midi = 126, });
            ListNote.Add(new HelperDemo() { Label = "G10", Midi = 127, });

            //ListNote[60].Ratio = 1f; // C3
            //ListNote[60].Frequence = 261.626f; // C3

            //foreach (HelperNote hn in ListNote)
            //{
            //    hn.Ratio = Mathf.Pow(_ratioHalfTone, hn.Midi);
            //    hn.Frequence = ListNote[48].Frequence * hn.Ratio;
            //    //Debug.Log("Position:" + hn.Position +" Hauteur:" + hn.Hauteur +" Label:" + hn.Label +" Ratio:" + hn.Ratio +" Frequence:" + hn.Frequence);
            //}
        }

        public enum Zone { INIT, BEGIN, END, CLEAN }
        static public int CountHorizontal;
        static public int CountVertical;

#if MPTK_PRO

        static public void GUI_EffectSoundFont(float indent, MPTKEffectSoundFont effectSF)
        {
            GUI_Horizontal(Zone.BEGIN, myStyle.BacgDemosLight);

            GUI_Indent(indent);

            GUI_Vertical(Zone.BEGIN);
            GUILayout.Label("The effects of the soundfont are part of the design of each instrument. The designer carefully chose them to give the samples the best sound without too much emphasis, but as close as possible to the real sound.", myStyle.TitleLabel3);
            //GUILayout.Label("There are three kinds of effects: low-pass filter, reverb, chorus. These parameters can be specifics for each instruments and even each notes.", myStyle.TitleLabel3);

            GUI_Horizontal(Zone.BEGIN);
            effectSF.EnableFilter = GUILayout.Toggle(effectSF.EnableFilter, "Enable Low Pass Filter");
            effectSF.EnableReverb = GUILayout.Toggle(effectSF.EnableReverb, "Enable Reverb");
            effectSF.EnableChorus = GUILayout.Toggle(effectSF.EnableChorus, "Enable Chorus");
            GUI_Horizontal(Zone.END);

            GUI_Vertical(Zone.END);

            GUI_Horizontal(Zone.END);
        }

        static public void GUI_EffectUnity(float indent, MPTKEffectUnity effect)
        {
            GUI_Horizontal(Zone.BEGIN, myStyle.BacgDemosLight);

            GUI_Indent(indent);

            GUI_Vertical(Zone.BEGIN);

            GUILayout.Label("Unity effects applied to all instruments/presets playing in the Maestro Prefab.", myStyle.TitleLabel3);

            effect.EnableReverb = GUILayout.Toggle(effect.EnableReverb, "Enable Reverb");
            if (effect.EnableReverb)
            {
                effect.ReverbDryLevel = GUI_Slider("      Dry Level", effect.ReverbDryLevel, 0, 1, alignCaptionRight: false, valueButton: 0.1f, widthCaption: 120, widthSlider: 200, widthLabelValue: 20);
                effect.ReverbRoom = GUI_Slider("      Room Size", effect.ReverbRoom, 0, 1, alignCaptionRight: false, valueButton: 0.1f, widthCaption: 120, widthSlider: 200, widthLabelValue: 20);
            }
            effect.EnableChorus = GUILayout.Toggle(effect.EnableChorus, "Enable Chorus");

            GUI_Vertical(Zone.END);

            GUI_Horizontal(Zone.END);
        }
#else
        static public void GUI_EffectSoundFont(float indent)
        {
            GUI_Horizontal(Zone.BEGIN, myStyle.BacgDemosLight);
            GUI_Indent(indent);
            GUI_Vertical(Zone.BEGIN);
            GUILayout.Label("The effects of the soundfont are part of the design of each instrument. The designer carefully chose them to give the samples the best sound without too much emphasis, but as close as possible to the real sound.", myStyle.TitleLabel3);
            GUILayout.Label("Available with Maestro MPTK Pro.", myStyle.TitleLabel3);
            GUI_Vertical(Zone.END);
            GUI_Horizontal(Zone.END);
        }

        static public void GUI_EffectUnity(float indent)
        {
            GUI_Horizontal(Zone.BEGIN, myStyle.BacgDemosLight);
            GUI_Indent(indent);
            GUI_Vertical(Zone.BEGIN);
            GUILayout.Label("Unity effects applied to all instruments/presets playing in the Maestro Prefab.", myStyle.TitleLabel3);
            GUILayout.Label("Available with Maestro MPTK Pro.", myStyle.TitleLabel3);
            GUI_Vertical(Zone.END);
            GUI_Horizontal(Zone.END);
        }
#endif

        static public void GUI_Horizontal(Zone zone, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (zone == Zone.BEGIN)
            {
                if (style == null)
                    GUILayout.BeginHorizontal(options);
                else
                    GUILayout.BeginHorizontal(style, options);
                CountHorizontal++;
            }
            else if (zone == Zone.END)
            {
                if (CountHorizontal > 0)
                    GUILayout.EndHorizontal();
                CountHorizontal--;
            }
            else if (zone == Zone.CLEAN)
            {
                //Debug.Log($"GUI_Horizontal CLEAN {CountHorizontal}");
                while (CountHorizontal > 0)
                {
                    CountHorizontal--;
                    //Debug.Log($"GUI_Horizontal EndHorizontal {CountHorizontal}");
                    GUILayout.EndHorizontal();
                }
            }
            else if (zone == Zone.INIT)
            {
                //Debug.Log($"GUI_Horizontal INIT {CountHorizontal}");
                CountHorizontal = 0;
            }
        }
        static public void GUI_Vertical(Zone zone, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (zone == Zone.BEGIN)
            {
                if (style == null)
                    GUILayout.BeginVertical(options);
                else
                    GUILayout.BeginVertical(style, options);
                CountVertical++;
            }
            else if (zone == Zone.END)
            {
                if (CountVertical > 0)
                    GUILayout.EndVertical();
                CountVertical--;
            }
            else if (zone == Zone.CLEAN)
            {
                //Debug.Log($"GUI_Horizontal CLEAN {CountHorizontal}");
                while (CountVertical > 0)
                {
                    CountVertical--;
                    //Debug.Log($"GUI_Horizontal EndHorizontal {CountHorizontal}");
                    GUILayout.EndVertical();
                }
            }
            else if (zone == Zone.INIT)
            {
                //Debug.Log($"GUI_Horizontal INIT {CountHorizontal}");
                CountVertical = 0;
            }
        }
        static public void GUI_Indent(float indent)
        {
            // Indent the gui
            GUI_Vertical(Zone.BEGIN, myStyle.BacgDemosLight, GUILayout.Width(indent), GUILayout.ExpandWidth(false));
            GUILayout.Space(indent);
            GUI_Vertical(Zone.END);
        }

        static public float GUI_Slider(string caption, float val, float min, float max, bool alignCaptionRight = false,
            bool enableButton = true, float valueButton = 1f, float widthCaption = 100, float widthSlider = 100, float widthLabelValue = 30)
        {
            GUI_Horizontal(Zone.BEGIN/*, myStyle.BacgDemosLight*/);

            if (!string.IsNullOrEmpty(caption) && widthCaption > 0f)
                GUILayout.Label(caption, alignCaptionRight ? myStyle.LabelRight : myStyle.LabelLeft, GUILayout.Width(widthCaption), GUILayout.Height(25));

            if (widthLabelValue > 0f)
                GUILayout.Label(Math.Round(val, 2).ToString(), myStyle.LabelRight, GUILayout.Width(widthLabelValue), GUILayout.Height(25));

            if (enableButton)
            {
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(20)))
                { val -= valueButton; if (val < min) val = min; }
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(20)))
                { val += valueButton; if (val > max) val = max; }
            }

            float ret = GUILayout.HorizontalSlider(val, min, max, myStyle.SliderBar, myStyle.SliderThumb, GUILayout.Width(widthSlider));

            GUI_Horizontal(Zone.END);
            return ret;
        }

    }
}
