using UnityEngine;
using UnityEngine.InputSystem;

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
        public bool useF6ForTeleport = true;
        public Vector2 GPLeftStick;
        public Vector2 GPRightStick;

        void Start()
        {
            instance = this;
        }
        void Update()
        {
            LeftGrip = ControllerInputPoller.instance.leftGrab;
            RightGrip = ControllerInputPoller.instance.rightGrab;
            LeftPrimaryButton = ControllerInputPoller.instance.leftControllerPrimaryButton;
            RightPrimaryButton = ControllerInputPoller.instance.rightControllerPrimaryButton;
            LeftTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f;
            RightTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f;
            TeleportCamera = useF6ForTeleport
                ? Keyboard.current.f6Key.isPressed
                : ControllerInputPoller.instance.leftControllerPrimaryButton;

            if (Gamepad.current != null)
            {
                GPLeftStick = Gamepad.current.leftStick.ReadValue();
                GPRightStick = Gamepad.current.rightStick.ReadValue();
            }
        }
    }
}
