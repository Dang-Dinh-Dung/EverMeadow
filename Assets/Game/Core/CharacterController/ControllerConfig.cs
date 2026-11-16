using System;
using System.Collections.Generic;

namespace Evermeadow.Core.CharacterController
{
    /// <summary>
    /// Tuning knobs of the Character Controller (GDD Character Controller › Tuning Knobs). Data, never hardcoded at call sites.
    /// Validate with <see cref="ControllerConfigValidator.Validate"/> before use.
    /// </summary>
    [Serializable]
    public sealed class ControllerConfig
    {
        /// <summary>
        /// World scale (GDD Character Controller decision C-12, 2026-11-01): the GDD and prototype numbers are in prototype units
        /// where the body is 1.3 u tall; the game uses art-bible units (1 u = 1 tile = 16 px, body 48 px = 3 u). Every length,
        /// speed and acceleration is the prototype value × K = 30/13; times and multipliers are unchanged.
        /// </summary>
        public const float K = 30f / 13f;

        // Jump and fall
        /// <summary>Jump take-off speed, u/s (12.5–13.4 prototype × K).</summary>
        public float JumpSpeed = 13f * K;
        /// <summary>Gravity multiplier on 9.81 (2.5–4 prototype × K).</summary>
        public float GravityScale = 3.2f * K;
        /// <summary>Gravity multiplier while falling (1.0–2.0; C-5).</summary>
        public float FallGravityMult = 1.6f;
        /// <summary>Terminal fall speed, u/s (12–17 prototype × K).</summary>
        public float MaxFallSpeed = 16f * K;
        /// <summary>Gravity multiplier during apex hang (0.3–1.0; 1 = off; C-6).</summary>
        public float ApexHangMult = 0.5f;
        /// <summary>|vy| below which the apex hang applies, u/s (0.5–2.5 prototype × K).</summary>
        public float ApexHangThreshold = 1.5f * K;
        /// <summary>Longest apex hang, s (0–0.10).</summary>
        public float ApexHangMaxTime = 0.08f;
        /// <summary>vy multiplier on early release (0.3–0.7).</summary>
        public float JumpCutMult = 0.5f;
        /// <summary>Double jump speed, u/s (11.8–12.4 prototype × K, below JumpSpeed).</summary>
        public float DoubleJumpSpeed = 12f * K;
        /// <summary>Coyote time, s (0.06–0.15).</summary>
        public float CoyoteTime = 0.10f;

        // Movement and dodge
        /// <summary>Run speed, u/s (5.5–8 prototype × K); used while the Run toggle is on.</summary>
        public float MoveSpeed = 6.5f * K;
        /// <summary>Walk speed, the default gait, u/s in game units (3–6; decision C-13). Always below <see cref="MoveSpeed"/>.</summary>
        public float WalkSpeed = 4f;
        /// <summary>Dodge speed, u/s (10–16 prototype × K).</summary>
        public float DodgeSpeed = 14f * K;
        /// <summary>Dodge duration and i-frames, s (0.15–0.25).</summary>
        public float DodgeDuration = 0.20f;
        /// <summary>Dodge cooldown counted from the end of the dodge, s (0.25–0.50; C-7).</summary>
        public float DodgeCooldown = 0.35f;

        // Attack constants removed 2026-11-14: attacks are skills (moveset + skill runtime), not Controller states.

        // Terrain and implementation
        /// <summary>Steepest walkable surface, degrees (30–55).</summary>
        public float MaxWalkAngle = 50f;
        /// <summary>Minimum ground snap distance, u (0.05–0.15 prototype × K).</summary>
        public float SnapMin = 0.10f * K;
        /// <summary>Cast skin, u (0.005–0.02 prototype × K).</summary>
        public float Skin = 0.01f * K;
        /// <summary>Ceiling corner correction, u (0.05–0.225 prototype × K; ≤ ColliderWidth / 4).</summary>
        public float CornerCorrection = 0.15f * K;
        /// <summary>Minimum time a dropped-through platform is ignored, s (0.10–0.20).</summary>
        public float DropIgnoreTime = 0.12f;
        /// <summary>Largest depenetration push, u (0.5–2.0 prototype × K).</summary>
        public float DepenMax = 1.0f * K;
        /// <summary>Frame dt clamp, s (0.05–0.20).</summary>
        public float MaxFrameDt = 0.10f;
        /// <summary>Collider width, u (0.8–1.0 prototype × K).</summary>
        public float ColliderWidth = 0.9f * K;
        /// <summary>Collider height, u (1.2–1.4 prototype × K; 1.3 × K = 3 u = 48 px at PPU 16).</summary>
        public float ColliderHeight = 1.3f * K;

