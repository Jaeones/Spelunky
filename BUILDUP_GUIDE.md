# BUILDUP_GUIDE

## 목적

이 문서는 이 저장소를 `그대로 수정`하는 것이 아니라,
새 Unity 프로젝트에서 같은 구조를 다시 만들 수 있도록 빌드업 순서를 제안한다.

핵심 원칙은 하나다.

**절차적 생성보다 먼저, 게임 루프와 커스텀 물리를 만든다.**

---

## 전체 빌드업 순서

1. 빈 프로젝트와 테스트 씬 준비
2. 게임 루프 뼈대 만들기
3. 커스텀 물리 만들기
4. 최소 플레이어 이동 만들기
5. 플레이어 상태 확장하기
6. 상호작용과 아이템 붙이기
7. 적 한 종류 붙이기
8. 타일/룸 시스템 만들기
9. 절차적 레벨 생성 붙이기
10. UI, 사운드, 폴리시 정리하기

---

## Step 0. 새 프로젝트를 어떤 방식으로 시작할까

새 Unity 프로젝트에서는 처음부터 완성형 씬 하나로 가지 말고, 씬을 역할별로 분리하는 것이 좋다.

### 추천 씬 구성

- `Bootstrap`
  - 최소 `GameManager` 와 매니저만 있는 시작 씬
- `PhysicsSandbox`
  - 바닥, 벽, 천장, 원웨이 플랫폼 물리 검증 씬
- `PlayerSandbox`
  - 플레이어 이동/점프/상호작용 검증 씬
- `RoomSandbox`
  - 손배치 룸과 타일 검증 씬
- `Game`
  - 나중에 절차 생성까지 연결하는 최종 씬

### 추천 폴더 구성

- `Assets/Scenes`
- `Assets/Scripts/Core`
- `Assets/Scripts/Managers`
- `Assets/Scripts/Entity`
- `Assets/Scripts/Player`
- `Assets/Scripts/Level`
- `Assets/Scripts/Items`
- `Assets/Scripts/Enemies`
- `Assets/Prefabs`
- `Assets/Sprites`

---

## Step 1. 게임 루프 뼈대부터 만들기

이 저장소 기준으로 가장 먼저 따라 만들어야 하는 것은 `GameManager` 와 하위 매니저 구조다.

### 참고 파일

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`

### 새 프로젝트에서 먼저 만들 것

- `GameManager`
- `EntityManager`
- `PlatformManager`
- `TimerManager`
- `IEarlyTickable`
- `ITickable`
- `ILateTickable`

### 목표

프레임 순서를 아래처럼 강제로 제어할 수 있어야 한다.

`input -> platform -> entity -> late -> timer`

### 이 단계에서 검증할 것

- 임의 테스트 오브젝트가 EarlyTick / Tick / LateTick 에서 순서대로 로그를 남긴다
- `GameManager` 가 매니저를 만들고 루프를 돌린다

### 아직 만들지 말 것

- 플레이어 상태 전체
- 절차 생성
- 적 AI
- 아이템 다양성

---

## Step 2. 커스텀 물리를 먼저 완성하기

이 프로젝트의 진짜 핵심은 `Player` 가 아니라 `EntityPhysics` 다.
플레이어, 적, 블록, 폭탄, 이동 플랫폼이 전부 여기에 기대고 있다.

### 참고 파일

- `Assets/Scripts/Entity/EntityPhysics.cs`
- `Assets/Scripts/Misc/PhysicsManager.cs`

### 구현 순서

1. 픽셀 위치 정수화
2. 서브픽셀 누적
3. X축 이동
4. Y축 이동
5. 바닥 체크
6. 블로킹 레이어 마스크
7. 원웨이 플랫폼 처리
8. overlap 이벤트
9. 외부 델타와 이동 플랫폼 대응

### 최소 성공 기준

- 바닥에서 정확히 멈춘다
- 벽을 뚫지 않는다
- 천장에 닿으면 위로 더 가지 않는다
- 원웨이 플랫폼은 아래에서 통과, 위에서 착지 가능하다

### 왜 이걸 먼저 해야 하나

플레이어 점프감, 행잉, 사다리, 폭탄, 블록 밀기, 적 충돌은 전부 물리가 안정돼야 정상적으로 쌓인다.

---

## Step 3. 최소 플레이어만 만든다

이 단계 목표는 `재미있는 완성 캐릭터`가 아니라 `신뢰할 수 있는 이동체`다.

### 참고 파일

- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerInput.cs`
- `Assets/Scripts/Player/States/PlayerState.cs`

