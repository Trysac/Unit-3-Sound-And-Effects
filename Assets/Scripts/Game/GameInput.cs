using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BalloonDash.Game
{
    public enum InputSource
    {
        Keyboard,
        Touch,
        Gamepad,
    }

    /// <summary>
    /// Keyboard, mouse, touch and gamepad through the Input System, read once per frame into the
    /// three things a run needs: jump pressed, jump held (for a full jump and for the balloon) and
    /// slide pressed. On a touch screen the right side of the screen jumps and the left side (or a
    /// swipe down anywhere) slides. The keyboard and the gamepad also drive the menus.
    /// </summary>
    public sealed class GameInput
    {
        const float StickStep = 0.6f;
        const float SwipeDown = 60f;

        int lastFrame = -1;
        int stickVertical;
        int stickHorizontal;
        int jumpFinger = -1;
        Vector2 jumpStart;
        bool padSlideHeld;

        static Keyboard Keys => Keyboard.current;

        /// <summary>What was used last (the HUD hints follow it).</summary>
        public InputSource Source { get; private set; } = InputSource.Keyboard;

        public bool UsingGamepad => Source == InputSource.Gamepad;

        public bool UsingTouch => Source == InputSource.Touch;

        public bool JumpPressed { get; private set; }

        public bool JumpHeld { get; private set; }

        public bool SlidePressed { get; private set; }

        /// <summary>Screen points (pixels, origin bottom-left) where a press began this frame, to ignore presses on HUD buttons.</summary>
        public Vector2? PressPosition { get; private set; }

        /// <summary>Reads the devices once per frame. <paramref name="blocked"/> says whether a screen point is over a HUD button.</summary>
        public void Read(Func<Vector2, bool> blocked)
        {
            if (lastFrame == Time.frameCount)
            {
                return;
            }
            lastFrame = Time.frameCount;
            JumpPressed = false;
            SlidePressed = false;
            PressPosition = null;
            bool held = false;

            Keyboard keys = Keys;
            if (keys != null)
            {
                if (keys.spaceKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame)
                {
                    JumpPressed = true;
                    Source = InputSource.Keyboard;
                }
                held |= keys.spaceKey.isPressed || keys.wKey.isPressed || keys.upArrowKey.isPressed;
                if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame || keys.leftShiftKey.wasPressedThisFrame ||
                    keys.leftCtrlKey.wasPressedThisFrame)
                {
                    SlidePressed = true;
                    Source = InputSource.Keyboard;
                }
            }

            Mouse mouse = Mouse.current;
            bool touched = false;
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                foreach (TouchControl finger in touch.touches)
                {
                    if (finger.press.wasPressedThisFrame)
                    {
                        Vector2 position = finger.position.ReadValue();
                        touched = true;
                        PressPosition = position;
                        if (blocked != null && blocked(position))
                        {
                            continue;
                        }
                        Source = InputSource.Touch;
                        if (position.x > Screen.width * 0.4f)
                        {
                            JumpPressed = true;
                            jumpFinger = finger.touchId.ReadValue();
                            jumpStart = position;
                        }
                        else
                        {
                            SlidePressed = true;
                        }
                    }
                    if (finger.isInProgress && finger.touchId.ReadValue() == jumpFinger)
                    {
                        touched = true;
                        held = true;
                        // A swipe down with the jump finger slides instead.
                        if (jumpStart.y - finger.position.ReadValue().y > SwipeDown)
                        {
                            SlidePressed = true;
                            jumpFinger = -1;
                            held = false;
                        }
                    }
                }
            }

            // Some platforms also report a tap as a mouse click.
            if (mouse != null && !touched)
            {
                Vector2 position = mouse.position.ReadValue();
                bool overButton = blocked != null && blocked(position);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    PressPosition = position;
                    if (!overButton)
                    {
                        JumpPressed = true;
                        Source = InputSource.Keyboard;
                    }
                }
                held |= mouse.leftButton.isPressed && !overButton;
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    SlidePressed = true;
                    Source = InputSource.Keyboard;
                }
            }

            foreach (Gamepad pad in Gamepad.all)
            {
                if (pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame)
                {
                    JumpPressed = true;
                    Source = InputSource.Gamepad;
                }
                held |= pad.buttonSouth.isPressed || pad.rightShoulder.isPressed || pad.rightTrigger.isPressed;
                bool slide = pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.leftTrigger.isPressed ||
                    pad.leftShoulder.isPressed || pad.dpad.down.isPressed || pad.leftStick.ReadValue().y < -StickStep;
                if (slide && !padSlideHeld)
                {
                    SlidePressed = true;
                    Source = InputSource.Gamepad;
                }
                padSlideHeld = slide;
            }

            JumpHeld = held;
        }

        public bool Pause => (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.pKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.startButton.wasPressedThisFrame);

        public bool Mute => Keys != null && Keys.mKey.wasPressedThisFrame;

        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame || Keys.spaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonSouth.wasPressedThisFrame);

        public bool Back =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonEast.wasPressedThisFrame);

        /// <summary>-1 for a step up a menu, +1 for a step down, 0 otherwise.</summary>
        public int VerticalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.upArrowKey.wasPressedThisFrame || Keys.wKey.wasPressedThisFrame) step = -1;
                if (Keys.downArrowKey.wasPressedThisFrame || Keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0)
            {
                Source = InputSource.Keyboard;
                return step;
            }
            return PadStep(true, ref stickVertical);
        }

        /// <summary>-1 for a step left through a setting, +1 for a step right, 0 otherwise.</summary>
        public int HorizontalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.leftArrowKey.wasPressedThisFrame || Keys.aKey.wasPressedThisFrame) step = -1;
                if (Keys.rightArrowKey.wasPressedThisFrame || Keys.dKey.wasPressedThisFrame) step = 1;
            }
            return step != 0 ? step : PadStep(false, ref stickHorizontal);
        }

        int PadStep(bool vertical, ref int held)
        {
            int step = 0;
            int stickSign = 0;
            foreach (Gamepad pad in Gamepad.all)
            {
                if (vertical)
                {
                    if (pad.dpad.up.wasPressedThisFrame) step = -1;
                    if (pad.dpad.down.wasPressedThisFrame) step = 1;
                }
                else
                {
                    if (pad.dpad.left.wasPressedThisFrame) step = -1;
                    if (pad.dpad.right.wasPressedThisFrame) step = 1;
                }
                Vector2 stick = pad.leftStick.ReadValue();
                float axis = vertical ? -stick.y : stick.x;
                if (Mathf.Abs(axis) > StickStep)
                {
                    stickSign = axis > 0f ? 1 : -1;
                }
            }
            if (step == 0 && stickSign != 0 && stickSign != held)
            {
                step = stickSign;
            }
            held = stickSign;
            if (step != 0)
            {
                Source = InputSource.Gamepad;
            }
            return step;
        }

        static bool AnyPad(Func<Gamepad, bool> test)
        {
            foreach (Gamepad pad in Gamepad.all)
            {
                if (test(pad))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
