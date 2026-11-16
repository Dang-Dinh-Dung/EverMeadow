using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>A press or release of one dev button (index into the binding list given to <see cref="DevButtonPoller"/>).</summary>
    public readonly struct DevButtonEvent
    {
        /// <summary>Index of the button in the binding list.</summary>
        public readonly int Index;
        /// <summary>True on press, false on release.</summary>
        public readonly bool Pressed;

        /// <summary>Creates an event.</summary>
        public DevButtonEvent(int index, bool pressed)
        {
            Index = index;
            Pressed = pressed;
        }
    }

    /// <summary>
    /// Dev-only buttons built in code (never in the project's action asset, so they cannot clash with the player's bindings
    /// or the Rule 2 table): one button action per entry, each entry one or more control paths separated by <c>;</c>.
    /// Polled once per frame. Used by dev tools such as the Moveset Tester until the real moveset actions exist in Input
    /// (GDD Skill &amp; Moveset OQ-S18). Lives in the Input adapter because only this assembly may read devices (AC-R01).
    /// Single-thread only; dispose to remove the actions.
    /// </summary>
    /// <example>
    /// <code>
    /// var dev = new DevButtonPoller(new[] { "&lt;Mouse&gt;/leftButton", "&lt;Keyboard&gt;/q", "&lt;Keyboard&gt;/f9" });
    /// dev.Poll(events);            // every frame
    /// dev.Dispose();
    /// </code>
    /// </example>
    public sealed class DevButtonPoller : IDisposable
    {
        private readonly InputActionMap _map;
        private readonly InputAction[] _actions;

        /// <summary>Creates and enables one button action per entry of <paramref name="bindings"/>.</summary>
        public DevButtonPoller(IReadOnlyList<string> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            _map = new InputActionMap("DevButtons");
            _actions = new InputAction[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
            {
                InputAction a = _map.AddAction("Dev" + i, InputActionType.Button);
                foreach (string path in (bindings[i] ?? "").Split(';'))
                    if (path.Trim().Length > 0) a.AddBinding(path.Trim());
                _actions[i] = a;
            }
            _map.Enable();
        }

        /// <summary>Number of buttons.</summary>
        public int Count => _actions.Length;

        /// <summary>Pointer position in screen pixels (origin bottom-left), or null without a mouse.</summary>
        public Vector2? PointerScreenPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2?)null;

        /// <summary>Appends this frame's presses and releases (press before release when both happened).</summary>
        public void Poll(List<DevButtonEvent> output)
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                InputAction a = _actions[i];
                if (a.WasPressedThisFrame()) output.Add(new DevButtonEvent(i, true));
                if (a.WasReleasedThisFrame()) output.Add(new DevButtonEvent(i, false));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
