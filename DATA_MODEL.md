# DATA_MODEL

## 목적

이 문서는 4스테이지 게임 제작에 필요한 데이터 구조를 정의한다.
핵심은 `씬에 직접 박힌 값`을 줄이고,
Stage, Spawn, Drop, Run Progress를 데이터 중심으로 관리하게 만드는 것이다.

---

## 1. 데이터 계층 제안

권장 계층은 아래 4단계다.

1. `Static Design Data`
- Stage 정의
- 적 정의
- 아이템 정의
- 드랍 규칙

2. `Runtime Session Data`
- 현재 런 상태
- 현재 스테이지 상태
- 플레이어 상태 스냅샷

3. `Persistent Save Data`
- 설정
- 최고 기록
- 해금 상태 필요 시

4. `Debug/Test Data`
- 테스트용 Stage 로드 설정
- 강제 자원 세팅
- 특정 룸/이벤트 강제 실행

---

## 2. 스테이지 정의 데이터

권장 타입:
- `StageDefinition : ScriptableObject`

권장 필드:
- `stageId`
- `displayName`
- `themeType`
- `musicTrack`
- `ambientLoop`
- `targetTimeMin`
- `targetTimeMax`
- `roomPool`
- `specialRoomPool`
- `enemySpawnTable`
- `trapSpawnTable`
- `pickupSpawnTable`
- `rewardRules`
- `entryRule`
- `exitRule`
- `stageClearMode`
- `finaleConfig(optional)`

예시:
```csharp
[CreateAssetMenu]
public class StageDefinition : ScriptableObject {
    public string stageId;
    public string displayName;
    public AudioClip musicTrack;
    public AudioClip ambientLoop;
    public Vector2Int targetMinutesRange;
    public Room[] normalRooms;
    public Room[] specialRooms;
    public EnemySpawnTable enemySpawnTable;
    public TrapSpawnTable trapSpawnTable;
    public PickupSpawnTable pickupSpawnTable;
    public StageRewardRules rewardRules;
    public StageFinaleConfig finaleConfig;
}
```

메모:
- Stage를 씬이 아니라 데이터로 정의하면 반복 수정이 훨씬 쉽다.

---

## 3. 적 스폰 데이터

권장 타입:
- `EnemySpawnTable : ScriptableObject`
- `EnemySpawnEntry`

권장 필드:
- `enemyId`
- `prefab`
- `minStageDepth`
- `weight`
- `minCount`
- `maxCount`
- `allowedRoomTags`
- `forbiddenRoomTags`
- `spawnOnGround`
- `spawnOnCeiling`
- `spawnNearDrop`

예시:
```csharp
[Serializable]
public class EnemySpawnEntry {
    public string enemyId;
    public GameObject prefab;
    public int weight;
    public int minCount;
    public int maxCount;
    public string[] allowedRoomTags;
}
```

제작 원칙:
- 적 스폰은 단순 랜덤보다 `룸 태그 기반`으로 관리하는 쪽이 좋다.
- 예: `vertical`, `corridor`, `treasure_side_path`, `final_escape`

---

## 4. 아이템 드랍 데이터

권장 타입:
- `PickupSpawnTable : ScriptableObject`
- `DropTable : ScriptableObject`
- `DropEntry`

분리 이유:
- `맵에 배치되는 아이템`과 `파괴/오브젝트에서 떨어지는 아이템`은 성격이 다르다.

### PickupSpawnTable

용도:
- 맵 위에 배치되는 BombBag, RopePile, Accessory, Treasure, Chest/Key 관리

권장 필드:
- `pickupId`
- `prefab`
- `spawnWeight`
- `minPerStage`
- `maxPerStage`
- `requiredRoomTags`
- `resourcePressureBias`

### DropTable

용도:
- Crate, Jar, Chest 결과물, 특정 이벤트 보상 관리

권장 필드:
- `dropId`
- `weight`
- `prefab`
- `minQuantity`
- `maxQuantity`

예시:
```csharp
[Serializable]
public class DropEntry {
    public string dropId;
    public GameObject prefab;
    public int weight;
    public int minQuantity;
    public int maxQuantity;
}
```

---

## 5. 플레이 진행 데이터

### RunState

권장 타입:
- 런타임 클래스 또는 직렬화 가능한 plain data class

