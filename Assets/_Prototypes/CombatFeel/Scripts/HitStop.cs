// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// Global hit-stop: brief timescale freeze on a landed hit. This is one of the
// three feel knobs the hypothesis says must be tuned numerically (the other
// two are the dodge/cancel window and move speed, both in CombatFeelPlayer).
//
// Round 2 fix (2026-10-25): always restore to NormalScale. The first version
// captured Time.timeScale at trigger time, so a second trigger during an
// active hit-stop captured the slowed value and "restored" to it — leaving the
// game stuck in slow motion.

using System.Collections;
using UnityEngine;

public static class HitStop
{
    const float NormalScale = 1f;

    static Coroutine _active;
    static MonoBehaviour _runner;

    public static void Trigger(MonoBehaviour runner, float duration = 0.06f, float scale = 0.05f)
    {
        if (_runner != null && _active != null)
            _runner.StopCoroutine(_active);

        _runner = runner;
        _active = runner.StartCoroutine(Run(duration, scale));
    }

    static IEnumerator Run(float duration, float scale)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = NormalScale;
        _active = null;
    }
}
