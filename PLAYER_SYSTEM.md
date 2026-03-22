# PLAYER_SYSTEM

## 목적

이 문서는 이 저장소에서 플레이어 관련 시스템만 따로 떼어 분석한 것이다.
중점은 `입력 -> 이동/점프 -> 낙하/착지 -> 아이템 사용 -> 피격/사망 -> 애니메이션` 흐름이다.

---

## 1. 입력 처리

### 관련 파일

- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Misc/GameManager.cs`

### 핵심 메서드

- `PlayerInput.EarlyTick()`
- `PlayerState.OnDirectionalInput()`
- `PlayerState.OnJumpInputDown()`
- `PlayerState.OnJumpInputUp()`
- `PlayerState.OnBombInputDown()`
- `PlayerState.OnRopeInputDown()`
- `PlayerState.OnUseInputDown()`
- `PlayerState.OnAttackInputDown()`

### 데이터 흐름

1. `GameManager.Update()` 가 `EntityManager.EarlyTick()` 호출
2. `PlayerInput` 가 early tick 대상이라 `EarlyTick()` 실행
3. `Input.GetAxisRaw`, `Input.GetButtonDown`, `Input.GetKeyDown` 로 입력 읽음
4. 입력은 직접 처리하지 않고 `CurrentPlayerState` 로 전달
5. 상태가 플레이어의 `directionalInput`, `requestedVelocity`, 행동 요청을 바꿈
6. 이후 physics phase 에서 `Player.Tick()` 이 실제 이동 계산 수행

핵심 포인트:

- 입력 처리는 `Player` 가 직접 하지 않는다.
- 상태머신이 입력 해석의 중심이다.
- 공격 버튼 하나가 집기/던지기/장비 사용/채찍 공격으로 분기된다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `Player`
- `PlayerInput`
- `PlayerGroundedState`
- `PlayerInAirState`
- `PlayerHangingState`
- `PlayerClimbingState`
- `PlayerCrawlToHangState`
- `PlayerEnterDoorState`
- `PlayerSplatState`
- Input Manager 축/버튼 설정
  - `Horizontal`
  - `Vertical`
  - `Jump`
  - `Sprint Keyboard`
  - `Sprint Controller`

---

## 2. 좌우 이동

### 관련 파일

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Entity/EntityVisuals.cs`

### 핵심 메서드

- `Player.Tick()`
- `Player.SetPlayerSpeed()`
- `Player.CalculateVelocity()`
- `PlayerGroundedState.UpdateState()`
- `PlayerGroundedState.ChangePlayerVelocity()`
- `PlayerState.OnDirectionalInput()`
- `EntityPhysics.Move()`
- `EntityVisuals.FlipCharacter()`

### 데이터 흐름

1. 방향 입력이 `PlayerState.OnDirectionalInput()` 에 들어감
2. 이때 `player.directionalInput` 이 설정되고 좌우 반전이 필요하면 `Visuals.FlipCharacter()` 실행
3. `Player.Tick()` 에서 현재 상태의 `UpdateState()` 호출
4. `SetPlayerSpeed()` 가 상황에 따라 `crawlSpeed`, `runSpeed`, `sprintSpeed` 선택
5. `CalculateVelocity()` 가 `requestedVelocity.x` 계산
6. 상태가 필요하면 `ChangePlayerVelocity()` 로 추가 보정
7. 마지막에 `Physics.Move(requestedVelocity * Time.deltaTime)` 로 실제 이동

핵심 포인트:

- 이동 속도 자체는 `Player` 가 계산한다.
- 지상 이동 제약은 `PlayerGroundedState` 가 건다.
- 실제 벽 충돌/픽셀 이동은 `EntityPhysics` 가 해결한다.
- 스프린트와 웅크리기 상태에 따라 속도와 애니메이션이 달라진다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `Player`
  - `accelerationTime`
  - `crawlSpeed`
  - `runSpeed`
  - `sprintSpeed`
  - `pushBlockSpeed`
- `EntityPhysics`
  - `blockingMask`
  - `canPushBlocks`
- `EntityVisuals`
  - 자식 `SpriteRenderer`
  - 자식 `SpriteAnimator`
- `PlayerGroundedState`
  - `crawlAnimation`
  - `pushAnimation`
  - `runAnimation`
  - `duckAnimation`
  - `idleAnimation`
  - `unsteadyAnimation`

---

