using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace YizziCamModV2.Comps
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager instance;
        public bool LeftGrip;
        public bool RightGrip;
        public bool LeftTrigger;
        public bool RightTrigger;
        public bool LeftPrimaryButton;
        public bool RightPrimaryButton;
        public bool TeleportCamera;
        /// <summary>0 = F6, 1 = left X/Y, 2 = custom binding.</summary>
        public int summonInputMode;
        public Vector2 GPLeftStick;
        public Vector2 GPRightStick;

        public bool waitingForCustomBind;
        /// <summary>Index into CustomBindNames / polling logic.</summary>
        public int customBindIndex = -1;

        public static readonly string[] CustomBindNames =
        {
            "A BUTTON",       // 0: right primary
            "B BUTTON",       // 1: right secondary
            "X BUTTON",       // 2: left primary
            "Y BUTTON",       // 3: left secondary
            "L GRIP",         // 4
            "R GRIP",         // 5
            "L TRIGGER",      // 6
            "R TRIGGER",      // 7
            "L JOYSTICK",     // 8: left stick click
            "R JOYSTICK",     // 9: right stick click
        };

        static readonly List<UnityEngine.XR.InputDevice> _xrDevices = new List<UnityEngine.XR.InputDevice>();
        /// <summary>Gorilla Tag resolves these once per frame — not the same as GetDevicesAtXRNode (OpenXR/Steam).</summary>
        static FieldInfo _fLeftCtrlDev;
        static FieldInfo _fRightCtrlDev;

        static UnityEngine.XR.InputDevice GetGorillaPairedDevice(bool leftHand)
        {
            _fLeftCtrlDev ??= typeof(ControllerInputPoller).GetField("leftControllerDevice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _fRightCtrlDev ??= typeof(ControllerInputPoller).GetField("rightControllerDevice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var poller = ControllerInputPoller.instance;
            if (poller == null || _fLeftCtrlDev == null || _fRightCtrlDev == null)
                return default;
            object raw = leftHand ? _fLeftCtrlDev.GetValue(poller) : _fRightCtrlDev.GetValue(poller);
            return raw is UnityEngine.XR.InputDevice d ? d : default;
        }

        /// <returns>True if this device reports a thumbstick/zones click (whatever the XR runtime exposes).</returns>
        static bool TryStickClickOnDevice(UnityEngine.XR.InputDevice dev)
        {
            if (!dev.isValid) return false;
            // Standard Unity mappings
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool pc) && pc) return true;
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondary2DAxisClick, out bool sc) && sc) return true;

            var boolUsages = new List<InputFeatureUsage>();
            if (!dev.TryGetFeatureUsages(boolUsages)) return false;

            foreach (var u in boolUsages)
            {
                if (u.type != typeof(bool)) continue;
                var n = u.name;
                if (n.IndexOf("2DAxisClick", System.StringComparison.OrdinalIgnoreCase) < 0
                    && n.IndexOf("AxisClick", System.StringComparison.OrdinalIgnoreCase) < 0
                    && n.IndexOf("thumbstick", System.StringComparison.OrdinalIgnoreCase) < 0
                    && n.IndexOf("joystick", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                try
                {
                    if (dev.TryGetFeatureValue(new InputFeatureUsage<bool>(n), out bool v) && v)
                        return true;
                }
                catch { /* ignore malformed usage reads */ }
            }

            return false;
        }

        void Start()
        {
            instance = this;
            customBindIndex = PlayerPrefs.GetInt("YizziCustomBind", -1);
        }

        void Update()
        {
            LeftGrip = ControllerInputPoller.instance.leftGrab;
            RightGrip = ControllerInputPoller.instance.rightGrab;
            LeftPrimaryButton = ControllerInputPoller.instance.leftControllerPrimaryButton;
            RightPrimaryButton = ControllerInputPoller.instance.rightControllerPrimaryButton;
            LeftTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f;
            RightTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f;

            if (waitingForCustomBind)
            {
                TeleportCamera = false;
                ListenForCustomBind();
                return;
            }

            TeleportCamera = summonInputMode == 0
                ? Keyboard.current != null && Keyboard.current.f6Key.isPressed
                : summonInputMode == 1
                    ? ControllerInputPoller.instance.leftControllerPrimaryButton
                    : CheckCustomBind();

            if (Gamepad.current != null)
            {
                GPLeftStick = Gamepad.current.leftStick.ReadValue();
                GPRightStick = Gamepad.current.rightStick.ReadValue();
            }
        }

        bool ReadButton(int idx)
        {
            switch (idx)
            {
                case 0: return ControllerInputPoller.instance.rightControllerPrimaryButton;
                case 1: return ControllerInputPoller.instance.rightControllerSecondaryButton;
                case 2: return ControllerInputPoller.instance.leftControllerPrimaryButton;
                case 3: return ControllerInputPoller.instance.leftControllerSecondaryButton;
                case 4: return ControllerInputPoller.instance.leftGrab;
                case 5: return ControllerInputPoller.instance.rightGrab;
                case 6: return ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f;
                case 7: return ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f;
                case 8: return ReadStickClick(true);
                case 9: return ReadStickClick(false);
                default: return false;
            }
        }

        static bool ReadStickClick(bool leftHand)
        {
            // Same InputDevices GT uses — GetDevicesAtXRNode often misses stick click on PC OpenXR
            if (TryStickClickOnDevice(GetGorillaPairedDevice(leftHand)))
                return true;

            _xrDevices.Clear();
            InputDevices.GetDevicesAtXRNode(leftHand ? XRNode.LeftHand : XRNode.RightHand, _xrDevices);
            foreach (var dev in _xrDevices)
            {
                if (TryStickClickOnDevice(dev))
                    return true;
            }

            try
            {
                var ctrl = leftHand
                    ? UnityEngine.InputSystem.XR.XRController.leftHand
                    : UnityEngine.InputSystem.XR.XRController.rightHand;
                if (ctrl != null)
                {
                    foreach (var name in new[] { "thumbstickClicked", "primary2DAxisClick", "secondary2DAxisClick" })
                    {
                        var click = ctrl.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(name);
                        if (click != null && click.isPressed) return true;
                    }
                }
            }
            catch { }

            return false;
        }
        void ListenForCustomBind()
        {
            // Check regular buttons first (0-7), then joystick clicks (8-9) last
            int[] order = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            foreach (int i in order)
            {
                if (ReadButton(i))
                {
                    customBindIndex = i;
                    PlayerPrefs.SetInt("YizziCustomBind", i);
                    PlayerPrefs.Save();
                    waitingForCustomBind = false;

                    if (CameraController.Instance != null && CameraController.Instance.GenSummonText != null)
                        CameraController.Instance.GenSummonText.text = "KEY:" + CustomBindNames[i];
                    return;
                }
            }
        }

        bool CheckCustomBind()
        {
            if (customBindIndex < 0 || customBindIndex >= CustomBindNames.Length) return false;
            return ReadButton(customBindIndex);
        }

        public string GetCustomBindLabel()
        {
            if (waitingForCustomBind) return "KEY:PRESS ANY...";
            if (customBindIndex >= 0 && customBindIndex < CustomBindNames.Length)
                return "KEY:" + CustomBindNames[customBindIndex];
            return "KEY:CUSTOM";
        }
    }
}
