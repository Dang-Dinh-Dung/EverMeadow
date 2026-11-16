<!-- PROTOTYPE - NOT FOR PRODUCTION -->
<!-- Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)? -->
<!-- Date: 2026-10-25 -->

# Concept Prototype Report: Combat Feel

> **Date**: 2026-10-24 → 2026-10-25
> **Prototype Path**: Engine (Unity 6.3 LTS, 6000.3.27f1)
> **Concept File**: design/gdd/game-concept.md

---

## Hypothesis

"If the player dodges → closes distance → lands 2-3 hits → retreats, with a
weapon paced slightly heavier than Hollow Knight but cancel-able into dodge,
combat will feel agile and weighted — evidenced by the player dodging an enemy
attack while it is actually active, and chaining 3+ dodge→hit→retreat cycles
within the first 2 minutes, unprompted."

The prototype was scoped to combat feel only. The day loop (farm → village →
forge → hunt → sleep) was split out as a separate, later prototype and is
**not** assessed here.

---

## Riskiest Assumption Tested

That "slightly heavier than Hollow Knight" was the right feel, and that it could
be reached by tuning numbers (hit-stop, cancel window, move speed). Pillars 2
(Your Build, Your Way) and 4 (Fewer Bosses, Each an Event) depend on it.

**Result: the assumption was wrong, and the fix was a design change, not just a
number.** The first playtest (~0.28s per hit) felt *slower* than Hollow Knight.
The developer's direction: base weapon speed must be **as fast as Hollow Knight
or faster**, and "fast vs. heavy" becomes a **build choice** (attack speed vs.
damage), not the baseline feel of the game.

---

## Approach

A self-building Unity scene: one component (`PrototypeBootstrap`) on an empty
GameObject creates the camera, ground, player, a dummy enemy and an on-screen
instrumentation readout from code. Solid-colour squares, no imported assets.

**Path chosen:** Engine
**Reason for path:** combat feel is the hypothesis; browser input latency
(50-133ms) would have misrepresented it.