## 3. 점프

### 관련 파일

- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`
- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Player/PlayerAudio.cs`

### 핵심 메서드

- `PlayerState.OnJumpInputDown()`
- `PlayerState.OnJumpInputUp()`
- `Player.CalculateJumpVelocityForHeight()`
- `Player.GetCurrentMaxJumpHeight()`
- `Player.CalculateVelocity()`
- `PlayerInAirState.OnJumpInputDown()`

### 데이터 흐름

1. 점프 입력이 `PlayerInput.EarlyTick()` 에서 감지됨
2. 현재 상태의 `OnJumpInputDown()` 호출
3. `PlayerState` 가 현재 점프 높이에 맞는 초기 Y 속도를 계산
4. `player.requestedVelocity.y` 에 점프 속도 입력
5. 점프 사운드 재생
6. 상태를 `inAirState` 로 전환
7. 점프 버튼을 떼면 `OnJumpInputUp()` 에서 Y 속도를 줄여 짧은 점프 구현
8. 이후 매 프레임 `Player.CalculateVelocity()` 가 중력을 누적

핵심 포인트:

- 점프 높이는 `maxJumpHeight`, `minJumpHeight`, `timeToJumpApex` 로 유도된다.
- 짧은 점프는 버튼을 뗄 때 Y 속도를 잘라서 만든다.
- 공중에서의 추가 점프는 완전한 더블점프가 아니라 `groundedGracePeriod` 기반의 짧은 유예 시간만 허용한다.
- 아래 방향 입력 + 원웨이 플랫폼 위 점프는 드롭다운으로 처리된다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `Player`
  - `maxJumpHeight`
  - `minJumpHeight`
  - `timeToJumpApex`
  - `groundedGracePeriod`
- `PlayerAudio`
  - `jumpClip`
- `PlayerInAirState`
  - `jumpAnimation`
- `PlayerAccessories`
  - 점프 보너스 관련 값

---

## 4. 떨어짐/착지 판정

### 관련 파일

- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`

### 핵심 메서드

- `PlayerGroundedState.UpdateState()`
- `PlayerInAirState.UpdateState()`
- `PlayerInAirState.OnEntityPhysicsCollisionEnter()`
- `EntityPhysics.Move()`
- `EntityPhysics.CheckGround()`
- `Player.BeginFallThroughPlatformWindow()`

### 데이터 흐름

1. `EntityPhysics.Move()` 가 이동 후 `collisionInfo.down` 을 갱신
2. 지상 상태에서는 `PlayerGroundedState.UpdateState()` 가 `collisionInfo.down` 을 검사
3. 바닥이 없으면 `inAirState` 로 전환
4. 공중 상태에서는 `PlayerInAirState` 가 collision enter 이벤트를 듣고 있음
5. `collisionInfo.becameGroundedThisFrame` 이 true 이면 `groundedState` 로 복귀
6. 착지 직후 `PlayerGroundedState.EnterState()` 가 이전 상태가 `inAirState` 면 land 사운드 재생
7. 원웨이 플랫폼 드롭은 `BeginFallThroughPlatformWindow()` 가 짧은 시간 동안 `fallingThroughPlatform` 플래그를 켠다

핵심 포인트:

- 바닥 판정은 `Player` 가 아니라 `EntityPhysics` 가 계산한다.
- 상태 전환은 그 충돌 결과를 읽어서 결정된다.
- 착지 판정은 `becameGroundedThisFrame` 플래그를 활용한다.
- moving platform 도 동일한 바닥 판정 흐름 위에 올라간다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `EntityPhysics`
  - `blockingMask`
  - `Collider offset/size`
- `Player`
  - `edgeGrabLayerMask`
- `PlayerAudio`
  - `landClip`
- 플랫폼 오브젝트의 `Collider2D`, `OneWayPlatform` 태그

---

## 5. 아이템 사용(밧줄, 폭탄 등)

### 관련 파일

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Player/PlayerAccessories.cs`

### 핵심 메서드

- `PlayerState.OnBombInputDown()`
- `PlayerState.OnRopeInputDown()`
- `PlayerState.OnAttackInputDown()`
- `Player.ThrowBomb()`
- `Player.ThrowRope()`
- `Player.CalculateThrowVelocity()`
- `PlayerHolding.TryPickupNearby()`
- `PlayerHolding.ThrowHeldItem()`
- `PlayerHolding.UseEquipment()`
- `PlayerInventory.UseBomb()`
- `PlayerInventory.UseRope()`
- `Bomb.OnThrown()`
- `Rope.Start()` / `Rope.Tick()`

