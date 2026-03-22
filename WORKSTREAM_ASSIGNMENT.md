# WORKSTREAM_ASSIGNMENT

## 목적

이 문서는 현재 Spelunky Unity 프로젝트를 `4스테이지 / 15-30분` 게임으로 확장하기 위해,
작업을 병렬로 진행할 수 있는 `실제 담당 분리표`를 정의한다.

핵심 기준:
- 기능 기준이 아니라 `충돌이 적은 경계`로 나눈다.
- 먼저 구조를 고정하고, 그 위에 콘텐츠를 올린다.
- `Player 이동/물리`는 초반에 별도 스트림으로 뜯지 않는다.

---

## 1. 권장 작업 스트림

### 담당 A. 런타임 흐름 / 스테이지 진행

역할:
- 게임 전체 진행 구조 담당
- `Boot/Menu/Game/Result` 흐름과 `Stage 1 -> 4 -> Result` 전환 구조 담당

핵심 작업:
- `RunState` 최소 구조 추가
- `StageFlowManager` 또는 `RunManager` 진입점 추가
- 출구 진입 시 씬 리로드 제거
- `GameManager`에서 런 시작 / 스테이지 시작 / 런 종료 경계 정리
- GameOver 처리와 Result 흐름 연결 준비

주요 파일:
- [GameManager.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Misc/GameManager.cs)
- [PlayerEnterDoorState.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/States/PlayerEnterDoorState.cs)
- [Exit.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Exit.cs)
- 새 `RunState.cs`
- 새 `StageFlowManager.cs` 또는 `RunManager.cs`

1주차 완료 조건:
- 출구 진입이 더 이상 직접 씬 리로드를 하지 않음
- 다음 Stage 요청 진입점이 생김
- RunState 최소 구조가 존재함

직접 테스트:
- 출구 진입 애니메이션 후 전환 요청이 1회만 발생하는지
- 사망 시 GameOver 흐름이 유지되는지
- 기존 Game 씬 플레이가 깨지지 않는지

플레이타임/난이도 영향:
- 직접 난이도 영향 없음
- 전체 게임 길이를 설계할 수 있게 만드는 핵심 구조

---

### 담당 B. 스테이지 데이터 / 레벨 생성 구조

역할:
- Stage별 룸 풀과 레벨 규칙을 분리
- `한 레벨 생성기`를 `4개 Stage 데이터`를 받는 구조로 확장

핵심 작업:
- `StageDefinition` 최소 구조 추가
- `LevelGenerator`가 Stage 데이터를 받도록 진입점 추가
- Stage별 normal/special room pool 분리
- 입구/출구/배경/특수 룸 분기 구조 준비

주요 파일:
- [LevelGenerator.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/LevelGenerator.cs)
- [Room.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Room.cs)
- [Tile.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Tile.cs)
- 새 `StageDefinition.cs`

1주차 완료 조건:
- Stage 1과 Stage 2가 서로 다른 룸 풀을 읽을 수 있음
- 레벨 생성이 Stage 데이터 기반으로 분기될 수 있음

직접 테스트:
- Stage별로 다른 룸 세트가 실제 생성되는지
- 입구/출구 생성이 깨지지 않는지
- hand-placed 테스트 씬이 여전히 작동하는지

플레이타임/난이도 영향:
- 스테이지 길이와 개성을 제어할 수 있게 됨
- 난이도 곡선을 Stage 단위로 분리 가능

---

### 담당 C. UI / HUD / 결과 흐름

역할:
- 플레이어 HUD, Transition, Result, GameOver 흐름 정리
- 씬 이름 의존성이 큰 UI를 유지보수 가능하게 바꿈

핵심 작업:
- `PlayerUI`의 `GameObject.Find()` 의존성 완화
- 최소 `UIManager` 또는 HUD Presenter 구조 준비
- GameOverUI를 Result 흐름으로 확장 가능한 구조로 정리
- Stage 전환 오버레이 설계 준비

주요 파일:
- [PlayerUI.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/PlayerUI.cs)
- [GameOverUI.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/UI/GameOverUI.cs)
- 새 `UIManager.cs` 가능

1주차 완료 조건:
- HUD가 인스펙터 참조 또는 바인딩 기반으로 동작하기 시작함
- 체력/폭탄/로프/골드 표시가 기존과 동일하게 유지됨
- Result 흐름으로 확장 가능한 UI 경계가 생김

직접 테스트:
- 기존 Game 씬에서 HUD 정상 갱신 여부
- 참조 누락 시 바로 알 수 있는지
- GameOver 발생 시 UI가 중복 생성되지 않는지

플레이타임/난이도 영향:
- 자원 가독성 개선은 체감 난이도에 직접 영향
- 플레이 자체는 바뀌지 않지만 판단 품질이 올라감

---

### 담당 D. 콘텐츠 / 적 / 함정 / 보상 배치

역할:
- 실제 플레이 내용 생산
- Stage별 적, 함정, 보상, 위험 루트, 안전 루트 구성

핵심 작업:
- Stage 1~4 핵심 룸 제작
- 적/함정/보상 배치 규칙 문서 기반 반영
- BombBag, RopePile, Treasure, Chest/Key, GoldIdol 위치 설계
- Stage 4 유물 회수 전/후 배치 차별화 설계

주요 범위:
- `Assets/Prefabs/Rooms/*`
- `Assets/Prefabs/Enemies/*`
- `Assets/Prefabs/Items/*`
- Stage 데이터가 생기면 해당 asset들

1주차 완료 조건:
- 각 Stage당 핵심 방 2-3개라도 정체성이 보이는 콘텐츠 확보
- Stage 1과 Stage 2의 체감 차이가 실제로 드러남

