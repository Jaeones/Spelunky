# Four-Stage QA Checklist

## Usage

Use this checklist during implementation, content lock, and release candidate testing.

Run types to log:

- first-clear style run
- cautious full-clear run
- greedy treasure run
- speed-focused run
- controller-only run
- keyboard-only run

---

## A. Core Systems

### Movement and collision

- Player never clips through solid tiles
- Player can consistently land on one-way platforms from above
- Player can drop through one-way platforms intentionally and only intentionally
- Ledge grab or hang features do not soft-lock the player
- Moving platforms do not desync player position
- Blocks cannot permanently trap the player without a death resolution

### Rope

- Rope placement never creates impossible-to-climb visuals
- Rope throws are consistent from ground and air
- Rope pile pickups update inventory immediately
- Rope can enable recovery, not trivialize every vertical challenge
- Rope placement cannot bypass the final stage entirely

### Bomb

- Bomb throws respect player facing and momentum
- Bomb fuse timing is readable
- Bomb explosions damage intended targets reliably
- Bombs cannot permanently destroy mandatory exits or hard-lock progression
- Bomb economy remains meaningful across the full run

### Combat and damage

- Stomp damage triggers only when the player clearly lands from above
- Contact damage does not double-apply in one instant
- Invulnerability frames are visible and reliable
- Whip hitbox timing feels consistent
- Enemy death and player death never leave lingering invalid states

### Game flow

- Entering an exit always transitions cleanly
- Full run start-to-finish works without editor intervention
- Game over always resolves to a valid restart path
- Resetting a run cleans run state properly

---

## B. Stage Identity

### Stage 1: Entry Shaft

- Stage reads as approachable within 30 seconds
- Player sees at least one safe treasure and one risky treasure
- First trap encounter is readable before lethal commitment
- First rope decision occurs naturally, not as a forced gimmick
- Stage clear time lands in target window

### Stage 2: Hanging Caverns

- Verticality is the dominant pressure, not raw enemy count
- Ceiling threats are readable before jump commitment
- Long drops have at least one recoverable route early in the stage
- Rope feels valuable but not mandatory in every room
- Stage clear time lands in target window

### Stage 3: Trap Works

- Trap density is higher but still legible
- At least one chest/key or pickaxe branch offers a real choice
- Trap lanes reward scouting and punish rushing
- Bomb usage has at least one smart shortcut opportunity
- Stage clear time lands in target window

### Stage 4: Idol Vault

- Stage feels like a final exam, not a chaos dump
- Final greed branch is obvious and tempting
- Mixed enemy plus trap rooms remain readable on first sight
- There is at least one recovery opportunity before the final stretch
- Stage clear time lands in target window

---

## C. Risk/Reward Economy

- Every stage contains at least one optional high-value branch
- Every stage contains at least one safe but lower-value route
- Treasure is often visible before it is safely reachable
- Key/chest rewards justify detours without feeling mandatory
- Bomb and rope refills are earned, not randomly overgenerous
- Greedy play is faster only sometimes, not always
- Conservative play remains viable and not boring

---

## D. Difficulty Curve

- Stage 1 feels fair even to a new player
- Stage 2 increases tension primarily through vertical commitment
- Stage 3 increases pressure primarily through decision cost
- Stage 4 combines existing lessons instead of adding unexplained rules
- No two consecutive rooms create unavoidable blind damage
- Difficulty rises steadily across the run without random spikes
- Final stage deaths usually feel deserved in tester notes

---

## E. Content Budgets

- Stage 1 uses the smallest enemy overlap budget
- Stage 2 limits floor clutter so vertical play can breathe
- Stage 3 limits surprise aerial threats while trap density is high
- Stage 4 may combine threats, but never in unreadable visual clutter
- Optional reward rooms are not more common than survival rooms
- Resource pickups do not erase stage identity by overfilling inventory

---

## F. Full-Run Playtime Validation

Record for each run:

- total duration
- stage split times
- bombs used
- ropes used
- health at exit of each stage
- number of optional branches taken
- reason for death if run fails

Acceptance targets:

- first-clear median: `22-28 min`
- repeat-run median: `17-22 min`
- expert fast run: `14-17 min`
- outlier long runs should still stay under `30 min`

---

## G. Technical Stability

- No null-reference errors during full runs
- No missing prefab references in stage content
- No exit fails to spawn
- No player spawn occurs inside collision
- No soft-lock after chest unlock or key use
- No trap stops functioning after scene reset
- No accessory pickup breaks player state or UI

---

## H. Release Candidate Criteria

Ship only when all are true:

- 10 consecutive full runs completed without blocker bugs
- 3 external testers can finish or reach Stage 4 without guidance
- stage roles are correctly described by testers after play
- no major resource exploit trivializes the run
- no single room has a death rate so high that it dominates all others without being an intentional climax room
- all required content exists in the build and is not editor-only