### 데이터 흐름

#### 폭탄/밧줄

1. 입력 단계에서 폭탄/밧줄 버튼 감지
2. 상태가 `Player.ThrowBomb()` 또는 `Player.ThrowRope()` 호출
3. `PlayerInventory` 에서 수량 감소
4. 연결된 bomb/rope 프리팹 Instantiate
5. 폭탄은 `CalculateThrowVelocity()` 로 던져짐
6. 로프는 현재 위치 또는 바닥 배치 위치 기준으로 생성됨
7. 이후 각 아이템은 자신의 로직으로 움직이거나 전개됨

#### 손에 든 아이템 사용

1. 공격 버튼이 들어오면 `PlayerState.OnAttackInputDown()` 가 현재 상황 분기
2. throwable 을 들고 있으면 던짐
3. crouch 중이면 집기/내려놓기
4. equipment 를 들고 있으면 `UseEquipment()` 실행
5. 아무것도 아니면 채찍 공격

핵심 포인트:

- `공격 버튼` 이 실제로는 다목적 상호작용 키 역할을 한다.
- 폭탄/밧줄은 인벤토리 기반 능력 사용이다.
- 들고 던지는 아이템은 `PlayerHolding` 이 관리한다.
- PitchersMitt, Paste 같은 액세서리가 투척 동작을 바꾼다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `Player`
  - `bomb` 프리팹 참조
  - `rope` 프리팹 참조
  - `throwAnimation`
  - `throwItemSpeed`
  - `placeItemSpeed`
- `PlayerInventory`
  - `numberOfBombs`
  - `numberOfRopes`
- `PlayerHolding`
  - `holdPosition`
  - `pickupLayerMask`
  - `pickupRadius`
- `Bomb` 프리팹
  - `Explosion`
  - `timeToExplode`
  - 사운드 클립
- `Rope` 프리팹
  - `ropeTop`
  - `ropeMiddle`
  - `ropeEnd`
  - `maxRopeLength`
  - `ropeSpeed`

프리팹 흔적:

- `Player.prefab` 에 `bomb`, `rope`, `holdPosition` 참조가 실제로 들어가 있다.

---

## 6. 피격/사망/리스폰

