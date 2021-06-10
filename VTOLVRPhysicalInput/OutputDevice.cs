using System;
using System.Collections.Generic;

namespace VTOLVRPhysicalInput
{
    public class OutputDevice
    {
        private string _name;

        // Latest state of each axis
        //private readonly Dictionary<string, float> _axisStates = new Dictionary<string, float>();
        private readonly Dictionary<
            /* Set Name */ string,
            /* State of axes in sets */ Dictionary<
                /* Axis Name */ string,
                /* Axis State */ float>> _axisSetStates
                    = new Dictionary<string, Dictionary<string, float>>();

        // Has a given set changed since last update?
        private readonly Dictionary<string, bool> _axisSetChanged 
            = new Dictionary<string, bool>();

        // Functions to call on SendUpdates for each set of Axes
        private readonly Dictionary</* Set Name */string, Action<Dictionary<string, float>>> _axisSetDelegates 
            = new Dictionary<string, Action<Dictionary<string, float>>>();

        private Dictionary<string, float> _touchpadState
            = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase){{"X", 0}, {"Y", 0}, {"Z", 0}};
        private bool _touchpadPressed;
        private bool _touchpadLastPressed;

        // Function to call on SendUpdates for each set of Touchpad
        private Action<Dictionary<string, float>, bool> _touchpadDelegate;

        private readonly Dictionary<string, bool> _buttonStates
            = new Dictionary<string, bool>();

        private readonly Dictionary<string, bool> _buttonChanged
            = new Dictionary<string, bool>();

        // Functions to call on SendUpdates for each Button
        private readonly Dictionary<string, Action<bool>> _buttonDelegates
            = new Dictionary<string, Action<bool>>();

        public OutputDevice(string name)
        {
            _name = name;
        }

        public OutputDevice AddAxisSet(string setName, List<string> axisNames)
        {
            _axisSetStates.Add(setName, new Dictionary<string, float>());
            //_axisSetChanged.Add(setName, false);
            _axisSetChanged.Add(setName, true); // Enable Axis updates every frame for now
            foreach (var axisName in axisNames)
            {
                _axisSetStates[setName].Add(axisName, 0);
            }

            return this;
        }

        public OutputDevice AddAxisSetDelegate(string setName, Action<Dictionary<string, float>> setDelegate)
        {
            _axisSetDelegates.Add(setName, setDelegate);
            return this;
        }

        public OutputDevice AddTouchpadDelegate(Action<Dictionary<string, float>, bool> tpDelegate)
        {
            _touchpadDelegate = tpDelegate;
            return this;
        }

        public OutputDevice AddButton(string name)
        {
            _buttonStates.Add(name, false);
            _buttonChanged.Add(name, false);
            return this;
        }

        public OutputDevice AddButtonDelegate(string name, Action<bool> buttonDelegate)
        {
            _buttonDelegates.Add(name, buttonDelegate);
            return this;
        }

        public void SetAxis(string axisName, string setName, float value)
        {
            var setStates = _axisSetStates[setName];
            setStates[axisName] = value;
            //_axisSetChanged[setName] = true;
        }

        public void SetButton(string buttonName, bool value)
        {
            _buttonStates[buttonName] = value;
            _buttonChanged[buttonName] = true;
        }

        public void SetTouchpad(float x, float y)
        {
            _touchpadState["X"] = x;
            _touchpadState["Y"] = y;
            _touchpadPressed = (Math.Abs(x) > 0 || Math.Abs(y) > 0);
        }

        public void SendUpdates()
        {
            foreach (var setDelegate in _axisSetDelegates)
            {
                var setName = setDelegate.Key;
                if (!_axisSetChanged[setName]) continue;
                var setStates = _axisSetStates[setName];
                setDelegate.Value(setStates);
                //_axisSetChanged[setName] = false;
            }

            foreach (var buttonDelegate in _buttonDelegates)
            {
                if (!_buttonChanged[buttonDelegate.Key]) continue;
                buttonDelegate.Value(_buttonStates[buttonDelegate.Key]);
                _buttonChanged[buttonDelegate.Key] = false;
            }

            // If Touchpad went from Pressed to Unpressed, or Touchpad is pressed, send TP update
            if (_touchpadDelegate != null)
            {
                if (_touchpadPressed || (!_touchpadPressed && _touchpadLastPressed))
                {
                    _touchpadDelegate(_touchpadState, _touchpadPressed);
                }
                _touchpadLastPressed = _touchpadPressed;
            }
        }

        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine($"PhysicalStickMod| Device: {_name} | {text}");
        }

    }
}
