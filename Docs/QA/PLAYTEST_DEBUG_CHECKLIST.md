# Playtest Debug Checklist

## Why this exists

This checklist is for fast balance and QA loops while the project is still using temporary stage reload flow.
Use the debug overlay to skip repeated early-stage setup and focus on the target pressure point.
Preset values now come from [PlaytestDebugPresetCatalog.asset](/C:/UnityProjects/study/unity/Spelunky/Assets/Data/Debug/Resources/PlaytestDebugPresetCatalog.asset), so balance-side debug tuning can be adjusted without editing overlay code.

## Runtime controls

- `F1`: toggle debug overlay
- `F2`: dump current `RunState` to console
- `F5`: apply Stage 1 baseline preset (`4 HP / 4 Bomb / 4 Rope`)
- `F6`: warp to Stage 1
- `F7`: warp to Stage 2
- `F8`: warp to Stage 3
- `F9`: warp to Stage 4

Session control:

- `Begin New QA Session`: auto-exports the previous session if it contains runs, then resets session ID, start time, run type, preset label, session note, and future session overview/export scope
- `Session Build` / `Tester Tag`: stamp the current QA session so markdown exports can be compared by build and tester without manual edits

Overlay presets:

- `Stage 1 Baseline`: verify fair opening and safe onboarding
- `Stage 2 Rope Pressure`: verify rope value without making it mandatory every room
- `Stage 3 Bomb Pressure`: verify bomb shortcuts and route value
- `Stage 4 Clutch`: verify end-stage tension with low remaining resources

## Required direct tests

### 1. Warp stability

- Warp to each stage with `F6` to `F9`
- Confirm the player can move, jump, use rope, use bomb, and enter exit
- Confirm camera follow, HUD, and input lock states are still valid after warp

### 2. Forced resource sync

- Set `HP / Bomb / Rope` from the overlay
- Confirm HUD matches the forced values immediately
- Spend one bomb and one rope
- Confirm actual inventory and HUD both decrease from the forced values

### 3. RunState dump

- Press `F2`
- Confirm console output contains:
- current stage index
- current health / bombs / ropes / gold
- total run time
- current stage time

### 4. Death result logging

- Die once to any damage source
- Confirm the overlay shows a completed result summary
- Confirm `run-results.jsonl` path is shown
- Confirm historical overview and recent-run comparison panels update without malformed rows breaking the view
- Confirm session overview and recent-run stats update after each new run
- Confirm the latest log line includes:
- final stage
- total duration
- final resources
- end reason
- death cause
- stage split results

### 5. Run clear result logging

- Clear the temporary 4-stage loop
- Confirm a clear result is shown instead of a death result
- Confirm the log file receives a new clear record

### 6. QA summary export

- Press `Export QA Summary Markdown`
- Confirm `Docs/QA/LATEST_PLAYTEST_SUMMARY.md` is created or updated
- Confirm `Docs/QA/Reports/PLAYTEST_SUMMARY_*.md` archive file is also created
- Confirm `Docs/QA/Reports/STAGE_01_SUMMARY.md` to `STAGE_04_SUMMARY.md` are updated
- Confirm the file contains:
- session ID
- active preset
- session run type
- session note
- session build
- tester tag
- highlighted signals
- session overview
- full history overview
- recent run stats
- recent run list

### 7. Session reset

- Press `Begin New QA Session`
- Confirm the previous session is auto-exported when it already has recorded runs
- Confirm session overview is reset to runs recorded after the button press
- Confirm a new session ID is assigned
- Confirm active preset, run type, and session note are reset
- Confirm session build resets to `Application.version` fallback and tester tag resets to the local user fallback unless overridden again
- Confirm the next export reflects only the new session window in the session overview section

## Balance review prompts

Use these prompts right after each focused run:

- Did Stage 1 still read as safe enough to learn?
- Did Stage 2 make rope feel valuable instead of mandatory?
- Did Stage 3 make bomb usage feel like a meaningful route decision?
- Did Stage 4 feel like resource payoff rather than random chaos?
- Did the death feel deserved, or did it feel unreadable?

## Tuning reminder

If the run feels off, check in this order:

1. Stage length
2. Resource refill placement
3. Threat combination density
4. Enemy frequency
5. Trap readability
6. Reward temptation
7. Control assist values
8. Damage numbers

Do not use debug presets as a reason to retune player core movement first.
