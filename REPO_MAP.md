# REPO_MAP

## 목적

이 문서는 현재 저장소를 `완성 게임`이 아니라 `따라 만들기 위한 참고 구조`로 읽을 수 있게 해부한 지도다.
핵심 질문은 다음이다.

- 게임은 어디서 시작되는가?
- 플레이어 이동/점프/상호작용은 어디에 들어있는가?
- 맵과 타일, 레벨 생성은 어디에서 관리되는가?
- 시스템끼리는 어떤 순서와 의존성으로 연결되는가?

---

## 1. 시작점

### 실제 시작 씬

빌드 세팅 기준 시작 씬은 `Assets/Scenes/Game.unity`다.

근거:

- `ProjectSettings/EditorBuildSettings.asset` 에 등록된 유일한 씬이 `Assets/Scenes/Game.unity`

### 게임 시작 흐름

`Game.unity`
-> `GameManager` 오브젝트가 시작
-> `GameManager.Awake()` 에서 하위 매니저 생성
-> `GameManager.Start()` 에서 `InitializeLevel()` 호출
-> `LevelGenerator.GenerateLevel()` 또는 테스트 모드 스캔 실행
-> `LevelGenerator.SetupLevel()` 실행
-> 필요 시 `PlaceEntranceAndExit()` 실행
-> 플레이어/카메라 스폰 또는 기존 플레이어 연결
-> 이후 매 프레임 `GameManager.Update()` 가 전체 틱 순서를 제어

### 핵심 시작 스크립트

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/TestingChamber.cs`

### 테스트용 시작 흐름

`Assets/Scenes/TestingChamber.unity` 에는 `TestingChamber` 스크립트가 붙어 있고,
이 스크립트는:

- `gameManager.useExistingSceneContent = true` 로 설정하고
- 씬에 손배치된 entrance/exit 를 `LevelGenerator` 에 직접 주입한다

즉, `TestingChamber` 는 `절차 생성 없이 기존 씬 배치물로 플레이 테스트`하기 위한 우회 진입점이다.

---

## 2. 폴더 단위 구조

### 씬

- `Assets/Scenes/Game.unity`
  - 실제 시작 씬
- `Assets/Scenes/TestingChamber.unity`
  - 생성기를 우회하는 통합 테스트 씬
- `Assets/Scenes/PhysicsInteractions.unity`
  - 이름상 물리 상호작용 테스트용
- `Assets/Scenes/Entities.unity`
  - 이름상 엔티티 테스트용
- `Assets/Scenes/Rooms.unity`
  - 이름상 방/룸 테스트용

### 스크립트 상위 분류

- `Assets/Scripts/Misc`
  - 게임 시작, 물리 전역값, 오디오 등
- `Assets/Scripts/Managers`
  - 틱 순서와 타이머 관리
- `Assets/Scripts/Entity`
  - 커스텀 물리, 체력, 상태머신, 충돌 이벤트
- `Assets/Scripts/Player`
  - 플레이어 본체, 입력, 인벤토리, 들기/던지기, 상태
- `Assets/Scripts/LevelGenerator`
  - 룸, 타일, 출구, 이동 플랫폼, 레벨 생성
- `Assets/Scripts/Items`
  - 폭탄, 로프, 투척 오브젝트, 장비
- `Assets/Scripts/Enemies`
  - 적 공통 베이스와 적별 상태
- `Assets/Scripts/UI`
  - 게임오버 UI 등

---

## 3. 플레이어 이동/점프/상호작용 핵심 스크립트

### 최상위 축

- `Assets/Scripts/Player/Player.cs`
  - 플레이어 본체
  - 이동 속도, 점프 높이, 공격, 폭탄/로프 사용, 적 접촉 판정 담당
  - 상태머신을 들고 있으며 실제 이동은 `EntityPhysics` 에 위임

### 입력

- `Assets/Scripts/Player/PlayerInput.cs`
  - `IEarlyTickable`
  - `GameManager -> EntityManager.EarlyTick()` 단계에서 입력을 읽음
  - 입력을 직접 처리하지 않고 현재 상태(`CurrentPlayerState`)에 전달

### 상태머신

- `Assets/Scripts/Entity/StateMachine.cs`
  - 플레이어/적 공용 상태머신
- `Assets/Scripts/Player/States/PlayerState.cs`
  - 플레이어 상태 베이스
  - 방향 입력, 점프 입력, 공격/사용/폭탄/로프 입력에 대한 공통 처리 정의
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Player/States/PlayerClimbingState.cs`
- `Assets/Scripts/Player/States/PlayerHangingState.cs`
- `Assets/Scripts/Player/States/PlayerCrawlToHangState.cs`
- `Assets/Scripts/Player/States/PlayerEnterDoorState.cs`
- `Assets/Scripts/Player/States/PlayerSplatState.cs`

