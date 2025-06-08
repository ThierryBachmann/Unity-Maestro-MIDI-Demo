using MidiPlayerTK;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MPTKDemoEuclidean
{
    public class PanelController : MonoBehaviour/*, IPointerDownHandler, IPointerClickHandler,
    IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler*/
    {
        //public virtual void OnBeginDrag(PointerEventData eventData)
        //{
        //   Debug.Log("Drag Begin: " + eventData.pointerCurrentRaycast.gameObject.name);
        //}

        //public virtual void OnDrag(PointerEventData eventData)
        //{
        //    Debug.Log("Drag: " + eventData.scrollDelta);
        //}

        //public virtual void OnEndDrag(PointerEventData eventData)
        //{
        //    Debug.Log("Drag Ended: " + eventData.pointerCurrentRaycast.gameObject.name);
        //}

        //public virtual void OnPointerClick(PointerEventData eventData)
        //{
        //    Debug.Log("Clicked: " + eventData.pointerCurrentRaycast.gameObject.name);
        //}

        //public virtual void OnPointerDown(PointerEventData eventData)
        //{
        //    Debug.Log("OnPointerDown: " + eventData.pointerCurrentRaycast.gameObject.name);
        //}

        //public virtual void OnPointerUp(PointerEventData eventData)
        //{
        //    Debug.Log("OnPointerUp: " + eventData.pointerCurrentRaycast.gameObject.name);
        //}

        //public virtual void OnPointerEnter(PointerEventData eventData)
        //{
        //    Debug.Log("Mouse Enter");
        //}

        //public virtual void OnPointerExit(PointerEventData eventData)
        //{
        //    Debug.Log("Mouse Exit");
        //}

        public enum Mode
        {
            EuclideDrums,
            PlayerDrums,
            PlayerInstrument,
        }

        public MidiStreamPlayer midiStream;

        public GameObject PanelTapDrum;
        public GameObject PanelDrumSample;
        public GameObject PanelTapSustain;
        public GameObject PanelRTEffect;
        public GameObject PanelEuclidean;
        public GameObject PanelView;

        public TextSlider Step;
        public TextSlider Fill;
        public TextSlider Accent;
        public TextSlider Offset;
        public Transform Arrow;
        public LightSlider SldVolume;

        public Toggle Mute;
        public Toggle Solo;

        /// <summary>@brief
        /// Number of step defined for the euclidean algo
        /// </summary>
        public int CountStep;

        /// <summary>@brief
        /// Number of step filled for the euclidean algo (between 0 and CountStep)
        /// </summary>
        public int CountFill;

        /// <summary>@brief
        /// Number of step with accentuation (between 0 and CountStep)
        /// </summary>
        public int CountAccent;

        /// <summary>@brief
        /// Offset of step for the euclidean algo (between 0 and CountStep)
        /// </summary>
        public int CountOffset;

        public Mode PlayMode;
        public int CurrentBeat;
        public int LastBeat;

        public int PresetInstrument;
        PopupListBox popupInstrument;
        public Text TxtSelectedInstrument;

        public bool IsReady = false;
        public bool ToBeRemoved;
        public bool ToBeRandomsed;
        public bool ToBeDuplicated;
        public PanelController DuplicateFrom;
        public int Channel;
        public Transform Parent;

        public GameObject SphereHitTemplate;

        // These materials are defined in EuclideSeq/Material
        public Material HitCurrent; // red
        public Material HitDisable; // transparent
        public Material HitEnable;  // green

        public fluid_gen_type GenTypeH;
        public fluid_gen_type GenTypeV;
        public float EffectH;
        public float EffectV;

        public TapEffect TapEffect;
        public TapSwitch SwitchSustain;

        public List<MPTKEvent> playerEvents;
        public MPTKEvent sequencerEvent;

        const int sizeBorder = 10;
        const int sizeTexture = 500;
        const int sizeCircle = 40;
        const float size3D = 5;

        //RawImage Image;
        private GameObject[] HitsPool;
        private GameObject[] HitsStep;

        public float ratioStep;

        private BjorklundAlgo BjorklundAlgoFill;
        private BjorklundAlgo BjorklundAlgoAccent;

        // Angle of the sprite arrow
        private float angleTargetArrow;
        private float velocityArrow = 0f;

        /// <summary>@brief
        /// Return true if a sound must be played for the current beat
        /// </summary>
        public bool SequenceHit
        {
            get
            {
                if (BjorklundAlgoFill != null && BjorklundAlgoFill.Sequence != null && CurrentBeat < BjorklundAlgoFill.Sequence.Count && CurrentBeat >= 0)
                    return BjorklundAlgoFill.Sequence[CurrentBeat];
                else
                    return false;
            }
        }

        public bool Accentuation
        {
            get
            {
                if (BjorklundAlgoAccent != null && BjorklundAlgoAccent.Sequence != null && CurrentBeat < BjorklundAlgoAccent.Sequence.Count && CurrentBeat >= 0)
                    return BjorklundAlgoAccent.Sequence[CurrentBeat];
                else
                    return false;
            }
        }

        private void Awake()
        {
            //Debug.Log("Awake " + name);
            IsReady = false;
            ToBeRandomsed = false;
            ToBeDuplicated = false;
            ToBeRemoved = false;
        }


        // Use this for initialization
        void Start()
        {
            if (name == "ControllerTemplate")
                return;

            //Debug.Log("Start " + name);
            playerEvents = new List<MPTKEvent>();

            SwitchSustain = PanelTapSustain.GetComponentInChildren<TapSwitch>();

            popupInstrument = PlayMode == Mode.EuclideDrums || PlayMode == Mode.PlayerDrums ? TestEuclideanRhythme.PopupListDrum : TestEuclideanRhythme.PopupListInstrument;
            PresetInstrument = popupInstrument.FirstIndex();

            BjorklundAlgoFill = new BjorklundAlgo();
            BjorklundAlgoFill.Generate(CountStep, CountFill, CountOffset);

            BjorklundAlgoAccent = new BjorklundAlgo();
            BjorklundAlgoAccent.Generate(CountStep, CountAccent, CountOffset);

            Parent = SphereHitTemplate.transform.parent;

            foreach (Transform t in Parent.transform)
                if (t.gameObject.name.StartsWith(SphereHitTemplate.name + " "))
                    Destroy(t.gameObject);

            SphereHitTemplate.SetActive(false);
            HitsPool = new GameObject[TestEuclideanRhythme.MaxStep];

            for (int i = 0; i < TestEuclideanRhythme.MaxStep; i++)
            {
                HitsPool[i] = Instantiate<GameObject>(SphereHitTemplate, Parent);
                // HitsPool[i].gameObject.name = string.Format("{0} {1,2:00}", SphereHitTemplate.name, i);
                HitsPool[i].gameObject.name = $"{SphereHitTemplate.name} {i,2:00}";
            }

            Step.OnEventValue.AddListener((int v) =>
            {
                //Debug.Log("Step");
                CountStep = v;
                HitsStep = new GameObject[CountStep];

                ratioStep = CountStep > 0 ? (float)TestEuclideanRhythme.MaxStep / (float)CountStep : TestEuclideanRhythme.MaxStep;

                Hit3dPosition(CountStep);

                Fill.SetRange(1, CountStep);
                Accent.SetRange(0, CountStep);
                Offset.SetRange(0, CountStep);

                BjorklundAlgoFill.Generate(CountStep, CountFill, CountOffset);
                BjorklundAlgoAccent.Generate(CountStep, CountAccent, CountOffset);
                RefreshPanelView();
            });

            Fill.OnEventValue.AddListener((int v) =>
            {
                //Debug.Log("Fill");
                CountFill = v;
                BjorklundAlgoFill.Generate(CountStep, CountFill, CountOffset);
                RefreshPanelView();
            });

            Accent.OnEventValue.AddListener((int v) =>
            {
                //Debug.Log("Accent");
                CountAccent = v;
                BjorklundAlgoAccent.Generate(CountStep, CountAccent, CountOffset);
                RefreshPanelView();
            });

            Offset.OnEventValue.AddListener((int v) =>
            {
                //Debug.Log("Offset");
                CountOffset = v;
                BjorklundAlgoFill.Generate(CountStep, CountFill, CountOffset);
                RefreshPanelView();
            });

            if (DuplicateFrom != null)
            {
                Step.Value = DuplicateFrom.Step.Value;
                Fill.Value = DuplicateFrom.Fill.Value;
                Accent.Value = DuplicateFrom.Accent.Value;
                Offset.Value = DuplicateFrom.Offset.Value;
                PresetInstrument = DuplicateFrom.PresetInstrument;
                DuplicateFrom = null;
            }

            Step.OnEventValue.Invoke(Step.Value);
            Fill.OnEventValue.Invoke(Fill.Value);
            Accent.OnEventValue.Invoke(Accent.Value);
            Offset.OnEventValue.Invoke(Offset.Value);

            GenTypeH = fluid_gen_type.GEN_STARTADDROFS; // real time generator not used 
            TapEffect.OnEventPadHorizontal.AddListener((float val, int idEffect) =>
            {
                GenTypeH = (fluid_gen_type)idEffect;
                Debug.Log($"EffectH {GenTypeH} {val}");
                EffectH = val;

                // Apply effects for EuclideanRhythme
                if (sequencerEvent != null)
                    sequencerEvent.ModifySynthParameter(GenTypeH, EffectH, MPTKModeGeneratorChange.Override);

                // Apply effects for Player Instrument
                foreach (MPTKEvent ev in playerEvents)
                    ev.ModifySynthParameter(GenTypeH, EffectH, MPTKModeGeneratorChange.Override);
            });

            GenTypeV = fluid_gen_type.GEN_STARTADDROFS; // real time generator not used 
            TapEffect.OnEventPadVertical.AddListener((float val, int idEffect) =>
            {
                GenTypeV = (fluid_gen_type)idEffect;
                Debug.Log($"EffectH {GenTypeV} {val}");

                // Apply effects for EuclideanRhythme
                EffectV = val;
                if (sequencerEvent != null)
                    sequencerEvent.ModifySynthParameter(GenTypeV, EffectV, MPTKModeGeneratorChange.Override);

                // Apply effects for Player Instrument
                foreach (MPTKEvent ev in playerEvents)
                    ev.ModifySynthParameter(GenTypeV, EffectV, MPTKModeGeneratorChange.Override);
            });

            // This event is trigerred only when state is changed.
            SwitchSustain.OnEventSwitchChangeState.AddListener((bool sustainOn) =>
            {
                if (!sustainOn)
                    // stop all samples
                    StopAll();

            });

            // This event is trigerred with a pointer down when switch is locked, state is not changed.
            SwitchSustain.OnEventSwitchLockedOn.AddListener((bool sustainOn) =>
            {
                // stop all samples, but sustain remains to On
                StopAll();
            });


            popupInstrument.Select(PresetInstrument);
            TxtSelectedInstrument.text = popupInstrument.LabelSelected(PresetInstrument);
            //Debug.Log($"Start PresetInstrument {PresetInstrument} {popupInstrument.LabelSelected(PresetInstrument)}");

            IsReady = true;
        }

        static int ExtractPresetNumber(string label)
        {
            int preset = 0;
            try
            {
                preset = Convert.ToInt32(label.Substring(0, label.IndexOf('-') - 1));

            }
            catch (Exception)
            {
            }
            return preset;
        }
        /// <summary>@brief
        /// When step change, build the list of spheres representing each hit from the caching spehere
        /// </summary>
        /// <param name="countStep"></param>
        private void Hit3dPosition(int countStep)
        {
            //                  size of the PanelColView as view in the inspector (90) /2 - size of the sphere (8) - marge
            float rayon = ((RectTransform)SphereHitTemplate.transform.parent).sizeDelta.x / 2f - SphereHitTemplate.transform.localScale.x - 4f;
            //Debug.Log(rayon);
            for (int i = 0; i < TestEuclideanRhythme.MaxStep; i++)
            {
                if (i < countStep)
                {
                    // Fill the HitSteps with the necessary Sphere from the caching HitsPool
                    HitsStep[i] = HitsPool[i];
                    // negative: turn clockwise ; 2PI: 360 deg ; +PI/2 to start at the top
                    float angle = -2f * Mathf.PI / countStep * i + Mathf.PI / 2f;
                    float x = rayon * Mathf.Cos(angle);
                    float y = rayon * Mathf.Sin(angle);
                    HitsStep[i].transform.localPosition = new Vector3(x, y, SphereHitTemplate.transform.localPosition.z);
                }
                else
                    HitsPool[i].gameObject.SetActive(false);
            }
        }

        public void SelectPreset()
        {
            popupInstrument.OnEventSelect.AddListener((MPTKListItem item) =>
            {
                Debug.Log($"SelectPreset {item.Index} {item.Label}");
                PresetInstrument = item.Index;
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = item.Label;
            });

            popupInstrument.OnEventClose.AddListener(() =>
            {
                Debug.Log($"Close");
                popupInstrument.OnEventSelect.RemoveAllListeners();
                popupInstrument.OnEventClose.RemoveAllListeners();
            });

            popupInstrument.Select(PresetInstrument);
            popupInstrument.gameObject.SetActive(true);
        }

        public void Generate()
        {
            BjorklundAlgoFill.Generate(CountStep, CountFill);
            BjorklundAlgoAccent.Generate(CountStep, CountAccent);
        }

        public void SetDefault()
        {
            Step.Value = TestEuclideanRhythme.MaxStep;
            Fill.Value = TestEuclideanRhythme.MaxStep;
            Accent.Value = 1;
            Fill.SetRange(1, CountStep);
            Accent.SetRange(0, CountStep);
        }

        /// <summary>@brief
        /// This action removes a controller. Set by GUI.
        /// Will be processed in the Update() of the main class EuclideanRhythme component
        /// </summary>
        public void RemoveController()
        {
            ToBeRemoved = true;
        }

        /// <summary>@brief
        /// This action randoms the controller. Set by GUI.
        /// Will be processed in the Update() of the main class EuclideanRhythme component
        /// </summary>
        public void RandomController()
        {
            ToBeRandomsed = true;
        }

        /// <summary>@brief
        /// This action dupplicate the controller. Set by GUI.
        /// Will be processed in the Update() of the main class EuclideanRhythme component
        /// </summary>
        public void DuplicateController()
        {
            ToBeDuplicated = true;
        }

        /// <summary>@brief
        /// Set random values
        /// From the GUI --> set flag --> processed by the Update() 
        /// </summary>
        public void Random()
        {
            Step.Value = UnityEngine.Random.Range(1, 33);
            Fill.Value = UnityEngine.Random.Range(1, Step.Value + 1);
            Accent.Value = UnityEngine.Random.Range(1, Step.Value + 1);
            Offset.Value = UnityEngine.Random.Range(1, Step.Value + 1);
            PresetInstrument = popupInstrument.RandomIndex();
            ToBeRandomsed = false;
        }

        /// <summary>@brief
        /// Update the panel view for Euclidean Rhythm display
        /// </summary>
        public void RefreshPanelView()
        {
            //Debug.Log("Panel Controller " + Parent.name + " " + ((RectTransform)Parent).CountCornersVisibleFrom(Camera.current));// Camera.main));
            if (IsReady /*&& ((RectTransform)Parent).CountCornersVisibleFrom(Camera.current) > 0 */&& PanelView.activeInHierarchy)
            {
                if (HitsStep == null)
                    // Not yet initialized
                    return;

                Transform viewport = this.transform.parent.parent;
                Rect viewportRect = ((RectTransform)viewport).GetScreenRect(Camera.main);
                //Debug.Log($"viewport {viewport.name}  viewportRect:{viewportRect}");

                // screen coordinate: from left bottom.
                Vector3 screenPos_viewport = Camera.main.WorldToScreenPoint(viewport.position);
                //Debug.Log($"         {viewport.name}  y:{screenPos_viewport.y} y:{screenPos_viewport} ");

                // Useful to hide 3D object as hit (Sphere) and arrow. The scrolview position is left bottom (0,0),
                // 
                Vector3 hitScreenPosition = Camera.main.WorldToScreenPoint(Arrow.transform.position);
                //if (hitScreenPosition.y >= 0f & hitScreenPosition.y <= TestEuclideanRhythme.ScrollviewScreenHeight)
                if (hitScreenPosition.y <= screenPos_viewport.y && hitScreenPosition.y >= viewportRect.y/*& hitScreenPosition.y <= TestEuclideanRhythme.ScrollviewScreenHeight*/)
                {
                    Arrow.gameObject.SetActive(true);

                    if (CountStep > 0)
                    {
                        // Therorical position of the arrow. This value could be used to update the sprite rotation
                        angleTargetArrow = -360f * (float)(CurrentBeat + 1) / (float)CountStep;
                    }
                }
                else
                    Arrow.gameObject.SetActive(false);

                for (int i = 0; i < CountStep; i++)
                {
                    // Get the gameobject to represent a hit
                    GameObject hit = HitsStep[i];

                    if (hit != null)
                    {
                        hitScreenPosition = Camera.main.WorldToScreenPoint(hit.transform.position);
                        // Useful to hide 3D object as hit (Sphere) and arrow. The scrolview position is left bottom (0,0),
                        // so we are testing only the Y position of the 3D object in the screen.
                        //if (i == 0) // only the first sphere
                        //    Debug.Log($"{this.name} hit.y:{hit.transform.position.y} hitScreenPosition:{hitScreenPosition.y} ScrollviewScreenHeight:{TestEuclideanRhythme.ScrollviewScreenHeight}");
                        //if (hitScreenPosition.y < 0f || hitScreenPosition.y > TestEuclideanRhythme.ScrollviewScreenHeight)
                        if (hitScreenPosition.y <= screenPos_viewport.y && hitScreenPosition.y >= viewportRect.y)
                        {
                            hit.gameObject.SetActive(true);
                            Renderer materialHit = hit.gameObject.GetComponent<Renderer>();

                            if (BjorklundAlgoFill.Sequence[i] && CurrentBeat == i)
                                // Red: current hit
                                materialHit.material = HitCurrent; // it's a material defined in EuclideSeq/Material
                            else if (BjorklundAlgoFill.Sequence[i])
                                // Green: not yet 
                                materialHit.material = HitEnable; // it's a material defined in EuclideSeq/Material
                            else
                                // This hit is not selected by the BjorklundAlgoFill algo, make sphere transparent.
                                materialHit.material = HitDisable; // it's a material defined in EuclideSeq/Material

                            // Size of the sphere depend on the ration MaxStep / CountStep
                            float sizeHit = size3D;
                            sizeHit *= ratioStep; // ratio from 1 (step=32) to 32 (step=1) to adapt size of the 3D to the count of step
                            sizeHit = Mathf.Clamp(sizeHit, 2f, 17f); // limit size to correct value

                            // Accentuated hits are more bigger 
                            if (BjorklundAlgoAccent.Sequence[i])
                                sizeHit *= 1.3f;

                            // Apply scale transformation
                            hit.transform.localScale = new Vector3(sizeHit, sizeHit, sizeHit);
                        }
                        else
                            hit.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void Update()
        {
            // Need to synch this action with the controller update
            if (ToBeRandomsed)
            {
                Random();
                popupInstrument.Select(PresetInstrument);
                TxtSelectedInstrument.text = popupInstrument.LabelSelected(PresetInstrument);
            }

            if (PanelView.activeInHierarchy)
            {
                // Update arrow position (rotation) 
                // smoothTime = Approximately the time it will take to reach the target. A smaller value will reach the target faster. 
                // Make speed dependant of the tempo. With Tempo=60 bpm, there is 1 second between each hit. It's the time max of the arrow to get the updated position
                float timeSmooth = (float)TestEuclideanRhythme.CurrentTempo / 1000F;
                if (CountStep <= 4) timeSmooth *= 0.6f;
                float angleInter = Mathf.SmoothDampAngle(Arrow.localEulerAngles.z, angleTargetArrow, ref velocityArrow, timeSmooth);
                // Rotate sprite with the interpolated angle
                Arrow.localEulerAngles = new Vector3(0f, 0f, angleInter);
            }
        }

        public void StopAll()
        {
            if (playerEvents != null && playerEvents.Count > 0)
            {
                foreach (MPTKEvent ev in playerEvents)
                    midiStream.MPTK_StopEvent(ev);
                playerEvents = new List<MPTKEvent>();
            }
        }

        public void PlayFromPlayerIntrument(MPTKEvent mptkEvent)
        {
            // Take time as soon as event has been detected
            mptkEvent.Tag = DateTime.UtcNow.Ticks;

            // Apply effects on new NoteOn
            if (GenTypeH > 0) mptkEvent.ModifySynthParameter(GenTypeH, EffectH, MPTKModeGeneratorChange.Override);
            if (GenTypeV > 0) mptkEvent.ModifySynthParameter(GenTypeV, EffectV, MPTKModeGeneratorChange.Override);

            midiStream.MPTK_PlayDirectEvent(mptkEvent);
            playerEvents.Add(mptkEvent);
        }


        /// <summary>@brief
        /// Play EuclideanRhythme from the sequencer. The caller is a system thread, so no Unity API call is possible here
        /// </summary>
        public void PlayEuclideanRhythme()
        {
            if (CountStep > 0 && !Mute.isOn)
            {
                CurrentBeat = TestEuclideanRhythme.CurrentBeat % CountStep;
                if (SequenceHit)
                {
                    int delayAlea = TestEuclideanRhythme.RndHumanize.Next(0, Convert.ToInt32(100f * TestEuclideanRhythme.PctHumanize)); // alea delay between 0 and 100 ms
                    float velAlea = (float)TestEuclideanRhythme.RndHumanize.NextDouble() * TestEuclideanRhythme.PctHumanize; // alea velocity between 0 and 1
                                                                                                                             //Debug.Log($"{delayAlea} {velAlea}");
                    sequencerEvent = new MPTKEvent()
                    {
                        // default MIDI event=NoteOn Command = MPTKCommand.NoteOn,
                        Channel = Channel,
                        Duration = -1, // drum are not looping, so duration is useless
                        Delay = delayAlea, // random delay to humanize
                        Value = PresetInstrument, // each note plays a different drum
                        Velocity = (int)((Accentuation ? 127f : 80f) * (TestEuclideanRhythme.GlobalVolume / 100f) * (1f - velAlea) * (SldVolume.Value / 100f))
                    };

                    // Will be used in noteon to calculate delay to process the note by UI and queuing
                    sequencerEvent.Tag = DateTime.UtcNow.Ticks;

                    // Apply effects on new NoteOn
                    if (GenTypeH > 0) sequencerEvent.ModifySynthParameter(GenTypeH, EffectH, MPTKModeGeneratorChange.Override);
                    if (GenTypeV > 0) sequencerEvent.ModifySynthParameter(GenTypeV, EffectV, MPTKModeGeneratorChange.Override);

                    // Play a NoteOn 
                    midiStream.MPTK_PlayDirectEvent(sequencerEvent);
                }
            }
        }
    }
}

