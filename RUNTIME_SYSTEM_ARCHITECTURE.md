# RUNTIME_SYSTEM_ARCHITECTURE

## 목적

이 문서는 `Game` 씬 안에서 실제 플레이를 운영하는 핵심 매니저 구조를 제안한다.
현재 저장소의 `GameManager + EntityManager + PlatformManager + TimerManager` 구조를 유지하되,
4스테이지 완성형 게임에 필요한 `Run`, `Stage`, `UI`, `Audio`, `Debug` 계층을 추가해 책임을 분리하는 것이 목표다.

---

## 1. 권장 매니저 구성

### 1. GameLoopManager

역할:
- 현재 `GameManager`의 프레임 순서 제어 책임만 담당
- `EarlyTick -> Platform -> Tick -> LateTick -> Timer` 순서 유지

기존 코드 연계:
- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/Managers/EntityManager.cs`
- `Assets/Scripts/Managers/PlatformManager.cs`
- `Assets/Scripts/Managers/TimerManager.cs`

메모:
- 지금의 `GameManager`는 너무 많은 책임을 가진다.
- 제품 구조에서는 `GameLoopManager`와 `GameSessionCoordinator`로 분리하는 것이 좋다.

### 2. RunManager

역할:
- 한 번의 런 전체 상태 관리
- 체력, 폭탄, 로프, 골드, 액세서리, 현재 스테이지 인덱스 관리
- 사망, 클리어, 재시작 정책 결정

주요 책임:
- `StartNewRun()`
- `ApplyRunStateToPlayer()`
- `CapturePlayerState()`
- `HandlePlayerDeath()`
- `HandleRunClear()`
- `ResetRun()`

### 3. StageManager

역할:
- 현재 Stage 데이터 로드
- `LevelGenerator` 또는 수동 룸 세트업 호출
- 스폰 포인트, 출구, 클라이맥스 이벤트 관리

주요 책임:
- `LoadStage(StageDefinition)`
- `BuildStage()`
- `SpawnPlayerAtEntry()`
- `CompleteStage()`
- `UnloadCurrentStage()`
- `StartFinalEscape()`

기존 코드 연계:
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Exit.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/Tile.cs`

### 4. SpawnManager

역할:
- 적, 함정, 아이템, 배경 오브젝트의 실제 인스턴스 생성
- StageManager는 `무엇을 스폰할지` 결정하고,
  SpawnManager는 `어떻게 스폰할지` 담당

주요 책임:
- `SpawnEnemy()`
- `SpawnTrap()`
- `SpawnPickup()`
- `DespawnAllStageEntities()`

이유:
- Stage 로딩 로직과 실제 Instantiate 로직을 분리하면 테스트가 쉬워진다.

### 5. UIManager

역할:
- HUD, Pause, Transition, Result, GameOver를 통합 관리
- 현재 저장소의 `GameOverUI`를 확장/대체하는 방향

주요 책임:
- `BindPlayer(Player)`
- `ShowHUD()`
- `ShowTransition(StageDefinition)`
- `ShowGameOver(RunResult)`
- `ShowRunClear(RunResult)`
- `SetPaused(bool)`

