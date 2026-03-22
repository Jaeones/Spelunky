# Four-Stage Production Package

## Project Frame

- Engine: Unity `2022.3.62f3`
- Product goal: a `ship-ready small prototype`, not a learning sample
- Target playtime: `15-30 minutes average` for a full run
- Level count: `4 stages`
- Core fantasy to preserve from the reference project:
  - precise movement
  - jump commitment and recovery
  - rope and bomb economy
  - exploration under pressure
  - constant risk/reward decisions

---

## 1. Core System Analysis

### What the repository already gives us

The current project already contains the backbone for a small commercial-quality prototype:

- Player movement, jump, rope, bomb, holding/throwing, stomp, damage, death
- Custom pixel-style collision via `EntityPhysics`
- Centralized game loop via `GameManager`
- Procedural room placement with handcrafted room prefabs via `LevelGenerator`
- Enemy base plus `Snake`, `Bat`, `Spider`, `Caveman`
- Trap support with `ArrowTrap`
- Carryables and risk/reward items such as `Chest`, `Key`, `GoldIdol`, `Treasure`, `Pickaxe`
- Inventory pickups for bombs and ropes
- Accessory progression via `ClimbingGlove`, `SpringBoots`, `PitchersMitt`, `Paste`
- Moving platforms, blocks, one-way platforms, ladders, spikes, altar-style room content

### What the repository does not yet give us as a finished game

The current repository is not yet a complete 15-30 minute product because it still lacks:

- a deliberate 4-stage structure with stage identity
- a tuned resource economy across a full run
- a consistent difficulty curve across all content
- robust reward routing and optional challenge lanes
- progression pacing between stages
- production QA targets and content lock criteria

### Production conclusion

We should treat the repository as a strong systems base and build a `four-stage cave campaign` on top of it, using existing mechanics rather than inventing new ones.

---

## 2. Production Target

### Definition of Done

The project is considered production-complete when it meets all of the following:

- One full run contains `4 distinct stages`
- Average run length lands between `15 and 30 minutes`
- A cautious first-clear run averages `22-28 minutes`
- A confident repeat run averages `15-20 minutes`
- Each stage has a clear gameplay role, not just new room art
- Bombs, ropes, treasure, and optional rewards force real trade-offs
- At least 3 enemy types and 2 hazard types are encountered per full run
- The final stage combines previously learned threats instead of introducing brand-new rules
- The build can be played start-to-finish without editor intervention

### Design Pillars

1. Movement clarity over spectacle
2. Resource pressure over raw combat density
3. Risk/reward every 30-60 seconds
4. Stage identity through structure and encounter composition
5. Short runs with replayable tension, not endless content volume

---

## 3. Four-Stage Structure

We will keep one broad cave aesthetic family and differentiate stages through layout, enemy composition, reward routing, and resource pressure.

### Stage 1: Entry Shaft

Role:
- onboarding without feeling like a tutorial
- teaches safe descent, whip range, stomp timing, rope value, and the cost of greed

Primary threats:
- Snake
- Bat
- isolated ArrowTrap
- simple fall risk

Primary rewards:
- small gold clusters
- one guaranteed rope pickup
- one optional chest/key side path

Layout identity:
- broad rooms
- forgiving landings
- visible treasure routes
- one obvious shortcut that costs a bomb or rope

Run time target:
- `3-5 minutes`

Success feeling:
- player exits with confidence and at least one meaningful resource decision made

### Stage 2: Hanging Caverns

Role:
- vertical tension stage
- emphasizes fall danger, overhead threats, and rope economy

Primary threats:
- Bat
- Spider
- one-way platform misreads
- longer vertical drops

Primary rewards:
- elevated treasure clusters
- one accessory route biased toward `ClimbingGlove` or `SpringBoots`
- extra rope pickup placed behind light danger

Layout identity:
- taller shafts
- more ceiling-based danger
- ladder and one-way platform interplay
- fewer safe flat floors

Run time target:
- `4-6 minutes`

Success feeling:
- player starts planning movement before moving, not reacting after jumping

### Stage 3: Trap Works

Role:
- deliberate attrition stage
- tests bomb usage, throwables, key/chest routing, and route commitment

Primary threats:
- ArrowTrap
- Caveman
- Block and crush spaces
- narrow corridors with punishment for rushing

Primary rewards:
- locked chest with guaranteed accessory value
- bomb pickup hidden behind trap lane
- treasure pockets in trap-protected rooms
- optional pickaxe route for expert shortcut/resource gain

Layout identity:
- tighter spaces
- horizontal ambushes
- more forced line-of-sight checks
- stronger use of safe route versus greedy route split

Run time target:
- `4-7 minutes`

Success feeling:
- player survives by reading rooms and spending resources intelligently

### Stage 4: Idol Vault

Role:
- final exam stage
- mixes learned threats under low-resource pressure and high reward temptation

