using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Suborned.World.Review
{
    /// <summary>One frame of review input, device-free: the camera modes and the overlay never read a device.</summary>
    public struct WalkFrame
    {
        // The camera's share: empty while a text field has the keyboard, and its pointer part while the pointer is over a panel.
        /// <summary>x right, y up, z forward, each −1..1 while its key is held (WASD, Q/E).</summary>
        public Vector3 Move;
        /// <summary>Degrees to turn this frame: x to the right, y upward (pointer drag and arrow keys).</summary>
        public Vector2 Look;
        /// <summary>Pointer travel in pixels since the last frame while a mouse button is held: the difference of two Pointer
        /// positions, so it is in the camera's own pixels whatever the Game view's scale (the device's motion delta is not).</summary>
        public Vector2 Drag;
        /// <summary>Scroll: wheel notches this frame (a trackpad gives fractions). Dial: +1 or −1 while ] or [ is held. Positive = more (faster, closer).</summary>
        public float Scroll, Dial;
        public bool Fast, Dragging, LevelUp, LevelDown, NextMode, Places, Note, Keys, ResetZoom, Fade;
        /// <summary>1 Walk, 2 Fly, 3 Tactical on the frame its key went down; else 0.</summary>
        public int Mode;

        // The overlay's share: always read.
        /// <summary>Where the pointer is, in pixels from the bottom-left.</summary>
        public Vector2 Pointer;
        /// <summary>Printable characters typed this frame (null = none).</summary>
        public string Typed;
        /// <summary>Enter, Esc and the left button on the frame they went down; Backspace also while held (key repeat).</summary>
        public bool Enter, Escape, Backspace, Click;
        /// <summary>−1 for ↑, +1 for ↓ (with key repeat); Wheel: the wheel's notches wherever the pointer is.</summary>
        public int Row;
        public float Wheel;
    }

    /// <summary>The key bindings: the Input System's keyboard and mouse (the project's only active input handler) read into a
    /// WalkFrame. Typed text comes from Keyboard.onTextInput: a Play Mode OnGUI gets no key events under the Input System.</summary>
    public sealed class WalkInput : IDisposable
    {
        const float DragLook = 0.15f; // degrees per pixel, as FreeFly
        const float KeyLook = 90f;    // degrees per second on the arrow keys
        const float RepeatAfter = 0.4f, RepeatEvery = 0.05f;
        const int Buffer = 64;        // characters kept between frames
        const float MaxWheel = 2f;    // notches taken from one frame

        readonly StringBuilder typed = new();
        Keyboard heard;
        float back, up, down; // when each repeating key next fires
        Vector2 held;         // where the pointer was on the last frame of the drag in progress
        bool dragging;

        /// <summary>typing: a text field has the keyboard; blocked: the pointer is over a panel (drags and the wheel belong to it).</summary>
        public WalkFrame Read(float dt, bool typing, Func<Vector2, bool> blocked)
        {
            var f = new WalkFrame();
            var mouse = Mouse.current;
            if (mouse != null)
            {
                f.Pointer = mouse.position.ReadValue();
                f.Click = mouse.leftButton.wasPressedThisFrame;
                f.Wheel = Mathf.Clamp(mouse.scroll.ReadValue().y, -MaxWheel, MaxWheel); // one notch = 1 (the Input System's uniform scroll range)
                if (!blocked(f.Pointer))
                {
                    f.Dragging = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
                    if (f.Dragging)
                    {
                        f.Look = mouse.delta.ReadValue() * DragLook;
                        if (dragging) f.Drag = f.Pointer - held; // the frame a button goes down starts the drag: no travel yet
                    }
                    f.Scroll = f.Wheel;
                }
                dragging = f.Dragging; held = f.Pointer;
            }
            var kb = Keyboard.current;
            Hear(kb);
            if (kb == null) return f;

            if (typing && typed.Length > 0) f.Typed = typed.ToString();
            typed.Clear(); // a key that opens a field is not the field's first character
            f.Enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            f.Escape = kb.escapeKey.wasPressedThisFrame;
            f.Backspace = Repeats(kb.backspaceKey, ref back);
            f.Row = (Repeats(kb.downArrowKey, ref down) ? 1 : 0) - (Repeats(kb.upArrowKey, ref up) ? 1 : 0);
            if (typing) return f;

            f.Move = new Vector3(Axis(kb.dKey, kb.aKey), Axis(kb.eKey, kb.qKey), Axis(kb.wKey, kb.sKey));
            f.Look += new Vector2(Axis(kb.rightArrowKey, kb.leftArrowKey), Axis(kb.upArrowKey, kb.downArrowKey)) * (KeyLook * dt);
            f.Dial = Axis(kb.rightBracketKey, kb.leftBracketKey);
            f.Fast = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            f.LevelUp = kb.eKey.wasPressedThisFrame; f.LevelDown = kb.qKey.wasPressedThisFrame;
            f.Mode = kb.digit1Key.wasPressedThisFrame ? 1 : kb.digit2Key.wasPressedThisFrame ? 2 : kb.digit3Key.wasPressedThisFrame ? 3 : 0;
            f.NextMode = kb.tabKey.wasPressedThisFrame;
            f.Places = kb.gKey.wasPressedThisFrame;
            f.Note = kb.nKey.wasPressedThisFrame;
            f.Keys = kb.hKey.wasPressedThisFrame;
            f.ResetZoom = kb.zKey.wasPressedThisFrame;
            f.Fade = kb.fKey.wasPressedThisFrame;
            return f;
        }

        public void Dispose() => Hear(null);

        /// <summary>Listens for text on the current keyboard (it changes when one is plugged in).</summary>
        void Hear(Keyboard kb)
        {
            if (kb == heard) return;
            if (heard != null) heard.onTextInput -= Type;
            heard = kb;
            if (kb != null) kb.onTextInput += Type;
        }

        /// <summary>Printable characters only: Enter, Backspace, Esc and the arrows (macOS sends them as U+F700…) are read as keys.</summary>
        void Type(char c)
        {
            if (c >= ' ' && c != '\u007f' && (c < '' || c > '') && typed.Length < Buffer) typed.Append(c);
        }

        /// <summary>True on the frame the key goes down, then again and again while it stays down.</summary>
        static bool Repeats(KeyControl key, ref float next)
        {
            float now = Time.unscaledTime;
            if (key.wasPressedThisFrame) { next = now + RepeatAfter; return true; }
            if (!key.isPressed || now < next) return false;
            next = now + RepeatEvery;
            return true;
        }

        static float Axis(KeyControl plus, KeyControl minus) => (plus.isPressed ? 1f : 0f) - (minus.isPressed ? 1f : 0f);
    }
}
