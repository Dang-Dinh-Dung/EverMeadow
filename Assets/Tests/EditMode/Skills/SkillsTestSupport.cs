using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>One skill fixture: the id (its file name) and the XML text.</summary>
    internal readonly struct Fx
    {
        public readonly string Id;
        public readonly string Xml;

        public Fx(string id, string xml)
        {
            Id = id;
            Xml = xml;
        }

        public Fx Replace(string find, string replace) => new Fx(Id, Xml.Replace(find, replace));

        public SkillSourceText Source => new SkillSourceText(Id + ".xml", Xml);
    }

    /// <summary>XML fixtures and helpers for the Skill &amp; Moveset core tests (constants, not files: tests do no I/O).</summary>
    internal static class SkillsTestSupport
    {
        // Modelled on the reference "Guard Rush": rush forward, hit (knock back and buff whoever is hit), search and look.
        public const string GuardRush = @"<?xml version=""1.0"" encoding=""utf-8""?>
<skill schema_version=""2"" owner=""player"">
  <meta payload_hash=""p""/>
  <node id=""0"" type=""Start"" x=""0"" y=""0""/>
  <node id=""5"" type=""Move"" x=""900"" y=""300"">
    <f n=""PivotOffsetType"" v=""LookOfTarget""/>
    <f n=""PivotOffset"" v=""0,0,4""/>
  </node>
  <node id=""6"" type=""Hit"" x=""-400"" y=""0"">
    <f n=""IsSteady"" v=""true""/>
    <f n=""HitEffectAddressable"" v=""FX_BOR_Guard_Hit""/>
    <f n=""SubValue"" v=""360,0.8,0""/>
  </node>
  <node id=""10"" type=""Search"" x=""-100"" y=""400"">
    <f n=""PivotCreatureGID"" ref=""0.OwnerGID""/>
  </node>
  <node id=""11"" type=""Animation"" x=""300"" y=""0"">
    <f n=""AnimationType"" v=""Run 02""/>
  </node>
  <node id=""12"" type=""Effect"" x=""300"" y=""350"">
    <f n=""ActionDelay"" v=""0.1""/>
    <f n=""PrefabName"" v=""FX_BOR_GuardRush""/>
    <f n=""Scale"" v=""1.5""/>
  </node>
  <node id=""13"" type=""LookAt"" x=""600"" y=""500"">
    <f n=""PivotCreatureGID"" ref=""6.LastHitCreatureGID""/>
  </node>
  <node id=""15"" type=""Knockback"" x=""900"" y=""600"">
    <f n=""TargetCreatureGID"" ref=""6.LastHitCreatureGID""/>
  </node>
  <link from=""0"" to=""6"" when=""start""/>
  <link from=""0"" to=""10"" when=""start""/>
  <link from=""0"" to=""11"" when=""start""/>
  <link from=""6"" to=""13"" when=""hit""/>
  <link from=""6"" to=""15"" when=""hit""/>
  <link from=""11"" to=""5"" when=""start""/>
  <link from=""11"" to=""12"" when=""start""/>
  <memo x=""-400"" y=""-250"" w=""600"" h=""140"" title=""Guard Rush"">Rush forward (front 180° / 1.5 m)
then a circular blast (radius 2.5 m).</memo>
</skill>";

        /// <summary>A minimal valid skill with the given id (file name): Start → Animation.</summary>
        public static Fx Skill(string id, string extra = "")
        {
            return new Fx(id, $@"<skill schema_version=""2""{extra}>
  <node id=""0"" type=""Start""/>
  <node id=""1"" type=""Animation""><f n=""AnimationType"" v=""a""/></node>
  <link from=""0"" to=""1"" when=""start""/>
</skill>");
        }

        /// <summary>Skill XML with the given nodes / links body.</summary>
        public static string Body(string inner, string owner = "player") =>
            $@"<skill schema_version=""2"" owner=""{owner}"">{inner}</skill>";

        public static SkillDef Parse(string xml, string id = "100000")
        {
            var errors = new List<SkillError>();
            return SkillXmlReader.TryParse(xml, id, out SkillDef def, errors) ? def : null;
        }

        public static SkillDef Parse(Fx f) => Parse(f.Xml, f.Id);

        public static List<SkillError> Errors(string xml, string id = "100000")
        {
            var errors = new List<SkillError>();
            SkillXmlReader.TryParse(xml, id, out _, errors);
            return errors;
        }

        public static List<SkillError> Errors(Fx f) => Errors(f.Xml, f.Id);

        public static SkillLibrary Library(params Fx[] skills)
        {
            var lib = new SkillLibrary();
            foreach (Fx f in skills)
            {
                SkillDef def = Parse(f);
                if (def != null) lib.Add(def);
            }
            return lib;
        }

        public static string Table(string family, params string[] rows) =>
            $@"<command_table schema_version=""1"" weapon_family=""{family}"">" + string.Join("", rows) + "</command_table>";
    }
}