Primary threats:
- Bat
- Spider
- Caveman
- ArrowTrap
- mixed vertical + horizontal hazard chains

Primary rewards:
- one high-value `GoldIdol` or treasure vault route
- one last guaranteed bomb or rope refill before late danger spike
- one optional accessory chest if player takes a risk-heavy detour

Layout identity:
- layered routes
- mixed threat stacking
- one major greed temptation near the end
- faster exit path for skilled players, longer safer route for cautious players

Run time target:
- `5-8 minutes`

Success feeling:
- players win because they managed tension, not because content suddenly became fairer

---

## 4. Playtime Design

### Target timing model

- Stage 1: `3-5 min`
- Stage 2: `4-6 min`
- Stage 3: `4-7 min`
- Stage 4: `5-8 min`

### Full-run target bands

- expert clean run: `14-17 min`
- expected repeat player run: `17-22 min`
- cautious first-clear run: `22-28 min`
- greedy, exploratory, recovery-heavy run: `25-30 min`

### How we achieve this without huge content volume

We do not need dozens of stages. We need:

- short but dense stages
- optional side routes that cost time and resources
- visible treasure that tempts detours
- hazards that force brief room reads
- one or two decision points per room cluster

### Playtime pacing rules

- Each stage should contain `1 fast path` and `1 greedy path`
- Each stage should contain `1 major decision point` where using a bomb or rope saves time but costs future safety
- No stage should feel longer than 8 minutes unless the player intentionally explores extra treasure routes
- Stage exits must stay readable; getting lost should not be the main time sink

---

## 5. Risk/Reward Structure

### Core loop

The run should repeatedly ask:

- Do I spend a rope now to secure health and time?
- Do I spend a bomb now for treasure or save it for safety later?
- Do I carry a treasure item through danger or cash out smaller safe rewards?
- Do I detour for a chest/key or preserve tempo?

### Reward categories

- Safe reward: exposed gold nuggets, minor treasure, visible inventory pickups
- Medium risk reward: treasure behind a fall or enemy guard
- High risk reward: chest/key branch, idol carry route, trap-protected vault, pickaxe shortcut

### Required reward cadence

- Every 45-75 seconds, the player should encounter a meaningful optional reward
- Every stage must offer at least one `resource-for-safety` choice and one `risk-for-upside` choice
- The last stage must contain the highest-value optional reward of the game

### Why this matters for replayability

Replay tension comes from making the same stage readable but not trivial. Optional branches and resource scarcity create different run states even when layouts are familiar.

---

## 6. Enemy, Trap, and Item Placement Principles

### Global placement rules

- Never stack two new threat types in the same teaching room
- Introduce, reinforce, combine: every stage follows that sequence internally
- Treasure should often be visible before it is safely reachable
- Hazards should protect treasure, not just fill empty space
- Resource pickups should feel earned, not random pity

### Enemy usage by role

- Snake: floor control, simple pressure, early stomping practice
- Bat: panic inducer, punishes vertical greed and exposed jumps
- Spider: ceiling denial, punishes tunnel vision and passive waiting
- Caveman: corridor disruptor, forces spacing and route commitment

### Trap usage by role

- ArrowTrap: line-of-sight tax; best used where the player sees it slightly before entering commitment range
- Block / crush spaces: positional hazard; use sparingly but memorably
- Spikes / fall pits: route-shaping hazard, not random death clutter

### Item usage by role

- Rope pickup: extend exploration and recovery; place after vertical stress
- Bomb pickup: enable greed path or bailout; place after trap-intensive sequences
- Chest/Key: planned optional branch, not random noise
- Pickaxe: stage-specific shortcut breaker and alternate resource route
- Accessories:
  - `SpringBoots`: rewards bold vertical play
  - `ClimbingGlove`: rewards route creativity
  - `PitchersMitt`: improves combat and utility throws
  - `Paste`: upgrades bomb identity late in the run

### Per-stage content matrix

| Stage | Core Enemies | Core Hazards | Core Rewards | Main Skill Test |
|---|---|---|---|---|
| 1 | Snake, Bat | Small falls, first ArrowTrap | Gold, rope pickup, early chest route | Basic movement and greed control |
| 2 | Bat, Spider | Long drops, one-way platforms | Rope pickup, vertical treasure, mobility accessory | Descent planning and aerial safety |
| 3 | Caveman, Snake | ArrowTrap, crush spaces | Bomb pickup, chest/key, pickaxe route | Room reading and resource commitment |
| 4 | Bat, Spider, Caveman | Mixed trap chains, vault pressure | Idol/vault reward, final refill, accessory branch | Full-system mastery under pressure |

---

## 7. Difficulty Curve

### Curve goals

- Stage 1 teaches confidence
- Stage 2 teaches caution
- Stage 3 teaches planning
- Stage 4 tests composure under scarcity

### Difficulty knobs

Use these in order before adding more raw enemies:

