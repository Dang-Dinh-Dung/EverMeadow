<!-- PROTOTYPE - NOT FOR PRODUCTION -->
<!-- Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)? -->
<!-- Date: 2026-10-24 -->

# Concept Prototype: Combat Feel

**Code location (Unity compile constraint):** this skill's convention is
`prototypes/[name]-concept/`, but Unity only compiles `Assets/` and
`Packages/` (`.claude/docs/directory-structure.md`). The runnable code lives
at **`Assets/_Prototypes/CombatFeel/Scripts/`**; this directory holds the
required docs (`README.md`, later `REPORT.md` / `PIVOT-NOTE.md`) per
`.claude/rules/prototype-code.md`.

## Hypothesis

> If the player dodges → closes distance → lands 2-3 hits → retreats, with a
> weapon paced slightly heavier than Hollow Knight but cancel-able into
> dodge, combat will feel agile and weighted — we'll know this is true if the
> player dodges an enemy attack while it is actually active (not just
> "pressed dodge at some point"), and chains 3+ dodge→hit→retreat cycles
> within the first 2 minutes of play, unprompted.

**Riskiest assumption:** "slightly heavier than Hollow Knight" is a concrete
feel that has to be tuned by numbers (hit-stop duration, cancel-window
timing, move speed), not settled on paper. Pillars 2 (Your Build, Your Way)
and 4 (Fewer Bosses, Each an Event) in `design/gdd/game-concept.md` both
depend on this feel landing.

## What this is

- 1 playable character: move, jump, a 3-hit melee string, a dodge with
  i-frames, cancel-into-dodge from a defined recovery window.
- 1 stationary dummy enemy with a telegraph → active → recover loop (no AI) —
  something real to read and punish.
- Hit-stop, hit-flash, on-screen instrumentation. No menus, no real HUD, no
  save, no second weapon, no final art/audio — all explicitly cut per the
  Phase 4 plan in `production/session-state/active.md`.

## How to run it

1. Open the project in Unity 6.3 LTS (`6000.3.27f1`, per `project.yaml`).
2. In the Editor: **File → New Scene** → Basic (2D). Do not use `SampleScene`.
3. In the new scene, create one empty GameObject (**GameObject → Create Empty**).
4. Add the `PrototypeBootstrap` component to it
   (`Assets/_Prototypes/CombatFeel/Scripts/PrototypeBootstrap.cs`).
5. Press Play. The script builds the camera, ground, player, dummy enemy and
   the on-screen readout entirely from code — nothing else to wire up.

**Controls:** A/D move, Space jump, J (or left mouse) attack, Left Shift dodge.

## What to watch for

- Does chaining dodge → attack → retreat feel like *your* choice, or does it
  feel like you're fighting the cancel window?
- Does the dummy's telegraph give you enough time to read and react, or does
  it feel unfair / too slow?
- Read the on-screen counters (session timer, landed hits, successful
  dodges, chain count) — the HUD also prints a live
  `HYPOTHESIS SIGNAL: met / pending / window closed` line.
- Try changing the tuning constants at the top of `CombatFeelPlayer.cs`
  (`windup`, `active`, `recovery`, `cancelLockout`, `dodgeDuration`,
  `moveSpeed`) between runs — that back-and-forth IS this prototype's job.

## Status

Concluded 2026-10-25 — **PROCEED**. Findings: [REPORT.md](REPORT.md).

## Sourcing note

`Rigidbody2D.linearVelocity` (not `.velocity`) is used throughout
`CombatFeelPlayer.cs`. This is not covered in
`docs/engine-reference/unity/modules/physics.md` (sourcing-gap, not
filled from training data); confirmed live against Unity's 6000.3 scripting
reference during this session: `linearVelocity` exists and is documented as
"the rate of change over time of the Rigidbody2D position in world-units"
(https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D-linearVelocity.html).
