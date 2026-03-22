# PRODUCTION_AUDIT

## 목적

이 문서는 이 Unity Spelunky 스타일 저장소를 `제작 관점`에서 감사한 결과다.
목표는 이 저장소를 기반으로 `15-30분 플레이 가능한 4스테이지 완성형 소형 게임`을 만들 수 있는지 판단하고,
현재 이미 쓸 수 있는 시스템과 추가 작업이 필요한 영역을 분리하는 것이다.

---

## 1. 현재 저장소에 이미 있는 것으로 보이는 핵심 시스템

### 1.1 코어 게임 루프

상태: `구현됨, 재사용 가치 높음`

관련 파일:
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`
- `Assets/Scripts/Core/ITickable.cs`

판단:
- `GameManager` 가 입력, 플랫폼, 엔티티, 후처리, 타이머 순서를 직접 제어한다.
- 작은 액션게임 프로토타입에서 흔히 필요한 deterministic한 업데이트 순서를 이미 갖고 있다.
- 특히 moving platform과 custom physics가 섞이는 구조에서 유리하다.

프로덕션 관점 평가:
- 그대로 재사용 가능
- 다만 stage progression과 run state를 붙이려면 `GameManager` 책임 분리가 필요하다

### 1.2 플레이어 기본 액션

상태: `핵심 구현됨`

관련 파일:
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/*`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Player/PlayerAccessories.cs`
- `Assets/Scripts/Player/PlayerAudio.cs`
- `Assets/Scripts/Player/CameraFollow.cs`

이미 있는 기능:
- 좌우 이동
- 점프와 짧은 점프
- 로프 사용
- 폭탄 사용
- 아이템 들기/던지기/장비 사용
- ledge hang / climb 관련 상태
- whip 공격
- 피격/넉백/사망
- 카메라 추적

프로덕션 관점 평가:
- 소형 완성형 게임의 플레이 감각 핵심은 이미 상당 부분 존재
- 가장 중요한 “이동, 점프, 로프, 폭탄” 판타지는 유지 가능

### 1.3 커스텀 물리와 픽셀형 충돌

상태: `프로젝트의 가장 중요한 자산`

관련 파일:
- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`
- `Assets/Scripts/Entity/Collision/*`
- `Assets/Scripts/Entity/PhysicsBody.cs`

이미 있는 기능:
- pixel-style integer movement
- 바닥/벽/천장 판정
- one-way platform 처리
- overlap/collision 이벤트
- moving platform 외부 델타 반영
- push/crush 상황 일부 처리
- physics body 공통 베이스

프로덕션 관점 평가:
- 이 저장소의 핵심 재사용 자산
- 완성형 4스테이지 게임도 이 기반 위에서 충분히 구축 가능
- 이 계층 안정성 검증이 최우선 리스크다

### 1.4 레벨/룸/타일 구조

상태: `초기 구현 존재, 확장 가능`

관련 파일:
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`

이미 있는 기능:
- Room prefab 기반 레벨 구조
- `Rooms[,]`, `Tiles[,]` 그리드
- handcrafted room + procedural placement 구조
- entrance / exit 배치
- level bounds / background 생성
- moving platform / pushable block

프로덕션 관점 평가:
- 소형 완성형 게임에는 매우 적합한 구조
- “4개 스테이지의 룸 풀” 생산 방식과 잘 맞는다
- 하지만 현재 생성 로직은 문서상/코드상 WIP이며 그대로는 콘텐츠 품질을 보장하지 못한다

### 1.5 적, 함정, 보상 루프의 기본 재료

상태: `좋은 출발점 확보`

관련 파일:
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Enemies/Snake/*`
- `Assets/Scripts/Enemies/Bat/*`
- `Assets/Scripts/Enemies/Spider/*`
- `Assets/Scripts/Enemies/Caveman/*`
- `Assets/Scripts/Traps/ArrowTrap.cs`
- `Assets/Scripts/Treasure/Treasure.cs`
- `Assets/Scripts/Pickups/InventoryPickup.cs`
- `Assets/Scripts/Pickups/AccessoryPickup.cs`
- `Assets/Scripts/Items/Throwables/Chest.cs`
- `Assets/Scripts/Items/Throwables/Key.cs`
- `Assets/Scripts/Items/Equipment/Pickaxe.cs`

이미 있는 기능:
- 4종 적 아키타입
- 1종 즉발 함정(ArrowTrap)
- 금전 보상(Treasure)
- 자원 보상(Bombs/Ropes)
- 액세서리 보상
- chest/key 리스크-보상 구조
- pickaxe 기반 지형 파괴형 보상 루프

