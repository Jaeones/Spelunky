# SYSTEM_GAP_REPORT

## 목적

이 문서는 이 저장소를 기반으로 `15-30분 플레이 가능한 4스테이지 완성형 소형 게임`을 만들기 위해,
현재 존재하는 시스템과 반드시 추가로 필요한 시스템 사이의 갭을 정리한 리포트다.

---

## 1. 이미 있는 시스템 vs 부족한 시스템

| 영역 | 현재 상태 | 판단 |
|---|---|---|
| 게임 루프 | 있음 | 재사용 가능 |
| 플레이어 이동/점프 | 있음 | 핵심 강점 |
| 로프/폭탄 | 있음 | 핵심 강점 |
| 적 기본 구조 | 있음 | 충분한 시작점 |
| 함정 기본 구조 | 일부 있음 | 확장 필요 |
| 타일/룸 기반 레벨 구조 | 있음 | production rules 필요 |
| procedural room placement | 있음 | 품질 통제 부족 |
| 인벤토리/액세서리 | 있음 | stage persistence 필요 |
| HUD | 최소 있음 | production UI 부족 |
| 게임오버/재시작 | 있음 | run flow 부족 |
| stage progression | 거의 없음 | 반드시 추가 필요 |
| run persistence | 없음 | 반드시 추가 필요 |
| stage balancing tools | 없음 | 반드시 추가 필요 |
| QA instrumentation | 없음 | 반드시 추가 필요 |

---

## 2. 완성형 4스테이지 게임을 만들려면 반드시 필요한 추가 시스템

### 2.1 Stage Flow System

왜 필요한가:
- 현재는 current scene reload 중심 흐름이다.
- 4개 stage를 연속 플레이 가능하게 만들어야 한다.

필수 역할:
- stage index 관리
- 다음 stage 로딩
- 마지막 stage clear 처리
- run complete 처리
- death/retry 정책 관리

추천 형태:
- `StageFlowManager`
- `StageDefinition` ScriptableObject

우선순위:
- 최고

### 2.2 Run State Persistence

왜 필요한가:
- bombs, ropes, gold, accessories가 stage를 넘어 유지되어야 run game이 된다.

필수 역할:
- 현재 체력
- bombs/ropes 수량
- gold
- acquired accessories
- current stage number
- optional progression flags

추천 형태:
- `RunState` plain data model
- `RunStateService` 또는 `RunManager`
- stage load 전에 snapshot, spawn 후 restore

우선순위:
- 최고

### 2.3 Stage Content Configuration

왜 필요한가:
- 지금 구조만으로는 4개 stage가 서로 다른 역할을 자동으로 갖지 않는다.

필수 역할:
- stage별 room pool
- enemy budget
- trap budget
- reward budget
- guaranteed pickups
- optional branch density
- pacing rules

추천 형태:
- `StageConfig` ScriptableObject
- `RoomArchetype` / `RoomTag` metadata

우선순위:
- 최고

### 2.4 Level Validation Pass

왜 필요한가:
- procedural assembly가 재미없는 조합이나 progression dead-end를 만들 수 있다.

필수 역할:
- entrance to exit reachable 검사
- mandatory route bomb-lock 검사
- reward branch density 검사
- impossible fall / impossible recovery 검사

추천 형태:
- generation 이후 validator pass
- failed seed reroll or room replacement

우선순위:
- 높음

### 2.5 Full-Run Reward Economy System

왜 필요한가:
- 반복 플레이 긴장감은 reward routing과 resource scarcity에서 나온다.

필수 역할:
- gold distribution rules
- rope/bomb refill cadence
- accessory placement rules
- chest/key route policy
- idol or high-risk reward placement

추천 형태:
- data-driven tables by stage

우선순위:
- 높음

### 2.6 Production UI Layer

왜 필요한가:
- 완성형 게임은 HUD만으로 부족하다.

필수 역할:
- stage start card
- stage clear card
- run complete screen
- game over summary
- optional score / treasure summary
- retry / quit / restart UX

추천 형태:
- separate UI presenters, not `GameObject.Find` HUD-only model

우선순위:
- 높음

### 2.7 Spawn and Transition Safety System

왜 필요한가:
- stage loading 후 플레이어가 충돌 안에 스폰되면 run이 깨진다.

필수 역할:
- safe spawn point selection
- fallback spawn logic
- camera initialization after restore
- stage transition invulnerability window if needed

우선순위:
- 높음

### 2.8 Content Authoring Rules

왜 필요한가:
- room production이 체계화되지 않으면 4 stages를 완성할 수 없다.

필수 역할:
- room naming convention
- room purpose categories
- allowed enemy counts by room type
- allowed trap counts by room type
- optional branch templates
- treasure lane templates

