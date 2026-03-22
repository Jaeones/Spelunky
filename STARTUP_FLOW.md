# STARTUP_FLOW

## 목적

이 문서는 이 Unity 프로젝트에서 게임이 어떻게 시작되고, 플레이어가 언제 조작 가능해지는지를 `호출 흐름` 중심으로 정리한 것이다.

---

## 1. 첫 실행 씬

첫 실행 씬은 `Assets/Scenes/Game.unity` 다.

근거:

- `ProjectSettings/EditorBuildSettings.asset` 에 등록된 활성 씬이 `Assets/Scenes/Game.unity` 하나뿐이다.

즉 기본 실행 경로는:

`Unity Player 실행 -> Game.unity 로드`

---

## 2. 부트스트랩 역할의 오브젝트/스크립트

### 메인 부트스트랩 오브젝트

`Assets/Scenes/Game.unity` 안의 `GameManager` 오브젝트가 사실상 부트스트랩 역할을 한다.

씬 내부 흔적:

- `Game.unity` 에 `m_Name: GameManager`
- 같은 씬에 `m_Name: LevelGenerator`
- `GameManager` 컴포넌트의 `levelGenerator` 필드가 씬의 `LevelGenerator` 를 참조

### 메인 부트스트랩 스크립트

- `Assets/Scripts/Misc/GameManager.cs`

이 스크립트가 하는 일:

1. `Awake()` 에서 하위 매니저 생성
2. `Start()` 에서 레벨 초기화 호출
3. 플레이어/카메라 스폰 또는 연결
4. 이후 프레임마다 게임 루프를 직접 실행

### 보조 부트스트랩

- `Assets/Scripts/UI/GameOverUI.cs`

이 스크립트는 `[RuntimeInitializeOnLoadMethod]` 로 씬 로드 직후 동작하지만,
메인 게임 시작을 담당하는 것은 아니고 `GameManager` 가 존재할 때 게임오버 UI 인스턴스를 보장하는 보조 부트스트랩이다.

### 테스트용 대체 부트스트랩

- `Assets/Scripts/TestingChamber.cs`

이 스크립트는 테스트 씬에서 `GameManager.useExistingSceneContent = true` 로 바꿔서 절차 생성을 우회한다.

---

## 3. GameManager 또는 그에 준하는 시스템

메인 조정자는 `GameManager` 다.

관련 파일:

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`

### `GameManager` 의 역할

`GameManager` 는 단순 데이터 보관자가 아니라 실제 런타임 오케스트레이터다.

호출 순서:

1. `GameManager.Awake()`
   - `CreateSubManagers()` 호출
   - 자식 오브젝트로 `PlatformManager`, `EntityManager`, `TimerManager` 생성

2. `GameManager.Start()`
   - `InitializeLevel()` 호출

3. `GameManager.Update()`
   - 매 프레임 게임 루프를 직접 돌림

### `Update()` 에서의 실제 프레임 순서

1. `EntityManager.EarlyTick()`
   - 입력 처리
2. `PlatformManager.Tick()`
   - 이동 플랫폼 먼저 갱신
3. `EntityManager.Tick()`
   - 플레이어/적/물리 오브젝트 본 업데이트
4. `EntityManager.LateTick()`
   - 후처리
5. `TimerManager.Tick()`
   - 타이머 처리

즉 이 프로젝트는 Unity 기본 `Update` 분산 구조보다 `GameManager 주도형 루프` 구조다.

---

## 4. 씬 전환 흐름

이 프로젝트는 현재 `복수 씬을 넘나드는 완성된 씬 전환 구조`는 거의 없다.

### 실제 확인되는 씬 전환 경로

#### A. 게임오버 시

- `Player` 체력이 0이 되면 `GameManager.HandlePlayerDeath()` 호출
- `GameManager.HandlePlayerDeath()` 는 `GameOverUI.ShowGameOver(score)` 호출
- `GameOverUI` 의 Restart 버튼이 눌리면 현재 활성 씬을 다시 로드

관련 파일:

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

#### B. 출구 진입 시

- 플레이어가 출구 트리거에 들어가면 `Exit` 가 플레이어에 출구 참조를 넘김
- 사용 입력 시 `PlayerEnterDoorState` 로 진입
- 문 진입 애니메이션이 끝나면 현재 씬 이름을 다시 로드

관련 파일:

- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/Player/States/PlayerEnterDoorState.cs`

### 결론

현재 구조상 씬 전환은:

- 새 레벨 씬으로 이동하는 구조가 아니라
- `현재 씬 재시작` 이 임시 구현으로 들어가 있다

즉 `scene progression system` 은 아직 본격 구현 전 단계다.

---

## 5. 게임 시작 후 플레이어가 조작 가능해질 때까지 호출되는 주요 로직

아래가 기본 게임 시작 흐름이다.

### A. 씬 로드

`Game.unity` 로드

### B. 씬 오브젝트 Awake 단계

#### `LevelGenerator.Awake()`

- `LevelGenerator.instance` 설정
- `Resources` 에서 타일/배경 프리팹 로드
- 씬의 `_BOUNDS`, `_BACKGROUND`, `_ROOMS`, `_DEBUG` 부모 오브젝트 참조 확보
- `Rooms[,]`, `Tiles[,]` 배열 생성

#### `GameManager.Awake()`

