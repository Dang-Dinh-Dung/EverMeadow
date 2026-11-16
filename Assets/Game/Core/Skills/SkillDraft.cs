using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>Mutable authoring model of one command-table row (combo system).</summary>
    public sealed class CommandRowDraft
    {
        /// <summary>Skill id.</summary>
        public string SkillId = "";
        /// <summary>Contexts; <see cref="SkillXmlReader.AllContexts"/> writes <c>any</c>.</summary>
        public SkillContext Context = SkillXmlReader.AllContexts;
        /// <summary>Moving filter.</summary>
        public MovingFilter Moving = MovingFilter.Any;
        /// <summary>True for <c>dir="any"</c>.</summary>
        public bool AnyDir = true;
        /// <summary>Direction when <see cref="AnyDir"/> is false.</summary>
        public SkillDir Dir = SkillDir.Neutral;
        /// <summary><c>after</c> entries: skill ids.</summary>
        public readonly List<string> After = new List<string>();
        /// <summary>Link window in seconds.</summary>
        public double LinkWindow = SkillXmlReader.DefaultLinkWindow;
        /// <summary>Priority.</summary>
        public int Priority;
    }

    /// <summary>Mutable authoring model of one weapon family's command table (combo system).</summary>
    public sealed class CommandTableDraft
    {
        /// <summary>Weapon family.</summary>
        public string WeaponFamily = "";
        /// <summary>Rows in declaration order.</summary>
        public readonly List<CommandRowDraft> Rows = new List<CommandRowDraft>();
        /// <summary>Optional graph hash, or empty.</summary>
        public string GraphHash = "";
    }
}
