using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;

namespace VTOLVRPhysicalInput
{
    public class VtolVrPhysicalInput : VTOLMOD
    {
        public static DirectInput DiInstance = new DirectInput();
        private VRJoystick _vrJoystick;
        private VRThrottle _vrThrottle;
        private bool _waitingForVrJoystick;
        private bool _pollingEnabled;

        private readonly Dictionary<string, bool> _deviceMapped = new Dictionary<string, bool>() { { "Stick", false }, { "Throttle", false } };

        private readonly MappingsDictionary _stickMappings = new MappingsDictionary();

        private OutputDevice _outputStick;
        private OutputDevice _outputThrottle;
        private Dictionary<string, OutputDevice> _outputDevices;

        private static readonly Dictionary<int, Vector3> PovAngleToVector3 = new Dictionary<int, Vector3>
        {
            { -1, new Vector3(0, 0, 0) },       //Center 
            { 0, new Vector3(0, 1, 0) },        // Up
            { 4500, new Vector3(1, 1, 0) },     // Up Right
            { 9000, new Vector3(1, 0, 0) },     // Right
            { 13500, new Vector3(1, -1, 0) },   // Down Right
            { 18000, new Vector3(0, -1, 0) },   // Down
            { 22500, new Vector3(-1, -1, 0) },  // Down Left
            { 27000, new Vector3(-1, 0, 0) },   // Left
            { 31500, new Vector3(-1, 1, 0) }    // Up Left
        };

