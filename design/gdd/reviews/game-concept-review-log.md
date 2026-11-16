# Review Log — design/gdd/game-concept.md

## Review — 2026-10-25 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: XL (concept doc — cross-cutting; informational only)
Specialists: game-designer, systems-designer, economy-designer, narrative-director, unity-specialist, qa-lead; senior: creative-director
Blocking items: 8 | Recommended: deferred to GDDs/ADRs (see below)
Summary: Core identity, hook, five pillars, anti-pillars, scope tiers and prototype evidence hold — not MAJOR REVISION NEEDED. Three contracts contradict themselves (calendar, story spine, "every weapon clears every boss") and would propagate into every GDD via /map-systems; all are text fixes. 8-section GDD check not applicable (concept template).
Prior verdict resolved: First review
Findings:
- [BLOCKING] Core Loop / Anti-Pillars: calendar contract undefined; "về trước khi trời tối" (Short-Term) vs "NOT a time-pressure game"; season-gated content vs "never lose content"
- [BLOCKING] Pillar 2 / Long-Term Progression: no weapon catch-up principle — tier ladder is a dominant strategy against Expression
- [BLOCKING] Pillar 2 / Scope Risks: "every weapon clears every boss" undefined; balance-matrix arithmetic undercounts
- [BLOCKING] Pillar 3 / Core Mechanics: no critical-path input rule (farm/affinity-independent path); P3 design test always picks "connect"
- [BLOCKING] Target Aesthetics / Long-Term Progression: story spine contradiction (L69 vs L143); no premise stub or tone line
- [BLOCKING] MVP Definition: no fun gate (only schedule signals); outside-tester requirement not recorded
- [BLOCKING] Pillar 1 / Content Volume: discovery budget ~24-28 items vs ~16-19 sources, no allocation rule
- [BLOCKING] Open Questions: prototype items missing (hitstun, dash i-frames, speed bands, weight sources, internal resolution)
- [RECOMMENDED] Deferred to GDDs: Combat (chain/i-frame limits, hyper-armor, frame data, tick-based hit-stop); Weapon Tree/Crafting (≈3 tiers, shared material pool, Skill Stone bounds, power scalar); Economy (currency/faucets/sinks); Affinity/Narrative (negative-affinity consequences, NPC wants); Home (homecoming beat); QA plan (matrix simulation, family bots, pillar audits)
- [RECOMMENDED] Deferred to ADRs: Input System migration, asmdef for Assets/_Prototypes, integer game-time source, save format, UI Toolkit vs UGUI, pixel-perfect tooling, 2D physics settings, tuning in ScriptableObject/JSON, Addressables
- [RECOMMENDED] Engine reference gap: docs/engine-reference/unity/ has no 2D coverage (Physics2D, URP 2D, Pixel Perfect, UI gamepad nav, timeScale, serialization) — unity-specialist marked these NOT SOURCEABLE
Reviewed-Content-Hash: design/gdd/game-concept.md 5a20c1aea2a59542cc90c006f4181e90082ceee4
Reviewed-Content-Hash: design/registry/entities.yaml dfeacb02917f751cb1dfd88cf490c7ec3d8d2d0f
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: prototypes/combat-feel-concept/REPORT.md 96f607c2e58ef8f6d5f131a59e5e333b2572e83a