우선순위:
- 높음

### 2.9 Telemetry / Debug Balance Tools

왜 필요한가:
- 15-30분 목표는 감으로 맞추기 어렵다.

필수 역할:
- stage split timing
- death cause logging
- resource usage logging
- room completion timing
- branch take-rate logging

우선순위:
- 중간 이상

---

## 3. 불명확해서 먼저 정해야 하는 정책

### 3.1 Death policy

정해야 할 것:
- death 시 current stage restart인가
- full run reset인가
- limited continues가 있는가

권장:
- full run reset 또는 Stage 1 restart가 가장 긴장감 유지에 좋음

### 3.2 Gold purpose

정해야 할 것:
- gold가 score only 인가
- end-run rank에 쓰는가
- unlock에는 쓰지 않는가

권장:
- small prototype에서는 score + end-run rank 정도로 제한

### 3.3 Accessory permanence

정해야 할 것:
- stage 사이에 유지되는가
- death 시 전부 잃는가

권장:
- run 내 유지, run 종료 시 초기화

### 3.4 Procedural vs curated stage ratio

정해야 할 것:
- fully procedural room chain인가
- curated critical rooms + procedural fillers인가

권장:
- curated critical rooms plus procedural fillers
- 특히 stage intro, mid-test, final greed room은 고정 의도가 있는 편이 좋음

---

## 4. 재사용 가능한 코드와 새 시스템의 경계

### 그대로 최대한 살릴 것

- `EntityPhysics`
- `PhysicsBody`
- `Player`
- `PlayerInput`
- player state system
- `Enemy` base and enemy states
- `Bomb`, `Rope`, `ThrowableItem`, `PlayerHolding`
- `InventoryPickup`, `AccessoryPickup`, `Treasure`
- `ArrowTrap`, `MovingPlatform`, `Block`

### 확장해서 쓸 것

- `GameManager`
- `LevelGenerator`
- `Room`
- `Tile`
- `Exit`
- `PlayerUI`

### 새로 넣어야 할 것

- `RunManager` or `RunStateService`
- `StageFlowManager`
- `StageConfig` data assets
- `RoomMetadata` / `RoomArchetype` layer
- `LevelValidationService`
- run summary UI
- stage transition UI
- analytics/debug logger

---

## 5. 리팩토링 필요도 우선순위

### 즉시 필요

1. `GameManager`
2. `LevelGenerator`
3. `PlayerUI`
4. exit and stage transition flow

### stage production 전에 필요

1. room metadata system
2. stage config data
3. run state persistence
4. validation pass

### content lock 전에 필요

1. telemetry/debug logging
2. bug-safe restart and stage reset flow
3. summary/result UI

---

## 6. 가장 먼저 검증해야 할 리스크

### R1. 30분 런에서 물리 안정성이 유지되는가

검증 방법:
- 연속 10회 full-run soak test
- moving platform, rope, bomb, one-way, stomp, block crush 집중

성공 기준:
- blocker 없음
- soft-lock 없음
- physics desync 없음

### R2. stage transition 후 player state 복원이 안정적인가

검증 방법:
- bomb/rope/accessory/gold 상태를 들고 다음 stage 진입 반복

성공 기준:
- inventory mismatch 없음
- camera/init mismatch 없음

### R3. procedural room chain이 재미와 안전을 동시에 만족하는가

검증 방법:
- 20개 seed 샘플링
- stage별 clear time, death concentration, dead-end 여부 기록

성공 기준:
- unwinnable seed 없음
- boring empty chain 비율 낮음

### R4. reward economy가 긴장감을 유지하는가

검증 방법:
- cautious run vs greedy run 자원 사용 비교

성공 기준:
- greedy play가 항상 정답이 아님
- conservative play도 지루하지 않음

### R5. 4 stages가 실제로 서로 다른가

검증 방법:
- 외부 플레이어 3명 이상에게 각 stage의 역할 설명 유도

성공 기준:
- “비슷한 동굴 4개”로 느끼지 않음

---

## 7. 제작 관점 결론

이 저장소는 `완성형 4스테이지 소형 게임`의 기반으로 충분히 가치가 있다.
하지만 남은 작업의 중심은 새로운 플레이어 액션 추가가 아니다.

남은 핵심 일은:
- stage progression 만들기
- run state 유지하기
- room production rules 세우기
- level validation 넣기
- reward economy와 playtime 조정하기
- production UI와 QA 도구 보강하기

즉 이 프로젝트는 이미 `게임 시스템 prototype` 단계는 상당히 넘었고,
앞으로는 `콘텐츠 생산과 런 구조 완성`이 메인 작업이다.
