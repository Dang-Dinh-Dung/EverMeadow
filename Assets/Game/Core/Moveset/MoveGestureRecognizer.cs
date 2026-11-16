using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>Timing knobs of gesture recognition (ticks of the clock, 1 µs).</summary>
    public readonly struct MoveGestureSettings
    {
        /// <summary>Second press within this time of the first = double tap.</summary>
        public readonly long DoubleTapTicks;
        /// <summary>Held this long = hold.</summary>
        public readonly long HoldTicks;
        /// <summary>Two chord buttons pressed within this time of each other = chord.</summary>
        public readonly long ChordTicks;

        /// <summary>Creates settings from seconds (rounded to 1/60 s steps like every skill time).</summary>
        public MoveGestureSettings(double doubleTapSeconds, double holdSeconds, double chordSeconds)
        {
            DoubleTapTicks = SkillTime.TickOf(doubleTapSeconds);
            HoldTicks = SkillTime.TickOf(holdSeconds);
            ChordTicks = SkillTime.TickOf(chordSeconds);
        }

        /// <summary>Defaults: double tap 0.25 s, hold 0.30 s, chord 0.05 s (3 frames).</summary>
        public static MoveGestureSettings Default => new MoveGestureSettings(0.25, 0.30, 0.05);
    }

    /// <summary>A recognised input and the tick it became known.</summary>
    public readonly struct MoveInputEvent
    {
        /// <summary>The input.</summary>
        public readonly MoveInput Input;
        /// <summary>Tick it was recognised (press, release, or window / threshold end).</summary>
        public readonly long Tick;

        /// <summary>Creates an event.</summary>
        public MoveInputEvent(MoveInput input, long tick)
        {
            Input = input;
            Tick = tick;
        }

        /// <inheritdoc />
        public override string ToString() => $"{Input}@{Tick}";
    }

    /// <summary>
    /// Turns raw presses / releases of the move buttons into moveset inputs (press, double tap, hold, chord), knowing which
    /// gestures the socketed movesets actually use (<see cref="Configure"/>), so a button only waits when it must:
    /// <list type="bullet">
    /// <item>a button with only Press rows fires on press — the promise "a quick press always attacks at once";</item>
    /// <item>a button that also has a Hold row fires Press on release before the hold time, Hold at the hold time;</item>
    /// <item>a button that is part of a Chord row waits up to the chord window for its partner (Press on release or window end);</item>
    /// <item>a DoubleTap row turns the second press within the window into DoubleTap (the first press already fired Press).</item>
    /// </list>
    /// Deterministic; zero allocation after <see cref="Configure"/>; single-thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// var g = new MoveGestureRecognizer(MoveGestureSettings.Default);
    /// g.Configure(inputsOfSocketedMovesets);
    /// g.Press(MoveButton.Button1, now, events);   // → Button1 at once when Button1 has no hold / chord rows
    /// g.Update(now, events);                      // every step: hold thresholds, chord windows
    /// </code>
    /// </example>
    public sealed class MoveGestureRecognizer
    {
        private const long None = long.MinValue;
        private static readonly int Count = Enum.GetValues(typeof(MoveButton)).Length;

        private readonly MoveGestureSettings _settings;
        private readonly bool[] _hasDouble = new bool[Count];
        private readonly bool[] _hasHold = new bool[Count];
        private readonly int[] _chordMask = new int[Count];
        private readonly bool[] _down = new bool[Count];
        private readonly bool[] _pending = new bool[Count];
        private readonly long[] _downTick = new long[Count];
        private readonly long[] _lastPress = new long[Count];

        /// <summary>Creates a recognizer; nothing waits until <see cref="Configure"/> declares hold / chord / double rows.</summary>
        public MoveGestureRecognizer(MoveGestureSettings settings)
        {
            _settings = settings;
            Reset();
        }

        /// <summary>Declares the inputs in use (the rows of every socketed moveset) and clears the state.</summary>
        public void Configure(IEnumerable<MoveInput> inputs)
        {
            Array.Clear(_hasDouble, 0, Count);
            Array.Clear(_hasHold, 0, Count);
            Array.Clear(_chordMask, 0, Count);
            if (inputs != null)
                foreach (MoveInput i in inputs)
                {
                    switch (i.Gesture)
                    {
                        case MoveGesture.DoubleTap: _hasDouble[(int)i.Button] = true; break;
                        case MoveGesture.Hold: _hasHold[(int)i.Button] = true; break;
                        case MoveGesture.Chord:
                            _chordMask[(int)i.Button] |= 1 << (int)i.Second;
                            _chordMask[(int)i.Second] |= 1 << (int)i.Button;
                            break;
                    }
                }
            Reset();
        }

        /// <summary>Inputs of every row of the socketed movesets (feed to <see cref="Configure"/> when sockets change).</summary>
        public static List<MoveInput> InputsOf(MovesetLoadout loadout, MovesetLibrary library)
        {
            var list = new List<MoveInput>();
            if (loadout == null || library == null) return list;
            for (int s = 0; s < loadout.SocketCount; s++)
                if (library.TryGet(loadout.Get(s), out MovesetDef def))
                    foreach (MovesetRow r in def.Rows) list.Add(r.Input);
            return list;
        }

        /// <summary>Forgets held buttons and pending presses (mode change, hitstun, focus loss).</summary>
        public void Reset()
        {
            Array.Clear(_down, 0, Count);
            Array.Clear(_pending, 0, Count);
            for (int i = 0; i < Count; i++)
            {
                _downTick[i] = None;
                _lastPress[i] = None;
            }
        }

        /// <summary>A button went down at <paramref name="tick"/>.</summary>
        public void Press(MoveButton button, long tick, List<MoveInputEvent> output)
        {
            int b = (int)button;
            if (_down[b]) return;   // key repeat
            _down[b] = true;
            _downTick[b] = tick;

            int mask = _chordMask[b];
            if (mask != 0)
                for (int p = 0; p < Count; p++)
                {
                    if ((mask & (1 << p)) == 0 || !_down[p] || !_pending[p] || tick - _downTick[p] > _settings.ChordTicks) continue;
                    _pending[p] = false;
                    _lastPress[b] = None;
                    _lastPress[p] = None;
                    output.Add(new MoveInputEvent(MoveInput.Chord(button, (MoveButton)p), tick));
                    return;
                }

            if (_hasDouble[b] && _lastPress[b] != None && tick - _lastPress[b] <= _settings.DoubleTapTicks)
            {
                _lastPress[b] = None;
                output.Add(new MoveInputEvent(MoveInput.DoubleTap(button), tick));
                return;
            }

            _lastPress[b] = tick;
            if (mask != 0 || _hasHold[b]) _pending[b] = true;
            else output.Add(new MoveInputEvent(MoveInput.Press(button), tick));
        }

        /// <summary>A button went up at <paramref name="tick"/>.</summary>
        public void Release(MoveButton button, long tick, List<MoveInputEvent> output)
        {
            int b = (int)button;
            if (!_down[b]) return;
            _down[b] = false;
            if (!_pending[b]) return;
            _pending[b] = false;
            output.Add(new MoveInputEvent(MoveInput.Press(button), tick));   // a tap that was waiting for hold / chord
        }

        /// <summary>Advances time: fires holds that reached the hold time and presses whose chord window ran out.</summary>
        public void Update(long now, List<MoveInputEvent> output)
        {
            for (int b = 0; b < Count; b++)
            {
                if (!_pending[b]) continue;
                if (_hasHold[b])
                {
                    if (_down[b] && now - _downTick[b] >= _settings.HoldTicks)
                    {
                        _pending[b] = false;
                        _lastPress[b] = None;
                        output.Add(new MoveInputEvent(MoveInput.Hold((MoveButton)b), _downTick[b] + _settings.HoldTicks));
                    }
                }
                else if (now - _downTick[b] > _settings.ChordTicks)
                {
                    _pending[b] = false;
                    output.Add(new MoveInputEvent(MoveInput.Press((MoveButton)b), _downTick[b] + _settings.ChordTicks));
                }
            }
        }
    }
}
