using System;
using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Unity
{
    /// <summary>
    /// A character drawn by Unity's Animator (player, NPC, creature): clips key the SpriteRenderer's sprite, the Animator Controller's
    /// transitions pick the clip from parameters set by a driver. A look is an Animator Override Controller that swaps every clip
    /// for another sprite set (the player's weapon, an NPC's seasonal clothes); switching keeps the state, its progress and the
    /// parameters. The Animator runs in Normal update mode, so hit-stop and pause slow it through <c>Time.timeScale</c>, which only
    /// the frame driver writes (ADR-0001).
    /// </summary>
    /// <example>
    /// <code>
    /// sprite.SetLook("spear");                 // weapon changed: every clip now shows the spear sprites
    /// sprite.SetFloat("Speed", 4f);            // driver sets parameters; transitions do the rest
    /// sprite.FacingLeft = true;
    /// </code>
    /// </example>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class AnimatedSprite : MonoBehaviour, IAnimatorParameters
    {
        /// <summary>A named look: an override controller of the base Animator Controller.</summary>
        [Serializable]
        public struct Look
        {
            [Tooltip("Khóa mà game gọi: id vũ khí (sword, spear…) hoặc mùa (spring, winter…).")]
            public string Key;
            [Tooltip("Animator Override Controller thay các clip của controller gốc bằng bộ sprite của look này.")]
            public AnimatorOverrideController Controller;
        }

        [SerializeField, Tooltip("Bật nếu sprite được vẽ quay mặt sang TRÁI. Mặc định (tắt) là vẽ quay sang phải.")]
        private bool artFacesLeft;
        [SerializeField, Tooltip("Các look thay toàn bộ sprite: mỗi vũ khí của người chơi, mỗi mùa của NPC.")]
        private Look[] looks = Array.Empty<Look>();
        [SerializeField, Tooltip("Look bật khi bắt đầu (để trống = controller gốc).")]
        private string startLook;

        private Animator _animator;
        private SpriteRenderer _renderer;
        private RuntimeAnimatorController _base;
        private string _look;
        private bool _facingLeft;
        private AnimatorControllerParameter[] _paramScratch = Array.Empty<AnimatorControllerParameter>();
        private float[] _floatScratch = Array.Empty<float>();
        private int[] _intScratch = Array.Empty<int>();

        /// <summary>The Animator.</summary>
        public Animator Animator => _animator != null ? _animator : _animator = GetComponent<Animator>();

        /// <summary>The renderer the clips key (afterimages and VFX copy its sprite).</summary>
        public SpriteRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<SpriteRenderer>();

        /// <summary>The character faces left; the sprite is mirrored as needed for <c>artFacesLeft</c>.</summary>
        public bool FacingLeft
        {
            get => _facingLeft;
            set
            {
                _facingLeft = value;
                Renderer.flipX = value != artFacesLeft;
            }
        }

        /// <summary>True when the sprites are drawn facing left (they are mirrored so the character faces the way it moves).</summary>
        public bool ArtFacesLeft
        {
            get => artFacesLeft;
            set
            {
                artFacesLeft = value;
                FacingLeft = _facingLeft;
            }
        }

        /// <summary>Colour multiplied into the sprite (i-frame look…). Clips must not key the colour.</summary>
        public Color Tint
        {
            get => Renderer.color;
            set => Renderer.color = value;
        }

        /// <summary>Key of the active look, or null for the base controller.</summary>
        public string ActiveLook => _look;

        /// <summary>The base Animator Controller (the one assigned in the Inspector).</summary>
        public RuntimeAnimatorController BaseController
        {
            get
            {
                if (_base == null) _base = Animator.runtimeAnimatorController;
                return _base;
            }
        }

        private void Awake()
        {
            _ = BaseController;
            FacingLeft = _facingLeft;
            if (!string.IsNullOrEmpty(startLook)) SetLook(startLook);
        }

        // ---------------------------------------------------------------- looks

        /// <summary>Replaces the look list (spawners loading NPC data, weapons unlocked at runtime); re-applies the active look.</summary>
        public void SetLooks(Look[] list)
        {
            looks = list ?? Array.Empty<Look>();
            SetLook(_look);
        }

        /// <summary>
        /// Shows look <paramref name="key"/>; null, empty or unknown = the base controller. Keeps the current state, its normalized
        /// time and every parameter, so swapping weapons mid-swing does not restart the clip.
        /// </summary>
        public void SetLook(string key)
        {
            RuntimeAnimatorController target = BaseController;
            _look = null;
            if (!string.IsNullOrEmpty(key))
            {
                foreach (Look l in looks)
                {
                    if (l.Key != key || l.Controller == null) continue;
                    target = l.Controller;
                    _look = key;
                    break;
                }
            }
            Swap(target);
        }

        private void Swap(RuntimeAnimatorController target)
        {
            Animator a = Animator;
            if (target == null || a.runtimeAnimatorController == target) return;

            bool live = a.isActiveAndEnabled && a.runtimeAnimatorController != null && a.isInitialized;
            int layers = live ? a.layerCount : 0;
            var states = new (int hash, float time)[layers];
            for (int l = 0; l < layers; l++)
            {
                AnimatorStateInfo s = a.GetCurrentAnimatorStateInfo(l);
                states[l] = (s.fullPathHash, s.normalizedTime);
            }
            if (live) SaveParameters(a);

            a.runtimeAnimatorController = target;

            if (!live) return;
            RestoreParameters(a);
            for (int l = 0; l < layers && l < a.layerCount; l++) a.Play(states[l].hash, l, states[l].time);
            a.Update(0f);
        }

        // Look changes are rare (weapon swap, season): the scratch arrays grow once and are reused.
        private void SaveParameters(Animator a)
        {
            _paramScratch = a.parameters;
            int n = _paramScratch.Length;
            if (_floatScratch.Length < n) _floatScratch = new float[n];
            if (_intScratch.Length < n) _intScratch = new int[n];
            for (int i = 0; i < n; i++)
            {
                AnimatorControllerParameter p = _paramScratch[i];
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float: _floatScratch[i] = a.GetFloat(p.nameHash); break;
                    case AnimatorControllerParameterType.Int: _intScratch[i] = a.GetInteger(p.nameHash); break;
                    case AnimatorControllerParameterType.Bool:
                    case AnimatorControllerParameterType.Trigger: _intScratch[i] = a.GetBool(p.nameHash) ? 1 : 0; break;
                }
            }
        }

        private void RestoreParameters(Animator a)
        {
            for (int i = 0; i < _paramScratch.Length; i++)
            {
                AnimatorControllerParameter p = _paramScratch[i];
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float: a.SetFloat(p.nameHash, _floatScratch[i]); break;
                    case AnimatorControllerParameterType.Int: a.SetInteger(p.nameHash, _intScratch[i]); break;
                    case AnimatorControllerParameterType.Bool: a.SetBool(p.nameHash, _intScratch[i] != 0); break;
                    case AnimatorControllerParameterType.Trigger: if (_intScratch[i] != 0) a.SetTrigger(p.nameHash); break;
                }
            }
        }

        // ---------------------------------------------------------------- IAnimatorParameters

        /// <inheritdoc />
        public void SetFloat(string name, float value) => Animator.SetFloat(Animator.StringToHash(name), value);

        /// <inheritdoc />
        public void SetBool(string name, bool value) => Animator.SetBool(Animator.StringToHash(name), value);

        /// <inheritdoc />
        public void SetInteger(string name, int value) => Animator.SetInteger(Animator.StringToHash(name), value);

        /// <inheritdoc />
        public void SetTrigger(string name) => Animator.SetTrigger(Animator.StringToHash(name));

        /// <inheritdoc />
        public void ResetTrigger(string name) => Animator.ResetTrigger(Animator.StringToHash(name));
    }
}
