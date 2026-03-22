# FIRST_CODE_CHANGES

## 목적

이 문서는 현재 저장소에서 `가장 작은 첫 코드 변경 3개`를 제안한다.
기준은 다음과 같다.

- 한 번에 크게 바꾸지 않는다.
- 구조를 먼저 만든다.
- 기존 플레이를 최대한 안 깨뜨린다.
- 임시 구현과 최종 구현을 구분한다.
- 변경 후 바로 직접 테스트 가능한 단위여야 한다.

---

## 변경 1. `PlayerEnterDoorState`의 직접 씬 리로드 제거

### 대상 파일

- `Assets/Scripts/Player/States/PlayerEnterDoorState.cs`
- `Assets/Scripts/Misc/GameManager.cs`

### 왜 필요한가

현재 이 프로젝트는 출구 진입 시 `SceneManager.LoadScene(scene.name)`로 현재 씬을 다시 로드한다.
이 구조는 4스테이지 게임의 가장 큰 blocker다.

- 다음 스테이지 개념이 없다.
- 런 상태를 유지할 수 없다.
- Stage 전환 연출을 넣을 수 없다.

### 가장 작은 변경 방법

- `PlayerEnterDoorState`는 더 이상 씬을 직접 로드하지 않는다.
- 대신 `GameManager`에 `HandlePlayerEnteredExit()` 같은 메서드를 호출한다.
- 당장은 그 메서드가 여전히 임시로 씬 리로드를 하더라도 괜찮다.
- 중요한 건 `전환 책임을 Player 상태에서 분리`하는 것이다.

### 임시 구현

- `PlayerEnterDoorState -> GameManager.HandlePlayerEnteredExit(player)` 호출
- `GameManager` 내부에서는 기존처럼 같은 씬을 다시 로드하거나, 로그만 남겨도 됨

### 최종 구현

- `GameManager` 또는 `StageManager`가 다음 Stage 로드
- RunState 캡처 후 Transition -> 다음 Stage 시작

### 직접 테스트할 것

- 출구 진입 애니메이션과 입력 잠금이 유지되는지
- 문 진입 후 씬 리로드가 아니라 GameManager 메서드가 호출되는지
- 중복 호출되지 않는지

### 플레이타임/난이도 영향

- 직접 난이도 변화 없음
- 하지만 4스테이지 진행 구조를 열기 때문에 전체 플레이타임 설계의 핵심 전제다.

---

## 변경 2. `RunState` 최소 데이터 클래스 추가

### 대상 파일

- 새 파일 제안: `Assets/Scripts/Core/RunState.cs`
- 연결 후보: `Assets/Scripts/Misc/GameManager.cs`
- 연결 후보: `Assets/Scripts/Player/Player.cs`

### 왜 필요한가

지금은 플레이어가 가진 체력, 폭탄, 로프, 골드, 액세서리를
`한 런의 상태`로 바라보는 구조가 없다.

이 데이터가 없으면:
- 다음 스테이지로 상태를 넘길 수 없고
- 결과 화면도 만들기 어렵고
- 밸런스도 조정하기 어렵다.

### 가장 작은 변경 방법

- 직렬화 가능한 `RunState` 데이터 클래스 하나만 추가한다.
- 필드는 최소한 아래만 포함한다.
  - `currentStageIndex`
  - `health`
  - `bombs`
  - `ropes`
  - `gold`
  - `accessoryIds`

- 아직 저장도, 매니저도 붙이지 않는다.
- 일단 `형태`를 먼저 만든다.

### 임시 구현

- `GameManager`가 `CurrentRunState`를 하나 들고 있게 함
- 디버그 로그로 플레이어 상태를 캡처해볼 수 있게 함

### 최종 구현

- `RunManager`가 소유
- Stage 전환과 Result 화면, 저장 시스템이 이 데이터를 사용

### 직접 테스트할 것

- 플레이 중 상태를 읽어 `RunState`로 채울 수 있는지
- 값이 실제 HUD/플레이어 상태와 일치하는지
- 액세서리 보유 여부를 최소 리스트 형태로 담을 수 있는지

### 플레이타임/난이도 영향

- 직접 난이도 변화 없음
- 자원 경제와 4스테이지 진행 설계의 기반이 된다.

---

## 변경 3. `PlayerUI`의 `GameObject.Find()` 의존성 줄이기

### 대상 파일

- `Assets/Scripts/Player/PlayerUI.cs`

### 왜 필요한가

이 파일은 현재 씬 이름 기반 오브젝트 검색에 강하게 묶여 있다.
씬 구조가 조금만 바뀌어도 HUD가 깨질 가능성이 높다.

특히 앞으로:
- Boot/Menu/Game/Result 구조가 생기고
- HUD, Transition, Result UI가 분리되면
가장 먼저 깨질 가능성이 큰 지점이다.

### 가장 작은 변경 방법

- 모든 UI를 한 번에 갈아엎지 않는다.
- 우선 가장 중요한 필드만 `SerializeField`로 바꾸고 인스펙터 연결로 옮긴다.
- 예: `LifeAmountText`, `BombAmountText`, `RopeAmountText`, `TotalGoldAmountText`, `CurrentGoldAmountText`

### 임시 구현

- `GameObject.Find()`와 `SerializeField`를 병행해도 괜찮다.
- 인스펙터 참조가 있으면 우선 사용하고,
  없으면 기존 Find 로직을 fallback으로 사용

### 최종 구현

- `UIManager`가 HUD를 소유
- `PlayerUI` 또는 HUD Presenter가 데이터 바인딩만 담당

### 직접 테스트할 것

- 기존 Game 씬에서 체력/폭탄/로프/골드가 여전히 정상 표시되는지
- 참조가 누락됐을 때 바로 에러를 확인할 수 있는지
- Stage 전환 구조를 붙여도 HUD가 덜 깨지는지

### 플레이타임/난이도 영향

- 직접 전투 난이도는 안 바뀜
- 하지만 HUD 가독성은 체감 난이도와 리소스 판단에 직접 영향 준다.

---

## 우선순위 요약

1. `PlayerEnterDoorState` 책임 이동
2. `RunState` 최소 구조 추가
3. `PlayerUI` 결합도 완화

이 순서가 좋은 이유:
- 첫 번째는 `진행 구조`를 연다.
- 두 번째는 `상태 유지 구조`를 만든다.
- 세 번째는 `씬 구조 확장`에 대비한다.

즉, 콘텐츠를 넣기 전에 반드시 필요한 최소 구조 변경 3개다.

---

## 이번 단계에서 하지 말아야 할 것

- `GameManager` 전체 분해
- 완전한 `StageManager` 구현
- 세이브 시스템 추가
- 결과 화면 전체 제작
- UI 전면 교체
- 룸 생성 로직 대수술

이건 모두 다음 단계에서 해도 늦지 않다.

---

## 결론

지금 가장 좋은 첫 변경은 `크게 새로 만드는 것`이 아니라,
이미 있는 코드의 책임을 조금만 옮겨서 `다음 확장 지점`을 만드는 것이다.

그 출발점은 아래 3개다.

- 출구 진입이 직접 씬을 다시 로드하지 않게 만들기
- 한 런의 상태를 담을 그릇 만들기
- HUD가 씬 이름 의존성에 덜 묶이게 만들기
