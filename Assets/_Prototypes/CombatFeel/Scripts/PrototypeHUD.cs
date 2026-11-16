// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// On-screen instrumentation, not game UI. The hypothesis needs a measurable
// signal, not just a vibe, so this reads the counters straight off the
// player and prints them live. OnGUI on purpose -- no Canvas, no prefab, one
// less thing to wire up by hand.

using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    public CombatFeelPlayer player;
    public DummyEnemy enemy;

    GUIStyle _style;

    void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 18;
            _style.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(10, 10, 520, 300), "");
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("COMBAT FEEL PROTOTYPE — not for production");
        lines.AppendLine("A/D move, Space jump, J (or LMB) attack, LShift dodge");
        lines.AppendLine("J in air = air attack · J while dodging = dash attack · S+J in air = plunge");
        lines.AppendLine("");
        if (player != null)
        {
            lines.AppendLine($"Session: {player.SessionSeconds:0.0}s   State: {player.DebugState}");
            lines.AppendLine($"Landed hits: {player.LandedHits}  (ground {player.GroundHits} · air {player.AirHits} · dash {player.DashHits} · plunge {player.PlungeHits})");
            lines.AppendLine($"Dodges during enemy active: {player.DodgeAttemptsDuringEnemyActive}");
            lines.AppendLine($"Successful dodges: {player.SuccessfulDodges}");
            lines.AppendLine($"Dodge->hit->retreat chains: {player.ComboChains}");
            lines.AppendLine("");
            string verdict = player.ComboChains >= 3 && player.SessionSeconds <= 120f
                ? "HYPOTHESIS SIGNAL: MET (3+ chains inside 2 min)"
                : player.SessionSeconds > 120f
                    ? $"HYPOTHESIS SIGNAL: window closed ({player.ComboChains}/3 chains)"
                    : $"HYPOTHESIS SIGNAL: pending ({player.ComboChains}/3 chains, {120f - player.SessionSeconds:0}s left)";
            lines.AppendLine(verdict);
        }
        if (enemy != null)
        {
            lines.AppendLine($"Enemy hits taken: {enemy.HitsTaken}/{enemy.HitsToKo}{(enemy.IsKo ? " (KO)" : "")}");
        }

        GUI.Label(new Rect(20, 16, 500, 290), lines.ToString(), _style);
    }
}