- `CreateSubManagers()` 호출
- 자식으로 `PlatformManager`, `EntityManager`, `TimerManager` 생성

#### 각 매니저 Awake

- `EntityManager.Instance`, `PlatformManager.Instance`, `TimerManager.Instance` 설정

### C. `GameManager.Start()`

- `InitializeLevel()` 호출

### D. `InitializeLevel()` 내부

#### 일반 시작 경로

1. `levelGenerator.GenerateLevel()`
   - 메인 경로용 룸 생성
   - 나머지 룸 생성

2. `levelGenerator.SetupLevel()`
   - 씬에 존재하는 `Tile` 들 초기화
   - 타일 비주얼 세팅
   - 레벨 외곽 경계 생성
   - 배경 생성

3. `levelGenerator.PlaceEntranceAndExit()`
   - 첫 룸/마지막 룸 기준 entrance, exit 생성

4. 플레이어 존재 여부 확인
   - 없으면 `SpawnPlayer(levelGenerator.entrance.transform.position)`
   - 있으면 기존 플레이어와 카메라 연결

#### 테스트 시작 경로

`TestingChamber` 씬이라면 먼저 `TestingChamber.Awake()` 가:

- `useExistingSceneContent = true` 설정
- hand-placed entrance/exit 연결

그 후 `GameManager.InitializeLevel()` 에서는:

- `GenerateLevel()` 대신 `ScanAndRegisterExistingEntities()` 실행
- 기존 씬 오브젝트를 등록하고 그대로 사용

### E. `SpawnPlayer()` 내부

1. `Player` 프리팹 Instantiate
2. `CameraFollow` 프리팹 Instantiate
3. `camInstance.Initialize(playerInstance)` 호출
4. `playerInstance.cam = camInstance`

### F. 플레이어 생성 직후 초기화

#### `Player.Awake()`

- `EntityPhysics`, `EntityHealth`, `EntityVisuals` 참조 확보
- 점프용 중력과 최대/최소 점프 속도 계산
- `PlayerInput`, `PlayerAudio`, `PlayerInventory`, `PlayerAccessories`, `PlayerHolding` 참조 확보
- 체력 이벤트 등록

#### `Player.OnEnable()`

- `EntityManager.Register(this)` 호출
- 즉 플레이어는 본 업데이트 대상이 된다

#### `PlayerInput.Awake()`

- `Player` 참조 확보

#### `PlayerInput.OnEnable()`

- `EntityManager.RegisterEarlyTickable(this)` 호출
- 즉 입력 단계 대상이 된다

#### `Player.Start()`

- `stateMachine.AttemptToChangeState(groundedState)` 호출
- 플레이어 초기 상태가 `Grounded` 로 설정됨

#### `PlayerGroundedState.EnterState()`

- 지상 상태 진입에 필요한 콜백 연결
- 이전 상태가 공중이었다면 착지 사운드 재생

### G. 조작 가능해지는 첫 프레임

그 다음 `GameManager.Update()` 부터 플레이어가 실제 조작 가능해진다.

순서:

1. `EntityManager.EarlyTick()`
2. `PlayerInput.EarlyTick()`
3. 현재 상태가 입력을 받음
4. `EntityManager.Tick()`
5. `Player.Tick()`
   - 현재 상태 업데이트
   - 속도 계산
   - `Physics.Move()` 실행
6. `EntityManager.LateTick()`
7. `Player.LateTick()`
   - 적 접촉 처리 등 후처리

즉 플레이어가 조작 가능해지는 시점은:

- `Player.Start()` 에서 `groundedState` 가 잡히고
- `PlayerInput.OnEnable()` 이 등록된 뒤
- 다음 `GameManager.Update()` 의 `EarlyTick()` 부터다

---

## 6. 호출 흐름 요약

### 정상 시작

`Game.unity`
-> `LevelGenerator.Awake()`
-> `GameManager.Awake()`
-> `CreateSubManagers()`
-> `GameManager.Start()`
-> `InitializeLevel()`
-> `LevelGenerator.GenerateLevel()`
-> `LevelGenerator.SetupLevel()`
-> `LevelGenerator.PlaceEntranceAndExit()`
-> `GameManager.SpawnPlayer()`
-> `Player.Awake()`
-> `Player.OnEnable()`
-> `PlayerInput.Awake()`
-> `PlayerInput.OnEnable()`
-> `Player.Start()`
-> `stateMachine.AttemptToChangeState(groundedState)`
-> 다음 프레임 `GameManager.Update()`
-> `EntityManager.EarlyTick()`
-> `PlayerInput.EarlyTick()`
-> 플레이어 조작 가능

### 테스트 시작

`TestingChamber.Awake()`
-> `gameManager.useExistingSceneContent = true`
-> `LevelGenerator` 에 entrance/exit 주입
-> 이후 `GameManager.InitializeLevel()`
-> `ScanAndRegisterExistingEntities()`
-> `SetupLevel()`
-> 기존 플레이어/카메라 연결 또는 스폰

---

## 7. 핵심 해석

이 프로젝트는 별도의 전용 부트 씬이나 복잡한 로더가 없다.
실질적인 시작점은 `Game.unity` 의 `GameManager` 이고,
`GameManager` 가 레벨을 준비한 뒤 플레이어를 스폰하며,
다음 프레임부터 자신이 직접 관리하는 루프 안에서 플레이어 입력과 움직임을 돌린다.