### 관련 파일

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Entity/EntityHealth.cs`
- `Assets/Scripts/Player/States/PlayerSplatState.cs`
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/UI/GameOverUI.cs`
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Entity/DamageReceiver.cs`

### 핵심 메서드

- `Player.HandleEnemyOverlaps()`
- `Player.TryStompEnemy()`
- `Player.ApplyContactDamage()`
- `Player.ApplyKnockback()`
- `Player.OnHealthChanged()`
- `EntityHealth.TakeDamage()`
- `EntityHealth.KillByCrush()`
- `PlayerSplatState.EnterState()`
- `GameManager.HandlePlayerDeath()`
- `GameOverUI.ShowGameOver()`
- `GameOverUI.Restart()`

### 데이터 흐름

#### 일반 피격

1. `Player.LateTick()` 에서 `HandleEnemyOverlaps()` 실행
2. 적 overlap 검사
3. 적 위를 밟았으면 stomp damage + bounce
4. 그렇지 않으면 `ApplyContactDamage()` 실행
5. 넉백 적용 후 `EntityHealth.TakeDamage()` 호출
6. `EntityHealth` 가 체력 감소, 혈흔 생성, 무적시간 처리
7. `HealthChangedEvent` 가 `Player.OnHealthChanged()` 로 전달됨

#### 사망

1. 체력이 0 이하가 되면 `Player.OnHealthChanged()` 실행
2. 한 번만 `GameManager.HandlePlayerDeath(this)` 호출
3. 동시에 `splatState` 로 전환 시도
4. `PlayerSplatState.EnterState()` 에서 애니메이션, 사운드, 혈흔, 콜라이더 비활성화, 입력 잠금 처리
5. `GameManager` 는 `GameOverUI.ShowGameOver(score)` 호출

#### 리스폰

이 프로젝트에는 독립된 `player respawn point` 기반 리스폰 로직이 보이지 않는다.
현재 구조는:

- 게임오버 UI 의 Restart 버튼
- 현재 활성 씬 재로드
- 씬 재시작 후 `GameManager.InitializeLevel()` 이 다시 실행
- entrance 기준으로 플레이어를 새로 스폰

즉 리스폰은 `플레이어만 즉시 부활`이 아니라 `씬 재시작 + 새 플레이어 생성` 방식이다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `Player`
  - `enemyOverlapMask`
  - `stompDamage`
  - `stompTopTolerance`
  - `knockbackDuration`
- `EntityHealth`
  - `maxHealth`
  - `bloodParticles`
  - `killedClip`
  - `crushedClip`
  - `spriteRenderer`
  - `invulnerabilityDuration`
  - `invulnerabilityFlashColor`
- `PlayerSplatState`
  - `splatAnimation`
  - `splatClip`
- `GameOverUI`
  - 기본 폰트/색상 설정

프리팹 흔적:

- `Player.prefab` 에 `maxHealth: 4`, `spriteRenderer` 참조가 들어가 있다.

---

## 7. 애니메이션과 연결되는 지점

### 관련 파일

- `Assets/Scripts/Entity/EntityVisuals.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Player/States/PlayerSplatState.cs`
- `Assets/Scripts/Player/States/PlayerEnterDoorState.cs`
- `Assets/Scripts/Player/PlayerHolding.cs`

### 핵심 메서드

- `EntityVisuals.FlipCharacter()`
- `PlayerGroundedState.HandleHorizontalInput()`
- `PlayerInAirState.UpdateState()`
- `Player.BeginAttack()`
- `Player.ThrowBomb()`
- `Player.ThrowRope()`
- `PlayerHolding.ThrowHeldItem()`
- `PlayerSplatState.EnterState()`
- `PlayerEnterDoorState.EnterState()` / coroutine

### 데이터 흐름

1. `EntityVisuals` 가 자식 `SpriteRenderer`, `SpriteAnimator` 를 찾음
2. 방향 입력이 들어오면 `FlipCharacter()` 로 스프라이트 뒤집기
3. 지상 상태에서는 run/crawl/duck/idle/push/unsteady 애니메이션을 재생
4. 공중 상태에서는 jump 애니메이션 재생
5. 공격 시 whip 애니메이션 재생
6. 폭탄/밧줄/투척 시 throw 애니메이션 재생
7. 사망 시 splat 애니메이션 재생
8. 문 진입 시 enterDoor 애니메이션 재생 후 화면이 검게 페이드

핵심 포인트:

- 애니메이션 선택은 대부분 상태 스크립트와 `Player` 본체에서 수행한다.
- `EntityVisuals` 는 재생 결정자라기보다 `renderer/animator 접근 창구` 역할이 크다.
- 아이템을 들었을 때는 `PlayerHolding` 이 held item 의 스프라이트 정렬과 flip 을 같이 맞춰준다.

### Unity Inspector에서 연결됐을 가능성이 높은 컴포넌트

- `EntityVisuals`
  - 자식 `SpriteRenderer`
  - 자식 `SpriteAnimator`
- `PlayerGroundedState`
  - run/crawl/duck/idle/push/lookUp/unsteady 애니메이션들
- `PlayerInAirState`
  - `jumpAnimation`
- `Player`
  - `attackWithWhipAnimation`
  - `throwAnimation`
- `PlayerSplatState`
  - `splatAnimation`
- `PlayerEnterDoorState`
  - `enterDoorAnimation`

프리팹 흔적:

- `Player.prefab` 에 `jumpClip`, `landClip`, `whipClip` 등 오디오도 연결되어 있어 애니메이션 연출과 함께 사용된다.

---

## 전체 요약

플레이어 시스템의 실질적인 구조는 아래처럼 볼 수 있다.

- 입력은 `PlayerInput`
- 입력 해석은 `PlayerState`
- 실제 이동 계산은 `Player`
- 충돌 해결은 `EntityPhysics`
- 상호작용은 `PlayerHolding` + `PlayerInventory`
- 체력/피격은 `EntityHealth`
- 죽음 후 게임 흐름은 `GameManager` + `GameOverUI`
- 시각 연출은 `EntityVisuals` + 각 상태의 애니메이션 참조

즉 플레이어는 하나의 거대한 스크립트가 아니라,
`입력 / 상태 / 물리 / 인벤토리 / 들기 / 체력 / 비주얼` 로 쪼개진 다중 컴포넌트 구조다.
