# LEVEL_SYSTEM

## 목적

이 문서는 이 프로젝트의 레벨, 타일, 맵 생성 구조를 새 Unity 프로젝트에서 따라 만들 수 있도록 해부한 것이다.
핵심 질문은 다음이다.

1. 타일맵을 어떤 방식으로 다루는가
2. 방(Room) / 구역 / 청크 개념이 있는가
3. 절차적 생성인지 수동 배치인지
4. 충돌 지형과 배경 지형은 어떻게 나뉘는가
5. 레벨 시작 시 맵은 어떤 순서로 구성되는가
6. 플레이어 스폰 위치는 어떻게 정해지는가

---

## 1. 타일맵을 다루는 코드

이 프로젝트는 Unity의 `Tilemap` 컴포넌트를 쓰지 않는다.
`UnityEngine.Tilemaps` 사용 흔적도 없다.

대신 아래 구조를 쓴다.

- `Tile` 라는 `MonoBehaviour` 가 개별 타일 역할을 한다.
- `LevelGenerator.Tiles[,]` 라는 2차원 배열이 런타임 타일맵 역할을 한다.
- 타일의 월드 좌표를 `16x16` 그리드로 해석해서 배열 인덱스로 바꾼다.

### 관련 파일

- `Assets/Scripts/LevelGenerator/Tile.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`

### 핵심 포인트

`Tile.cs`
- `public const int Width = 16`
- `public const int Height = 16`
- `InitializeTile(x, y)` 에서 자기 좌표를 저장하고 `LevelGenerator.instance.Tiles[x, y] = this` 로 등록
- `SetupTile()` 에서 상하좌우 이웃 타일을 보고 비주얼을 결정

`LevelGenerator.cs`
- `Tiles = new Tile[roomsHorizontal * RoomWidth, roomsVertical * RoomHeight]`
- `InitializeTiles()` 에서 씬 안의 모든 `Tile` 을 찾아 배열에 등록
- `SetupTiles()` 에서 등록된 타일을 다시 순회하며 장식/스프라이트 세팅

### 해석

즉 이 프로젝트의 타일맵은:

- 에디터에서 그려진 Unity Tilemap이 아니라
- 씬에 배치된 타일 프리팹 오브젝트들을
- 런타임에 `Tiles[,]` 배열로 다시 묶어 쓰는 구조다.

---

## 2. 방(Room) / 구역 / 청크 개념이 있는지

있다. 핵심 개념은 `Room` 이다.

### 관련 파일

- `Assets/Scripts/LevelGenerator/Room.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Resources/Rooms/Caves/*.prefab`

### Room 구조

`Room` 은 단순한 비주얼 덩어리가 아니라 메타데이터를 가진 룸 프리팹이다.

`Room.cs`
- `index`
- `top`, `right`, `down`, `left`

이 네 값은 그 룸이 어느 방향으로 열려 있는지 나타낸다.
즉 `Room` 은 일종의 청크/구역 단위다.

### 레벨 그리드 구조

`LevelGenerator`
- `roomsHorizontal = 4`
- `roomsVertical = 4`
- `Rooms[,]` 배열을 가짐
- 각 `Room` 의 논리 크기는 `10 x 8 tiles`

즉 현재 레벨은:

- `4 x 4 개의 룸 슬롯`
- 각 슬롯은 `10 x 8 타일`
- 전체는 하나의 큰 타일 그리드

### 해석

이 프로젝트에는 `chunk` 라는 이름은 없지만,
실제로는 `Room` 이 chunk 역할을 한다.

---

## 3. 절차적 생성인지, 수동 배치인지

정답은 `둘 다`다.
정확히는 `수동 제작된 룸 프리팹을 절차적으로 배치하는 하이브리드 구조`다.

### 절차적인 부분

`LevelGenerator.GenerateLevel()`
- 메인 경로를 만드는 룸 배치
- 남은 빈 칸에 룸 채우기
- special room 확률 배치

즉 `어느 룸이 어느 위치에 놓이는가` 는 절차적으로 정한다.

### 수동인 부분

실제 룸 내부 타일 배치는 룸 프리팹 안에 이미 만들어져 있다.

예:
- `Assets/Resources/Rooms/Caves/Room.prefab`
- `Assets/Resources/Rooms/Caves/Room_2.prefab`
- `Assets/Resources/Rooms/Caves/RoomSpecialAltar.prefab`

즉 `방의 내부 모양` 은 수동 제작이다.

### 테스트 씬 경로

`TestingChamber.cs` 는 절차 생성을 완전히 건너뛴다.

- `useExistingSceneContent = true`
- entrance/exit 도 손배치한 것을 사용

즉 테스트 씬에서는 완전 수동 배치도 가능하다.

### 결론

- 본 게임 경로: `절차적 룸 배치 + 수동 룸 콘텐츠`
- 테스트 경로: `완전 수동 배치`

---

## 4. 충돌 지형과 배경 지형의 구분

충돌 지형과 배경 지형은 분리되어 있다.

### 배경 지형

관련 리소스:
- `Assets/Resources/Backgrounds/Prefabs/*`

