# MINIMAL_LEVEL_REBUILD

## 목표

이 문서는 새 Unity 프로젝트에서 이 저장소의 레벨 구조를 전부 복제하는 대신,
`간단한 1스테이지`를 가장 적은 구성으로 복제하는 방법을 설명한다.

핵심 원칙은 하나다.

**처음에는 절차 생성까지 따라 하지 말고, 손배치 1스테이지를 먼저 만든다.**

이 프로젝트 자체도 `TestingChamber` 패턴으로 같은 접근을 쓰고 있다.

---

## 1. 가장 먼저 복제할 구조

새 프로젝트에서 1스테이지를 만들려면 아래 네 가지면 충분하다.

1. 타일 오브젝트
2. 방 또는 방처럼 쓰는 부모 오브젝트
3. entrance / exit
4. GameManager 가 시작 시 플레이어를 entrance 근처에 스폰하는 로직

처음에는 아래를 굳이 안 만들어도 된다.

- 메인 경로 생성 알고리즘
- special room 확률 배치
- 배경 데칼 랜덤 생성
- 여러 룸 조합

---

## 2. 최소 오브젝트 구성

### 씬 루트 추천 구조

- `GameManager`
- `LevelGenerator`
- `_ROOMS`
- `_BACKGROUND`
- `_BOUNDS`
- `_DEBUG`

이 구조는 원본 프로젝트와 같은 부모 구조다.

### 최소 플레이 공간

`_ROOMS` 아래에 다음을 배치한다.

- `Room_00` 같은 부모 오브젝트 하나
- 그 안에 `Dirt` 타일 여러 개
- 바닥용 `Dirt` 줄
- 벽용 `Dirt` 줄
- 필요하면 `OneWayPlatform` 한 개
- `Entrance` 하나
- `Exit` 하나

### 최소 배경

`_BACKGROUND` 아래에는 배경 스프라이트 1장 또는 타일형 배경 몇 개만 깔아도 충분하다.
원본처럼 랜덤 데칼까지는 필요 없다.

### 최소 경계

`_BOUNDS` 아래에는 바깥으로 떨어지지 않게 큰 BoxCollider2D 네 개만 만들어도 된다.
원본의 `BoundsStraight`, `BoundsCorner` 비주얼은 나중에 붙여도 된다.

---

## 3. 최소로 복제해야 할 스크립트 개념

### 꼭 필요한 것

- `GameManager`
- `LevelGenerator` 또는 그 축약판
- `Tile`
- `Room` 또는 간단한 room metadata 컴포넌트

### 선택적이지만 원본 구조를 닮게 만드는 것

- `Exit`
- `TestingChamber` 스타일의 수동 시작 설정

### 꼭 이해해야 할 점

원본 프로젝트는 `Tilemap` 을 쓰지 않으므로,
새 프로젝트에서도 처음엔 Unity Tilemap 시스템보다 `16x16 타일 오브젝트` 방식으로 가는 편이 더 비슷하다.

---

## 4. 간단한 1스테이지를 만드는 가장 쉬운 방법

### 방법 A. 완전 수동 배치

이 방식이 가장 쉽다.

1. `LevelGenerator.GenerateLevel()` 은 아예 쓰지 않는다.
2. 씬에 바닥, 벽, 플랫폼, entrance, exit 를 직접 놓는다.
3. `GameManager` 는 `useExistingSceneContent = true` 같은 모드로 시작한다.
4. `LevelGenerator.SetupLevel()` 만 호출해서 타일 등록과 비주얼 정리만 한다.
5. 플레이어는 entrance 기준으로 스폰한다.

이 방식은 원본의 `TestingChamber` 와 같은 철학이다.

### 방법 B. 방 1개만 두고 반자동 구성

1. `Room` 프리팹 하나를 만든다.
2. 그 안에 타일 배치를 수동으로 만든다.
3. `LevelGenerator` 가 그 룸 하나만 `_ROOMS` 아래에 생성하게 한다.
4. 타일 초기화 후 entrance/exit 를 자동 배치한다.

이 방식은 나중에 여러 룸 확장으로 가기 좋다.

---

## 5. 내가 추천하는 최소 복제 방식

가장 추천하는 방식은 아래다.

### 1단계

`TestingChamber` 스타일로 완전 수동 1스테이지를 만든다.