직접 테스트:
- 입구 직후 즉사 구간이 없는지
- 보상 루트가 위험 대비 가치가 있는지
- Stage별 핵심 학습 요소가 플레이로 느껴지는지

플레이타임/난이도 영향:
- 직접 영향 가장 큼
- 난이도와 긴장감의 실체를 만드는 스트림

주의:
- 담당 D는 가급적 `GameManager`, `Player.cs`는 수정하지 않는 것이 좋음
- 구조 변경 없이 프리팹/룸/데이터 중심으로 작업하는 편이 안전

---

### 담당 E. 밸런스 / 디버그 / QA

역할:
- 빠른 반복 테스트 기반 마련
- 플레이타임, 자원 사용량, 사망 원인, Stage별 난이도 측정

핵심 작업:
- `DebugManager` 또는 디버그 콘솔 추가
- Stage 워프
- 체력/폭탄/로프 강제 세팅
- 현재 RunState 출력
- Stage별 체류 시간 측정
- 사망 원인 수집 구조 초안

주요 파일:
- 새 `DebugManager.cs`
- 새 `RunResult.cs` 가능
- 기존 관리자에 디버그 훅 소량 추가 가능

1주차 완료 조건:
- Stage 이동 테스트를 빠르게 할 수 있음
- 자원 상태를 강제로 세팅해 밸런스 점검 가능

직접 테스트:
- Stage 워프 후 카메라, 플레이어, HUD가 정상인지
- 강제 자원 지급이 실제 인벤토리/HUD와 일치하는지

플레이타임/난이도 영향:
- 직접적인 플레이 변경은 적음
- 하지만 밸런스 작업 속도와 품질에 가장 큰 영향

---

## 2. 실제 작업 순서

### 1단계. 담당 A 선행

A가 먼저 고정해야 하는 것:
- `RunState` 구조
- `다음 Stage 요청` 진입점
- `런 종료` 진입점

이 3개가 있어야 B, C, D, E가 안정적으로 병렬 작업 가능

### 2단계. A/B/C 병렬

- A: 흐름 고정
- B: Stage 데이터와 룸 분리
- C: HUD와 결과 흐름 정리

이 세 팀은 인터페이스만 맞추면 병렬 가능

### 3단계. D 병렬 본격 투입

조건:
- B가 최소한 `StageDefinition` 또는 Stage별 룸 풀 구조를 고정한 뒤

그다음 D는 콘텐츠를 대량으로 넣어도 됨

### 4단계. E는 처음부터 계속 병행

이유:
- 디버그와 측정은 나중에 붙이면 너무 늦음
- 최소한 Stage 워프와 자원 지급은 초반부터 있어야 함

---

## 3. 코드 충돌 방지 규칙

절대 한 명만 담당해야 할 파일:
- [GameManager.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Misc/GameManager.cs)
- [Player.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/Player.cs)
- [PlayerEnterDoorState.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/States/PlayerEnterDoorState.cs)
- [LevelGenerator.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/LevelGenerator.cs)
- [PlayerUI.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/PlayerUI.cs)

규칙:
- A만 `GameManager.cs`, `PlayerEnterDoorState.cs` 수정
- B만 `LevelGenerator.cs` 수정
- C만 `PlayerUI.cs`, `GameOverUI.cs` 수정
- D는 가능한 코드 수정 없이 프리팹/룸/데이터 위주
- E는 새 파일 중심, 기존 파일 수정은 최소한으로 제한

---

## 4. 브랜치 / 병합 권장 방식

권장 브랜치 예시:
- `codex/runtime-flow`
- `codex/stage-data`
- `codex/ui-flow`
- `codex/content-pass-01`
- `codex/debug-tools`

병합 순서:
1. runtime-flow
2. stage-data
3. ui-flow
4. debug-tools
5. content-pass-01

이유:
- 구조가 먼저 들어와야 콘텐츠 병합 시 충돌과 재작업이 줄어듦

---

## 5. 데일리 동기화 항목

매일 짧게 공유할 것:
- 오늘 바꾼 인터페이스
- 다음 사람이 의존하는 파일
- 깨질 가능성이 있는 씬/프리팹
- 직접 테스트한 항목
- 아직 임시 구현인 부분

필수 공유 예시:
- `HandlePlayerEnteredExit()` 시그니처 변경
- `StageDefinition` 필드 추가
- HUD 참조 방식 변경
- Stage 4 클라이맥스 트리거 이름 변경

---

## 6. 가장 현실적인 1주차 분업안

### 담당 A
- RunState 최소 구조
- 출구 전환 진입점 분리
- GameManager 메서드 경계 정리

### 담당 B
- StageDefinition 최소판
- Stage별 룸 풀 분리 준비
- LevelGenerator 분기점 추가

### 담당 C
- PlayerUI 참조 방식 완화
- GameOverUI 중복 생성/재시작 흐름 정리

### 담당 D
- Stage 1 핵심 룸 2개
- Stage 2 핵심 룸 2개
- Stage 3/4는 러프 레이아웃 스케치

### 담당 E
- Stage 워프
- 자원 지급 디버그
- 현재 RunState 출력

---

## 7. 결론

이 프로젝트를 병렬로 밀려면 `플레이어 물리`, `GameManager`, `LevelGenerator`, `UI`, `콘텐츠`를 동시에 다 건드리면 안 된다.
가장 효율적인 분리는 아래와 같다.

- A: 흐름
- B: Stage 데이터와 생성 구조
- C: UI와 결과 흐름
- D: 콘텐츠 생산
- E: 디버그와 밸런스 측정

이렇게 나누면 구조가 먼저 고정되고, 콘텐츠는 그 위에서 안전하게 증식할 수 있다.