### 실제 이동/충돌 기반

- `Assets/Scripts/Entity/EntityPhysics.cs`
  - 이 프로젝트의 가장 중요한 기반 계층
  - 픽셀 단위 이동, 충돌 판정, 원웨이 플랫폼 처리, 외부 델타, 겹침 이벤트 담당
- `Assets/Scripts/Misc/PhysicsManager.cs`
  - 중력 상수 제공

### 상호작용 보조 컴포넌트

- `Assets/Scripts/Player/PlayerHolding.cs`
  - 주변 오브젝트 집기, 들기, 내려놓기, 던지기, 장비 사용
- `Assets/Scripts/Player/PlayerInventory.cs`
  - 폭탄, 로프, 골드 수량 관리
- `Assets/Scripts/Player/PlayerAccessories.cs`
  - 장신구 효과 반영
- `Assets/Scripts/Player/PlayerAudio.cs`
  - 점프/채찍 등 사운드
- `Assets/Scripts/Player/PlayerUI.cs`
  - 인벤토리 수치와 UI 연결
- `Assets/Scripts/Player/CameraFollow.cs`
  - 카메라 추적

### 플레이어 관련 읽기 순서 추천

1. `PlayerInput.cs`
2. `PlayerState.cs`
3. `Player.cs`
4. `EntityPhysics.cs`
5. `PlayerHolding.cs`
6. `PlayerInventory.cs`
7. 각 상태 스크립트

---

## 4. 맵/타일/레벨 생성 핵심 스크립트

### 최상위 축

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
  - 룸 그리드 생성
  - 메인 경로 생성
  - 남은 룸 채우기
  - 타일 초기화/셋업
  - 배경/경계 생성
  - entrance/exit 배치

### 룸 단위

- `Assets/Scripts/LevelGenerator/Room.cs`
  - 룸 프리팹의 연결 방향(top/right/down/left) 보유
  - 해당 룸 안의 타일을 조회해 적절한 entrance/exit 위치를 찾음

### 타일 단위

- `Assets/Scripts/LevelGenerator/Tile.cs`
  - 타일 좌표 등록
  - 주변 타일을 보고 장식/스프라이트 결정
  - 타일 제거 시 `LevelGenerator.Tiles` 갱신

### 레벨 내 특수 오브젝트

