using MidiPlayerTK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemoMVPSwitchScene
{

    public class SwitchSceneController : MonoBehaviour
    {

        private void Awake()
        {
            Debug.Log("Awake: SwitchSceneController");
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // **** Don't forget to add the scene SwitchSceneChild to your "Scenes in Build" in the build settings ****
        // Linked to UI button on the scene
        public void LoadSceneChild()
        {
            Debug.Log("LoadSceneChild: <b>the MidiFilePlayer is still playing</b>");
            Debug.LogWarning("When running in Unity Editor, possible some 'glitch' with the MIDI Player when switching.");
            Debug.LogWarning("Luckiliy, nothing weird detected from a built application, thanks to IL2CPP.");
            SceneManager.LoadScene("SwitchSceneChild", LoadSceneMode.Single);
        }

        // **** Don't forget to add the scene SwitchScene to your "Scenes in Build" in the build settings ****
        // Linked to UI button on the scene
        public void LoadSceneHome()
        {
            Debug.Log("LoadSceneHome: <b>the MidiFilePlayer is still playing</b>");
            Debug.LogWarning("When running in Unity Editor, possible some 'glitch' with the MIDI Player when switching.");
            Debug.LogWarning("Luckiliy, nothing weird detected from a built application, thanks to IL2CPP.");
            SceneManager.LoadScene("SwitchScene", LoadSceneMode.Single);
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}