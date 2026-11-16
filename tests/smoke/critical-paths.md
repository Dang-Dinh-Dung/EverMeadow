# Smoke Test: Critical Paths

**Purpose**: Run these 10-15 checks in under 15 minutes before any QA hand-off.
**Run via**: `/smoke-check` (which reads this file)
**Update**: Add new entries when new core systems are implemented.

## Core Stability (always run)

1. Game launches to main menu without crash
2. New game / session can be started from the main menu
3. Main menu responds to all inputs without freezing

## Core Mechanic (update per sprint)

4. Input (ADR-0001): keyboard and gamepad both drive the same actions in the test scene; switching device mid-play swaps prompt glyphs the same frame and never jams input
5. Input: closing a menu/dialogue with E or Space never triggers Attack/Jump in gameplay (mode barrier)
6. [Character Controller — add when implemented: move, jump, double jump, dodge, air dodge on flat ground and slopes]

## Data Integrity

7. Save game completes without error (once save system is implemented)
8. Load game restores correct state (once load system is implemented)

## Performance

9. No visible frame rate drops on target hardware (60fps target)
10. No memory growth over 5 minutes of play (once core loop is implemented)
