# Review Log — design/gdd/game-time-calendar.md

## Review — 2026-10-26 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: L (Fun-Gate slice ≈ 4–5 days only with Build-now/Stub/Defer cuts)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, gameplay-programmer; senior: creative-director
Blocking items: 13 | Recommended: 14
Summary: Core model is sound (one integer clock, pause tokens, ordered day pipeline, lazy catch-up; F1–F13 recompute correctly). Revision needed for the restore-token contradiction, missing state transitions, event/save ordering, the over-promising quit/crash contract (Rule 15a), the Continue slot rule, and numeric fixes (H1 ceiling s ≤ 1.0). Biggest open issue is concept-level: a 5–15 min trip cannot fit the 5-segment day within H1 — CD recommends reframing the day as one outing per day and a non-time defeat cost in the Fun-Gate build.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Core Rules / Formulas F12: day length vs concept trip length vs H1 — needs user decision D-1 (CD rec: reframe day as one outing into dusk; update concept Session-Level)
- [BLOCKING] Rules 6/16/19, T3/T10: restore token wiped by T10; fix with Time-owned epoch restore handle; arena retry skips Restoring; legal exit when n=0
- [BLOCKING] Rule 6 / AC-R06.4: "auto-release same frame" not buildable — reword to end-of-frame via IDisposable + liveness sweep; scene unload/exception wording
- [BLOCKING] States and Transitions: add DayTransition→Restoring, Restoring→Uninitialized; T1 under Region Loading token; reject T8 during catch-up; present as orthogonal fields
- [BLOCKING] Rule 11/12, F1: stage 2 is sole emitter of segment events on both paths; F1 next_dawn clamp is dead code; T7/RequestSleep set pending flag; day = N+1 before phases stated
- [BLOCKING] Rule 12 stage 8: DayStarted/SeasonChanged/YearChanged notification-only (no persistent mutation); emit then release
- [BLOCKING] Rule 2: closed list of clock-write paths (restore/migration, stage 2, AdvanceBy for defeat, debug); fix Rule 15→16 citation; move stray "(Tạm thời…)" tail to Rule 15; Rule 15 mentions in-place save_retry_count
- [BLOCKING] Edge Cases › Continue: L570 "newest" vs L580 "max game_minute"; use last-written slot by monotonic save-sequence number
- [BLOCKING] Rule 15a: orderly quit only; abnormal exit bound set by Save/Load; quit during DayTransition loses phases — needs user decision D-3
- [BLOCKING] Edge Cases degenerate table / Dependencies: free die-to-go-home invalidates Fun-Gate night-risk measurement — Defeat & Retry must ship a non-time cost; user decision D-2
- [BLOCKING] F1 / Rule 16: NaN passes Math.Max → permanent freeze; restore rejects negative/overflowing game_minute
- [BLOCKING] F12 / Tuning Knobs / AC: H1 ceiling s ≤ 1.0 not 1.1; AC-PT3 u ≤ 0.36; AC-R15a.1 fixture 540 not 900; AC-F01.4/AC-V02 clamp out of range (validator hard bounds are authority); AC-E46 rewrite; Player Fantasy line 48 stale (D=28)
- [BLOCKING] Open Questions: OQ-1..OQ-4 due before first Time story; add time/app-lifecycle/object-lifetime module to docs/engine-reference/unity/ (blocks coding, not approval)
- [RECOMMENDED] Detailed Design: synchronous pipeline at Fun-Gate; tag async-only edge cases/ACs (E10, E13, E14, E15, E23)
- [RECOMMENDED] Scope: Build-now/Stub/Defer split (defer F8–F11, F13, season ordering, Rule 19 specifics, watchdog, timeout)
- [RECOMMENDED] Interactions: read-only fractional time for lighting/audio only
- [RECOMMENDED] UI: "Ngủ rồi thoát" bed option
- [RECOMMENDED] Edge Cases: rollover and sleep grant identical benefits; stage-7 hitch budget in OQ-5
- [RECOMMENDED] Rule 9 / validator: every segment needs a consumer; minimum segment length and non-Night segment ranges
- [RECOMMENDED] Edge Cases › reroll: seed = hash(world_seed, day, system, entity)
- [RECOMMENDED] Open Questions OQ-6: U3 migration must convert dependents' last_updated
- [RECOMMENDED] Rule 8: dev-only toggle for focus token (Editor noise)
- [RECOMMENDED] OQ-1: integer tick accumulator; exact-value ACs rewritten after
- [RECOMMENDED] Acceptance Criteria: UI BLOCKING tags, "documented playtest", stub callers for C01–C13, missing coverage (crash, save retry 0/3, validator ranges, F7 boundary, U3, UI contracts)
- [RECOMMENDED] Edge Cases: leaked-token sleep escape lasts one day — document or clear stale tokens at bed
- [RECOMMENDED] Registry: rename night_start_clock → segment_start_clock[Night]; add max_real_delta, seasons_enabled, sleep_target, defeat_wake_minute, F13 queries
- [RECOMMENDED] Acceptance Criteria H.6: split PT1 question, behavioural metrics, forced dusk scenario — user decision D-4
Open user decisions: D-1 day rhythm vs trip vs H1 · D-2 Fun-Gate defeat cost · D-3 crash loss bound · D-4 status of fantasy check (AC-PT1)
Disagreements: 18:00 earliest sleep — CD keeps D3 (measure, don't redesign; user may reopen) vs game-designer drop/move
Reviewed-Content-Hash: design/gdd/game-time-calendar.md 14e1c22c884415bb33166bb16e0a16c0b09cd43d
Reviewed-Content-Hash: design/registry/entities.yaml 1d11ecae9d9f52044cb7035c3ceeb1362436e888
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/game-concept.md 8877151bc47575bf48d69acf27bfb0d9122468d6

## Review — 2026-10-26 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: XL by rubric (cross-cutting Foundation, ~20 dependents, 4 ADRs before code); Fun-Gate slice ≈ L, 7–9 days (producer to re-estimate)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, gameplay-programmer; senior: creative-director
Blocking items: 9 | Recommended: 9
Summary: Re-review. Clock model and D-1–D-4 hold; the 13 prior blockers are addressed. Nine contract defects remain, led by boundary events that report the wrong state (stage 2 and AdvanceBy), undefined bounds (MAX_GAME_MINUTE, min_segment), false guarantees (Rule 6 liveness, world_seed vs "only state", AC-PF4), and missing night-reward/defeat-bag constraints the Fun-Gate night reading depends on.
Prior verdict resolved: Yes (13/13 prior blockers addressed in text)
Findings:
- [BLOCKING] Rule 11 / stage 2 / F7: skipped-boundary events emitted after clock set to new-day 06:00 (Evening→Night handler sees IsNight=false); AdvanceBy crosses boundaries/dawn without emitting — payload (from,to,boundary_minute) or step-by-boundary; same rule for AdvanceBy
- [BLOCKING] Rule 9 / validator / Tuning Knobs: min_segment_minutes knob 60–180 vs hard-coded 120; Night range 19:00–22:00 unreachable with default Evening 18:00
- [BLOCKING] Rule 16 / F2: MAX_GAME_MINUTE has no design value (≤ typeMax − 2·1440); AC-R16.2/F02.1 depend on it
- [BLOCKING] Rule 6 / States: no closed TokenKind set (Busy undecidable); undefined: load while Restoring, new game from Active, quit/safe-point save during catch-up, T10 with pending flag, Focus token across epoch bump — add state × command table
- [BLOCKING] Rule 6: "leak ≤ one tick" only holds for UnityEngine.Object owners — reword by owner kind
- [BLOCKING] Rule 1 / Edge Cases reroll: world_seed must be saved, contradicts "only state is game_minute"; owner + fixed hash algorithm
- [BLOCKING] Dependencies / H.6: record night-reward (kind not quantity; nothing progression-critical night-only) and defeat-bag constraints ("unstored" defined; death never cheaper than walking home) as required items for Drops/Enemy/Defeat GDDs; AC-PT1/4/5 night reading conditional on them
- [BLOCKING] Rule 12 / AC-PF4: one-frame pipeline with stage-7 I/O vs every-frame ≤16.6 ms — state invariant (seal at stage 7, token held until write resolves, stage 8 after); exclude/budget transition frame
- [BLOCKING] Acceptance Criteria H.4: split cross-system ACs — Time-output ACs via stub stay in DoD; consumer-behaviour ACs tagged Defer
- [RECOMMENDED] F12: 5–15 min trip claim holds only for 14:00 departure (11:00 overnight at s=0.9 = 17.1); add travel/loading budget; u_max < 0 at s > 1.0 → H1 fails outright; 02:00 sleepers reach Day 4 at 63.5 min
- [RECOMMENDED] Player Fantasy line 43: scope "each segment differs" to VS+; Fun-Gate tests Night only
- [RECOMMENDED] Rule 9: presentation consumer (Audio) satisfies Noon's consumer rule at Fun-Gate
- [RECOMMENDED] H.6: operational H1 window; close OQ-9; AC-PT2 min/max; log NotSleepy refusals
- [RECOMMENDED] Rule 15a: state multi-day crash loss capped at one day (dawn autosave)
- [RECOMMENDED] F1: double/integer accumulator; ~12 exact-value ACs rewritten as observable behaviour (OQ-1)
- [RECOMMENDED] Acceptance Criteria: qa-lead rewrites (E28b, E34, R12.5, R12.8, R15a.1/.3, async N/A, UI evidence names) + coverage gaps (Rule 2 list, Rule 22 newer version, sleep under Region token, min_segment validator, Visual/Feel ACs)
- [RECOMMENDED] Coding-blocking (ADR/OQ): composition root vs statics; AdvanceBy enforcement via assembly not reflection; PlayMode in project.yaml test command; per-handler try/catch event API; H.5 budgets
- [RECOMMENDED] Registry: next_occurrence unit mismatch (F13c minutes); register DAYS_PER_WEEK, SEASONS_PER_YEAR, segment defaults, min_segment_minutes, MAX_GAME_MINUTE
Rulings (creative-director): earliest sleep stays 18:00 (log NotSleepy, OQ-12 stays open to user); night/bag content downstream but constraints recorded here; save I/O mechanism → ADR, GDD states invariant; cross-system ACs split with Defer; remove 4–5-day claim, producer re-estimates (7–9 credible)
Reviewed-Content-Hash: design/gdd/game-time-calendar.md e9e96b22f12a14df27ed2d54e5a983520b73fded
Reviewed-Content-Hash: design/registry/entities.yaml 832b46f8f9e8737326ee44c11db72cc523867c2a
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/game-concept.md d5ef42f151469a2a57479dc1412b7a526d371cc3
Reviewed-Content-Hash: design/gdd/systems-index.md c29fab48e4db814a16bbf2a3eb7b83bc251a226e

## Review — 2026-10-26 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: S for this revision (~8 local text edits); system remains XL by rubric (cross-cutting Foundation, ~20 dependents, 4 ADRs before code)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, gameplay-programmer; senior: creative-director
Blocking items: 8 approval + 6 coding/pre-playtest | Recommended: 8
Summary: Pass 3. Clock model, F1–F13 (all recomputed clean by systems-designer), pipeline order and D-1–D-4 hold; 9 pass-2 blockers addressed. Approval blocked by internal contradictions (AC-R12.8 fixture, T3 epoch vs restore handle, token_count definition) and three missing player guarantees (no permanent freeze on handler exception, no lost day/mid-load save, no active-region reset on mid-trip rollover), plus Rule 15a over-promising vs engine quit semantics. CD anti-churn ruling: pass 4 checks only the 8 approval items, no new specialist sweep; GDD scope frozen to fixes.
Prior verdict resolved: Yes (9/9 pass-2 blockers addressed in text)
Findings:
- [BLOCKING] Acceptance Criteria H.1 Rule 12: AC-R12.8 fixture 1500 (07:00) expects Accepted; F6 gives NotSleepy
- [BLOCKING] Core Rules Rule 6 / States: T3 listed as epoch bump orphans restore handle; Focus re-grant at T11 lands in Uninitialized; need is_focused independent of tokens
- [BLOCKING] States and Transitions: token_count definition (includes Time-issued tokens; pending ⇒ n ≥ 1); T7/T8/T9 n values; Active vs DayTransition before stage 1
- [BLOCKING] Core Rules Rule 11/12: event handlers not exception-isolated; DayTransition token must be released even if a handler throws
- [BLOCKING] Core Rules Rule 15a: "never loses progress" only for engine-deferrable quits; other OS quits → safe-point tier; quit during catch-up must not hang
- [BLOCKING] Core Rules Rule 12: stage 7 must never save while RegionLoad/Restore token held
- [BLOCKING] Core Rules Rule 12: in-place rollover phases must not mutate entities of the active region (stage 3 respawn vs L170, Rule 14)
- [BLOCKING] Core Rules Rule 6/8: ScriptableObject owners have no liveness; Focus token is best-effort over the clamp
- [BLOCKING-CODE] Open Questions OQ-3: re-entrancy, intra-frame order, game_minute committed before events
- [BLOCKING-CODE] Open Questions OQ-2: no static state / Enter Play Mode without domain reload; pin Run In Background
- [BLOCKING-CODE] Acceptance Criteria: test hooks to set acc and read real_eff
- [BLOCKING-CODE] Acceptance Criteria: AC reclassification (type a/b, UI and PF Defer), Time-owned quit/safe-point ACs, 42-cell state×command test, MAX_GAME_MINUTE saturation AC
- [BLOCKING-CODE] Open Questions OQ-11: hardware spike checklist incl. Steam Deck
- [PRE-PLAYTEST] Acceptance Criteria H.6: danger/bag bucket, knowledge probe, positive "an tâm" probe, PT5 "uninformed" tag
- [RECOMMENDED] Formulas F12: u_max hard-codes 60 (negative for 30-min session, s=1.5, large N); missing variable rows
- [RECOMMENDED] Formulas F12 / Tuning Knobs: 5–15 min fit exact only for 14:00 departure; real_min_outing negative across dawn
- [RECOMMENDED] Edge Cases L692 / registry next_occurrence / F9–F10: unit and table fixes
- [RECOMMENDED] Formulas F1/F6: saturation at MAX_GAME_MINUTE
- [RECOMMENDED] Dependencies: N5 homecoming shows elapsed growth; Visual/Feel AC for dawn cue
- [RECOMMENDED] Detailed Design Rule 9: document night exposure of D-1 trips
- [RECOMMENDED] Acceptance Criteria: duplicates/misplaced ACs, evidence filenames, save-fail toast screenshot
- [RECOMMENDED] Open Questions: never tick from FixedUpdate; fade opaque before pipeline frame; phases load no assets; no events into mid-activation scenes
Rulings (creative-director): game-designer rollover-payoff → N5 recommended (lazy growth on return); active-region mutation → blocking; H.6 changes pre-playtest not approval; hour-based progress does not stop sleep-to-skip (18:00 gate + OQ-12 is the control; reword OQ-8); qa-lead classification → coding-blocking except AC-R12.8; no D-5 (Rule 15a narrowing stays within D-3 tier 2 — user confirmed wording: quit during catch-up exits immediately, no continue save)
Revision applied same session: all 8 approval items + items 9–14 (F1 saturation term added; recommended items otherwise not applied)
Receipt note: target doc intentionally NOT stamped — it was revised after this review, so the next run must see it as CHANGED/NEW and re-review (pass 4: 8 approval items only, per CD).
Reviewed-Content-Hash: design/registry/entities.yaml a8abc105d0163851d8a5a32cca47f1685e9d4180
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/game-concept.md d5ef42f151469a2a57479dc1412b7a526d371cc3
Reviewed-Content-Hash: design/gdd/systems-index.md 922338414564e60b9fc22c0ec6e81d169375fb25

## Review — 2026-10-27 — Verdict: NEEDS REVISION
Review mode: lean · Workflow: full
Scope signal: S for this revision; system XL by rubric
Specialists: none (lean)
Blocking items: 4 | Recommended: 5
Summary: Pass 5a (lean). Note: the pass-4 entry (2026-10-27, 4 blocking, Revision 4 in the GDD header) was never appended to this log; Revision 4 edits were verified present in text. Four small blockers found and fixed same session.
Prior verdict resolved: Yes (pass-3 approval items 8/8 in text; Revision 4 edits present)
Findings:
- [BLOCKING] Interactions / Dependencies: Interaction & Context Prompts (bed, sole sleep caller) missing — added to both tables
- [BLOCKING] systems-index.md L48: Defeat & Retry stub said "giữ đồ", contradicting Rule 18 / D-2 — synced
- [BLOCKING] Rule 12 / States: invariant "cờ chờ ⇔ token DayTransition" false between stage 1 and 8 — now ⇒
- [BLOCKING] Rule 2 path 3: AdvanceBy had no precondition — rejects > 30 (user choice), AC-R02.6 added
- [RECOMMENDED] F1 lacks lifecycle gate; CanSaveContinue ignores Defeat token; registry gaps; pass-4 log entry missing; doc size ~200 KB

## Review — 2026-10-27 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: XL by rubric (~21 dependents, 4 ADRs before code); Fun-Gate slice L (7–9 days called optimistic — producer to verify)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, gameplay-programmer; senior: creative-director
Blocking items: 5 approval + BLOCKING-CODE list | Recommended: ~20
Summary: Pass 5b. Clock model, formulas and state table hold; CD applied pass-4 anti-churn filter (approval blocks only for internal contradictions or ACs that fail a correct implementation). Five one-line fixes block approval; all fixed same session. CD ruling: pass 6 = main reviewer checks only these 5, no specialist re-sweep; APPROVED if made as specified.
Prior verdict resolved: Yes (lean pass-5a blockers fixed in text)
Findings:
- [BLOCKING] Formulas F1: zero-condition lacked lifecycle ≠ Active (clock would tick in Uninitialized) — fixed + variable row
- [BLOCKING] States › state × command table: "chờ T9" undefined in sync pipeline (deadlock) — now queued, runs right after T9
- [BLOCKING] Acceptance Criteria AC-F02.1: MAX_GAME_MINUTE is 08:07, sleep not allowed — asserts pure F6 function; saturation never reaches 18:00
- [BLOCKING] Acceptance Criteria conventions: batch-mode CI unfocused → n = 2 — fixtures inject is_focused = true (H-9)
- [BLOCKING] Formulas F12: "all outputs positive" false for u_max (−0.275 at s = 1.5) — u_max ≤ 0 ⇒ H1 unreachable
- [BLOCKING-CODE] ADRs: re-entrancy + intra-frame order (OQ-3), Restore signature + API surface list, fake-null owner == null, Update + DefaultExecutionOrder, Application.isFocused at T1/T2, static events without domain reload, sync save on Deck SD vs token-held-until-write, asmdef/InternalsVisibleTo, struct handle, F13b int32 near MAX, int tick accumulator (OQ-1); run OQ-11 spike before ADRs (recommended)
- [BLOCKING-CODE] Acceptance Criteria: AC-S17 explicit oracle table; inline Defer tags (+E14, C08, C03 hit-stop, C15 non-vacuous); rewrite R02.2, R02.1/R01.3, S09; sync-race ACs (Defeat token same frame as flag; sleep before tick)
- [PRE-PLAYTEST] H.6: question (c) "skipped something because the day was too short?", chores-skipped metric, bags lost to second defeat in AC-PT4(3), non-leading positive question
- [RECOMMENDED] Formulas errata: F5 real range 3.2–15.0; s thresholds 0.714 / 0.938; "earliest sleep 21:00 ⇒ night only" false when Night = 22:00; Restoring cancel labelled T3b; AdvanceBy(0) no-op
- [RECOMMENDED] Rule 15a: sync quit write fails → abnormal tier; D-3 bound assumes token discipline; per-kind watchdog for RegionLoad/Restore (dev error ~30 s, release telemetry)
- [RECOMMENDED] Dependencies: Defeat token in C3 / N3–N4 so quit mid-defeat cannot dodge the bag; crash-loss budget number (Save/Load); Farming/Crafting hour-based wording when Farming GDD written
- [RECOMMENDED] Acceptance Criteria: consolidate duplicates; hooks for Step(), pending flag, quit sink, runtime tuning; AC-E33 release handle first; E28b, R06.4 wording; Visual/Feel ACs Defer; UI evidence filenames
Rulings (creative-director): no D-5 — second-defeat bag loss is combat risk chosen in D-2 (merge bags may be raised in Defeat & Retry GDD); D-1 not reopened (long trips → overnight is the fantasy; measure via question (c)); "never force-release" stands (player "Khôi phục đồng hồ" rejected, debug only); qa-lead items: only Q2, Q3 block approval, rest BLOCKING-CODE via /story-readiness
Revision applied same session: all 5 approval blockers (GDD header Revision 6)
Receipt note: target doc intentionally NOT stamped — revised after this review; pass 6 must see it as CHANGED.
Reviewed-Content-Hash: design/registry/entities.yaml a8abc105d0163851d8a5a32cca47f1685e9d4180
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/game-concept.md d5ef42f151469a2a57479dc1412b7a526d371cc3
Reviewed-Content-Hash: design/gdd/systems-index.md 06bf41b3ec58f7360fdd8d1c77ed1caf0d30b4e1
