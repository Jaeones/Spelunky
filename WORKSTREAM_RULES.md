# WORKSTREAM_RULES

## 목적

이 문서는 현재 Spelunky Unity 프로젝트를 여러 작업 스레드가 병렬로 진행할 때,
충돌을 줄이고 인수인계를 명확하게 하기 위한 작업 규칙을 정의한다.

대상 스레드:
- A: 런타임 흐름 / 스테이지 진행
- B: 스테이지 데이터 / 레벨 생성 구조
- C: UI / HUD / 결과 흐름
- D: 콘텐츠 / 적 / 함정 / 보상 배치
- E: 밸런스 / 디버그 / QA

핵심 원칙:
- 구조가 콘텐츠보다 우선이다.
- 소유권이 없는 파일은 건드리지 않는다.
- 임시 구현과 최종 구현을 항상 구분한다.
- 변경 후 직접 테스트 항목을 반드시 남긴다.

---

## 1. 공통 작업 원칙

1. 한 번에 큰 구조 변경을 하지 않는다.
- 기존 플레이가 유지되는 작은 단계로 쪼갠다.
- 한 PR 또는 한 작업 단위는 하나의 책임만 가진다.

2. 구조를 먼저 만들고 콘텐츠를 넣는다.
- 흐름, RunState, StageDefinition, UI 경계를 먼저 고정한다.
- 그 위에 룸, 적, 함정, 보상을 얹는다.

3. 임시 구현을 숨기지 않는다.
- 주석, 로그, 문서에 `Temporary` 또는 `Final target`을 명시한다.
- 임시 구현을 최종 구조처럼 설명하지 않는다.

4. 변경 이유를 먼저 설명한다.
- 무엇을 바꾸는지보다 `왜 지금 필요한지`를 먼저 적는다.
- 플레이타임, 난이도, 유지보수 중 어느 쪽에 영향을 주는지 명시한다.

5. 직접 테스트 항목을 남긴다.
- Unity에서 어떤 씬을 열고 무엇을 확인해야 하는지 명확히 적는다.
- "테스트 필요" 수준으로 끝내지 않는다.

---

## 2. 소유권 규칙

### 담당 A만 수정 가능한 핵심 파일

- [GameManager.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Misc/GameManager.cs)
- [PlayerEnterDoorState.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/States/PlayerEnterDoorState.cs)
- [Exit.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Exit.cs)
- [RunManager.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Misc/RunManager.cs)

이유:
- 게임 전체 진행 구조와 직결된다.
- 중복 수정 시 가장 위험한 충돌이 발생한다.

### 담당 B만 수정 가능한 핵심 파일

- [LevelGenerator.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/LevelGenerator.cs)
- [Room.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Room.cs)
- [Tile.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/LevelGenerator/Tile.cs)
- [StageDefinition.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Data/Definitions/StageDefinition.cs)

### 담당 C만 수정 가능한 핵심 파일

- [PlayerUI.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/PlayerUI.cs)
- [GameOverUI.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/UI/GameOverUI.cs)
- [UIManager.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Runtime/UI/UIManager.cs)

### 담당 D가 우선 소유하는 영역

- `Assets/Prefabs/Stage/Stage01`
- `Assets/Prefabs/Stage/Stage02`
- `Assets/Prefabs/Stage/Stage03`
- `Assets/Prefabs/Stage/Stage04`
- `Assets/Prefabs/Enemies`
- `Assets/Prefabs/Items`

규칙:
- 담당 D는 가능하면 코드 수정 없이 프리팹/룸/데이터 중심으로 작업한다.

### 담당 E가 우선 소유하는 영역

- `Assets/Scripts/Runtime/Debug`
- `Assets/Data/Debug`
- `Docs/QA`

---

## 3. 수정 금지 / 수정 주의 대상

초기 단계에서 공용 수정 금지:
- [Player.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Player/Player.cs)
- [EntityPhysics.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Entity/EntityPhysics.cs)
- [Enemy.cs](/C:/UnityProjects/study/unity/Spelunky/Assets/Scripts/Enemies/Enemy.cs)
- [Game.unity](/C:/UnityProjects/study/unity/Spelunky/Assets/Scenes/Game.unity)
- [Player.prefab](/C:/UnityProjects/study/unity/Spelunky/Assets/Prefabs/Player/Player.prefab)

이유:
- 참조 수가 많고, 여러 스레드가 동시에 건드리면 복구 비용이 크다.

예외:
- 반드시 수정해야 한다면 사전에 `누가 언제 어떤 이유로 수정하는지` 명확히 공유한다.

---

## 4. 브랜치 규칙

권장 브랜치:
- `codex/runtime-flow`
- `codex/stage-data`
- `codex/ui-flow`
- `codex/content-pass-01`
- `codex/debug-tools`