        /// <summary>
        /// When running in game, called at start
        /// </summary>
        public void Start()
        {
            DontDestroyOnLoad(this.gameObject); // Required, else mod stops working when you leave opening scene and enter ready room
            base.ModLoaded();
            InitSticks();
            InitUpdates();
            HarmonyInstance instance = HarmonyInstance.Create("me.mymod");
            instance.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void InitSticks(bool standaloneTesting = false)
        {
            Log("DBGVIEWCLEAR");
            Log("VTOL VR Mod loaded");
            ProcessSettingsFile(standaloneTesting);
            var diDeviceInstances = DiInstance.GetDevices();

            var foundSticks = new Dictionary<string, Joystick>();

            foreach (var device in diDeviceInstances)
            {
                if (!IsStickType(device)) continue;
                var foundStick = new Joystick(DiInstance, device.InstanceGuid);
                if (foundSticks.ContainsKey(foundStick.Information.ProductName)) continue; // ToDo: Handle duplicate stick names?
                foundSticks.Add(foundStick.Information.ProductName, foundStick);
            }

            foreach (var polledStick in _stickMappings.Sticks)
            {
                if (!foundSticks.ContainsKey(polledStick.Key))
                {
                    ThrowError($"Joystick {polledStick.Value.Stick} not found");
                }
                Log($"Joystick {polledStick.Key} found");
                polledStick.Value.Stick = foundSticks[polledStick.Key];
                polledStick.Value.Stick.Properties.BufferSize = 128;
                polledStick.Value.Stick.Acquire();
            }

            foreach (var device in _deviceMapped)
            {
                Log($"Output Device {device.Key} mapped = {device.Value}");
            }

        }

        public void InitUpdates(bool standaloneTesting = false)
        {
            // Stick Output
            _outputStick = new OutputDevice("Stick");
            _outputStick
                .AddAxisSet("StickXyz", new List<string> { "Roll", "Pitch", "Yaw" })
                .AddAxisSet("Touchpad", new List<string> { "X", "Y", "Z" })
                //.AddAxisSet("ThrottleTouchpad", new List<string> { "X", "Y", "Z" }) // comment out for throttle touchpad instead of stick
                .AddAxisSet("TriggerAxis", new List<string> { "TriggerAxis" })
                .AddButton("Menu")
                .AddButton("Trigger")
                .AddButton("Thumbstick");

            if (standaloneTesting)
            {
                _outputStick
                    .AddAxisSetDelegate("StickXyz", TestingUpdateStickXyz)
                    .AddTouchpadDelegate(TestingUpdateStickTouchpad)
                    .AddButtonDelegate("Menu", TestingUpdateStickMenuButton)
                    .AddButtonDelegate("Trigger", TestingUpdateStickTriggerButton)
                    .AddButtonDelegate("Thumbstick", TestUpdatestickTouchpadPressButton)
                    //.AddAxisSetDelegate("ThrottleTouchpad", TestingUpdateThrottleTouchpad) // comment out for throttle touchpad instead of stick
                    .AddAxisSetDelegate("TriggerAxis", TestingUpdateStickTriggerAxis);

            }
            else
            {
                _outputStick
                    .AddAxisSetDelegate("StickXyz", UpdateStickXyz)
                    .AddTouchpadDelegate(UpdateStickTouchpad)
                    .AddButtonDelegate("Menu", UpdateStickMenuButton)
                    .AddButtonDelegate("Trigger", UpdateStickTriggerButton)
                    .AddButtonDelegate("Thumbstick", UpdateStickTouchpadPressButton)
                    //.AddAxisSetDelegate("ThrottleTouchpad", UpdateThrottleTouchpad) // comment out for throttle touchpad instead of stick
                    .AddAxisSetDelegate("TriggerAxis", UpdateStickTriggerAxis);
            }

            // Throttle Output
            _outputThrottle = new OutputDevice("Throttle");
            _outputThrottle
                .AddAxisSet("Throttle", new List<string> { "Throttle" })
                .AddAxisSet("ThrottleTouchpad", new List<string> { "X", "Y", "Z" }) // uncomment for throttle touchpad instead of on stick
                .AddAxisSet("Trigger", new List<string> { "Trigger" })
                .AddButton("Menu");


            if (standaloneTesting)
            {
                _outputThrottle
                    .AddAxisSetDelegate("Throttle", TestingUpdateThrottlePower)
                    .AddAxisSetDelegate("ThrottleTouchpad", TestingUpdateThrottleTouchpad)  // uncomment for throttle touchpad instead of on stick
                    .AddButtonDelegate("Menu", TestingUpdateThrottleMenuButton)
                    .AddAxisSetDelegate("Trigger", TestingUpdateThrottleTriggerAxis);
            }
            else
            {
                _outputThrottle
                    .AddAxisSetDelegate("Throttle", UpdateThrottlePower)
                    .AddAxisSetDelegate("ThrottleTouchpad", UpdateThrottleTouchpad)  // uncomment for throttle touchpad instead of on stick
                    .AddButtonDelegate("Menu", UpdateThrottleMenuButton)
                    .AddAxisSetDelegate("Trigger", UpdateThrottleTriggerAxis);
            }

            _outputDevices = new Dictionary<string, OutputDevice> { { "Stick", _outputStick }, { "Throttle", _outputThrottle } };
        }

        #region Delegates
        #region Stick Delegates
        private void UpdateStickXyz(Dictionary<string, float> values)
        {
            _vrJoystick.OnSetStick.Invoke(new Vector3(values["Pitch"], values["Yaw"], values["Roll"]));

        }

        private void TestingUpdateStickXyz(Dictionary<string, float> values)
        {
            Log($"Update StickXyz: {values.ToDebugString()}");

        }

        private void UpdateStickTouchpad(Dictionary<string, float> values, bool pressed)
        {
            if (pressed)
            {
                _vrJoystick.OnSetThumbstick.Invoke(new Vector3(values["X"], values["Y"], values["Z"]));
            }
            else
            {
                // Upon release, Reset Thumbstick so TGP acquires target
                _vrJoystick.OnResetThumbstick.Invoke();
            }
        }

        private void TestingUpdateStickTouchpad(Dictionary<string, float> values, bool pressed)
        {
            Log($"Update StickTouchpad: {values.ToDebugString()}, pressed: {pressed}");
        }

        private void UpdateStickMenuButton(bool value)
        {
            if (value) _vrJoystick.OnMenuButtonDown.Invoke();
            else _vrJoystick.OnMenuButtonUp.Invoke();
        }

        private void TestingUpdateStickMenuButton(bool value)
        {
            Log($"Update StickMenuButton: {value}");
        }

        private void UpdateStickTriggerButton(bool value)
        {
            // use single fire if an AMRAMM
            if (VTOLAPI.GetPlayersVehicleGameObject().GetComponent<WeaponManager>().currentEquip is HPEquipRadarML && value)
                VTOLAPI.GetPlayersVehicleGameObject().GetComponent<WeaponManager>().SingleFire();
        
            else
            {
                if (value) _vrJoystick.OnTriggerDown.Invoke();
                else _vrJoystick.OnTriggerUp.Invoke();
            }


        }
        private void UpdateStickTriggerAxis(Dictionary<string, float> values)
        {
            _vrJoystick.OnTriggerAxis.Invoke(values["TriggerAxis"]);
        }

        private void TestingUpdateStickTriggerButton(bool value)
        {
            Console.WriteLine($"Update StickTriggerButton: {value}");
        }

        private void TestingUpdateStickTriggerAxis(Dictionary<string, float> values)
        {
            Console.WriteLine($"Update StickTriggerAxis: {values.ToDebugString()}");
        }

        private void UpdateStickTouchpadPressButton(bool value)
        {
            if (value) _vrJoystick.OnThumbstickButtonDown.Invoke();
            else _vrJoystick.OnThumbstickButtonUp.Invoke();
        }

        private void TestUpdatestickTouchpadPressButton(bool value)
        {
            Console.WriteLine($"Update ThumbstickTouchpadButton: {value}");
        }

        #endregion

        #region Throttle Delegates
        private void UpdateThrottlePower(Dictionary<string, float> values)
        {
            _vrThrottle.OnSetThrottle.Invoke(values["Throttle"]);

        }

        private void TestingUpdateThrottlePower(Dictionary<string, float> values)
        {
            Log($"Update ThrottlePower: {values.ToDebugString()}");
        }

        private void UpdateThrottleTouchpad(Dictionary<string, float> values)
        {
            _vrThrottle.OnSetThumbstick.Invoke(new Vector3(values["X"], values["Y"], values["Z"]));
        }

        private void UpdateThrottleMenuButton(bool value)
        {
            if (value) _vrThrottle.OnMenuButtonDown.Invoke();
            else _vrThrottle.OnMenuButtonUp.Invoke();
        }

        private void TestingUpdateThrottleMenuButton(bool value)
        {
            Log($"Update ThrottleMenuButton: {value}");
        }

        private void TestingUpdateThrottleTouchpad(Dictionary<string, float> values)
        {
            Log($"Update ThrottleTouchpad: {values.ToDebugString()}");
        }

        private void UpdateThrottleTriggerAxis(Dictionary<string, float> values)
        {
            _vrThrottle.OnTriggerAxis.Invoke(values["Trigger"]);
        }

        private void TestingUpdateThrottleTriggerAxis(Dictionary<string, float> values)
        {
            Log($"Update ThrottleTriggerAxis: {values.ToDebugString()}");
        }

        #endregion
        #endregion

        /// <summary>
        /// Called by Unity each frame
        /// </summary>
        public void Update()
        {
            if (_pollingEnabled)
            {
                if (!VrControlsAvailable() || !InCockpit())
                {
                    _pollingEnabled = false;
                    _waitingForVrJoystick = false;
                    _vrJoystick = null;
                    _vrThrottle = null;
                    Log("Left cockpit");
                }
            }
            else
            {
                if (!VrControlsAvailable() && InCockpit() && !_waitingForVrJoystick)
                {
                    // Entered cockpit
                    Log("Entered cockpit");
                    _waitingForVrJoystick = true;
                    StartCoroutine(FindScripts());
                    return;
                }
            }

            // Take state from physical stick and apply to VRJoystick
            PollSticks();

            if (!VrControlsAvailable()) return;

            SendUpdates();
        }

        /// <summary>
        /// When playing game, called by Update when we are in the cockpit
        /// When testing via console app, called directly
        /// </summary>
        public void SendUpdates()
        {
            if (_deviceMapped["Stick"])
            {
                _outputStick.SendUpdates();
            }

            if (_deviceMapped["Throttle"])
            {
                _outputThrottle.SendUpdates();
            }
        }

        /// <summary>
        /// When playing game, called by Update when we are in the cockpit
        /// When testing via console app, called directly
        /// </summary>
        public void PollSticks()
        {

            foreach (var mappedStick in _stickMappings.Sticks.Values)
            {
                var data = mappedStick.Stick.GetBufferedData();
                foreach (var state in data)
                {
                    var ov = (int)state.Offset;
                    if (ov <= 28)
                    {

                        // Axes
                        if (mappedStick.AxisToVectorComponentMappings.TryGetValue(state.Offset, out var vectorComponentMapping))
                        {
                            var device = _outputDevices[vectorComponentMapping.OutputDevice];
                            device.SetAxis(
                                vectorComponentMapping.OutputComponent,
                                vectorComponentMapping.OutputSet,
                                ConvertAxisValue(state.Value, vectorComponentMapping.Invert, vectorComponentMapping.MappingRange));



                        }
                    }
                    else if (ov <= 44)
                    {
                        // POV Hats
                        if (mappedStick.PovToTouchpadMappings.TryGetValue(state.Offset, out var touchpadMapping))
                        {
                            Console.WriteLine(($"PovToTouchpad: POV={state.Offset}, Value={state.Value}, OutputDevice={touchpadMapping.OutputDevice}"));
                            var vec = PovAngleToVector3[state.Value];
                            var device = _outputDevices[touchpadMapping.OutputDevice];
                            device.SetTouchpad(vec.x, vec.y);
                        }
                    }
                    else if (ov <= 175)
                    {
                        // Buttons
                        if (mappedStick.ButtonToButtonMappings.TryGetValue(state.Offset, out var buttonToButtonMapping))
                        {
                            Console.WriteLine(($"ButtonToButton: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToButtonMapping.OutputDevice}, OutputButton={buttonToButtonMapping.OutputButton}"));
                            var device = _outputDevices[buttonToButtonMapping.OutputDevice];
                            device.SetButton(buttonToButtonMapping.OutputButton, state.Value == 128);
                        }

                        if (mappedStick.ButtonToVectorComponentMappings.TryGetValue(state.Offset, out var buttonToVectorMapping))
                        {
                            Console.WriteLine(($"ButtonToVector: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToVectorMapping.OutputDevice}, Component={buttonToVectorMapping.OutputComponent}"));
                            var device = _outputDevices[buttonToVectorMapping.OutputDevice];
                            device.SetAxis(
                                buttonToVectorMapping.OutputComponent,
                                buttonToVectorMapping.OutputSet,
                                state.Value == 128 ? buttonToVectorMapping.PressValue : buttonToVectorMapping.ReleaseValue);
                        }

                    }
                }
            }
        }

        private IEnumerator FindScripts()
        {
            while (_vrJoystick == null)
            {
                //Searches the whole game scene for the script, there should only be one so its fine
                _vrJoystick = FindObjectOfType<VRJoystick>();
                _vrThrottle = FindObjectOfType<VRThrottle>();
                if (VrControlsAvailable()) continue;
                Log("Waiting for VRJoystick...");
                yield return new WaitForSeconds(1);
            }
            _waitingForVrJoystick = false;
            _pollingEnabled = true;
            Log("Got VRJoystick");
        }

        private static float ConvertAxisValue(int value, bool invert, string mappingRange = "Full")
        {
            float retVal;
            if (value == 65535) retVal = 1;
            else retVal = (((float)value / 32767) - 1);
            if (invert) retVal *= -1;
            if (mappingRange == "High")
            {
                retVal /= 2;
                retVal += 0.5f;
            }
            else if (mappingRange == "Low")
            {
                retVal /= 2;
                retVal -= 0.5f;
            }
            return retVal;
        }

        private static bool InCockpit()
        {
            return SceneManager.GetActiveScene().buildIndex == 7 || SceneManager.GetActiveScene().buildIndex == 11;
        }

        private bool VrControlsAvailable()
        {
            return _vrJoystick != null && _vrThrottle != null;
        }

        public static bool IsStickType(DeviceInstance deviceInstance)
        {
            return deviceInstance.Type == SharpDX.DirectInput.DeviceType.Joystick
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Gamepad
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.FirstPerson
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Flight
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Driving
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Supplemental;
        }

        public void ProcessSettingsFile(bool standaloneTesting = false)
        {
            string settingsFile;
            if (standaloneTesting)
            {
                settingsFile = Path.Combine(Directory.GetCurrentDirectory(), @"VTOLVRPhysicalInputSettings.xml");
            }
            else
            {
                settingsFile = Path.Combine(Directory.GetCurrentDirectory(), @"VTOLVR_ModLoader\Mods\VTOLVRPhysicalInput\VTOLVRPhysicalInputSettings.xml");
            }

            if (File.Exists(settingsFile))
            {
                Log($"Loading Settings file from {settingsFile}");
                var deserializer = new XmlSerializer(typeof(Mappings));
                TextReader reader = new StreamReader(settingsFile);
                var obj = deserializer.Deserialize(reader);
                var stickMappings = (Mappings)obj;
                reader.Close();

                // Build Dictionary
                // ToDo: How to do this as part of XML Deserialization?
                foreach (var stick in stickMappings.MappingsList)
                {
                    if (!_stickMappings.Sticks.ContainsKey(stick.StickName))
                    {
                        _stickMappings.Sticks.Add(stick.StickName, new StickMappings() { StickName = stick.StickName });
                    }

                    var mapping = _stickMappings.Sticks[stick.StickName];

                    foreach (var axisToVectorComponentMapping in stick.AxisToVectorComponentMappings)
                    {
                        mapping.AxisToVectorComponentMappings.Add(JoystickOffsetFromName(axisToVectorComponentMapping.InputAxis), axisToVectorComponentMapping);
                        _deviceMapped[axisToVectorComponentMapping.OutputDevice] = true;
                    }

                    foreach (var buttonToVectorComponentMapping in stick.ButtonToVectorComponentMappings)
                    {
                        mapping.ButtonToVectorComponentMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToVectorComponentMapping.InputButton)), buttonToVectorComponentMapping);
                        _deviceMapped[buttonToVectorComponentMapping.OutputDevice] = true;
                    }

                    foreach (var buttonToButtonMapping in stick.ButtonToButtonMappings)
                    {
                        mapping.ButtonToButtonMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToButtonMapping.InputButton)), buttonToButtonMapping);
                        _deviceMapped[buttonToButtonMapping.OutputDevice] = true;
                    }

                    foreach (var povToTouchpadMapping in stick.PovToTouchpadMappings)
                    {
                        mapping.PovToTouchpadMappings.Add(JoystickOffsetFromName(PovNameFromIndex(povToTouchpadMapping.InputPov)), povToTouchpadMapping);
                        _deviceMapped[povToTouchpadMapping.OutputDevice] = true;
                    }
                }
            }
            else
            {
                Log($"{settingsFile} not found");
                throw new Exception($"{settingsFile} not found");
            }
        }

        private string ButtonNameFromIndex(int index)
        {
            return "Buttons" + (index - 1);
        }

        private string PovNameFromIndex(int index)
        {
            return "PointOfViewControllers" + (index - 1);
        }

        private JoystickOffset JoystickOffsetFromName(string n)
        {
            return (JoystickOffset)Enum.Parse(typeof(JoystickOffset), n);
        }

        private void ThrowError(string text)
        {
            Log(text);
            throw new Exception(text);
        }

        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine($"PhysicalStickMod| {text}");
        }
    }
}