구성:
- 바닥 타일
- 벽 타일
- 한 칸짜리 발판
- entrance
- exit
- player spawn

### 2단계

`Tile.InitializeTile()` 과 `Tile.SetupTile()` 개념을 넣는다.

즉:
- 각 타일이 `x, y` 좌표를 가진다
- 타일이 주변 이웃을 보고 스프라이트를 바꾼다

### 3단계

나중에 `Room` 개념을 넣는다.

즉:
- 룸이 자기 내부 타일 범위를 알고
- 적절한 entrance/exit 바닥 타일을 찾을 수 있게 만든다

### 4단계

마지막에 절차 생성기를 붙인다.

---

## 6. 원본 프로젝트를 기준으로 한 1스테이지 복제 절차

### Step 1. 타일 크기를 16x16으로 정한다

원본은 `Tile.Width = 16`, `Tile.Height = 16` 이다.
처음부터 같은 기준으로 두면 계산이 단순해진다.

### Step 2. `Dirt` 타일 프리팹을 만든다

최소 구성:
- `SpriteRenderer`
- `BoxCollider2D`
- `Tile` 스크립트

처음엔 장식용 child 오브젝트는 없어도 된다.

### Step 3. 바닥과 벽을 손으로 배치한다

예를 들면:
- 가로 20칸 바닥
- 양 끝 벽 6칸 높이
- 중간에 4칸짜리 발판 하나

### Step 4. entrance 와 exit 를 수동 배치한다

원본처럼 하려면 entrance 는 `바닥 타일 바로 위`에 둔다.
출구도 같은 방식이다.

### Step 5. `GameManager` 가 entrance 위치에서 플레이어를 스폰하게 한다

원본 로직상 최종 플레이어 위치는 entrance 위치에서 반 타일 오른쪽으로 보정된다.
즉 대략:

- `playerSpawn = entrance.position + (8, 0, 0)`

### Step 6. 배경은 별도 부모에 둔다

`_BACKGROUND` 아래에 배경 스프라이트를 깔고,
플레이어가 서는 타일과는 충돌을 분리한다.

### Step 7. 외곽 경계도 따로 둔다

맵 밖으로 빠져나가지 않게 `_BOUNDS` 아래에 큰 충돌체를 둔다.

---

## 7. 아주 최소한의 코드 흐름

새 프로젝트에서 1스테이지만 복제할 때는 아래 흐름이면 충분하다.

1. 씬 로드
2. `GameManager.Start()`
3. `LevelGenerator.SetupLevel()`
   - 씬 안 모든 `Tile` 찾기
   - `Tiles[,]` 배열에 등록
   - 각 타일 스프라이트 세팅
4. `Entrance` 참조 찾기 또는 미리 연결
5. `SpawnPlayer(entrance.position)`

즉 이 단계에서는 `GenerateLevel()` 이 없어도 된다.

---

## 8. 최소 복제에 필요한 핵심 개념만 다시 요약

### 타일맵을 복제하려면

- Unity Tilemap 말고 `Tile` 오브젝트를 쓴다
- 런타임에 `Tile[,]` 배열에 등록한다

### 방 개념을 복제하려면

- 방 하나를 부모 오브젝트로 둔다
- 나중에 `Room` 컴포넌트로 확장한다

### 맵 생성 느낌을 복제하려면

- 일단 수동 배치로 시작한다
- 나중에 룸을 여러 개 랜덤 배치한다

### 배경/충돌 분리를 복제하려면

- `_BACKGROUND` 는 비충돌
- `_ROOMS` 와 `_BOUNDS` 는 충돌 지형

### 스폰 방식을 복제하려면

- entrance 를 바닥 위에 놓고
- player 는 entrance 기준 반 타일 오프셋으로 스폰한다

---

## 9. 가장 현실적인 첫 목표

새 프로젝트에서 가장 현실적인 첫 목표는 이것이다.

**바닥 타일, 벽 타일, 발판 하나, entrance, exit 가 있는 손배치 1스테이지를 만들고, 게임 시작 시 entrance 위치에 플레이어가 스폰되게 하는 것**

여기까지 되면 이미 이 프로젝트의 레벨 시스템 핵심 철학은 복제한 셈이다.

그 다음에만:

- room metadata
- tile decoration
- entrance/exit 자동 선택
- procedural room placement

를 차례대로 얹으면 된다.
