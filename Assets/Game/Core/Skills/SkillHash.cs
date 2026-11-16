using System.Security.Cryptography;
using System.Text;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// <c>payload_hash</c> of ADR-0002 Decision 6: SHA-256 (lower-case hex, 64 chars) of the UTF-8 bytes of the document with
    /// the first <c>payload_hash="…"</c> value emptied and every other byte kept. Detects an XML body edited without its graph.
    /// </summary>
    /// <example>
    /// <code>
    /// bool intact = SkillHash.Matches(xmlText, skill.PayloadHash);
    /// </code>
    /// </example>
    public static class SkillHash
    {
        private const string Attribute = "payload_hash=\"";

        /// <summary>Computes the payload hash of <paramref name="xml"/>.</summary>
        public static string PayloadHash(string xml)
        {
            string blanked = Blank(xml ?? "");
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(blanked));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>True when <paramref name="declared"/> equals the hash of <paramref name="xml"/>.</summary>
        public static bool Matches(string xml, string declared) =>
            !string.IsNullOrEmpty(declared) && string.Equals(PayloadHash(xml), declared, System.StringComparison.Ordinal);

        private static string Blank(string xml)
        {
            int start = xml.IndexOf(Attribute, System.StringComparison.Ordinal);
            if (start < 0) return xml;
            int valueStart = start + Attribute.Length;
            int end = xml.IndexOf('"', valueStart);
            return end < 0 ? xml : xml.Substring(0, valueStart) + xml.Substring(end);
        }
    }
}