생성 위치:
- 씬의 `_BACKGROUND` 부모 아래 생성

생성 코드:
- `LevelGenerator.CreateBackground()`

역할:
- 배경 타일과 데칼을 채움
- 플레이어 충돌용이 아니라 시각용 레이어

### 충돌 지형

관련 리소스:
- `Assets/Resources/Tiles/Prefabs/*`
- `Assets/Resources/Bounds/*`
- 룸 프리팹 내부의 Tile 오브젝트

생성 위치:
- 룸 내부 타일은 `_ROOMS` 아래
- 외곽 경계는 `_BOUNDS` 아래

대표 예시:
- `Dirt`
- `MetalBlock`
- `OneWayPlatform`
- `Ladder`
- `Spikes`
- `Block`
- `Exit`

### 씬 부모 구조

`Game.unity` 안에는 아래 부모 오브젝트가 있다.

- `_BACKGROUND`
- `_DEBUG`
- `_BOUNDS`
- `_ROOMS`

이 구조만 봐도 배경과 충돌/맵 본체가 분리되어 있음을 알 수 있다.

### 해석

새 프로젝트에서 복제할 때는 최소한:

- `Background visuals`
- `Solid tiles / platforms`
- `Outer bounds`

세 층을 분리하는 것이 좋다.

---

## 5. 레벨 시작 시 맵이 구성되는 순서

맵 구성 시작점은 `GameManager.InitializeLevel()` 이다.

### 관련 파일

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`

### 정상 시작 순서

1. `GameManager.Start()`
2. `InitializeLevel()`
3. `levelGenerator.GenerateLevel()`
   - 메인 경로 룸 생성
   - 나머지 룸 생성
4. `levelGenerator.SetupLevel()`
   - `InitializeTiles()`
   - `SetupTiles()`
   - `CreateLevelBounds()`
   - `CreateBackground()`
5. `levelGenerator.PlaceEntranceAndExit()`
6. `SpawnPlayer(levelGenerator.entrance.transform.position)`

### 중요한 해석

생성 순서는 `배경 먼저`가 아니라 아래 순서다.

1. 룸 프리팹 배치
2. 그 안의 타일들을 배열에 등록
3. 타일 비주얼 정리
4. 외곽 경계 생성
5. 배경 생성
6. 입구/출구 생성
7. 플레이어 스폰

즉 배경은 맵의 기반 데이터가 아니라 후처리 시각 레이어다.

### 테스트 씬 순서

`TestingChamber` 모드에서는:

1. `GenerateLevel()` 생략
2. `ScanAndRegisterExistingEntities()` 실행
3. 그래도 `SetupLevel()` 은 수행
4. entrance/exit 는 hand-placed 참조 사용

즉 수동 배치 씬에서도 `InitializeTiles()` 와 `SetupTiles()` 는 여전히 중요하다.

---

## 6. 플레이어 스폰 위치 결정 방식

### 관련 파일

- `Assets/Scripts/Misc/GameManager.cs`
- `Assets/Scripts/LevelGenerator/LevelGenerator.cs`
- `Assets/Scripts/LevelGenerator/Room.cs`

### 스폰 위치 계산 과정

1. `LevelGenerator` 가 `firstRoom` 과 `lastRoom` 을 기억함
2. `PlaceEntranceAndExit()` 에서
   - `firstRoom.GetSuitableEntranceOrExitTile()` 호출
   - 그 타일 위에 `Entrance` 타일 프리팹 생성
3. `GameManager.SpawnPlayer(levelGenerator.entrance.transform.position)` 호출
4. 실제 플레이어는 `Entrance` 위치에서 `+ new Vector3(8, 0, 0)` 만큼 오른쪽으로 보정되어 스폰

### `GetSuitableEntranceOrExitTile()` 의 의미

`Room.cs` 에서:
- 방 안 타일을 모두 조사
- 이름에 `Dirt` 가 포함된 타일만 후보로 사용
- 그 타일 위 칸이 비어 있어야 함
- 방 윗경계를 넘지 않아야 함

즉 entrance/exit 는 `바닥 역할을 하는 적절한 타일 위`에 놓인다.

### 새 프로젝트에서 중요한 해석

플레이어 스폰은 랜덤 위치가 아니다.

- 룸 선택은 생성기가 한다
- 스폰 타일 선택은 `Room` 이 한다
- 최종 플레이어 위치는 entrance 오브젝트 기준 반 타일 오프셋이다

---

## 7. 새 프로젝트에서 꼭 기억할 구조 요약

이 레벨 시스템은 아래처럼 이해하면 된다.

- `Tile` = 개별 지형 단위
- `Tiles[,]` = 런타임 타일맵
- `Room` = 수동 제작된 방 단위
- `Rooms[,]` = 레벨의 룸 그리드
- `LevelGenerator` = 룸 배치 + 타일 초기화 + 배경/경계/입구/출구 생성
- `GameManager` = 위 과정을 시작하고 플레이어를 스폰

가장 중요한 결론:

**이 프로젝트는 Unity Tilemap 기반이 아니라, Room prefab + Tile object + runtime arrays 기반의 하이브리드 레벨 시스템이다.**
