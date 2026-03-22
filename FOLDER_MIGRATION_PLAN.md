# FOLDER_MIGRATION_PLAN

## 목적

이 문서는 현재 저장소를 `4스테이지 완성형 게임` 구조로 정리하기 위해,
기존 파일을 어떤 폴더 체계로 재배치할지와 그 이동 순서를 정의한다.

중요 원칙:
- 실제 파일 이동은 한 번에 하지 않는다.
- `참조 충돌이 적은 파일 -> 중간 허브 파일 -> 프리팹/씬` 순서로 옮긴다.
- Unity 메타와 참조 손실을 막기 위해 `폴더 생성 -> 파일 이동 -> 씬/프리팹 검증` 순으로 진행한다.

---

## 1. 목표 폴더 구조

권장 최종 구조:

```text
Assets/
├─ Scenes/
│  ├─ Boot.unity
│  ├─ Menu.unity
│  ├─ Game.unity
│  └─ Result.unity
│
├─ Scripts/
│  ├─ Core/
│  │  ├─ GameLoop/
│  │  ├─ StateMachine/
│  │  └─ Common/
│  ├─ Runtime/
│  │  ├─ Run/
│  │  ├─ Stage/
│  │  ├─ Spawn/
│  │  ├─ UI/
│  │  ├─ Audio/
│  │  └─ Debug/
│  ├─ Player/
│  │  ├─ States/
│  │  ├─ Combat/
│  │  ├─ Inventory/
│  │  └─ Presentation/
│  ├─ World/
│  │  ├─ Level/
│  │  ├─ Rooms/
│  │  ├─ Tiles/
│  │  ├─ Traps/
│  │  └─ Interactables/
│  ├─ Enemies/
│  │  ├─ Common/
│  │  ├─ Bat/
│  │  ├─ Snake/
│  │  ├─ Spider/
│  │  └─ Caveman/
│  └─ Data/
│     ├─ Definitions/
│     ├─ SpawnTables/
│     ├─ DropTables/
│     └─ RuntimeModels/
│
├─ Prefabs/
│  ├─ Player/
│  ├─ Enemies/
│  ├─ Traps/
│  ├─ Items/
│  ├─ UI/
│  └─ Stage/
│     ├─ Common/
│     ├─ Stage01/
│     ├─ Stage02/
│     ├─ Stage03/
│     └─ Stage04/
│
├─ Data/
│  ├─ Stages/
│  ├─ SpawnTables/
│  ├─ DropTables/
│  ├─ Audio/
│  └─ Debug/
└─ Docs/
   ├─ Production/
   ├─ StageSpecs/
   ├─ Balance/
   └─ QA/
```

---

## 2. 현재 구조에서의 실제 매핑

### A. Misc / Managers -> Runtime / Core

현재:
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Misc/AudioManager.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`

이동 목표:
- `GameManager.cs` -> `Assets/Scripts/Core/GameLoop/GameManager.cs`
- `PhysicsManager.cs` -> `Assets/Scripts/Core/GameLoop/PhysicsManager.cs`
- `EntityManager.cs` -> `Assets/Scripts/Core/GameLoop/EntityManager.cs`
- `PlatformManager.cs` -> `Assets/Scripts/Core/GameLoop/PlatformManager.cs`
- `TimerManager.cs` -> `Assets/Scripts/Core/GameLoop/TimerManager.cs`
- `AudioManager.cs` -> `Assets/Scripts/Runtime/Audio/AudioManager.cs`

이유:
- 틱 순서 제어 계층과 런타임 서비스 계층을 분리하기 위함

### B. LevelGenerator -> World

현재:
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`

이동 목표:
- `LevelGenerator.cs` -> `Assets/Scripts/World/Level/LevelGenerator.cs`
- `Room.cs` -> `Assets/Scripts/World/Rooms/Room.cs`
- `Tile.cs` -> `Assets/Scripts/World/Tiles/Tile.cs`
- `Exit.cs` -> `Assets/Scripts/World/Interactables/Exit.cs`
- `MovingPlatform.cs` -> `Assets/Scripts/World/Level/MovingPlatform.cs`
- `Block.cs` -> `Assets/Scripts/World/Level/Block.cs`