기존 코드 연계:
- `Assets/Scripts/Player/PlayerUI.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

메모:
- 제품 구조에서는 `UIManager + 각 화면 Presenter` 조합이 유지보수에 좋다.

### 6. AudioManager

역할:
- BGM, Ambient, SFX를 통합 관리
- 현재 저장소의 `AudioManager`를 유지하되,
  Stage 전환 음악, 클라이맥스 상태음, UI SFX까지 포함하도록 확장

주요 책임:
- `PlayMusic(track)`
- `CrossfadeMusic(track)`
- `PlayAmbient(loop)`
- `PlayUISfx(clip)`
- `PushTensionState(level)`

기존 코드 연계:
- `Assets/Scripts/Misc/AudioManager.cs`

주의:
- 현재 코드에는 `Music` 그룹이 `ambientGroup`에 연결되는 부분이 있어 검토 필요

### 7. SaveManager

역할:
- 설정 저장
- 최고 기록 저장
- 이어하기 지원 시 최소 런 스냅샷 저장

메모:
- 런 자체를 언제든 이어서 하게 할지 여부는 선택사항이다.
- 최소 버전에서는 `Settings + BestStats`만 저장해도 충분하다.

### 8. DebugManager

역할:
- 빠른 Stage 테스트
- 자원 강제 지급
- 특정 룸/적/함정 테스트
- 로그와 검증 도구 노출

주요 책임:
- `WarpToStage(index)`
- `GiveBombs(amount)`
- `GiveRopes(amount)`
- `ToggleGodMode()`
- `SpawnTestEnemy(type)`
- `ReloadStage()`

---

## 2. 권장 상위 조정자

### GameSessionCoordinator

역할:
- 실제 게임 흐름의 중심
- RunManager, StageManager, UIManager, AudioManager 사이 조정

권장 메서드:
- `StartNewRun()`
- `StartStage(index)`
- `HandleStageClear()`
- `HandlePlayerDeath()`
- `HandleRunEnd()`

이유:
- 제품 구조에서 가장 흔한 문제는 매니저끼리 직접 서로를 많이 호출하는 것이다.
- Coordinator 하나가 흐름을 잡아주면 결합도가 줄어든다.

---

## 3. 권장 호출 흐름

### 새 런 시작

```text
Menu
-> GameSessionCoordinator.StartNewRun()
-> RunManager.CreateRun()
-> StageManager.LoadStage(Stage 1)
-> SpawnManager.BuildStageEntities()
-> RunManager.ApplyStateToPlayer()
-> UIManager.BindPlayer()
-> AudioManager.PlayMusic(Stage 1)
```

### 스테이지 클리어

```text
Exit reached
-> StageManager.CompleteStage()
-> RunManager.CapturePlayerState()
-> UIManager.ShowTransition()
-> AudioManager.CrossfadeMusic()
-> StageManager.UnloadCurrentStage()
-> StageManager.LoadStage(next)
-> RunManager.ApplyStateToPlayer()
```

### 플레이어 사망

```text
Player death
-> RunManager.HandlePlayerDeath()
-> UIManager.ShowGameOver()
-> SaveManager.RecordRunResult()
-> Menu or Restart
```

### Stage 4 클라이맥스

```text
Final relic picked
-> StageManager.StartFinalEscape()
-> AudioManager.PushTensionState(high)
-> UIManager.ShowEscapeState()
-> Exit unlock or timer start
-> player escapes
-> Run clear
```

---

## 4. 현재 저장소에서 유지할 것과 분리할 것

### 유지 추천

- `EntityManager`
- `PlatformManager`
- `TimerManager`
- `PhysicsManager`
- `LevelGenerator`
- `AudioManager`의 재생 유틸리티

### 분리 추천

- 현재 `GameManager`의
  - 레벨 초기화
  - 플레이어 스폰
  - 게임오버 처리
  - 런 흐름 관리

이 책임은 각각
- `StageManager`
- `RunManager`
- `UIManager`
- `GameSessionCoordinator`
로 나누는 편이 좋다.

---

## 5. 디버그 기능 제안

필수 디버그 메뉴:
- Stage 즉시 이동
- 체력/폭탄/로프 설정
- 액세서리 즉시 지급
- GoldIdol 클라이맥스 강제 시작
- 적/함정 단건 스폰
- 현재 RunState 출력
- 현재 StageDefinition 이름 표시
- 최근 사망 원인 출력

품질 확인용 디버그:
- 스테이지별 플레이타임 측정
- 방별 사망 수 기록
- 자원 소비 로그
- 출구 도달률
- 클라이맥스 진입률 / 탈출 성공률

---

## 6. 제작 결론

제품 구조에서는 `GameManager` 하나에 흐름, UI, 런, 스테이지를 다 몰아두지 말고,
아래처럼 나누는 것이 가장 유지보수하기 좋다.

- `GameLoopManager`: 프레임 순서
- `RunManager`: 한 판 전체 상태
- `StageManager`: 현재 스테이지 생성/교체
- `SpawnManager`: 실제 스폰
- `UIManager`: HUD/전환/결과
- `AudioManager`: 음악/환경음/상태음
- `SaveManager`: 설정/기록/선택적 런 스냅샷
- `DebugManager`: 빠른 검증

이 구조면 혼자 개발하더라도 Stage 확장, 밸런스 수정, UI 변경을 비교적 안전하게 진행할 수 있다.
