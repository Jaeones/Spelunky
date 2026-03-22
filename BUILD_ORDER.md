# BUILD_ORDER

## 목적

이 문서는 이 Spelunky Unity 프로젝트를 `처음부터 따라 만든다`고 가정했을 때,
가장 먼저 복제해야 할 기능 7개를 우선순위대로 정리한 것이다.

핵심 원칙은 하나다.

**나중 기능이 기대는 기반부터 만든다.**

즉:
- 레벨 생성보다 물리를 먼저 만들고
- 아이템보다 플레이어 이동을 먼저 만들고
- 폴리시보다 게임 루프를 먼저 만든다.

---

## 1. 게임 루프와 매니저 구조

### 왜 이 순서가 좋은지

이 프로젝트는 `GameManager` 가 프레임 순서를 직접 통제한다.
이 뼈대가 없으면 입력, 플랫폼, 플레이어 물리, 타이머가 원본처럼 같은 순서로 돌지 않는다.
뒤 기능을 안정적으로 쌓으려면 가장 먼저 필요하다.

### 관련 원본 파일

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`
- `Assets/Scripts/Core/ITickable.cs`

### 필요한 Unity 개념

- `MonoBehaviour`
- `Awake`, `Start`, `Update`
- 런타임 오브젝트 생성
- Singleton 패턴 또는 전역 접근
- 인터페이스 기반 업데이트 분리

### 최소 구현 버전

- `GameManager` 하나
- `EntityManager` 하나
- `IEarlyTickable`, `ITickable`, `ILateTickable`
- `GameManager.Update()` 에서
  - `EarlyTick`
  - `Tick`
  - `LateTick`
순서만 보장하면 충분하다.

### 완성본 대비 생략 가능한 요소

- `PlatformManager`
- `TimerManager`
- 디버그 표시용 리스트
- 게임오버 처리

### 예상 난이도

- 중

---

## 2. 커스텀 물리와 충돌 시스템

### 왜 이 순서가 좋은지

이 프로젝트의 핵심은 플레이어가 아니라 `EntityPhysics` 다.
플레이어, 적, 블록, 아이템, 원웨이 플랫폼이 전부 이 위에 올라간다.
이걸 먼저 만들지 않으면 이후 기능이 계속 흔들린다.

### 관련 원본 파일

- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`
- `Assets/Scripts/Entity/Collision/*`

### 필요한 Unity 개념

- `Rigidbody2D` 와 `BoxCollider2D`
- `Physics2D.OverlapBox`
- `LayerMask`
- 픽셀 단위 이동
- 트리거와 충돌 이벤트 개념

### 최소 구현 버전

- 사각형 하나가 바닥, 벽, 천장에 정확히 멈춤
- 원웨이 플랫폼 위에는 착지 가능
- 아래에서 점프해서 통과 가능
- `Move(Vector2 delta)` 한 메서드로 이동 해결

### 완성본 대비 생략 가능한 요소

- overlap enter/stay/exit 이벤트 전체
- moving platform external delta
- crush 처리
- push block 처리
- attached entity 처리

### 예상 난이도

- 상

---

## 3. 플레이어 기본 이동과 상태 2개

### 왜 이 순서가 좋은지

스펠렁키에서 가장 먼저 손에 들어와야 하는 것은 조작감이다.
그리고 이 프로젝트는 `플레이어 본체 + 상태머신` 구조이기 때문에,
최소 상태 두 개만 있어도 이후 확장이 쉬워진다.

### 관련 원본 파일

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Entity/StateMachine.cs`
- `Assets/Scripts/Entity/EntityVisuals.cs`

### 필요한 Unity 개념

- 입력 처리
- 상태머신
- 캐릭터 이동 속도 보간
- 스프라이트 flip
- 컴포넌트 분리

### 최소 구현 버전

- 좌우 이동
- 바라보는 방향 전환
- `Grounded` / `InAir` 두 상태
- 현재 상태가 입력을 받아 행동 결정
- `Player.Tick()` 에서 속도 계산 후 `EntityPhysics.Move()` 호출

### 완성본 대비 생략 가능한 요소

- `Climbing`
- `Hanging`
- `CrawlToHang`
- `EnterDoor`
- `Splat`
- 액세서리 보너스
- 카메라 세부 동작

### 예상 난이도

- 중

---

## 4. 점프, 낙하, 착지 감각

### 왜 이 순서가 좋은지

이동이 된다고 해서 스펠렁키 느낌이 나는 건 아니다.
짧은 점프, 중력 감각, 착지 전환이 맞아야 플레이어가 “게임 같다”고 느낀다.
또 사다리, ledge grab, 적 밟기 같은 기능도 이 기반이 필요하다.

### 관련 원본 파일

- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerGroundedState.cs`
- `Assets/Scripts/Player/States/PlayerInAirState.cs`
- `Assets/Scripts/Player/PlayerAudio.cs`

### 필요한 Unity 개념

- 중력 계산
- 가변 점프 높이
- 코요테 타임 비슷한 유예 시간
- 착지 판정
- 사운드 재생

### 최소 구현 버전

- 점프 버튼 누르면 뜬다
- 빨리 떼면 낮게 뜬다
- 공중에서 중력 적용
- 착지 시 지상 상태로 복귀
- land 사운드 1개 재생

### 완성본 대비 생략 가능한 요소