권장 필드:
- `runId`
- `currentStageIndex`
- `health`
- `maxHealth`
- `bombs`
- `ropes`
- `gold`
- `accessories`
- `elapsedTime`
- `relicsCollected`
- `isFinalEscapeActive`
- `deathCount(optional)`

예시:
```csharp
[Serializable]
public class RunState {
    public int currentStageIndex;
    public int health;
    public int bombs;
    public int ropes;
    public int gold;
    public List<string> accessoryIds;
    public float elapsedTime;
    public bool isFinalEscapeActive;
}
```

### StageRuntimeState

권장 필드:
- `stageId`
- `seed`
- `timeSpent`
- `goldCollected`
- `damageTaken`
- `bombsUsed`
- `ropesUsed`
- `roomsVisited`
- `specialEventsTriggered`

용도:
- 스테이지별 밸런스 로그
- Transition 화면 요약
- QA 데이터 축적

### RunResult

권장 필드:
- `reachedStage`
- `clearedRun`
- `totalGold`
- `totalTime`
- `causeOfDeath`
- `relicsCollected`

용도:
- Result 화면
- 기록 저장
- QA 분석

---

## 6. 세이브 범위 제안

### 반드시 필요한 세이브

1. `SettingsSave`
- 볼륨
- 해상도/화면 모드
- 입력 설정 필요 시

2. `ProfileStatsSave`
- 최고 골드
- 최고 도달 Stage
- 최고 클리어 시간
- 총 플레이 횟수

이 둘은 거의 필수다.

### 선택적 세이브

3. `SuspendRunSave`
- 현재 런 중간 저장
- Game 종료 후 이어하기 지원

권장 여부:
- 초반 프로덕션에서는 `선택 사항`
- 혼자 개발이면 우선순위는 낮다.

이유:
- 런 길이가 15-30분이면 반드시 중간 저장이 필요한 길이는 아니다.
- 하지만 플랫폼/타겟 유저에 따라 편의성 차원에서 후반에 넣을 수 있다.

### 권장 결론

최초 버전에서는:
- `SettingsSave`
- `ProfileStatsSave`
만 구현

후반에 여유가 있으면:
- `SuspendRunSave`
추가

---

## 7. 디버그용 데이터와 기능 제안

### DebugRunPreset

용도:
- 특정 Stage를 특정 자원 상태로 바로 시작

권장 필드:
- `startStageIndex`
- `health`
- `bombs`
- `ropes`
- `gold`
- `accessories`
- `finalEscapeActive`

### DebugSpawnPreset

용도:
- 적/함정/아이템 조합을 빠르게 재현

권장 필드:
- `enemyIds`
- `trapIds`
- `pickupIds`
- `roomTag`

### Debug 기능 목록

필수 추천:
- Stage 워프
- 폭탄/로프/체력 강제 세팅
- 액세서리 지급/제거
- GoldIdol 클라이맥스 즉시 시작
- 현재 RunState JSON 출력
- 현재 StageDefinition 이름 출력
- 현재 방 태그 표시
- 스폰 포인트 gizmo 표시

---

## 8. 폴더 구조 제안

권장 폴더:

```text
Assets/Data/Stages
Assets/Data/SpawnTables
Assets/Data/DropTables
Assets/Data/Run
Assets/Data/Debug
Assets/Data/Audio
```

세부 예시:
- `Assets/Data/Stages/Stage01_EntryMine.asset`
- `Assets/Data/Stages/Stage02_HangingCaverns.asset`
- `Assets/Data/SpawnTables/Enemies_Stage01.asset`
- `Assets/Data/SpawnTables/Pickups_Stage03.asset`
- `Assets/Data/DropTables/CrateDrops_Default.asset`
- `Assets/Data/Debug/DebugRun_Stage4Escape.asset`

---

## 9. 제작 결론

4스테이지 게임을 유지보수 가능하게 만들려면,
값과 규칙을 씬에 박아두기보다 `StageDefinition`, `SpawnTable`, `DropTable`, `RunState`로 나눠 관리하는 것이 핵심이다.

그리고 저장은 처음부터 크게 잡을 필요 없다.
초기 버전은 `설정 + 최고 기록`만 저장하고,
중간 런 저장은 후반에 정말 필요할 때 추가하는 것이 현실적이다.
