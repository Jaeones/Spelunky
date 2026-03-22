# SCENE_ARCHITECTURE

## 목적

이 문서는 현재 저장소를 `4스테이지 분량의 실제 제작 가능한 게임`으로 재정리할 때 권장되는
씬 구조를 정의한다.

핵심 목표:
- 런 시작, 스테이지 진행, 결과 표시, 재시작 흐름이 분리되어 유지보수가 쉬울 것
- `GameManager 하나에 모든 책임`이 몰리지 않도록 씬 단위로 역할을 나눌 것
- 초반에는 단순하게 시작하되, 나중에 연출과 UI를 붙이기 쉬운 구조일 것

---

## 1. 권장 씬 구성

### 1. Boot

역할:
- 앱 시작 시 가장 먼저 로드되는 씬
- 공용 시스템 초기화
- 저장 데이터 로드
- 사용자 설정 적용
- 다음 씬으로 라우팅

권장 포함 오브젝트:
- `AppRoot`
- `SaveService`
- `SettingsService`
- `AudioBootstrap`
- `SceneRouter`

권장 동작:
1. 필수 서비스 생성
2. 사용자 설정/볼륨/언어 로드
3. 저장 데이터 유효성 검사
4. `Menu` 씬 로드

메모:
- Boot는 가볍게 유지한다.
- 실제 게임 콘텐츠나 HUD는 넣지 않는다.

### 2. Menu

역할:
- 메인 메뉴
- 새 게임 시작
- 이어하기 가능 여부 표시
- 옵션/크레딧 진입

권장 포함 오브젝트:
- `MenuUIRoot`
- `MenuController`
- `BackgroundPresenter`
- `VersionLabel`

권장 버튼:
- `New Run`
- `Continue` 또는 `Resume` 필요 시
- `Options`
- `Quit`

메모:
- 프로토타입 단계에서는 단일 메뉴 씬으로 충분하다.
- 결과 화면도 Menu로 돌아오게 하면 흐름이 단순해진다.

### 3. Game

역할:
- 실제 플레이가 일어나는 메인 씬
- 한 런 동안 반복적으로 재사용되는 공용 플레이 씬
- Stage 데이터에 따라 지형, 적, 함정, 보상, 배경을 구성

권장 포함 오브젝트:
- `GameRoot`
- `RuntimeManagers`
- `StageRoot`
- `EntityRoot`
- `UIRoot`
- `CameraRoot`
- `AudioRoot`

메모:
- 가장 추천하는 구조는 `Game` 씬 하나를 런 전체에서 유지하고,
  Stage 전환 시 씬을 바꾸는 대신 Stage 콘텐츠만 갈아끼우는 방식이다.
- 이유는 플레이어, 카메라, UI, 런 데이터, 오디오를 안정적으로 유지하기 쉽기 때문이다.

### 4. Transition

역할:
- Stage 사이 전환 연출
- 요약 정보 표시
- 다음 스테이지 목표와 현재 자원 상태 정리

권장 사용 방식:
- 별도 씬으로 분리해도 되고,
- 더 현실적으로는 `Game` 씬 안의 전체화면 오버레이 UI로 구현해도 충분하다.

권장 표시 정보:
- 현재 Stage 이름
- Stage 클리어 보상 요약
- 남은 체력/폭탄/로프
- 다음 Stage 미리보기 1줄

메모:
- 혼자 개발이면 별도 씬보다 오버레이 UI 방식이 유지보수에 유리하다.

### 5. Result

역할:
- 런 종료 후 결과 표시
- 사망 결과 또는 클리어 결과
- 점수, 시간, 회수 보상, 재도전 버튼 제공

권장 포함 정보:
- `Run Result`
- `Gold`
- `Time`
- `Reached Stage`
- `Cause of Death` 가능 시
- `Retry`
- `Back to Menu`

메모:
- 이 씬도 별도 분리 가능하지만,
- 초반 제작에서는 `Menu` 씬으로 돌아가기 전에 잠깐 쓰는 단일 결과 씬이면 충분하다.

---

## 2. 현실적인 씬 운영 방식

혼자 개발 기준 최적안:

```text
Boot
-> Menu
-> Game
   -> Stage 1
   -> Transition Overlay
   -> Stage 2
   -> Transition Overlay
   -> Stage 3
   -> Transition Overlay
   -> Stage 4
   -> Result or Menu
```

이 구조가 좋은 이유:
- `Game` 씬을 계속 유지하므로 런타임 상태 관리가 쉽다.
- 카메라, HUD, 플레이어, 오디오를 매번 재생성하지 않아도 된다.
- Stage 사이의 데이터 전달 버그를 줄일 수 있다.
- 나중에 씬 수가 늘어나도 핵심 플레이 코드는 한 곳에 남는다.

---

## 3. 각 씬의 책임 경계

### Boot가 하면 안 되는 것

- 메뉴 UI 표시
- 런 데이터 생성
- 플레이 씬 오브젝트 생성

### Menu가 하면 안 되는 것

- 실제 스테이지 데이터 생성
- 플레이어 인스턴스 유지
- Game용 HUD 유지

### Game이 책임지는 것

- 플레이어 생성/유지
- Stage 시작과 종료
- 적/함정/보상 스폰
- HUD 갱신
- 런 데이터 소비와 갱신

### Result가 책임지는 것

- 런 요약 표시
- 저장 가능한 통계 반영
- 재도전/메뉴 복귀 분기

---

## 4. Stage 구성 방식 제안

권장 방식은 `Stage를 씬으로 나누지 않고 데이터로 나누는 것`이다.

예시:
- `Stage01_EntryMine.asset`
- `Stage02_HangingCaverns.asset`
- `Stage03_RuinedWorks.asset`
- `Stage04_IdolVault.asset`

이 데이터를 `Game` 씬의 `StageManager`가 읽어
- 룸 풀
- 적 풀
- 함정 규칙
- 배경/음악
- 플레이타임 목표
를 적용한다.

이렇게 하면:
- 씬 수가 과도하게 늘지 않는다.
- Stage 수정이 데이터 중심이 된다.
- 같은 `Game` 씬 안에서 반복 테스트가 쉽다.

---

## 5. 권장 Hierarchy 예시

`Game` 씬 예시:

```text
GameRoot
|- RuntimeManagers
|  |- GameLoopManager
|  |- RunManager
|  |- StageManager
|  |- SpawnManager
|  |- UIManager
|  |- AudioManager
|  |- SaveManager(optional)
|  |- DebugManager
|- CameraRoot
|  |- MainCamera
|  |- CameraFollow
|- PlayerRoot
|- StageRoot
|  |- Tiles
|  |- Background
|  |- Props
|  |- Enemies
|  |- Traps
|  |- Pickups
|- UIRoot
|  |- HUDCanvas
|  |- OverlayCanvas
|  |- TransitionPanel
|  |- PausePanel
|  |- ResultPanel(optional)
|- AudioRoot
|- SystemsRoot
```

---

## 6. 제작 결론

4스테이지 게임 기준으로 가장 유지보수하기 좋은 구조는
`Boot`, `Menu`, `Game`, `Result`의 얇은 씬 구조 위에,
실제 Stage는 `Game` 씬 내부 데이터 전환으로 처리하는 방식이다.

즉:
- 씬은 앱 흐름을 나누고
- Stage는 데이터로 나누며
- 실제 게임 시스템은 `Game` 씬에 집중시키는 것이 가장 현실적이다.
