using MidiPlayerTK;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MPTKDemoEuclidean
{
    public class TapEffect : TapPad
    {
        public Button BtSelectEffetH;
        public Button BtSelectEffetV;
        PopupListBox popupEffect;
        public int EffectH;
        public int EffectV;
        public Text TxtEffectH;
        public Text TxtEffectV;

        new void Start()
        {

            popupEffect = TestEuclideanRhythme.PopupListEffect;
            if (popupEffect != null)
            {
                EffectH = popupEffect.FirstIndex();
                EffectV = popupEffect.FirstIndex();

                //popupEffect.Select(EffectH);
                TxtEffectH.text = popupEffect.LabelSelected(EffectH);
                TxtEffectV.text = popupEffect.LabelSelected(EffectV);
                //Debug.Log($"Start PresetInstrument {EffectH} {popupEffect.LabelSelected(EffectH)}");
            }

            BtSelectEffetH.onClick.AddListener(() =>
            {
                //Debug.Log($"BtSelectEffetH");
                SelectEffect(true);
            });

            BtSelectEffetV.onClick.AddListener(() =>
            {
                //Debug.Log($"BtSelectEffetV");
                SelectEffect(false);
            });
            base.Start();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            TriggerEffects(eventData);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            TriggerEffects(eventData);
        }

        /// <summary>@brief
        /// Select an effect from the popup
        /// </summary>
        /// <param name="hori"></param>
        public void SelectEffect(bool hori)
        {
            popupEffect.OnEventSelect.AddListener((MPTKListItem item) =>
            {
                //Debug.Log($"SelectEffect {item.Index} {item.Label}");
                if (hori)
                {
                    EffectH = item.Index;
                    TxtEffectH.text = item.Label;
                }
                else
                {
                    EffectV = item.Index;
                    TxtEffectV.text = item.Label;
                }
                popupEffect.Select(item.Index);
            });

            popupEffect.OnEventClose.AddListener(() =>
            {
                //Debug.Log($"Close");
                popupEffect.OnEventSelect.RemoveAllListeners();
                popupEffect.OnEventClose.RemoveAllListeners();
            });

            popupEffect.Select(hori ? EffectH : EffectV);
            popupEffect.gameObject.SetActive(true);
        }

        /// <summary>@brief
        /// Apply effect regarding the position on the panel
        /// </summary>
        /// <param name="eventData"></param>
        private void TriggerEffects(PointerEventData eventData)
        {
            float rx, ry;
            if (PointerPosition(eventData, out rx, out ry))
            {
                if (OnEventPadHorizontal != null && EffectH >= 0)
                    OnEventPadHorizontal.Invoke(rx, EffectH);

                if (OnEventPadVertical != null && EffectV >= 0)
                    OnEventPadVertical.Invoke(ry, EffectV);

                if (Label != null) Label.text = $"x:{rx:F2} y:{ry:F2}";
            }
        }
    }
}
