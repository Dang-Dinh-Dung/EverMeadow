# Gate Check: Concept → Systems Design

**Date**: 2026-10-25
**Checked by**: gate-check skill
**Config**: workflow `full`, review_mode `full`, qa.level `full`, team.size `studio` (all from `modes.rigor: full`)
**Director Panel**: 4 of 4 (creative, technical, producer, art)

### Required Artifacts: 3/3 present
- [x] `design/gdd/game-concept.md` — exists, ~31 KB
- [x] Game pillars — 5 pillars with design tests + 6 anti-pillars (in game-concept.md)
- [x] Visual Identity Anchor section — present in game-concept.md

### Recommended (not blocking)
- [x] Concept prototype with PROCEED — `prototypes/combat-feel-concept/REPORT.md` (combat feel only; CD-PLAYTEST CONCERNS, accepted)
- [ ] Day-loop prototype — none (core fantasy / Pillar 3 unvalidated)

### Quality Checks: 3/4 passing
- [ ] Game concept reviewed by `/design-review` — **never run** (no review report in `design/gdd/`)
- [x] Core loop described — all four levels (30 s, 5-15 min, session, long-term)
- [x] Target audience identified — Core Identity + Target Player Profile
- [x] Visual Identity Anchor has a one-line rule and ≥2 principles — 1 rule + 3 principles, each with a design test

### Observations
- `artifact-check.sh --phase concept`: `game-concept` PRESENT; `engine-setup` PRESENT; `art-bible` ABSENT; `map-systems` ABSENT; `brainstorm` and `design-review-concept` NO_CHECK (not detectable from disk — the latter confirmed absent by hand).
- **Catalog vs. gate divergence:** `.claude/docs/workflow-catalog.yaml` lists `/art-bible` and `/map-systems` as *required* Concept-phase steps; this gate's definition (`references/gate-systems-design.md`) does not require either. This report follows the gate definition. `/map-systems` is itself Systems Design work.
- Verified by re-reading `design/gdd/game-concept.md`: combat tempo is contradictory — lines 84 and 178 say "nhanh hơn một nhịp", line 126 still says "chậm hơn Hollow Knight một chút — mỗi đòn có trọng lượng", and the Inspiration table still says "Nặng hơn một nhịp". Lines 268, 277, 295 and 364 still describe Unity as unpinned / Godot as pinned (resolved by `/setup-engine`).

## Director Panel Assessment

Creative Director:  CONCERNS
  Pillars, core fantasy, hook, core loop, audience and Visual Identity Anchor all pass. Concerns: tempo wording contradicts itself (line 126 vs. 178); prototype rules ("no state locks attacking", attack speed vs. damage as a Pillar 2 build axis) not in the doc; the combat-feel criterion sits under Pillar 4 (bosses) but applies to every fight — move it to Core Loop / a Combat Feel subsection; `/design-review` never run; Pillar 3 unvalidated — make Calendar/Time the first `/design-system` and run a day-loop prototype soon. Recommends proceeding to `/map-systems`; fix tempo + add the two rules + `/design-review` before Combat or Weapon Tree GDDs.

Technical Director: CONCERNS
  Engine settled (6.3 LTS pin = installed 6000.3.27f1); the prototype proved the riskiest tech (kinematic controller, buffer, hit-stop, cancel windows) on this exact editor. Concerns: four-context moveset uncosted — systems index must not lock family count / animation architecture until costed (backs shared-context + one signature per family); concept doc's technical sections stale (lines 268, 277, 295, 364) and pacing contradictory (126, 240 vs. 84, 178); prototype facts to carry into GDDs (legacy `Input` + "Both" handling → Input System only in production; Physics2D "Queries Start In Colliders" is on — controller GDD must state the ground-check approach; tuning measured at ortho size 5 — rescale at chosen resolution; `Assets/_Prototypes/` compiles into the game — needs an asmdef/exclusion before any production build); boss-fight time rule decides Calendar/Time and save architecture; camera/parallax, save schema and day loop untested — prototype camera early.

Producer:           CONCERNS
  Nothing blocks entry. Concerns: four-context moveset uncosted and MVP not re-estimated — rough multiplier: fully bespoke ~2-3× art per family and a ~450-670-check balance matrix; shared + signature ~1.3-1.5×; MVP likely +1-3 weeks, threatening the "MVP in 4 months" and "second weapon ≤2 weeks" signals — TD + producer cost both options first, developer decides before `/map-systems` locks; team-size mismatch (`studio` vs. actual solo, first game) — timebox Systems Design (e.g. 4-6 weeks) and decide explicitly whether `standard` fits; `/design-review` not run; day loop unproven — short spike or mark Calendar/Farming/Save GDDs provisional; put weapon families and the time rule first in GDD order.

Art Director:       CONCERNS
  Visual Identity Anchor passes the quality check; art bible not a finding at this gate. Concerns: anchor's "shared animation set per family" is outdated by the four-context finding — record shared-context + family-specific look + one signature context as a constraint before Combat/Weapon scoping; internal resolution (640×360 vs. 480×270) still open and now a systems input (silhouettes at 1×, tuning rescale); at ~0.20 s/hit swings are mostly smear frames — Principle 2 must cover carry/idle pose and smear shape; element colour families, boss-hazard colour and player-VFX dimming are cross-system constraints for `/map-systems`. Recommends `/art-bible` before `/map-systems`, or at least lock resolution, animation budget per family/context and the paper-doll layering rule before `/design-system combat`.

### Blockers
None. No required check is NOT ASSESSED.

### Concerns
1. Concept not reviewed (`/design-review` never run) and internally contradictory on combat tempo (lines 84/178 vs. 126 and the Inspiration table).
2. Prototype rules missing from the concept doc: no state locks attacking (air / dash / plunge); attack speed vs. damage as a Pillar 2 build axis; combat-feel criterion should move out of Pillar 4.
3. Technical sections stale (lines 268, 277, 295, 364).
4. Four-context moveset uncosted; must be costed (TD + producer) and decided before `/map-systems` locks weapon-family structure.
5. Day loop / Pillar 3 unvalidated — Calendar/Time first in GDD order; day-loop prototype soon.
6. Team-size mismatch: config `studio` vs. actual solo full-time — developer to decide on rigor and a Systems Design timebox.
7. Internal resolution undecided; it feeds silhouettes and tuning rescale.
8. Catalog vs. gate divergence on `/art-bible`; art director recommends it (or its three Asset Standards decisions) before `/map-systems`.

### Recommendations
- Fix concept doc (concerns 1-3) → `/design-review design/gdd/game-concept.md` → re-run `/gate-check`.
- Cost the four-context moveset before `/map-systems`.
- `/art-bible` (or at least resolution, per-family/context animation budget, paper-doll layering) before `/design-system combat`.
- First GDDs: Calendar/Time, then weapon families.

### Verdict: CONCERNS
Chain-of-Verification: 5 questions checked (2 by tool: re-read game-concept.md lines 122-129 and 176-179; grep for stale engine/tempo lines) — verdict unchanged. Considered whether the missing `/design-review` should be FAIL: kept as CONCERNS because the gate lists it as a quality check, not a required artifact, and all four directors rated it non-blocking.

**Stage not advanced** — remains `Concept`. The user chose to fix the concept and run `/design-review` before re-running this gate.