규칙:
- 한 스레드는 자기 브랜치만 사용한다.
- 공용 브랜치에 직접 푸시하지 않는다.
- 브랜치 목적이 바뀌면 새 브랜치를 만든다.

---

## 5. 커밋 규칙

커밋은 아래 조건을 만족해야 한다.

1. 한 커밋은 하나의 책임만 갖는다.
- 예: `출구 전환 책임 분리`
- 예: `StageDefinition 첫 도입`
- 예: `HUD 참조 방식 정리`

2. 임시 구현이면 메시지에 드러나야 한다.
- 예: `Add temporary run clear result flow`
- 예: `Introduce temporary stage definition hook`

3. 프리팹 대량 수정과 코드 수정을 같은 커밋에 섞지 않는다.

---

## 6. 인계 메시지 규칙

각 스레드는 작업 후 반드시 아래 5가지를 남긴다.

1. 변경 이유
2. 수정한 파일 목록
3. 아직 임시인 부분
4. 직접 테스트 항목
5. 다음 스레드가 바로 이어서 할 수 있는 다음 단계

권장 형식:
- `왜 필요한가`
- `수정 파일`
- `임시 구현 / 최종 구현`
- `직접 테스트`
- `플레이타임 / 난이도 영향`
- `다음 작업`

---

## 7. 씬 / 프리팹 작업 규칙

### 씬 규칙

- 공용 플레이 씬은 초기에는 [Game.unity](/C:/UnityProjects/study/unity/Spelunky/Assets/Scenes/Game.unity) 하나만 기준으로 본다.
- 테스트용 작업은 가능하면 새 테스트 씬 또는 Debug 진입점으로 분리한다.
- `Game.unity`를 수정했다면 반드시 무엇이 바뀌었는지 텍스트로 남긴다.

### 프리팹 규칙

- Player prefab은 초반 공용 수정 금지
- 적/아이템 프리팹 수정은 Stage 콘텐츠 목적일 때만 수행
- Stage 전용 룸 프리팹은 Stage 폴더 아래에서 관리한다.

---

## 8. 데이터 규칙

- Stage별 차이는 되도록 `StageDefinition`이나 데이터 자산으로 표현한다.
- 코드 하드코딩은 임시 구현일 때만 허용한다.
- Stage 1~4의 룸 풀, 함정 비율, 적 풀은 asset 기반으로 분리하는 방향을 유지한다.

금지:
- Stage 1~4 분기를 여러 스크립트에 중복 하드코딩
- UI 문자열과 Stage 규칙을 씬 오브젝트 이름으로 판별

---

## 9. 테스트 규칙

코드 수정 후 최소 테스트:
- 해당 씬이 열리는지
- 콘솔에 Missing Script / compile error가 없는지
- 변경한 흐름이 최소 1회 정상 동작하는지

흐름 수정 후 추가 테스트:
- 출구 진입 1회
- 사망 1회
- 재시작 1회
- 디버그 워프 1회 가능 시

콘텐츠 수정 후 추가 테스트:
- 입구 직후 즉사 여부
- 출구 도달 가능 여부
- 자원 부족 소프트락 여부
- 보상 루트 가치 여부

---

## 10. 밸런스 규칙

초기 밸런스 조정 순서:
1. 스테이지 길이
2. 자원 위치
3. 위협 조합 밀도
4. 적 출현 빈도
5. 함정 가시성
6. 보상 유혹 강도
7. 조작감 보조 수치
8. 피해량

금지:
- 어렵다고 바로 적 피해 증가
- 짧다고 바로 플레이어 속도 감소
- 첫 번째 해법으로 Player 물리 수치 변경

---

## 11. 병합 규칙

권장 병합 순서:
1. runtime-flow
2. stage-data
3. ui-flow
4. debug-tools
5. content-pass-01

이유:
- 구조가 먼저 들어와야 콘텐츠 충돌이 줄어든다.
- 디버그 툴은 콘텐츠와 병행 가능하지만, 구조보다 먼저 들어가진 않는다.

---

## 12. Definition of Done

작업이 끝났다고 말하려면 아래를 만족해야 한다.

- 변경 이유가 명확하다.
- 수정 파일이 제한적이다.
- 임시 구현과 최종 구현이 구분되어 있다.
- 직접 테스트 항목이 있다.
- 플레이타임/난이도 영향이 설명되어 있다.
- 다음 작업자가 이어받을 수 있게 상태가 정리되어 있다.

---

## 결론

이 프로젝트에서 병렬 작업이 망가지는 가장 흔한 이유는 두 가지다.
- 공용 허브 파일을 여러 스레드가 동시에 수정하는 것
- 임시 구현을 최종 구조처럼 취급하는 것

따라서 규칙은 단순해야 한다.
- 소유권 없는 파일은 건드리지 않는다.
- 변경 이유와 테스트 항목을 반드시 남긴다.
- 구조가 먼저, 콘텐츠는 그 다음이다.
