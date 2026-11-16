using System.Collections.Generic;
using System.IO;
using Evermeadow.Core.Skills;
using UnityEngine;

namespace Evermeadow.Core.Moveset.Unity
{
    /// <summary>
    /// Scene host of the game's <see cref="MovesetRepository"/>: loads <c>StreamingAssets/Movesets/</c> on Awake. In the
    /// Editor the Moveset Tool pushes each saved file into it while playing, so an edited moveset is used by the next press
    /// without leaving Play Mode. <c>GameBootstrap</c> adds it when the scene has none.
    /// </summary>
    /// <example>
    /// <code>
    /// MovesetLibrary movesets = GetComponent&lt;MovesetLibraryRunner&gt;().Repository.Library;
    /// MoveChoice c = MoveSelector.Select(loadout, movesets, in query);
    /// </code>
    /// </example>
    [DefaultExecutionOrder(-1890)]
    public sealed class MovesetLibraryRunner : MonoBehaviour
    {
        /// <summary>Folder under <c>StreamingAssets</c> that holds the moveset files.</summary>
        public const string FolderName = "Movesets";

        /// <summary>The live movesets (created on Awake or on the first <see cref="Ensure"/>).</summary>
        public MovesetRepository Repository { get; private set; }

        /// <summary>Absolute path of the movesets folder.</summary>
        public static string MovesetsDirectory => Path.Combine(Application.streamingAssetsPath, FolderName);

        private void Awake() => Ensure();

        private void OnDestroy()
        {
            if (Repository != null) Repository.Changed -= OnChanged;
        }

        /// <summary>Creates and loads the repository if needed; returns it.</summary>
        public MovesetRepository Ensure()
        {
            if (Repository != null) return Repository;
            Repository = new MovesetRepository(new FolderSkillTextSource(MovesetsDirectory));
            var errors = new List<SkillError>();
            Repository.LoadAll(errors);
            foreach (SkillError e in errors) Debug.LogWarning("[Moveset] " + e);
            Debug.Log($"[Moveset] Loaded {Repository.Library.Count} moveset(s) from {MovesetsDirectory}.");
            Repository.Changed += OnChanged;
            return Repository;
        }

        private static void OnChanged(SkillReloadEvent e)
        {
            if (e.FileName != "*") Debug.Log("[Moveset] Hot reload: " + e);
        }
    }
}