이유:
- 레벨 생성, 룸 정의, 타일 정의, 상호작용 오브젝트 책임을 분리하기 위함

### C. Player -> Player 하위 분리

현재:
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Player/PlayerAccessories.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerAudio.cs`
- `Assets/Scripts/Player/PlayerUI.cs`
- `Assets/Scripts/Player/CameraFollow.cs`
- `Assets/Scripts/Player/States/*`

이동 목표:
- `Player.cs` -> `Assets/Scripts/Player/Player.cs` 유지
- `PlayerInput.cs` -> `Assets/Scripts/Player/Combat/PlayerInput.cs` 또는 `Player/Input/` 별도 폴더
- `PlayerInventory.cs` -> `Assets/Scripts/Player/Inventory/PlayerInventory.cs`
- `PlayerAccessories.cs` -> `Assets/Scripts/Player/Inventory/PlayerAccessories.cs`
- `PlayerHolding.cs` -> `Assets/Scripts/Player/Inventory/PlayerHolding.cs`
- `PlayerAudio.cs` -> `Assets/Scripts/Player/Presentation/PlayerAudio.cs`
- `PlayerUI.cs` -> `Assets/Scripts/Player/Presentation/PlayerUI.cs`
- `CameraFollow.cs` -> `Assets/Scripts/Player/Presentation/CameraFollow.cs`
- `States/*` -> `Assets/Scripts/Player/States/*` 유지

이유:
- Player 본체는 유지하고, 주변 시스템만 기능별로 분리
- 초반 충돌을 줄이기 위해 `Player.cs` 자체는 폴더 이동 최소화

### D. UI -> Runtime/UI

현재:
- `Assets/Scripts/UI/GameOverUI.cs`

이동 목표:
- `GameOverUI.cs` -> `Assets/Scripts/Runtime/UI/GameOverUI.cs`
- 새 `UIManager.cs` -> `Assets/Scripts/Runtime/UI/UIManager.cs`

이유:
- HUD, Result, Transition을 공용 UI 계층 아래 통합하기 위함

### E. 새 데이터 계층

새로 생길 위치:
- `StageDefinition.cs` -> `Assets/Scripts/Data/Definitions/StageDefinition.cs`
- `RunState.cs` -> `Assets/Scripts/Data/RuntimeModels/RunState.cs`
- `EnemySpawnTable.cs` -> `Assets/Scripts/Data/SpawnTables/EnemySpawnTable.cs`
- `DropTable.cs` -> `Assets/Scripts/Data/DropTables/DropTable.cs`

에셋 위치:
- Stage asset -> `Assets/Data/Stages/`
- Spawn table asset -> `Assets/Data/SpawnTables/`
- Drop table asset -> `Assets/Data/DropTables/`
- Debug preset -> `Assets/Data/Debug/`

---

## 3. 절대 한 번에 옮기면 안 되는 것

초기 이동 금지 대상:
- `Player.cs`
- `EntityPhysics.cs`
- `Enemy.cs`
- `Game.unity`
- `Player.prefab`

이유:
- 참조 수가 많고, 한 번 꼬이면 복구 비용이 큼
- 초반에는 구조 추가가 우선이고, 이 핵심 축은 그대로 둬도 개발 가능

---

## 4. 실제 이동 순서

### Phase 1. 새 폴더만 먼저 생성

우선 생성:
- `Assets/Scripts/Runtime/Run`
- `Assets/Scripts/Runtime/Stage`
- `Assets/Scripts/Runtime/UI`
- `Assets/Scripts/Runtime/Audio`
- `Assets/Scripts/Runtime/Debug`
- `Assets/Scripts/Data/Definitions`
- `Assets/Scripts/Data/RuntimeModels`
- `Assets/Data/Stages`
- `Assets/Data/SpawnTables`
- `Assets/Prefabs/Stage/Stage01`
- `Assets/Prefabs/Stage/Stage02`
- `Assets/Prefabs/Stage/Stage03`
- `Assets/Prefabs/Stage/Stage04`

왜 먼저 하는가:
- 기존 파일을 안 건드리고도 각 스트림이 새 파일을 바로 추가 가능

### Phase 2. 새 파일부터 새 구조에 생성

먼저 새 위치에 만들 파일:
- `RunState.cs`
- `StageDefinition.cs`
- `RunManager.cs`
- `StageManager.cs`
- `UIManager.cs`
- `DebugManager.cs`

왜 이 순서인가:
- 기존 참조를 깨지 않고 새 구조를 먼저 자라게 할 수 있음

### Phase 3. 참조가 적은 기존 파일 이동

먼저 옮길 후보:
- `AudioManager.cs`
- `GameOverUI.cs`
- `PlayerUI.cs`

이유:
- 상대적으로 영향 범위가 좁고, 옮긴 뒤 에러 확인이 쉬움

### Phase 4. 레벨 구조 파일 이동

다음 후보:
- `Exit.cs`
- `Room.cs`
- `Tile.cs`
- `Block.cs`
- `MovingPlatform.cs`
- `LevelGenerator.cs`

이유:
- Stage 구조 작업이 시작되면 이쪽 폴더 정리가 중요해짐
- 단, 이동 후 즉시 씬/프리팹 참조 검증 필요

### Phase 5. 허브 파일 이동은 가장 나중

마지막 후보:
- `GameManager.cs`
- `EntityManager.cs`
- `PlatformManager.cs`
- `TimerManager.cs`
- `PhysicsManager.cs`

이유:
- 가장 많은 스크립트가 물고 있는 허브 계층이라,
  기능 분리까지 같이 할 때 옮기는 편이 안전함

---

## 5. 스레드별 소유 폴더

### 담당 A
- `Assets/Scripts/Runtime/Run`
- `Assets/Scripts/Runtime/Stage`
- `Assets/Scripts/Core/GameLoop`

### 담당 B
- `Assets/Scripts/World/Level`
- `Assets/Scripts/World/Rooms`
- `Assets/Scripts/World/Tiles`
- `Assets/Scripts/Data/Definitions`
- `Assets/Data/Stages`

### 담당 C
- `Assets/Scripts/Runtime/UI`
- `Assets/Scripts/Player/Presentation`
- `Assets/Prefabs/UI`

### 담당 D
- `Assets/Prefabs/Stage/Stage01`
- `Assets/Prefabs/Stage/Stage02`
- `Assets/Prefabs/Stage/Stage03`
- `Assets/Prefabs/Stage/Stage04`
- `Assets/Data/Stages`

### 담당 E
- `Assets/Scripts/Runtime/Debug`
- `Assets/Data/Debug`
- `Docs/QA`

---

## 6. 각 이동 후 직접 테스트할 것

파일 이동 후 공통 테스트:
- Unity가 meta를 유지하는지
- 씬에서 Missing Script가 없는지
- Player prefab 참조가 유지되는지
- Game 씬이 Play 가능한지
- 콘솔에 type/namespace 로드 에러가 없는지

특히 확인할 것:
- `LevelGenerator` 이동 후 `Game.unity`
- `PlayerUI` 이동 후 HUD 갱신
- `GameOverUI` 이동 후 재시작 버튼
- `GameManager` 이동 후 런 초기화 순서

---

## 7. 가장 현실적인 결론

지금 바로 해야 할 것은 `기존 파일 전체 이동`이 아니라 아래 두 가지다.

1. 새 구조 폴더를 먼저 만든다.
2. 새로 추가하는 파일은 처음부터 새 구조에 둔다.

그 다음에만,
- 영향 범위가 작은 기존 파일부터 차례대로 옮긴다.

즉, 이 프로젝트의 폴더 정리는 `대이동`이 아니라 `점진적 이주` 방식이 맞다.