### 처음에는 이것만 구현

- 좌우 이동
- 점프
- 점프 버튼을 일찍 떼면 낮게 뛰기
- 바라보는 방향 전환
- 기본 중력

### 상태는 두 개만

- `Grounded`
- `InAir`

### 이 단계에서 중요한 것

- 입력은 `PlayerInput` 에서 읽고
- 처리 로직은 현재 상태가 받고
- 실제 이동은 `Player` 가 속도를 계산한 뒤
- 마지막 이동은 `EntityPhysics.Move()` 로 해결한다

### 성공 기준

- 빈 방에서 조작감이 좋다
- 착지 직후 떨림이 없다
- 점프 높이가 예측 가능하다

---

## Step 4. 플레이어 상태를 확장한다

최소 이동이 안정되면 그때 상태를 늘린다.

### 참고 파일

- `Assets/Scripts/Player/States/*`
- `Assets/Scripts/Entity/StateMachine.cs`

### 추천 추가 순서

1. `Grounded`
2. `InAir`
3. `Climbing`
4. `Hanging`
5. `CrawlToHang`
6. `EnterDoor`
7. `Splat`

### 이유

- `Grounded` 와 `InAir` 가 제일 많은 분기점을 만든다
- `Climbing`, `Hanging` 은 스냅과 충돌 예외가 필요해서 늦게 넣는 편이 낫다
- `EnterDoor` 는 비교적 단순하다
- `Splat` 은 결과 상태이므로 가장 뒤로 밀 수 있다

### 이 단계에서 자주 생기는 함정

- 행잉이 불안정하면 상태가 아니라 물리나 위치 보정이 문제일 가능성이 크다
- 원웨이 플랫폼 낙하 처리가 없으면 점프/드롭다운이 꼬인다

---

## Step 5. 상호작용과 아이템을 올린다

이 프로젝트에서 상호작용은 `상태 + 플레이어가 들고 있는 것 + 현재 자세`의 조합으로 결정된다.

### 참고 파일

- `Assets/Scripts/Player/PlayerHolding.cs`
- `Assets/Scripts/Player/PlayerInventory.cs`
- `Assets/Scripts/Items/ThrowableItem.cs`
- `Assets/Scripts/Items/Bomb.cs`
- `Assets/Scripts/Items/Rope.cs`
- `Assets/Scripts/Entity/PhysicsBody.cs`

### 추천 구현 순서

1. `PlayerHolding`
2. `PlayerInventory`
3. 기본 throwable 하나
4. `Bomb`
5. `Rope`
6. 장비류

### 먼저 검증할 행동

- crouch + action 으로 집기/내려놓기
- 들고 있는 throwable 던지기
- 폭탄 사용 시 인벤토리 감소
- 로프 사용 시 인벤토리 감소

### 중요한 이유

이 단계에서야 비로소 Spelunky 특유의 `공격 버튼 하나에 여러 행동이 겹치는 구조`가 살아난다.

---

## Step 6. 적은 한 종류만 붙인다

여기서 욕심내지 말고 적 한 종류만 만들어도 충분하다.

### 참고 파일

- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Enemies/Snake/*`
- `Assets/Scripts/Entity/EntityHealth.cs`
- `Assets/Scripts/Entity/DamageReceiver.cs`

### 추천 첫 적

- `Snake`

### 이유

- 이동 패턴이 비교적 단순하다
- stomp 와 contact damage 검증이 쉽다
- 플레이어와 적 모두 같은 물리/체력 기반을 공유하는지 확인하기 좋다

### 이 단계 성공 기준

- 플레이어가 적을 밟아 공격할 수 있다
- 적이 플레이어를 밀치고 데미지를 준다
- 적이 죽는다
- 플레이어도 죽으면 게임오버 흐름으로 들어간다

---

## Step 7. 타일과 룸 시스템을 만든다

절차 생성 전에 먼저 `손으로 만든 방`을 구성할 수 있어야 한다.

### 참고 파일

- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/Block.cs`
- `Assets/Scripts/LevelGenerator/MovingPlatform.cs`

### 먼저 구현할 것

- 타일 좌표 등록
- 타일 이웃 조회
- 타일 장식/스프라이트 결정
- 룸 오프닝 메타데이터(top/right/down/left)
- 손배치 room prefab
- entrance/exit 수동 배치

### 성공 기준

- 방 하나를 손으로 만들고 자연스럽게 플레이 가능하다
- 타일 제거 시 주변 비주얼이 갱신된다
- 출구 위치를 룸 안에서 찾을 수 있다

---

## Step 8. 절차적 레벨 생성은 마지막에 붙인다

여기까지 안정화된 뒤에야 `LevelGenerator` 를 따라 만드는 것이 맞다.

### 참고 파일

- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`

### 구현 순서

1. 룸 2차원 배열
2. 메인 경로 생성
3. 남은 칸 채우기
4. 씬 안의 타일 스캔
5. 타일 초기화
6. 타일 셋업
7. 경계 오브젝트 생성
8. 배경 생성
9. entrance/exit 자동 배치

### 이 단계에서 자주 놓치는 것

- `LevelGenerator` 는 리소스 로딩과 씬의 부모 오브젝트 이름에 의존한다
- `Tile` 등록이 틀리면 entrance/exit 찾기가 망가진다
- 룸 오프닝 메타데이터가 틀리면 경로 생성이 붕괴한다

### 성공 기준

- 생성된 맵에 실제로 들어가서 끝까지 플레이 가능하다

---

## Step 9. 그 다음은 폴리시다

마지막에 붙여야 하는 것들:

- UI 연결
- 사운드 정리
- 더 많은 적
- 더 많은 아이템
- 디버그 툴
- 테스트 씬 정리

이 단계는 `게임이 이미 동작하는 상태`에서 들어가야 한다.

---

## 새 프로젝트에서 추천하는 실제 작업 순서

### 1주차

- `GameManager`, `EntityManager`, `TimerManager` 구축
- `EntityPhysics` 기본 이동/충돌 구축
- `PhysicsSandbox` 에서 바닥/벽/천장/원웨이 검증

### 2주차

- `PlayerInput`, `Player`, `Grounded`, `InAir` 구현
- 이동/점프 감각 조정

### 3주차

- `PlayerHolding`, `PlayerInventory`, throwable, bomb, rope 구현
- `PlayerSandbox` 에서 상호작용 검증

### 4주차

- 적 한 종류 구현
- 체력/데미지/넉백/죽음 흐름 연결

### 5주차

- `Tile`, `Room`, `Exit`, 손배치 룸 테스트

### 6주차

- `LevelGenerator` 붙이기
- `Game` 씬 통합

---

## 절대 순서를 바꾸지 않는 것이 좋은 것들

아래는 가능하면 뒤집지 않는 것이 좋다.

1. `EntityPhysics` 보다 먼저 플레이어를 완성하려고 하지 말 것
2. 손배치 룸보다 먼저 절차 생성을 만들지 말 것
3. 적 여러 종류를 한 번에 넣지 말 것
4. 상호작용보다 먼저 장비/아이템 다양성을 늘리지 말 것
5. 테스트 씬 없이 바로 최종 씬에서 디버깅하지 말 것

---

## 새 프로젝트의 첫 마일스톤

가장 좋은 첫 마일스톤은 이것이다.

**플레이어가 빈 테스트 씬에서 달리고, 점프하고, 원웨이 플랫폼 위에 착지하고, 투척 아이템 하나를 던질 수 있는 상태**

여기까지 오면 이 저장소의 60퍼센트 이상을 따라 만들 수 있는 기반이 생긴다.

---

## 이 저장소를 참고할 때의 읽기 순서

새 프로젝트에서 막히면 아래 순서로 다시 보면 된다.

1. `REPO_MAP.md`
2. `Assets/Scripts/Misc/GameManager.cs`
3. `Assets/Scripts/Entity/EntityPhysics.cs`
4. `Assets/Scripts/Player/Player.cs`
5. `Assets/Scripts/Player/States/PlayerState.cs`
6. `Assets/Scripts/Player/PlayerHolding.cs`
7. `Assets/Scripts/LevelGenerator/Tile.cs`
8. `Assets/Scripts/LevelGenerator/Room.cs`
9. `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
