// uncomment to activate RTP and BLE MIDI
#define USE_RTP_BLE_PLUGIN

#if USE_RTP_BLE_PLUGIN
using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using MidiPlayerTK;
using jp.kshoji.midisystem;
using jp.kshoji.unity.midi;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemoMPTK
{
    public class TestMidiKaoruShoji : MonoBehaviour, IMidiAllEventsHandler, IMidiDeviceEventHandler
    {
        // UI related
        public Button ButtonRefreshMidiDevices;
        public Dropdown ComboDeviceOutput;
        public Dropdown ComboDeviceInput;

        public Button ButtonScanBLEDevices;
        public Button ButtonActAsBLEMidiDevice;

        private Color ColorSalmon = new Color(0.90f, 0.72f, 0.72f);
        private Color ColorWhite = new Color(1, 1, 1);

        public InputInt InputChannel;
        public InputInt InputPreset;
        public Button ButtonSendPreset;
        public InputInt InputNote;
        public Button ButtonSendNote;

        public Button ButtonPlayStopMidi;
        public Button ButtonPlayPrevious;
        public Button ButtonPlayNext;

        // Maestro prefab for playing MIDI event or MIDI file.
        public MidiStreamPlayer midiStreamPlayer;
        public MidiFilePlayer midiFilePlayer;

        // Select which device to send MIDI event. To MPTK if null.
        private string selectedOutputDeviceId;

        // Select wich device to receive MIDI event. From all devices if null.
        private string selectedInputDeviceId = null;

#if !UNITY_IOS && !UNITY_WEBGL
        private NetworkInterface[] netInterfaces;
#endif

#if !UNITY_IOS && !UNITY_WEBGL
        //private string ipAddress = "192.168.0.100";
        //private string port = "5004";
#endif

        private void Awake()
        {

#if !UNITY_IOS && !UNITY_WEBGL
            netInterfaces = NetworkInterface.GetAllNetworkInterfaces();
#endif

            MidiManager.Instance.RegisterEventHandleObject(gameObject);
            MidiManager.Instance.InitializeMidi(() =>
            {
                Debug.Log("MidiManager initialized");
#if DEVELOPMENT_BUILD
#if (UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
                MidiManager.Instance.StartScanBluetoothMidiDevices(0);
#endif
#endif
            });
        }

        private void Start()
        {
            midiStreamPlayer = FindFirstObjectByType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
                Debug.LogWarning("Can't find a MidiStreamPlayer Prefab in the current scene.");

            midiFilePlayer = FindFirstObjectByType<MidiFilePlayer>();
            if (midiFilePlayer == null)
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current scene.");

            MidiConnectionWindow();

            // Combo and buttons for selecting Soundfont 
            // ------------------------------------------
            ComboDeviceOutput.onValueChanged.AddListener(delegate
                {
                    if (ComboDeviceOutput.value == 0)
                        // Because the first element is "All output devices"
                        selectedOutputDeviceId = null;
                    else
                        // Get the ID (string) of the selected device
                        selectedOutputDeviceId = MidiManager.Instance.OutputDeviceIdSet.ElementAt(ComboDeviceOutput.value - 1);
                    Debug.Log($"Selected output device: {(selectedOutputDeviceId ?? "All")}");
                });

            ComboDeviceInput.onValueChanged.AddListener(delegate
            {
                if (ComboDeviceInput.value == 0)
                    // Because the first element is "All input devices"
                    selectedInputDeviceId = null;
                else
                    // Get the ID (string) of the selected device
                    selectedInputDeviceId = MidiManager.Instance.InputDeviceIdSet.ElementAt(ComboDeviceInput.value - 1);
                Debug.Log($"Selected input device: {(selectedInputDeviceId ?? "All")}");
            });

            ButtonRefreshMidiDevices.onClick.AddListener(() =>
            {
                RefreshDevicesInp();
                RefreshDevicesOut();
            });

            ButtonScanBLEDevices.onClick.AddListener(() =>
            {
                if (isBluetoothScanning)
                {
                    ButtonScanBLEDevices.GetComponentInChildren<Text>().text = "Scan BLE MIDI devices";
                    ButtonScanBLEDevices.GetComponent<Image>().color = ColorWhite;
#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
                    Debug.Log("Stop Scan Bluetooth MidiDevices");
                    MidiManager.Instance.StopScanBluetoothMidiDevices();
#else
                    Debug.Log("Not implemented in editor and available only on Android");
#endif
                    isBluetoothScanning = false;
                }
                else
                {
                    ButtonScanBLEDevices.GetComponentInChildren<Text>().text = "Stop scan BLE MIDI devices";
                    ButtonScanBLEDevices.GetComponent<Image>().color = ColorSalmon;
#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
                    Debug.Log("Start Scan Bluetooth MidiDevices");
                    MidiManager.Instance.StartScanBluetoothMidiDevices(0);
#else
                    Debug.Log("Not implemented in editor and available only on Android");
#endif
                    isBluetoothScanning = true;
                }
            });

            ButtonActAsBLEMidiDevice.onClick.AddListener(() =>
            {
                if (isBluetoothAdvertising)
                {
                    ButtonActAsBLEMidiDevice.GetComponentInChildren<Text>().text = "Act as BLE MIDI device";
                    ButtonActAsBLEMidiDevice.GetComponent<Image>().color = ColorWhite;
#if UNITY_ANDROID && !UNITY_EDITOR
                    Debug.Log("Stop AdvertisingBluetoothMidiDevice");
                    MidiManager.Instance.StopAdvertisingBluetoothMidiDevice();
#else
                    Debug.Log("Not implemented in editor and available only on Android");
#endif
                    isBluetoothAdvertising = false;
                }
                else
                {
                    ButtonActAsBLEMidiDevice.GetComponentInChildren<Text>().text = "Stop act as BLE MIDI device";
                    ButtonActAsBLEMidiDevice.GetComponent<Image>().color = ColorSalmon;
#if UNITY_ANDROID && !UNITY_EDITOR
                    Debug.Log("Start AdvertisingBluetoothMidiDevice");
                    MidiManager.Instance.StartAdvertisingBluetoothMidiDevice();
#else
                    Debug.Log("Not implemented in editor and available only on Android");
#endif
                    isBluetoothAdvertising = true;
                }
            });

            ButtonSendPreset.onClick.AddListener(() =>
            {
                if (selectedOutputDeviceId != null)
                    MidiManager.Instance.SendMidiProgramChange(selectedOutputDeviceId, 0, InputChannel.Value, InputPreset.Value);
                else
                    Debug.Log("No output device selected");
            });

            ButtonSendNote.onClick.AddListener(() =>
            {
                if (selectedOutputDeviceId != null)
                {
                    Debug.Log($"Play on {selectedOutputDeviceId}, note {InputNote.Value} {HelperNoteLabel.LabelFromMidi(InputNote.Value)}");
                    MidiManager.Instance.SendMidiNoteOn(selectedOutputDeviceId, 0, InputChannel.Value, InputNote.Value, velocity: 100);
                    StartCoroutine(DelayedCall(delay: 1000, () =>
                    {
                        MidiManager.Instance.SendMidiNoteOff(selectedOutputDeviceId, 0, InputChannel.Value, InputNote.Value, velocity: 100);
                    }));
                }
                else
                {
                    Debug.Log($"Play on Maestro MPTK, note {InputNote.Value} {HelperNoteLabel.LabelFromMidi(InputNote.Value)}");
                    midiStreamPlayer.MPTK_PlayDirectEvent(new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOn,
                        Value = InputNote.Value,
                        Channel = InputChannel.Value,
                        Velocity = 100,
                        Delay = 0,
                        Duration = 1000 // play until note off is received
                    });
                }
            });

            ButtonPlayStopMidi.onClick.AddListener(() =>
            {
                if (midiFilePlayer.MPTK_IsPlaying)
                    midiFilePlayer.MPTK_Stop();
                else
                    midiFilePlayer.MPTK_Play();
                StartCoroutine(DelayedCall(delay: 100, SetUiButtonPlayMidi));
            });
            ButtonPlayPrevious.onClick.AddListener(() =>
            {
                midiFilePlayer.MPTK_Previous();
                StartCoroutine(DelayedCall(delay: 100, SetUiButtonPlayMidi));

            });
            ButtonPlayNext.onClick.AddListener(() =>
            {
                midiFilePlayer.MPTK_Next();
                StartCoroutine(DelayedCall(delay: 100, SetUiButtonPlayMidi));
            });

            SetUiButtonPlayMidi();

        }

        void SetUiButtonPlayMidi()
        {
            if (midiFilePlayer.MPTK_IsPlaying)
            {
                ButtonPlayStopMidi.GetComponent<Image>().color = ColorSalmon;
                ButtonPlayStopMidi.GetComponentInChildren<Text>().text = "Stop MIDI";
            }
            else
            {
                ButtonPlayStopMidi.GetComponent<Image>().color = ColorWhite;
                ButtonPlayStopMidi.GetComponentInChildren<Text>().text = "Play MIDI";
            }
        }
        private IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSeconds(delay / 1000f);
            Debug.Log($"Coroutine called after {delay} ms to stop the note, uodate the UI, ... or anything else");
            action?.Invoke();
        }

        private void RefreshDevicesInp()
        {
            ComboDeviceInput.ClearOptions();
            ComboDeviceInput.options.Add(new Dropdown.OptionData() { text = "0 - All input devices" });
            try
            {
                var deviceIds = MidiManager.Instance.InputDeviceIdSet.ToArray();
                Debug.Log("Device to receive MIDI: ");
                if (deviceIds.Length == 0)
                {
                    Debug.Log("   No input devices connected");
                }
                else
                {
                    for (var i = 0; i < deviceIds.Length; i++)
                    {
                        string device = $"{i + 1} -  {MidiManager.Instance.GetDeviceName(deviceIds[i])} - {deviceIds[i]}";
                        ComboDeviceInput.options.Add(new Dropdown.OptionData() { text = device });
                        Debug.Log($"   " + device);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in RefreshDevicesInp: {ex.Message}");
            }
            ComboDeviceInput.RefreshShownValue();
        }

        private void RefreshDevicesOut()
        {
            ComboDeviceOutput.ClearOptions();
            ComboDeviceOutput.options.Add(new Dropdown.OptionData() { text = "0 - Maestro MPTK" });
            try { 
            var deviceIds = MidiManager.Instance.OutputDeviceIdSet.ToArray();
            Debug.Log("Device to send MIDI: ");
            if (deviceIds.Length == 0)
            {
                Debug.Log("   No output devices connected");
            }
            else
            {
                for (var i = 0; i < deviceIds.Length; i++)
                {
                    string device = $"{i + 1} -  {MidiManager.Instance.GetDeviceName(deviceIds[i])} - {deviceIds[i]}";
                    ComboDeviceOutput.options.Add(new Dropdown.OptionData() { text = device });
                    Debug.Log($"   " + device);
                }
            }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in RefreshDevicesOut: {ex.Message}");
            }
            ComboDeviceOutput.RefreshShownValue();
        }

        // Mandatory to avoid crashes when application is closed (binary plugin could be not unloaded)
        private void OnApplicationQuit()
        {
            MidiManager.Instance.TerminateMidi();
        }

        private bool isBluetoothScanning = false;
        private bool isBluetoothAdvertising = false;

        public void MidiConnectionWindow()
        {
            // NOTE: To use Nearby Connections MIDI feature, follow these processes below.
            // Add a package with Unity Package Manager.
            // Select `Add package from git URL...` and input this URL.
            // ssh://git@github.com/kshoji/Nearby-Connections-for-Unity.git
            // or this URL
            // git+https://github.com/kshoji/Nearby-Connections-for-Unity
            //
            // Add a Scripting Define Symbol to Player settings
            // ENABLE_NEARBY_CONNECTIONS
#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || ((UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR)) && ENABLE_NEARBY_CONNECTIONS
                    GUILayout.Label("Nearby MIDI:");
                    if (isNearbyDiscovering)
                    {
                        if (GUILayout.Button("Stop discover Nearby MIDI devices"))
                        {
                            MidiManager.Instance.StopNearbyDiscovering();
                            isNearbyDiscovering = false;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Discover Nearby MIDI devices"))
                        {
                            MidiManager.Instance.StartNearbyDiscovering();
                            isNearbyDiscovering = true;
                        }
                    }
                    GUILayout.Space(20f);
                    if (isNearbyAdvertising)
                    {
                        if (GUILayout.Button("Stop advertise Nearby MIDI devices"))
                        {
                            MidiManager.Instance.StopNearbyAdvertising();
                            isNearbyAdvertising = false;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Advertise Nearby MIDI devices"))
                        {
                            MidiManager.Instance.StartNearbyAdvertising();
                            isNearbyAdvertising = true;
                        }
                    }
                    GUILayout.Space(20f);
#endif

#if !UNITY_IOS && !UNITY_WEBGL
            foreach (var netInterface in netInterfaces)
            {
                var properties = netInterface.GetIPProperties();
                foreach (var unicast in properties.UnicastAddresses)
                {
                    var address = unicast.Address;
                    if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal || address.IsIPv4MappedToIPv6 || address.IsIPv6Teredo)
                    {
                        continue;
                    }
                    if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        continue;
                    }
                    if (IPAddress.IsLoopback(address))
                    {
                        continue;
                    }
                    Debug.Log(address.ToString());
                }
            }
            int port = 5006;
            if (MidiManager.Instance.IsRtpMidiRunning(port))
                MidiManager.Instance.StopRtpMidi(port);
            //MidiManager.Instance.StartRtpMidiServer("RtpMidiSession", port);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("169.168.17.1"), port);
            Debug.Log($"RTP MIDI destination: {endPoint} ");
            MidiManager.Instance.ConnectToRtpMidiServer(sessionName: "RtpMidiSession", listenPort: 5006, ipEndPoint: endPoint);
#endif
        }

        public void OnMidiNoteOn(string deviceId, int group, int channel, int note, int velocity)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
            {
                Debug.Log($"OnMidiNoteOn from: {deviceId}, channel: {channel}, note: {note}, velocity: {velocity}");
                MPTKEvent midiStreamEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn,
                    Value = note,
                    Channel = channel,
                    Velocity = velocity,
                    Delay = 0,
                    Duration = -1 // play until note off is received
                };
                midiStreamPlayer.MPTK_PlayEvent(midiStreamEvent);
            }
        }

        public void OnMidiNoteOff(string deviceId, int group, int channel, int note, int velocity)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
            {
                Debug.Log($"OnMidiNoteOff from: {deviceId}, channel: {channel}, note: {note}, velocity: {velocity}");
                MPTKEvent midiStreamEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOff,
                    Value = note,
                    Channel = channel,
                    Velocity = velocity,
                    Delay = 0,
                };
                midiStreamPlayer.MPTK_PlayEvent(midiStreamEvent);
            }
        }
        public void OnMidiContinue(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiContinue from: {deviceId}");
        }

        public void OnMidiReset(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiReset from: {deviceId}");
        }

        public void OnMidiStart(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiStart from: {deviceId}");
        }

        public void OnMidiStop(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiStop from: {deviceId}");
        }

        public void OnMidiActiveSensing(string deviceId, int group)
        {
            //if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
            // too many events received, so commented out
            // Debug.Log("OnMidiActiveSensing");
        }

        public void OnMidiCableEvents(string deviceId, int group, int byte1, int byte2, int byte3)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiCableEvents from: {deviceId}, byte1: {byte1}, byte2: {byte2}, byte3: {byte3}");
        }

        public void OnMidiChannelAftertouch(string deviceId, int group, int channel, int pressure)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiChannelAftertouch from: {deviceId}, channel: {channel}, pressure: {pressure}");
        }

        public void OnMidiPitchWheel(string deviceId, int group, int channel, int amount)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiPitchWheel from: {deviceId}, channel: {channel}, amount: {amount}");
        }

        public void OnMidiPolyphonicAftertouch(string deviceId, int group, int channel, int note, int pressure)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiPolyphonicAftertouch from: {deviceId}, channel: {channel}, note: {note}, pressure: {pressure}");
        }

        public void OnMidiProgramChange(string deviceId, int group, int channel, int program)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiProgramChange from: {deviceId}, channel: {channel}, program: {program}");
        }

        public void OnMidiControlChange(string deviceId, int group, int channel, int function, int value)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiControlChange from: {deviceId}, channel: {channel}, function: {function}, value: {value}");
        }

        public void OnMidiSongSelect(string deviceId, int group, int song)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiSongSelect from: {deviceId}, song: {song}");
        }

        public void OnMidiSongPositionPointer(string deviceId, int group, int position)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiSongPositionPointer from: {deviceId}, song: {position}");
        }

        public void OnMidiSingleByte(string deviceId, int group, int byte1)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiSingleByte from: {deviceId}, byte1: {byte1}");
        }

        public void OnMidiSystemExclusive(string deviceId, int group, byte[] systemExclusive)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiSystemExclusive from: {deviceId}, systemExclusive: {BitConverter.ToString(systemExclusive).Replace("-", " ")}");
        }

        public void OnMidiSystemCommonMessage(string deviceId, int group, byte[] message)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiSystemCommonMessage from: {deviceId}, message: {BitConverter.ToString(message).Replace("-", " ")}");
        }

        public void OnMidiTimeCodeQuarterFrame(string deviceId, int group, int timing)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiTimeCodeQuarterFrame from: {deviceId}, timing: {timing}");
        }

        public void OnMidiTimingClock(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiTimingClock from: {deviceId}");
        }

        public void OnMidiTuneRequest(string deviceId, int group)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiTuneRequest from: {deviceId}");
        }

        public void OnMidiMiscellaneousFunctionCodes(string deviceId, int group, int byte1, int byte2, int byte3)
        {
            if (selectedInputDeviceId == null || selectedInputDeviceId == deviceId)
                Debug.Log($"OnMidiMiscellaneousFunctionCodes from: {deviceId}, byte1: {byte1}, byte2: {byte2}, byte3: {byte3}");
        }

        // ---------------------------------
        // Log attached and detached devices
        // ---------------------------------
        public void OnMidiInputDeviceAttached(string deviceId)
        {
            Debug.Log($"MIDI Input device attached. deviceId: {deviceId}, name: {MidiManager.Instance.GetDeviceName(deviceId)}, vendor: {MidiManager.Instance.GetVendorId(deviceId)}, product: {MidiManager.Instance.GetProductId(deviceId)}");
            RefreshDevicesInp();
        }

        public void OnMidiOutputDeviceAttached(string deviceId)
        {
            Debug.Log($"MIDI Output device attached. deviceId: {deviceId}, name: {MidiManager.Instance.GetDeviceName(deviceId)}, vendor: {MidiManager.Instance.GetVendorId(deviceId)}, product: {MidiManager.Instance.GetProductId(deviceId)}");
            RefreshDevicesOut();
        }

        public void OnMidiInputDeviceDetached(string deviceId)
        {
            Debug.Log($"MIDI Input device detached. deviceId: {deviceId}");
        }

        public void OnMidiOutputDeviceDetached(string deviceId)
        {
            Debug.Log($"MIDI Output device detached. deviceId: {deviceId}");
        }

        // Set from the UI
        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}
#endif