        // Modifier clamps
        /// <summary>Lowest combined speed multiplier.</summary>
        public float ModifierSpeedMin = 0.3f;
        /// <summary>Highest combined speed multiplier.</summary>
        public float ModifierSpeedMax = 1.5f;
        /// <summary>Lowest combined jump multiplier (by height).</summary>
        public float ModifierJumpMin = 0.5f;
        /// <summary>Highest combined jump multiplier (by height).</summary>
        public float ModifierJumpMax = 1.2f;

        // Buffer windows (Rule 28), seconds on the scaled clock
        /// <summary>Jump buffer window, s.</summary>
        public float JumpWindow = 0.12f;
        /// <summary>DropThrough buffer window, s.</summary>
        public float DropThroughWindow = 0.12f;
        /// <summary>Dodge buffer window, s.</summary>
        public float DodgeWindow = 0.15f;
        /// <summary>Run toggle buffer window, s (a Run press older than this is ignored).</summary>
        public float RunWindow = 0.10f;

        /// <summary>Gravity, u/s².</summary>
        public double Gravity => GravityScale * 9.81;

        /// <summary>Returns a copy.</summary>
        public ControllerConfig Clone() => (ControllerConfig)MemberwiseClone();
    }

    /// <summary>
    /// Pure validator (GDD Character Controller › validator; hook H-7). Out-of-range knobs fall back to their default; a violated
    /// cross-constraint resets every knob that takes part.
    /// </summary>
    public static class ControllerConfigValidator
    {
        // Safe ranges of lengths, speeds and accelerations are the GDD (prototype) ranges × K (decision C-12).
        private const float K = ControllerConfig.K;