- edge grab
- ladder transition
- 원웨이 플랫폼 드롭다운 타이밍 세부 조정
- head-hit 보정
- unsteady 애니메이션

### 예상 난이도

- 중상

---

## 5. 손에 드는 아이템과 능력 사용 1세트

### 왜 이 순서가 좋은지

스펠렁키의 개성은 이동만이 아니라 `행동 버튼 하나로 상황별 다른 액션`이 나온다는 점에 있다.
아이템 시스템을 너무 늦게 붙이면 게임성이 안 살아난다.
하지만 모든 아이템을 한 번에 만들 필요는 없다.

### 관련 원본 파일

- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Items/ThrowableItem.cs`

### 필요한 Unity 개념

- 프리팹 Instantiate
- 부모-자식 Transform
- 인터페이스(`IHoldable`, `IThrowable`, `IEquipment`)
- 반경 탐지(`OverlapCircle`)
- 인벤토리 수량 관리

### 최소 구현 버전

둘 중 하나만 먼저 해도 된다.

추천 A:
- throwable 오브젝트 하나
- 집기 -> 들기 -> 던지기

추천 B:
- 폭탄 하나
- 인벤토리 수량 감소
- 앞쪽으로 투척

### 완성본 대비 생략 가능한 요소

- PitchersMitt
- Paste
- Equipment 사용 분기
- hold item 정렬 순서 세부 처리
- rope 시스템 전체
- 여러 throw angle 분기

### 예상 난이도

- 중

---

## 6. 피격, 체력, 사망, 적 1종

### 왜 이 순서가 좋은지

여기까지 들어가야 플레이 테스트가 실제 게임처럼 된다.
체력/데미지/넉백이 있어야 함정, 적, 아이템이 모두 같은 규칙 위에 올라간다.
적도 여러 종이 아니라 하나만 있어도 충분하다.

### 관련 원본 파일

- `Assets/Scripts/Entity/EntityHealth.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/States/PlayerSplatState.cs`
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Enemies/Snake/*`
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

### 필요한 Unity 개념

- 체력 관리
- 무적 시간
- 넉백
- overlap 기반 접촉 판정
- 상태 전환
- 씬 재시작 또는 게임오버 UI

### 최소 구현 버전

- 적 하나가 좌우로 움직임
- 접촉 시 플레이어 체력 감소
- 넉백 적용
- 체력이 0이면 입력 잠금 또는 사망 상태
- 게임오버 텍스트만 띄워도 충분

### 완성본 대비 생략 가능한 요소

- stomp kill
- whip 전투
- blood particles
- crush death 구분
- GameOverUI 완전 자동 생성
- 점수 표시

### 예상 난이도

- 중

---

## 7. 손배치 1스테이지와 플레이어 스폰

### 왜 이 순서가 좋은지

절차 생성보다 먼저 “플레이 가능한 공간”이 필요하다.
이 프로젝트도 결국 룸 내부는 수동 제작이고, 테스트 씬은 완전 수동 배치다.
따라서 처음에는 hand-built 1스테이지를 만드는 것이 가장 효율적이다.

### 관련 원본 파일

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/TestingChamber.cs`
- `Assets/Scripts/Misc/GameManager.cs`

### 필요한 Unity 개념

- 프리팹 기반 타일 배치
- 부모 오브젝트 구조
- 충돌체와 배경 분리
- 씬 안에서 오브젝트 찾기
- 시작 위치 설정

### 최소 구현 버전

- 바닥 타일
- 벽 타일
- 발판 하나
- entrance 하나
- exit 하나
- `GameManager.Start()` 에서 entrance 기준으로 플레이어 스폰

이 단계에서는 절차 생성 없이 수동 배치만으로 충분하다.

### 완성본 대비 생략 가능한 요소

- `Rooms[,]` 그리드
- 메인 경로 생성
- special room 배치
- background decal 랜덤 생성
- auto entrance/exit tile search
- tile decoration 시스템

### 예상 난이도

- 하

---

## 최종 우선순위 요약

1. 게임 루프와 매니저 구조
2. 커스텀 물리와 충돌 시스템
3. 플레이어 기본 이동과 상태 2개
4. 점프, 낙하, 착지 감각
5. 손에 드는 아이템과 능력 사용 1세트
6. 피격, 체력, 사망, 적 1종
7. 손배치 1스테이지와 플레이어 스폰

---

## 추천 제작 원칙

이 순서를 실제로 진행할 때는 아래 규칙을 지키는 것이 좋다.

- 절차 생성은 마지막에 한다.
- 테스트 씬 없이 바로 최종 씬으로 가지 않는다.
- 적은 하나만 먼저 만든다.
- 아이템도 하나만 먼저 만든다.
- 플레이 감각이 이상하면 항상 `Player` 보다 아래 계층인 `EntityPhysics` 를 먼저 의심한다.
- hand-built 1스테이지가 재미없으면 procedural generation 을 붙여도 재미없다.

---

## 가장 좋은 첫 마일스톤

이 7개 중 앞의 4개만 끝나도 매우 큰 진전이다.

가장 좋은 첫 마일스톤은:

**플레이어가 hand-built 테스트 씬에서 달리고, 점프하고, 원웨이 플랫폼에 착지하고, 상태가 Grounded/InAir 사이에서 안정적으로 전환되는 상태**

여기까지 오면 이후 기능은 비교적 안전하게 얹을 수 있다.