**Shortcuts taken (intentional):**
- Hardcoded tuning values as public fields; no data files
- Placeholder art (procedurally generated coloured squares), no audio
- Dummy enemy with a fixed telegraph → active → recover loop, no AI
- OnGUI readout instead of real UI; no menus, no save, no game over
- Legacy `Input` class (project's Active Input Handling is "Both")

**Code location:** `Assets/_Prototypes/CombatFeel/Scripts/` (6 files). Unity
only compiles `Assets/` and `Packages/`, so the runnable code cannot live under
`prototypes/`; this directory holds the docs.

---

## Result

Five builds over two days (round 1, 2, 2b, 3, 4). Observations, in the
developer's words where given:

| Round | Developer observation | Change made |
|---|---|---|
| 1 | Ran first time. Dodge → attack → retreat "chạy tốt" (works well). Attacks ~0.28s/hit were **slower than Hollow Knight**. "Vẫn hơi bị khựng" — identified as **the character stopping dead on every swing**. | — |
| 2 | (replayed) | Base timing ~0.28s → ~0.20s/hit; movement kept at 60% speed while swinging; hit-stop double-trigger bug fixed |
| 2b | "Khi đang tấn công thì không ấn nhảy được" (couldn't jump while attacking) | Jump allowed mid-swing |
| 3 | Design rule stated: combat has **air attacks, dash attacks and landing attacks; no state locks attacking**. Developer chose to build all three as distinct moves. | Air attack, dash attack (attack during dodge), plunge/landing attack (Down + attack in air, fall hit + landing shockwave). Two instrumentation bugs fixed (below). |
| 4 | "**Cảm giác combat rất tốt**" (combat feels very good). Jump a bit low, dash a bit slow, dash **arced instead of going straight then falling**. | jumpForce 9.5 → 13; dodgeSpeed 10 → 14; dodgeDuration 0.22 → 0.20; gravity off during dodge |
| Final | "**Combat đã ổn về tất cả**" (combat is fine on every count). HUD showed **HYPOTHESIS SIGNAL: MET**. No surprises reported. | — |

**Instrumentation was broken for rounds 1-2, and this matters for reading the
result.** Found while building round 3:
1. The dummy enemy had **no collider**, so no player attack ever registered.
   Landed hits, chain count and the HYPOTHESIS SIGNAL line read 0 throughout
   rounds 1-2 — those rounds produced **no valid numeric signal**. The
   developer's feel observations from those rounds stand, since none depended
   on landing a hit.
2. The ground check raycast started inside the player's own collider (Physics2D
   "Queries Start In Colliders" is on in this project) and always reported
   "grounded", allowing infinite air jumps.

The **MET** signal comes from the final build only, after both fixes.

---

## Metrics

| Metric | Value |
|--------|-------|
| Path used | Engine (Unity 6000.3.27f1) |
| Iterations to playable | 1 (round 1 ran first time); 5 builds total |
| Prototype duration | ~1 working session across 2026-10-24 and 2026-10-25 (within the 1-day cap) |
| Playtesters | 1 internal (the developer); 0 external |
| Feel assessment | ~0.28s/hit felt slower than Hollow Knight; ~0.20s/hit with 60% move-while-swinging, jump-cancel and all four attack types: "rất tốt" / "ổn về tất cả" |
| Numeric signal | HYPOTHESIS SIGNAL: **MET** in the final build (self-reported; exact chain count not recorded). Rounds 1-2: invalid (see Result) |
| Hypothesis verdict | **PARTIALLY CONFIRMED** — the dodge → hit → retreat loop is confirmed; "one notch heavier than Hollow Knight" is refuted |

---

## Recommendation: PROCEED

The developer called PROCEED. The evidence supports it: the core combat loop
worked from round 1, the one real problem (pacing and rooted swings) was fixed
inside the prototype, and the final build met the hypothesis's numeric signal.
Two caveats keep this from being a strong result. There was one tester, and that
tester designed the game; a naive player has not touched it. And the signal was
only measured in the final build. Neither is a reason to stop, but both are
reasons to re-test feel with an outside player at `/vertical-slice`.

---

## If Proceeding

- **Core tuning values discovered** (starting points for the Combat GDD's
  Tuning Knobs; the developer did not report changing them in the Inspector):

  | Knob | Value | Note |
  |---|---|---|
  | Ground string | windup 0.04 / active 0.06 / recovery 0.10 s (~0.20 s/hit), 3 hits | ~0.28 s/hit was too slow |
  | Cancel lockout | 0.03 s into recovery | then attack or dodge can cancel |
  | Move while swinging | 60% of move speed | 0% ("rooted") felt like stopping dead |
  | Move speed | 6.5 units/s | not changed during testing |
  | Jump | jumpForce 13, gravityScale 3.2 (apex ~2.7 units ≈ 2 body heights) | 9.5 (~1.4 units) felt low |
  | Dodge | speed 14, duration 0.20 s (~2.8 units), cooldown 0.35 s, gravity off | 10 felt slow; arc felt wrong |
  | Air attack | 0.03 / 0.07 / 0.08 s, full air control | |
  | Dash attack | 0.02 / 0.10 / 0.14 s, carries 80% of dodge speed | |
  | Plunge | fall speed 18, landing shockwave radius 1.5, landing recovery 0.16 s | |
  | Hit-stop | 0.05 s at timeScale 0.08 on landed hits; 0.07 s at 0.05 on taking a hit | |
  | Hitstun | 0.25 s | the only remaining attack lockout |

  Character ≈ 0.9 × 1.3 units. Values are for a 2D side-view at orthographic
  size 5; rescale when the real pixel resolution (640×360 or 480×270) is chosen.

- **Assumptions confirmed:**
  - Dodge → hit → retreat as the 30-second loop (Core Loop, moment-to-moment).
  - Cancel-into-dodge as the responsiveness lever.
  - Hit-stop and hit-flash as the main sources of impact on placeholder art.

- **Assumptions disproved:**
  - "Nhanh như Hollow Knight nhưng chậm hơn một nhịp / nặng hơn một nhịp" —
    stated in `design/gdd/game-concept.md` (Core Loop › Moment-to-Moment, and
    Pillar 4's supporting combat-feel criterion). **Replace with:** base weapon
    speed as fast as Hollow Knight or faster; heavy vs. fast is a build axis.
  - Committed, rooted swings as the source of "weight".

- **Emergent mechanics (design rules worth formalizing in the Combat GDD):**
  - **No state locks attacking.** Ground, air, during a dodge (dash attack),
    and on landing (plunge). Hitstun is the one exception, kept by this
    prototype as a default, not decided by the developer — the GDD should
    decide it.
  - **Attack speed vs. damage as a sidegrade axis** — fits Pillar 2's design
    test ("khác hướng, không phải tốt hơn") and gives the weapon tree a
    concrete branching dimension.
  - **Jump is never locked by attacking.**
  - **Dash is a flat line**, then the fall begins.
  - Open design knob: does a dash attack keep the dodge's i-frames?
    (`dashAtkKeepsIframes`, prototyped as **off**.)

- **New risk to carry into the GDDs and architecture:** each weapon family now
  needs **four** move sets (ground string, air, dash, plunge) instead of one.
  This multiplies TD-FEASIBILITY's blocker #1 (moveset cost per weapon family,
  ~1,000-1,700 synced frames before this) and grows PR-SCOPE's balance matrix
  (every weapon × every boss now × four attack contexts).

**Next steps:**
1. Update `design/gdd/game-concept.md` with the disproved assumption and the new rules above
2. `/art-bible` (required before the Technical Setup gate at `full`)
3. `/design-review design/gdd/game-concept.md`
4. `/map-systems`
5. `/design-system combat` — use the tuning table above for Tuning Knobs and Formulas
6. Day-loop prototype (`/prototype` — the half split out of this one)

---

## Creative Director Review (CD-PLAYTEST)

**Verdict: CONCERNS** (2026-10-25). The developer accepted the recommendation
with the concerns noted; **PROCEED stands.**

The director confirmed the moment-to-moment loop, endorsed dropping "one notch
heavier" (rooted swings read as stutter, not weight), and found the speed-vs-
damage build axis a near-exact fit for Pillar 2. Four gaps were raised:

1. **"Weighted" is unvalidated.** The prototype confirmed *agile*; at base speed,
   weight comes only from hit-stop and flash on placeholder squares. The Combat
   GDD must state where weight lives (hit-stop, knockback, audio, enemy
   reaction), or the game drifts toward "Hollow Knight with a farm". Pillar 4's
   supporting criterion ("nặng hơn một nhịp") is now false and must be rewritten.
2. **Pillar 2 vs. Pillar 4 got sharper.** At HK-fast base with full mobility,
   slow/heavy weapons and branches face dense boss patterns with longer
   commitment, so "every weapon clears every boss" is harder to guarantee. With
   ~14 types kept, speed cannot be the main differentiator: **speed band belongs
   to a weapon family's identity; branches shift within that band.**
3. **Moveset scope ×4.** Keep "no state locks attacking" as a rule, but not every
   family needs four bespoke movesets: shared context moves (air, dash, plunge)
   with a family-specific look/effect, plus **one signature context per family**.
   TD and producer should cost this before `/map-systems` locks it.
4. **Evidence quality.** One tester, who is the designer; signal self-reported,
   no chain count recorded, final build only. An **outside-player feel test at
   `/vertical-slice` is a hard success criterion**, not optional.

Open items for the Combat GDD: does hitstun lock attacking; does a dash attack
keep i-frames (`dashAtkKeepsIframes`). Both affect boss readability under Pillar 4.

Day loop: combat GDD work need not wait for it, but the core fantasy ("a home to
return to") stays unvalidated until the day-loop prototype runs.

**Required follow-ups:** (a) rewrite Core Loop › Moment-to-Moment and Pillar 4's
supporting criterion in `design/gdd/game-concept.md`; (b) Combat GDD defines the
sources of weight and per-family speed bands; (c) scope review of the
four-context moveset; (d) outside-player feel test at the vertical slice.

---

## Lessons Learned

- **What assumptions were broken by actually building this?**
  "One notch heavier than Hollow Knight" read well in the concept doc and felt
  wrong in the hands within one round. Weight came from rooting the character,
  and rooting read as stutter ("khựng"), not weight.

- **What surprised us that didn't show up in the brainstorm?**
  The developer reported no surprises. On the build side: the instrumentation
  meant to measure the hypothesis was itself broken for two rounds, and nothing
  on screen said so — a reading of 0 looked the same as "the player did not do
  it". The scope also grew mid-prototype from one attack string to four attack
  types; that was the developer's call and it is why the moveset risk above
  exists.

- **What would we test differently next time?**
  Check that the instrumentation can register a positive (land one hit by
  hand, watch the counter move) before the first playtest. Record the actual
  counter values at the end of each round instead of the summary line. Get one
  player who has not seen the game.

---

> *Prototype code location: `Assets/_Prototypes/CombatFeel/Scripts/` (docs in `prototypes/combat-feel-concept/`)*
> *This code is throwaway. Never refactor into production.*
