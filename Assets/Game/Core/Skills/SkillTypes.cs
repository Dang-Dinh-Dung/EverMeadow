using System;
using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>Where a press happens (command-table rows; combo system). Flags: a row may match several.</summary>
    [Flags]
    public enum SkillContext
    {
        /// <summary>None.</summary>
        None = 0,
        /// <summary>Standing or running on the ground.</summary>
        Ground = 1,
        /// <summary>In the air.</summary>
        Air = 2,
        /// <summary>Attack consumed during a dodge.</summary>
        Dash = 4
    }

    /// <summary>Direction of the press record, relative to the facing at consumption (command-table rows).</summary>
    public enum SkillDir
    {
        /// <summary>No direction held.</summary>
        Neutral,
        /// <summary>Up held.</summary>
        Up,
        /// <summary>Down held.</summary>
        Down,
        /// <summary>Toward the facing.</summary>
        Forward,
        /// <summary>Against the facing.</summary>
        Back
    }

    /// <summary>A problem found in a skill or command table file; the reader returns these instead of throwing.</summary>
    public readonly struct SkillError
    {
        /// <summary>Skill id, table family or asset name the error belongs to.</summary>
        public readonly string Subject;
        /// <summary>Short stable code, e.g. <c>IdFormat</c>, <c>SchemaTooNew</c>.</summary>
        public readonly string Code;
        /// <summary>Human-readable detail.</summary>
        public readonly string Message;

        /// <summary>Creates an error.</summary>
        public SkillError(string subject, string code, string message)
        {
            Subject = subject;
            Code = code;
            Message = message;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{Code}] {Subject}: {Message}";
    }

    /// <summary>Name and text of one XML source (a skill or a command table).</summary>
    public readonly struct SkillSourceText
    {
        /// <summary>File or asset name, for error messages.</summary>
        public readonly string Name;
        /// <summary>The XML text.</summary>
        public readonly string Xml;

        /// <summary>Creates a source text.</summary>
        public SkillSourceText(string name, string xml)
        {
            Name = name;
            Xml = xml;
        }
    }

    /// <summary>Where skill XML comes from (ADR-0002 Decision 4); read once at boot.</summary>
    public interface ISkillTextSource
    {
        /// <summary>Appends every skill and command table text to <paramref name="into"/>.</summary>
        void ReadAll(List<SkillSourceText> into);
    }
}
