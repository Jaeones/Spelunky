# FIRST_FILES_TO_READ

## 목적

이 문서는 이 저장소를 처음 읽을 때 어떤 파일부터 봐야 전체 구조를 빠르게 잡을 수 있는지 우선순위대로 추천한다.

원칙은 다음이다.

- 먼저 `시작 흐름` 을 본다.
- 다음으로 `업데이트 루프` 를 본다.
- 그 다음 `공통 물리` 를 본다.
- 이후 `플레이어` 와 `레벨` 을 본다.

---

## 우선순위 1. `Assets/Scripts/Misc/GameManager.cs`

가장 먼저 읽어야 한다.

이유:

- 게임이 어디서 시작하는지 알 수 있다.
- 어떤 매니저가 생성되는지 보인다.
- 프레임 순서가 무엇인지 바로 이해된다.
- `LevelGenerator`, `Player`, `Camera` 가 어떻게 이어지는지 알 수 있다.

읽을 때 볼 포인트:

- `Awake()`
- `Start()`
- `InitializeLevel()`
- `Update()`

---

## 우선순위 2. `Assets/Scripts/Managers/EntityManager.cs`

이유:

- 이 프로젝트가 일반적인 Unity `Update` 난사 구조가 아니라는 걸 이해하게 해준다.
- `IEarlyTickable`, `ITickable`, `ILateTickable` 이 어떻게 돌아가는지 보인다.
- 입력과 물리 후처리가 왜 분리되어 있는지 이해할 수 있다.

읽을 때 볼 포인트:

- `EarlyTick()`
- `Tick()`
- `LateTick()`
- `Register()` / `Unregister()`

---

## 우선순위 3. `Assets/Scripts/Entity/EntityPhysics.cs`

이유:

- 이 저장소의 실제 핵심이다.
- 플레이어, 적, 블록, 아이템 움직임을 모두 떠받친다.
- 이 파일을 이해하면 왜 프로젝트 전체 감각이 Unity 기본 물리와 다른지 알 수 있다.

읽을 때 볼 포인트:

- `Move()`
- `MoveX()`
- `MoveY()`
- `CheckGround()`
- 원웨이 플랫폼 처리
- overlap/collision 이벤트 처리

---

## 우선순위 4. `Assets/Scripts/Player/Player.cs`

이유:

- 플레이어가 어떤 컴포넌트 조합으로 이루어졌는지 보인다.
- 이동, 점프, 공격, 폭탄, 로프, 적 접촉 처리까지 플레이어 본체 책임을 확인할 수 있다.
- 상태머신이 어디에서 실제로 사용되는지 보인다.

읽을 때 볼 포인트:

- `Awake()`
- `Tick()`
- `LateTick()`
- `CalculateVelocity()`
- `HandleEnemyOverlaps()`
- `OnHealthChanged()`

---

## 우선순위 5. `Assets/Scripts/Player/States/PlayerState.cs`

이유:

- 입력이 어떻게 상태별 행동으로 바뀌는지 이해하게 해준다.
- 점프, 공격, 폭탄, 로프, 사용 입력의 공통 규칙이 여기 있다.
- 플레이어 설계가 `거대한 Player 클래스 하나`가 아니라 상태 기반이라는 점이 선명해진다.

읽을 때 볼 포인트:

- `OnDirectionalInput()`
- `OnJumpInputDown()`
- `OnJumpInputUp()`
- `OnAttackInputDown()`
- `ChangePlayerVelocity()`

---

## 우선순위 6. `Assets/Scripts/Player/PlayerInput.cs`

이유:

- 입력이 실제로 어디서 읽히는지 알 수 있다.
- 입력이 곧바로 이동으로 이어지지 않고 상태에 전달된다는 점을 확인할 수 있다.

읽을 때 볼 포인트:

- `EarlyTick()`
- 각 버튼이 어떤 상태 메서드에 연결되는지

---

## 우선순위 7. `Assets/Scripts/LevelGenerator/LevelGenerator.cs`

이유:

- 절차적 생성의 큰 그림을 잡을 수 있다.
- 방 생성, 타일 초기화, 배경/경계 생성, 입구/출구 배치 순서를 볼 수 있다.
- 이 프로젝트에서 레벨 시스템이 어디까지 책임지는지 알 수 있다.

읽을 때 볼 포인트:

- `GenerateLevel()`
- `SetupLevel()`
- `PlaceEntranceAndExit()`
- `CreateMainPathRooms()`
- `CreateRemainingRooms()`

---

## 우선순위 8. `Assets/Scripts/LevelGenerator/Tile.cs`

이유:

- 타일이 단순 스프라이트가 아니라 좌표를 가진 데이터 단위라는 걸 보여준다.
- 이웃 검사와 비주얼 갱신 방식이 보인다.
- 레벨 생성이 실제로 어떤 단위 위에서 작동하는지 알 수 있다.

읽을 때 볼 포인트:

- `InitializeTile()`
- `SetupTile()`
- `Remove()`

---

## 우선순위 9. `Assets/Scripts/LevelGenerator/Room.cs`

이유:

- 룸 프리팹이 단순 배경 덩어리가 아니라 연결 메타데이터를 가진다는 걸 보여준다.
- entrance/exit 배치가 룸 안에서 어떻게 결정되는지 알 수 있다.

읽을 때 볼 포인트:

- `top/right/down/left`
- `GetRoomTiles()`
- `GetSuitableEntranceOrExitTile()`

---

## 우선순위 10. `Assets/Scripts/TestingChamber.cs`

이유:

- 이 저장소를 어떻게 테스트 중심으로 읽어야 하는지 힌트를 준다.
- 절차 생성을 우회하고 손배치 콘텐츠로 검증하는 패턴을 보여준다.
- 새 프로젝트에서 따라 만들 때도 매우 좋은 개발 방식이다.

읽을 때 볼 포인트:

- `gameManager.useExistingSceneContent = true`
- entrance/exit 수동 주입 방식

---

## 읽는 순서 요약

가장 좋은 10개 읽기 순서는 아래와 같다.

1. `Assets/Scripts/Misc/GameManager.cs`
2. `Assets/Scripts/Managers/EntityManager.cs`
3. `Assets/Scripts/Entity/EntityPhysics.cs`
4. `Assets/Scripts/Player/Player.cs`
5. `Assets/Scripts/Player/States/PlayerState.cs`
6. `Assets/Scripts/Player/PlayerInput.cs`
7. `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
8. `Assets/Scripts/LevelGenerator/Tile.cs`
9. `Assets/Scripts/LevelGenerator/Room.cs`
10. `Assets/Scripts/TestingChamber.cs`

---

## 이 다음에 읽으면 좋은 파일

위 10개를 본 뒤에는 아래 순서로 확장하면 좋다.

- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`
- `Assets/Scripts/Entity/StateMachine.cs`
- `Assets/Scripts/Entity/PhysicsBody.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

---

## 한 줄 추천

시간이 정말 없으면 `GameManager -> EntityManager -> EntityPhysics -> Player -> PlayerState -> LevelGenerator` 순서만 먼저 읽어도 전체 구조가 크게 보인다.
