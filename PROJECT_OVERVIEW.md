# PROJECT_OVERVIEW

## 목적

이 문서는 이 Unity Spelunky 저장소를 `수정 대상`이 아니라 `따라 만들기 위한 참고 구조`로 요약한 개요다.

---

## 1. 최상위 구조 요약

이 저장소는 대략 아래 레이어로 나뉜다.

1. 씬 레이어
   - 실제 실행 씬과 테스트 씬이 있다.

2. 게임 루프/매니저 레이어
   - `GameManager` 가 시작 흐름과 프레임 순서를 통제한다.
   - `EntityManager`, `PlatformManager`, `TimerManager` 가 세부 업데이트를 담당한다.

3. 공통 엔티티 레이어
   - `EntityPhysics`, `EntityHealth`, `StateMachine`, `PhysicsBody` 같은 공통 기반이 있다.

4. 플레이어/적/아이템 레이어
   - 플레이어 상태머신, 적 상태머신, 투척 아이템, 장비 등이 공통 엔티티 기반 위에 올라간다.

5. 레벨/타일 레이어
   - `LevelGenerator`, `Room`, `Tile`, `Exit`, `MovingPlatform` 이 맵과 절차 생성을 담당한다.

6. UI/연출 레이어
   - UI 표시, 게임오버, 오디오, 카메라 추적이 여기에 속한다.

핵심은 이 프로젝트가 `Unity 기본 Rigidbody 중심` 구조보다 `GameManager + custom physics + state machine` 중심 구조라는 점이다.

---

## 2. 핵심 폴더와 파일 분류

### 씬

- `Assets/Scenes/Game.unity`
  - 실제 시작 씬
- `Assets/Scenes/TestingChamber.unity`
  - 생성기 우회 테스트 씬
- `Assets/Scenes/PhysicsInteractions.unity`
  - 물리 상호작용 테스트용으로 보이는 씬
- `Assets/Scenes/Entities.unity`
  - 엔티티 테스트용으로 보이는 씬
- `Assets/Scenes/Rooms.unity`
  - 룸/타일 테스트용으로 보이는 씬

### 게임 시작/루프

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`
- `Assets/Scripts/Core/ITickable.cs`

### 공통 엔티티 기반

- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Entity/PhysicsBody.cs`
- `Assets/Scripts/Entity/EntityHealth.cs`
- `Assets/Scripts/Entity/EntityVisuals.cs`
- `Assets/Scripts/Entity/StateMachine.cs`

### 플레이어

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Player/PlayerUI.cs`
- `Assets/Scripts/Player/CameraFollow.cs`
- `Assets/Scripts/Player/States/*`

### 적

- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Enemies/Bat/*`
- `Assets/Scripts/Enemies/Snake/*`
- `Assets/Scripts/Enemies/Spider/*`
- `Assets/Scripts/Enemies/Caveman/*`

### 레벨/타일/맵

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`

### 아이템/상호작용

- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Items/ThrowableItem.cs`
- `Assets/Scripts/Items/Equipment/*`
- `Assets/Scripts/Items/Throwables/*`

### UI/기타

- `Assets/Scripts/UI/GameOverUI.cs`
- `Assets/Scripts/Misc/AudioManager.cs`
- `Assets/Scripts/TestingChamber.cs`

---

## 3. 게임 시작 흐름의 진입점

### 시작 씬

빌드 세팅의 시작 씬은 `Assets/Scenes/Game.unity` 다.

### 실제 진입점

실제 코드 진입점은 `Assets/Scripts/Misc/GameManager.cs` 다.

흐름은 다음과 같다.

1. `Game.unity` 가 로드된다.
2. `GameManager.Awake()` 에서 하위 매니저를 만든다.
3. `GameManager.Start()` 에서 `InitializeLevel()` 을 호출한다.
4. `LevelGenerator.GenerateLevel()` 또는 테스트 모드의 기존 씬 스캔을 수행한다.
5. `LevelGenerator.SetupLevel()` 로 타일/배경/경계를 구성한다.
6. 필요하면 entrance/exit 를 배치한다.
7. 플레이어와 카메라를 스폰하거나 기존 씬 오브젝트에 연결한다.
8. 이후 `GameManager.Update()` 가 매 프레임 아래 순서로 흐름을 돌린다.
   - 입력
   - 플랫폼
   - 엔티티
   - 후처리
   - 타이머

### 테스트 씬에서의 우회 진입점

`Assets/Scripts/TestingChamber.cs` 는 `useExistingSceneContent = true` 를 설정해서 절차 생성을 건너뛴다.

즉:

- `Game.unity` 는 정상 게임 시작 흐름
- `TestingChamber.unity` 는 손배치 테스트용 시작 흐름

---

## 4. 씬, 매니저, 플레이어, 레벨, UI 관련 핵심 파일 추정

### 씬 관련

- `Assets/Scenes/Game.unity`
- `Assets/Scenes/TestingChamber.unity`

### 매니저 관련

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`

### 플레이어 관련

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Player/CameraFollow.cs`

### 레벨 관련

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`

### UI 관련

- `Assets/Scripts/UI/GameOverUI.cs`
- `Assets/Scripts/Player/PlayerUI.cs`
- `Assets/Scripts/Misc/AudioManager.cs`

---

## 5. 이 저장소를 읽을 때 기억해야 할 구조 포인트

1. 시작 흐름은 `씬 -> GameManager -> LevelGenerator -> Player/Camera` 순서다.
2. 입력은 `PlayerInput` 에서 읽지만, 행동은 상태가 결정한다.
3. 실제 움직임의 핵심은 `Player` 보다 `EntityPhysics` 다.
4. 적과 아이템도 대부분 같은 물리/체력 기반을 공유한다.
5. 레벨 생성은 상위 레이어이며, 먼저 읽기보다 나중에 읽는 편이 이해가 쉽다.

---

## 6. 저장소를 한 줄로 요약하면

이 프로젝트는 `GameManager가 업데이트 순서를 제어하고, EntityPhysics가 움직임을 해결하며, Player와 Enemy가 상태머신 위에서 행동하고, LevelGenerator가 그 위에 맵을 얹는 구조`라고 보면 된다.