1. Route visibility
2. Safe landing availability
3. Resource generosity
4. Threat overlap
5. Recovery space after mistakes
6. Reward temptation density

### Stage-by-stage curve

#### Stage 1

- High readability
- Low punishment density
- Generous recovery space
- One real trap lesson

#### Stage 2

- Moderate readability
- Higher positional punishment
- More vertical commitment
- Resource pressure begins

#### Stage 3

- Moderate readability, higher decision cost
- Strongest trap density in the game
- High mental load, not yet highest lethal overlap

#### Stage 4

- Highest combined threat density
- Lower spare resources
- Multiple optional greed routes
- Must feel winnable but tense from start to exit

### Anti-frustration rules

- Do not put unavoidable damage at the start of a stage
- Do not chain blind ArrowTrap hits into instant pit deaths
- Give one safe read window before every major hazard combo
- Keep the final stage hard through mixed pressure, not cheap surprise spam

---

## 8. Implementation Priority

### Phase 1: Core completion

- Lock player movement, jump, rope, bomb feel
- Lock `EntityPhysics`
- Lock enemy contact damage and stomp behavior
- Lock level start/end and scene restart loop

### Phase 2: Content-ready systems

- Finish chest/key flow
- Finish accessory pickup flow
- Finish arrow trap reliability
- Finish moving platform and block stability
- Finish stage transition and run state persistence between stages

### Phase 3: Stage production

- Build Stage 1 room pool and exit rules
- Build Stage 2 vertical room pool
- Build Stage 3 trap room pool
- Build Stage 4 mixed final room pool
- Add reward route logic and placement rules

### Phase 4: Progression and balance

- Add stage-specific item tables
- Tune resource distributions
- Tune enemy/trap budgets per stage
- Lock run-length targets through playtests

### Phase 5: Shipping polish

- UI clarity
- audio mix pass
- bug fixing
- onboarding copy and controller verification
- content lock and regression testing

---

## 9. Test Checklist Summary

A detailed QA list lives in the companion checklist document, but the production package requires these headline gates.

### System gates

- Player can complete a full run without soft-locking
- Rope cannot create progression dead-ends
- Bomb use cannot destroy mandatory progression
- Enemy overlap damage behaves consistently
- Stage exits always resolve correctly

### Content gates

- Every stage has a distinct role and is identifiable in play within 60 seconds
- Every stage contains at least one optional reward branch
- No stage contains more than one unfair blind hazard cluster
- Resource economy supports both cautious and aggressive playstyles

### Playtime gates

- First clear median: `22-28 min`
- Repeat run median: `17-22 min`
- Speedy expert run: `14-17 min`

### Quality gates

- At least 10 consecutive full runs without blocker bugs
- At least 3 external playtesters can explain stage roles correctly after play
- At least 70 percent of tester deaths feel deserved in post-run survey notes

---

## 10. Actual Production Schedule

Assumption: solo or very small team, part-time but disciplined, `8 weeks`.

### Week 1

- Freeze engine version to `2022.3.62f3`
- Audit repository and lock core movement targets
- Stabilize player, rope, bomb, collision, restart loop
- Define room naming and content conventions

### Week 2

- Finish resource pickups, chest/key, accessories
- Verify arrow trap, block, moving platform reliability
- Build debug spawn tools and fast run-reset workflow

### Week 3

- Produce Stage 1 room pool
- Build Stage 1 encounter budgets and reward routes
- Internal balance pass for first 5 minutes

### Week 4

- Produce Stage 2 room pool
- Focus on vertical spaces, falls, spiders, bats, rope pressure
- Run first full two-stage pacing test

### Week 5

- Produce Stage 3 room pool
- Build trap lanes, chest/key branches, pickaxe content
- Tune bomb economy and corridor readability

### Week 6

- Produce Stage 4 room pool
- Build mixed-threat final routes and high-value greed branch
- Add final stage refill points and end-of-run balance logic

### Week 7

- Full-run balancing week
- Tune playtime, rewards, death heatmaps, and frustration spikes
- Fix blocker bugs and remove weak rooms

### Week 8

- Content lock
- QA sweep
- controller and keyboard validation
- audio/UI polish
- release candidate build and final acceptance playtests

---

## 11. Deliverable Structure

To keep this actually buildable, production should use these working docs and boards:

- stage room spreadsheet by stage and purpose
- item and enemy budget sheet per stage
- bug board with severity labels
- full-run test log with duration, deaths, resource use, and quit point
- room blacklist list for low-quality layouts

---

## 12. Recommended Scope Locks

To finish on time, do not expand beyond these unless the run is already stable.

Do not add:

- extra biomes
- boss fights
- shops or economy vendors
- new enemy archetypes beyond the current pool
- long meta progression
- story scenes

Instead, finish strong on:

- stage identity
- route tension
- resource economy
- room quality
- readable danger
- full-run reliability