        /// <summary>Validates <paramref name="raw"/>; <paramref name="violations"/> lists what was reset.</summary>
        public static ControllerConfig Validate(ControllerConfig raw, out List<string> violations)
        {
            var d = new ControllerConfig();
            var c = raw?.Clone() ?? new ControllerConfig();
            var v = new List<string>();
            violations = v;

            c.JumpSpeed = R(c.JumpSpeed, 12.5f * K, 13.4f * K, d.JumpSpeed, nameof(c.JumpSpeed), v);
            c.GravityScale = R(c.GravityScale, 2.5f * K, 4f * K, d.GravityScale, nameof(c.GravityScale), v);
            c.FallGravityMult = R(c.FallGravityMult, 1f, 2f, d.FallGravityMult, nameof(c.FallGravityMult), v);
            c.MaxFallSpeed = R(c.MaxFallSpeed, 12f * K, 17f * K, d.MaxFallSpeed, nameof(c.MaxFallSpeed), v);
            c.ApexHangMult = R(c.ApexHangMult, 0.3f, 1f, d.ApexHangMult, nameof(c.ApexHangMult), v);
            c.ApexHangThreshold = R(c.ApexHangThreshold, 0.5f * K, 2.5f * K, d.ApexHangThreshold, nameof(c.ApexHangThreshold), v);
            c.ApexHangMaxTime = R(c.ApexHangMaxTime, 0f, 0.10f, d.ApexHangMaxTime, nameof(c.ApexHangMaxTime), v);
            c.JumpCutMult = R(c.JumpCutMult, 0.3f, 0.7f, d.JumpCutMult, nameof(c.JumpCutMult), v);
            c.DoubleJumpSpeed = R(c.DoubleJumpSpeed, 11.8f * K, 12.4f * K, d.DoubleJumpSpeed, nameof(c.DoubleJumpSpeed), v);
            c.CoyoteTime = R(c.CoyoteTime, 0.06f, 0.15f, d.CoyoteTime, nameof(c.CoyoteTime), v);
            c.MoveSpeed = R(c.MoveSpeed, 5.5f * K, 8f * K, d.MoveSpeed, nameof(c.MoveSpeed), v);
            c.WalkSpeed = R(c.WalkSpeed, 3f, 6f, d.WalkSpeed, nameof(c.WalkSpeed), v);
            c.DodgeSpeed = R(c.DodgeSpeed, 10f * K, 16f * K, d.DodgeSpeed, nameof(c.DodgeSpeed), v);
            c.DodgeDuration = R(c.DodgeDuration, 0.15f, 0.25f, d.DodgeDuration, nameof(c.DodgeDuration), v);
            c.DodgeCooldown = R(c.DodgeCooldown, 0.25f, 0.50f, d.DodgeCooldown, nameof(c.DodgeCooldown), v);
            c.MaxWalkAngle = R(c.MaxWalkAngle, 30f, 55f, d.MaxWalkAngle, nameof(c.MaxWalkAngle), v);
            c.SnapMin = R(c.SnapMin, 0.05f * K, 0.15f * K, d.SnapMin, nameof(c.SnapMin), v);
            c.Skin = R(c.Skin, 0.005f * K, 0.02f * K, d.Skin, nameof(c.Skin), v);
            c.CornerCorrection = R(c.CornerCorrection, 0.05f * K, 0.225f * K, d.CornerCorrection, nameof(c.CornerCorrection), v);
            c.DropIgnoreTime = R(c.DropIgnoreTime, 0.10f, 0.20f, d.DropIgnoreTime, nameof(c.DropIgnoreTime), v);
            c.DepenMax = R(c.DepenMax, 0.5f * K, 2f * K, d.DepenMax, nameof(c.DepenMax), v);
            c.MaxFrameDt = R(c.MaxFrameDt, 0.05f, 0.20f, d.MaxFrameDt, nameof(c.MaxFrameDt), v);
            c.ColliderWidth = R(c.ColliderWidth, 0.8f * K, 1f * K, d.ColliderWidth, nameof(c.ColliderWidth), v);
            c.ColliderHeight = R(c.ColliderHeight, 1.2f * K, 1.4f * K, d.ColliderHeight, nameof(c.ColliderHeight), v);
            c.JumpWindow = R(c.JumpWindow, 0f, 0.40f, d.JumpWindow, nameof(c.JumpWindow), v);
            c.DropThroughWindow = R(c.DropThroughWindow, 0f, 0.40f, d.DropThroughWindow, nameof(c.DropThroughWindow), v);
            c.DodgeWindow = R(c.DodgeWindow, 0f, 0.40f, d.DodgeWindow, nameof(c.DodgeWindow), v);
            c.RunWindow = R(c.RunWindow, 0f, 0.40f, d.RunWindow, nameof(c.RunWindow), v);

            // 1. double jump below jump
            if (!(c.DoubleJumpSpeed < c.JumpSpeed))
            {
                v.Add("DoubleJumpSpeed must be < JumpSpeed; both reset.");
                c.DoubleJumpSpeed = d.DoubleJumpSpeed;
                c.JumpSpeed = d.JumpSpeed;
            }
            // 6. dodge spam slower than running
            if (!(c.DodgeDuration + c.DodgeCooldown > c.DodgeSpeed * c.DodgeDuration / c.MoveSpeed))
            {
                v.Add("DodgeDuration + DodgeCooldown must exceed dodge distance / MoveSpeed; all four reset.");
                c.DodgeDuration = d.DodgeDuration;
                c.DodgeCooldown = d.DodgeCooldown;
                c.DodgeSpeed = d.DodgeSpeed;
                c.MoveSpeed = d.MoveSpeed;
            }
            // 7. apex hang adds at most 0.05 s
            if (!(c.ApexHangMaxTime * (1f - c.ApexHangMult) <= 0.05f + 1e-6f))
            {
                v.Add("ApexHangMaxTime × (1 − ApexHangMult) must be ≤ 0.05 s; both reset.");
                c.ApexHangMaxTime = d.ApexHangMaxTime;
                c.ApexHangMult = d.ApexHangMult;
            }
            // 9. corner correction ≤ collider width / 4
            if (!(c.CornerCorrection <= c.ColliderWidth / 4f + 1e-6f))
            {
                v.Add("CornerCorrection must be ≤ ColliderWidth / 4; both reset.");
                c.CornerCorrection = d.CornerCorrection;
                c.ColliderWidth = d.ColliderWidth;
            }
            return c;
        }

        private static float R(float value, float min, float max, float fallback, string name, List<string> v)
        {
            if (!float.IsNaN(value) && value >= min && value <= max) return value;
            v.Add($"{name} {value} outside {min}–{max}; using {fallback}.");
            return fallback;
        }
    }
}
