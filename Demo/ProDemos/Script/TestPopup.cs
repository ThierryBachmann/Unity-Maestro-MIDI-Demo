using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestPopup : MonoBehaviour
    {
        public PopupListItem PopTest;
        public PopupListItem PopMidi;
        public PopupListItem PopPreset;
        public CustomStyle myStyle;
        public Vector2 scrollerWindow;
        public List<MPTKListItem> ItemsTest;
        [Range(0, 1000)]
        public int countItem;
        public int selectedItem;
        [Range(0, 20)]
        public int ColCount = 5;
        public int ColWidth = 200;
        public int ColHeight = 30;
        [Header("Count of rows (readonly)")]
        public int CountRowsTest;
        public Vector2 position = new Vector2(0, 50);

        private void Awake()
        {
            // MidiPlayerGlobal is a singleton: only one instance can be created. 
            // This demo don't hold MAestro prefab, so we instanciate one (mandatory to access information like MIDI in the MidiDB
            if (MidiPlayerGlobal.Instance == null)
                gameObject.AddComponent<MidiPlayerGlobal>();
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }

        // Use this for initialization
        void Start()
        {
            PopTest = new PopupListItem()
            {
                Title = "Test Popup - change attribute in inspector",
                OnSelect = CallbackPopup,
                Tag = "Test Popup",
                // ColCount and ColWidth can be changed dynamicallu in the inspector
            };
            PopMidi = new PopupListItem()
            {
                Title = "Popup MIDI",
                OnSelect = CallbackPopup,
                Tag = "Popup MIDI",
                ColCount = 3,
                ColWidth = 250,
            };
            PopPreset = new PopupListItem()
            {
                Title = "Popup Preset",
                OnSelect = CallbackPopup,
                Tag = "Popup Preset",
                ColCount = 4,
                ColWidth = 200,
            };
            
        }

        private void CallbackPopup(object tag, int index, int indexList)
        {
            Debug.Log("CallbackPopup " + index + " for " + tag);
            selectedItem = index;
        }

        void OnGUI()
        {
            // Set custom Style. Good for background color 3E619800
            if (myStyle == null)
                myStyle = new CustomStyle();

            if (ItemsTest == null || countItem != ItemsTest.Count)
            {
                ItemsTest = new List<MPTKListItem>();
                for (int i = 0; i < countItem; i++)
                    ItemsTest.Add(new MPTKListItem() { Index = i, Label = i.ToString(), });
            }

            scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));

            PopTest.ColCount = ColCount;
            PopTest.ColHeight = ColHeight;
            PopTest.ColWidth = ColWidth;

            // Display popup in first to avoid activate other layout behind
            PopTest.Draw(ItemsTest, selectedItem, myStyle);
            
            PopMidi.Draw(MidiPlayerGlobal.MPTK_ListMidi, selectedItem, myStyle);
            PopPreset.Draw(MidiPlayerGlobal.MPTK_ListPreset, selectedItem, myStyle);

            CountRowsTest = PopTest.CountRow;

            GUILayout.EndScrollView();
        }

        public void ShowPopupTest()
        {
            PopTest.Show = !PopTest.Show;
            PopTest.Position(position);
        }

        public void ShowPopupMidi()
        {
            PopMidi.Show = !PopMidi.Show;
            PopMidi.Position(position);
        }
        public void ShowPopupPreset()
        {
            PopPreset.Show = !PopPreset.Show;
            PopPreset.Position(position);
        }
        // Update is called once per frame
        void Update()
        {

        }
    }
}