## Review — 2026-10-25 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: XL (concept doc — cross-cutting; the revision itself is S)
Specialists: game-designer, systems-designer, economy-designer, narrative-director, unity-specialist, qa-lead; senior: creative-director
Blocking items: 7 | Recommended: 9 (see below)
Summary: Re-review after the 8 prior blockers were revised. 5 of 8 resolved (calendar contract, catch-up principle, main-path rule, discovery budget, prototype open questions); 3 partial (every-weapon-clears-every-boss, story spine, fun gate). The new World Premise is a strong addition but the Core Fantasy (L38) now contradicts it. All 7 remaining blockers are text fixes; a lean re-check of those items is enough, the full panel need not run again.
Prior verdict resolved: Partially (5 resolved, 3 partial)
Findings:
- [BLOCKING] Core Fantasy L38: "người duy nhất tìm ra" contradicts premise (L60/L67); rewrite as the one the gods choose to return the weapons to
- [BLOCKING] World Premise / Long-Term Progression (L60, L86, L168): final boss undefined vs "thần không chết"; state that defeat = recognition and that bosses can be re-challenged (Key Dynamic L92 depends on it), or carry it as an owned Open Question
- [BLOCKING] Pillar 2 (L199, L226): vulnerability window needs margin beyond the slowest swing; ranged/catalyst need an equivalent opening; add P2↔P4 tie-break (change the boss, don't nerf the weapon; tune to median family)
- [BLOCKING] Pillar 2 (L197): define "highest tier reached" as a global ceiling unlocked by boss progress; catch-up cost is a bounded fraction (number in Weapon Tree GDD)
- [BLOCKING] Scope Risks (L341): arithmetic mixes counting models; correct is 288-672 (no branches) / 864-2,016 (3 branches); state or mark uncounted Full Vision
- [BLOCKING] MVP Definition (L368-371): one n (>=5) with k/n thresholds; remove "ngưỡng có thể chỉnh"; freeze before testing; define what a fail triggers
- [BLOCKING] Factual fixes: L264 (only 640x360 scales cleanly to 1080p and 1440p; 480x270 is 5.33x at 1440p); L156 vs L163 (evening/night ordering); L343 vs L406 (second-weapon threshold)
- [RECOMMENDED] Visual Identity Anchor L243: reconcile "sương rút cạn màu" with mist as the gods' breath; keep festival colour clear of the boss hazard colour
- [RECOMMENDED] World Premise L60: do not read as a farming gate; NPC-held weapons are never main-path; not every boss returns a weapon
- [RECOMMENDED] Calendar L133 vs L349: check "small fixed time cost" option against "không mất ngày"; NPC appointments and date events must recur
- [RECOMMENDED] Hint system: add to MVP and tier tables
- [RECOMMENDED] L394 vs L346: say where weapons from a cut region move to
- [RECOMMENDED] Producer pass: re-estimate moveset cost after paper-doll decision (L343); sanity-check tier timelines (MVP→Demo longer than Demo→EA)
- [RECOMMENDED] H1 tests only the day/night loop; seasons untested until Demo
- [RECOMMENDED] Housekeeping: owners on Open Questions; L311 repeats "12-14"; L67 double negative; keep the premise light (Narrative is ranked 6th)
- [RECOMMENDED] Deferred to GDDs: Calendar/Time (boss-loss mechanism, night-drop multiplier, out-of-season growth, non-boss deaths); Combat (hitstun, i-frames, TTK bound); Weapon Tree (catch-up k, gating materials, speed bands); Economy (currency, sinks, post-credits, non-farm route cost); Farming (scarcity lever); Discovery/Quest (hint system); Narrative (Mist Village, per-boss ritual); QA plan (H2 as observed behaviour, reachability test)
- [RECOMMENDED] Deferred to ADRs: time domains (game clock vs timeScale/hit-stop); pixel-perfect camera + Cinemachine; asmdef for _Prototypes + Input System; save format; kinematic controller + fixed timestep; Addressables; MonoBehaviour not DOTS. Refresh 2D coverage in docs/engine-reference/unity/ first
Reviewed-Content-Hash: design/gdd/game-concept.md 926d35278f387739dd65b6b32df4b21b0b65a3b1
Reviewed-Content-Hash: design/registry/entities.yaml dfeacb02917f751cb1dfd88cf490c7ec3d8d2d0f
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: prototypes/combat-feel-concept/REPORT.md 96f607c2e58ef8f6d5f131a59e5e333b2572e83a

## Review — 2026-10-25 — Verdict: APPROVED
Review mode: lean · Workflow: full
Scope signal: XL (concept doc — cross-cutting; informational only)
Specialists: none (lean)
Blocking items: 0 | Recommended: 8
Summary: Re-review after the 7 remaining blockers were revised. All 7 resolved (Core Fantasy vs premise, final-boss/recognition rule, P2 vulnerability margin and P2↔P4 tie-break, global tier ceiling, scope arithmetic 288-672 / 864-2,016, frozen n=5 fun gate, factual fixes). Calendar, story spine and "every weapon clears every boss" contracts are now consistent; remaining items are staleness and housekeeping.
Prior verdict resolved: Yes (7 of 7)
Findings:
- [RECOMMENDED] Open Questions L358 / Next Steps L418 / Art Style L309: internal resolution and /art-bible are stale — art bible draft fixes 640x360, 48 px player, HD-2D direction
- [RECOMMENDED] MVP / Scope Tiers: hint system (mandatory per Pillar 1) absent from MVP list and tier table
- [RECOMMENDED] Scope Risks L348: say where weapons from a cut region move to
- [RECOMMENDED] Scope Tiers: MVP→Demo (8-10 mo) vs Demo→EA (4-6 mo) timeline and moveset cost need producer re-check
- [RECOMMENDED] Visual Identity Anchor L245: reconcile "mist drains colour" with mist as the gods' breath; keep festival colour distinct from boss hazard colour
- [RECOMMENDED] World Premise L60: not every boss returns a weapon; NPC-held weapons never main-path
- [RECOMMENDED] Open Questions L351: remove or mark rejected the "fixed small time cost" option vs "không mất ngày"
- [RECOMMENDED] MVP: seasons untested until Demo — record as known risk
- Nice-to-have: L313 duplicated "12-14", L67 double negative
Reviewed-Content-Hash: design/gdd/game-concept.md 8877151bc47575bf48d69acf27bfb0d9122468d6
Reviewed-Content-Hash: design/registry/entities.yaml dfeacb02917f751cb1dfd88cf490c7ec3d8d2d0f
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: prototypes/combat-feel-concept/REPORT.md 96f607c2e58ef8f6d5f131a59e5e333b2572e83a
Reviewed-Content-Hash: design/art/art-bible.md 90960157f980c809f03dccbb1666941928e93b75
