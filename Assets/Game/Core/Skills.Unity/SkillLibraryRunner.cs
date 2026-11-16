using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Evermeadow.Core.Skills.Unity
{
    /// <summary>
    /// Scene host of the game's <see cref="SkillRepository"/>: loads <c>StreamingAssets/Skills/</c> on Awake and keeps the
    /// repository for the systems that cast skills (they look skills up by id on every cast). In the Editor, the Skill Tool
    /// finds this component while playing and pushes each saved file into <see cref="Repository"/>, so an edited skill is
    /// used by the next cast without leaving Play Mode. <see cref="Evermeadow.Bootstrap"/> adds it when the scene has none.
    /// </summary>
    /// <example>
    /// <code>
    /// SkillLibraryRunner skills = GetComponent&lt;SkillLibraryRunner&gt;();
    /// if (skills.Repository.Library.TryGet("100001", out SkillDef def)) Cast(def);
    /// </code>
    /// </example>
    [DefaultExecutionOrder(-1900)]
    public sealed class SkillLibraryRunner : MonoBehaviour
    {
        /// <summary>Folder under <c>StreamingAssets</c> that holds the skill files.</summary>
        public const string FolderName = "Skills";

        /// <summary>The live skill data (created on Awake or on the first <see cref="Ensure"/>).</summary>
        public SkillRepository Repository { get; private set; }

        /// <summary>Absolute path of the skills folder.</summary>
        public static string SkillsDirectory => Path.Combine(Application.streamingAssetsPath, FolderName);

        private void Awake() => Ensure();

        private void OnDestroy()
        {
            if (Repository != null) Repository.Changed -= OnChanged;
        }

        /// <summary>Creates and loads the repository if that has not happened yet; returns it.</summary>
        public SkillRepository Ensure()
        {
            if (Repository != null) return Repository;
            // In the Editor also check the payload stamp, so a hand edit is reported the moment it loads.
            Repository = new SkillRepository(new FolderSkillTextSource(SkillsDirectory), verifyPayloadHash: Application.isEditor);
            var errors = new List<SkillError>();
            Repository.LoadAll(errors);
            foreach (SkillError e in errors) Debug.LogWarning("[Skills] " + e);
            Debug.Log($"[Skills] Loaded {Repository.Library.SkillCount} skill(s) from {SkillsDirectory}.");
            Repository.Changed += OnChanged;
            return Repository;
        }

        private static void OnChanged(SkillReloadEvent e)
        {
            if (e.FileName != "*") Debug.Log("[Skills] Hot reload: " + e);
        }
    }
}