프로덕션 관점 평가:
- 반복 플레이 긴장감을 설계할 수 있을 정도의 재료는 있다
- 적 수는 적지만 역할 분배가 분명해서 소형 게임에는 충분하다

### 1.6 최소 UI / 게임오버 흐름

상태: `있음, 하지만 얕음`

관련 파일:
- `Assets/Scripts/Player/PlayerUI.cs`
- `Assets/Scripts/UI/GameOverUI.cs`
- `Assets/Scripts/Misc/AudioManager.cs`

이미 있는 기능:
- 생명, 폭탄, 로프, 골드 UI 반영
- 액세서리 아이콘 표시
- 게임오버 UI
- 씬 재시작 버튼
- 오디오 믹서 그룹 기반 사운드 출력

프로덕션 관점 평가:
- 내부 테스트와 소형 출시형 프로토타입의 최소 UI 토대는 된다
- 그러나 stage progression UI, 결과 화면, run summary는 부재

---

## 2. 부족하거나 불명확한 시스템

### 2.1 stage progression / multi-stage run state

상태: `부족`

문제:
- 현재 출구 진입과 게임오버 모두 사실상 현재 씬 재로드 중심이다.
- 4개 스테이지를 연속으로 잇는 런 상태 보존 구조가 보이지 않는다.
- bombs, ropes, accessories, score를 다음 스테이지로 넘기는 시스템이 없다.

영향:
- 15-30분짜리 run 기반 구조를 만들 수 없음

### 2.2 level generation quality control

상태: `불명확/부족`

문제:
- `LevelGenerator` 자체가 코드 주석에서 WIP라고 밝힌다.
- 한 스테이지의 완성형 룸 풀 설계, hazard budget, reward routing 규칙이 없다.
- 룸 조합이 “재미있는 4스테이지”를 자동으로 보장하지 않는다.

영향:
- stage identity와 difficulty curve를 만들기 어렵다

### 2.3 content pipeline and production rules

상태: `부족`

문제:
- 룸 제작 규칙, 적/함정 배치 규칙, stage별 budget이 코드로도 문서로도 없다.
- 팀/솔로 제작 시 room quantity와 quality를 통제할 생산 체계가 없다.

영향:
- 콘텐츠가 늘수록 밸런스와 유지보수가 급격히 어려워진다

### 2.4 progression feedback

상태: `부족`

문제:
- 현재 UI는 인-run HUD 중심이다.
- 스테이지 시작/종료, 중간 성과, 최종 결과, 다음 스테이지 진입 피드백이 없다.

영향:
- 완성형 4스테이지 게임의 리듬이 약해진다

### 2.5 analytics-grade debug tools

상태: `부족`

문제:
- 플레이타임 검증, death heatmap, resource usage, stage clear ratio를 추적하는 도구가 없다.

영향:
- 15-30분 목표 밸런싱이 감으로만 진행될 위험이 크다

### 2.6 fail-safe and content safety

상태: `불명확`

문제:
- bomb나 tile destruction이 progression-critical route를 망가뜨릴 가능성 관리가 불명확하다.
- chest/key, idol, destroyable tiles, exits의 상호작용이 완전한지 보장하기 어렵다.

영향:
- soft-lock 리스크 존재

---

## 3. 재사용 가능한 코드

아래 영역은 적극 재사용하는 것이 좋다.

### 매우 높은 재사용 가치

- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Entity/PhysicsBody.cs`
- `Assets/Scripts/Misc/GameManager.cs`의 update-order 개념
- `Assets/Scripts/Managers/*`
- `Assets/Scripts/Player/*`
- `Assets/Scripts/Entity/StateMachine.cs`
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Enemies/*` 개별 적 로직
- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`

### 높은 재사용 가치

- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`
- `Assets/Scripts/Traps/ArrowTrap.cs`
- `Assets/Scripts/Pickups/*`
- `Assets/Scripts/Treasure/Treasure.cs`

### 조건부 재사용 가치

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
  - 구조는 재사용 가치가 높지만, 완성형 게임용으로는 확장과 안전장치가 필요
- `Assets/Scripts/Player/PlayerUI.cs`
  - 프로토타입에는 쓸 수 있으나 현재는 `GameObject.Find` 의존도가 높음

---

## 4. 리팩토링이 필요한 코드 영역

### 4.1 `GameManager` 책임 과다

문제:
- level bootstrap
- manager creation
- player spawning
- game loop
- death handling
이 한 곳에 몰려 있다.

필요 작업:
- `RunManager` 또는 `StageFlowManager` 분리
- `PlayerSpawnService` 또는 spawn policy 분리
- stage progression 책임 분리

### 4.2 `LevelGenerator` 확장성 부족

문제:
- 단일 generator가 room placement, tiles setup, bounds, background, entrance/exit까지 모두 담당
- stage rules와 room pool selection이 코드에 충분히 구조화되어 있지 않음

필요 작업:
- stage별 content config 분리
- room tags / budgets / reward lane metadata 확장
- generation validation pass 추가

### 4.3 `PlayerUI`의 `GameObject.Find` 의존

문제:
- 씬 이름/오브젝트 이름에 강하게 묶여 있다
- production-scale scene maintenance에 취약

필요 작업:
- serialized references 또는 HUD presenter 구조로 변경

### 4.4 progression-critical data가 런타임 전역으로 남아 있지 않음

문제:
- inventory, accessories, score를 다음 stage로 들고 갈 구조가 없음

필요 작업:
- `RunState` 데이터 모델 추가
- stage transition 시 snapshot / restore 구현

### 4.5 `PlayerEnterDoorState`와 `GameOverUI`의 씬 재로드 중심 흐름

문제:
- 현재는 progression보다는 test restart에 가까운 구조

필요 작업:
- 다음 stage load
- run complete flow
- partial retry 정책 정리

### 4.6 콘텐츠 시스템의 데이터 외부화 부족

문제:
- stage별 enemy budget, pickup rates, reward categories, room weights가 명시적 data asset으로 보이지 않음

필요 작업:
- ScriptableObject 기반 stage config
- room set config
- difficulty tuning tables

---

## 5. 가장 먼저 검증해야 할 리스크

### 리스크 1: `EntityPhysics` 안정성

이유:
- 모든 핵심 재미가 이 위에 올라간다.
- moving platform, rope, bomb, one-way platform, stomp, knockback까지 다 연결된다.

먼저 검증할 항목:
- 30분 플레이 동안 이동 soft bug가 없는지
- one-way platform drop / land 안정성
- moving platform crush / carry 안정성
- bomb and throwable overlap damage 안정성

### 리스크 2: stage-to-stage progression 부재

이유:
- 지금 저장소는 단일 stage test/loop 구조에 더 가깝다.
- 4-stage product를 만들려면 가장 먼저 run state 설계를 확정해야 한다.

먼저 검증할 항목:
- bombs/ropes/accessories/gold를 stage transition 후 유지하는 구조 설계 가능 여부
- next stage load 시 player restoration 정상 작동 여부

### 리스크 3: level generation quality

이유:
- 시스템은 있어도 재미있는 stage를 자동으로 만들지 못하면 production 실패다.

먼저 검증할 항목:
- hand-authored room pool만으로 stage identity를 만들 수 있는지
- current generator가 unsafe/boring room chains를 얼마나 자주 만드는지

### 리스크 4: content breadth 대비 tuning cost

이유:
- 적 종류는 많지 않지만 room variation, reward routing, resource balance가 잘못되면 반복 플레이가 단조로워진다.

먼저 검증할 항목:
- 4-stage run에서 각 stage가 실제로 다른 느낌을 주는지
- risk/reward choice frequency가 45-75초 간격으로 유지되는지

### 리스크 5: production UI and feedback

이유:
- 15-30분 런 기반 게임은 stage clear, fail, run completion 피드백이 중요하다.

먼저 검증할 항목:
- stage intro/outro 최소 UI
- run result screen 필요 범위
- gold, inventory, accessory persistence feedback

---

## 6. 제작 관점 최종 판단

이 저장소는 `학습용 최소 복제`를 넘어,
실제로 `작은 완성형 액션-탐험 프로토타입`의 엔진 역할을 할 수 있는 기반을 이미 갖고 있다.

강점:
- 플레이어 코어 판타지 구현 수준이 높음
- 적, 함정, 보상 재료가 이미 존재
- custom physics와 centralized loop가 strong backbone 역할을 함

약점:
- multi-stage progression, content pipeline, production-safe level rules, QA instrumentation이 부족함

결론:
- `4스테이지 / 15-30분` 제품은 충분히 가능
- 단, 새 기능을 많이 추가하기보다
  - stage flow
  - run state
  - room production rules
  - balancing and QA
에 집중해야 한다.