- `Assets/Scripts/LevelGenerator/Exit.cs`
  - 출구 트리거, 버튼 프롬프트, 플레이어의 문 진입 가능 상태 연결
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`
  - 이동 플랫폼
  - `PlatformManager` 에 의해 플레이어보다 먼저 이동
- `Assets/Scripts/LevelGenerator/Block.cs`
  - 밀 수 있는 블록
  - `PhysicsBody` 기반
  - 충돌/겹침으로 crush 처리 가능

### 레벨 관련 읽기 순서 추천

1. `LevelGenerator.cs`
2. `Room.cs`
3. `Tile.cs`
4. `Exit.cs`
5. `MovingPlatform.cs`
6. `Block.cs`

---

## 5. 공통 기반 시스템

### 게임 루프와 매니저

- `Assets/Scripts/Misc/GameManager.cs`
  - 전체 시작과 프레임 순서를 관리
- `Assets/Scripts/Managers/EntityManager.cs`
  - `IEarlyTickable`, `ITickable`, `ILateTickable` 목록을 관리
- `Assets/Scripts/Managers/PlatformManager.cs`
  - 이동 플랫폼을 엔티티보다 먼저 갱신
- `Assets/Scripts/Managers/TimerManager.cs`
  - 코루틴 대신 명시적 타이머 처리

### 엔티티 공통 기반

- `Assets/Scripts/Entity/EntityPhysics.cs`
  - 이동/충돌 기반
- `Assets/Scripts/Entity/PhysicsBody.cs`
  - 폭탄, 보물, 블록 같은 물리 오브젝트 공통 베이스
- `Assets/Scripts/Entity/EntityHealth.cs`
  - 체력 시스템
- `Assets/Scripts/Entity/DamageReceiver.cs`
  - 데미지 수신 처리
- `Assets/Scripts/Entity/EntityVisuals.cs`
  - 바라보는 방향, 스프라이트/애니메이션 보조
- `Assets/Scripts/Entity/StateMachine.cs`
  - 플레이어/적 공용 상태 전환

### 적 시스템

- `Assets/Scripts/Enemies/Enemy.cs`
  - 플레이어와 유사하게 `EntityPhysics + EntityHealth + StateMachine` 구조 사용
- `Assets/Scripts/Enemies/*`
  - Bat, Snake, Spider, Caveman 등 적별 상태 구현

### 아이템 시스템

- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Items/ThrowableItem.cs`
- `Assets/Scripts/Items/Equipment/*`
- `Assets/Scripts/Items/Throwables/*`

이들은 대체로 `PlayerHolding`, `PlayerInventory`, `PhysicsBody`, `EntityPhysics` 위에 올라간다.

---

## 6. 시스템 의존성 지도

아래 순서로 읽으면 현재 프로젝트의 의존성이 보인다.

### A. 시작과 루프

`Game.unity`
-> `GameManager`
-> `PlatformManager`, `EntityManager`, `TimerManager`

### B. 레벨 준비

`GameManager.InitializeLevel()`
-> `LevelGenerator.GenerateLevel()` 또는 기존 씬 스캔
-> `LevelGenerator.SetupLevel()`
-> `LevelGenerator.PlaceEntranceAndExit()`
-> 플레이어/카메라 배치

### C. 플레이어 입력과 상태

`PlayerInput.EarlyTick()`
-> 현재 `PlayerState` 로 입력 전달
-> 상태가 `Player` 의 입력값/속도/행동 요청 변경
-> `Player.Tick()` 에서 속도 계산
-> `EntityPhysics.Move()` 로 실제 이동/충돌 해결
-> `Player.LateTick()` 에서 후처리

### D. 상호작용

`PlayerState.OnAttackInputDown()`
-> 투척 중이면 던짐
-> 웅크리고 있으면 집기/내려놓기
-> 장비 들고 있으면 사용
-> 아니면 채찍 공격

즉 상호작용 키 하나가 `플레이어 상태 + 들고 있는 아이템 타입 + 현재 자세`에 따라 분기된다.

### E. 물리 오브젝트

`PhysicsBody`
-> 중력 적용
-> 마찰 적용
-> `EntityPhysics.Move()` 호출
-> 충돌 이벤트/겹침 이벤트로 반응

### F. 적

`Enemy`
-> 상태 업데이트
-> 중력/이동은 `EntityPhysics` 사용
-> 플레이어와 접촉 시 데미지/넉백 연결

### G. 타일과 룸

`LevelGenerator`
-> 룸 프리팹 배치
-> 씬 내 `Tile` 찾기
-> `Tile.InitializeTile()` 로 그리드 배열 등록
-> `Tile.SetupTile()` 로 주변 기반 스프라이트/장식 구성
-> `Room.GetSuitableEntranceOrExitTile()` 로 출구 위치 선정

---

## 7. 새 프로젝트에서 참고해야 할 핵심 사실

이 저장소를 따라 만들 때 가장 중요한 구조적 사실은 다음이다.

1. 이 게임은 `GameManager` 가 프레임 순서를 직접 통제한다.
2. 이동의 진짜 핵심은 `Player` 가 아니라 `EntityPhysics` 다.
3. 플레이어는 입력을 직접 처리하지 않고 `상태머신` 으로 위임한다.
4. 아이템, 적, 블록은 전부 같은 물리 기반 위에 쌓인다.
5. 절차적 생성은 가장 위 레이어이며, 먼저 만들 대상이 아니다.
6. `TestingChamber` 패턴이 있기 때문에 손배치 테스트 씬부터 만드는 것이 맞다.

---

## 8. 따라 읽기용 최소 파일 세트

처음 해부할 때는 이 파일들만 읽어도 전체 구조가 잡힌다.

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Entity/StateMachine.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/TestingChamber.cs`
