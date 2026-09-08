# 남은 작업 · 결정 대기 목록

던전 · 플랫폼 작업(통과 발판 / 오비 방 / 낙사 무적 / 게이트 거울 / 하드코딩 정리)을 진행하면서
**의도적으로 미루거나, 기획 판단이 필요해 손대지 않은** 항목만 모아 둔 문서다.

이미 끝난 일은 여기 적지 않는다. 구현 현황은 Notion "게임 구현 관리 페이지" 를 따른다.

---

## 1. 기획 판단이 필요한 것

### 1-1. UI · 스킬 문구 다국어 전환

플레이어에게 보이는데 `LocalizedString` 이 아니라 한국어 `string` 으로 남아 있는 표면들이다.
영어 빌드에서 번역이 안 된다.

| 위치 | 문구 |
| --- | --- |
| `SaveMirror.label` · `NpcDialogue.label` · `DungeonGate.label` | `"정비하기"` · `"대화하기"` · `"들어가기"` |
| `SkillLoadoutView` | `"아직 해금한 스킬이 없습니다."` · `"비어 있음"` |
| `SkillRowView` | `"슬롯 {0} 장착 중"` · `"쿨타임 {0:F1}초"` |
| `SkillSlotView` | `"비어 있음"` |
| `SkillBase` | `skillName` · `description` |
| `MainLobbyView` | `"저장된 기록 없음"` |
| `MainLobbyPrefabBuilder` | 메뉴 줄 라벨(`"이어하기"` 등)이 프리팹에 구워져 있음 |

**왜 아직 안 했나** — 새 `UI` String Table 생성 + 키 발급뿐 아니라,
`IInteractable.InteractLabel` 이 `string` 을 즉시 반환하는 구조라 `PlayerInteractor` 의 프롬프트 표시
경로까지 비동기 조회로 바꿔야 한다. 절반만 옮기면 오히려 더 나빠진다.

**결정할 것** — 독립 작업으로 언제 진행할지. 상호작용 프롬프트(3개)만 먼저 떼어낼지.

### 1-2. 오비 방이 기믹 풀에서 차지하는 비중

기믹 풀이 `Hazard_A/B/C` + `Obby_A/B/C/D` 7종인데, 오비 방은 폭 76으로 기존 Hazard(38)의
**두 배**다. 기본 리듬(`DefaultSequence`)이 기믹 구간을 3번 쓰므로 던전 길이가 체감상 꽤 늘어난다.

**선택지**
1. 그대로 두고 플레이해 본 뒤 조정한다.
2. `DungeonRoom.RoomRole` 에 오비 전용 역할을 추가하고 `DungeonGenerator` 에 풀을 하나 더 단다.
   그러면 `sequence` 에서 "던전당 오비 1구간" 처럼 정확히 배치할 수 있다.

### 1-3. 무적 중에는 낙사 데미지도 막힌다

낙사 시 체력 10 감소 + 3초 무적이 걸린다. 그래서 **3초 안에 또 떨어지면 두 번째는 체력이 깎이지 않는다.**
"무적" 의 일관된 동작이라 그렇게 뒀다.

**결정할 것** — 낙사만은 무적을 무시하고 항상 깎을지.

### 1-4. 던전을 클리어한 뒤 죽어서 나와도 거울이 부서진다

전투방을 전부 깨서 클리어가 기록된 뒤 자아 게이지가 0이 되어 밖으로 되돌려진 경우에도
거울 파괴 연출이 재생된다. 클리어는 이미 기록됐으니 맞다고 보고 그렇게 뒀다.

**결정할 것** — 걸어 나온 경우에만 부수고, 죽어서 나오면 거울을 남길지.

### 1-5. 오비 방의 리스폰 지점이 방 입구 한 곳뿐

`DungeonRoom` 이 `respawnPoint` 를 하나만 갖는 구조라, 오비 방 후반부에서 떨어지면
코스를 처음부터 다시 해야 한다. 폭 76짜리 방에서는 꽤 번거로울 수 있다.

**결정할 것** — 플레이해 보고 중간 체크포인트가 필요하면 `DungeonRoom` 에 리스폰 지점
목록을 두고 "가장 최근에 지나온 지점" 을 추적하는 방식으로 확장한다.

---

## 2. 알려진 버그 · 제약

### 2-1. `GameSpeedController` 의 `Space` 충돌  🔴

디버그용 배속 조절 컴포넌트가 `Update()` 에서 무조건 돌고, **`KeyCode.Space` 일시정지 토글이
`MainLobbyView` · `SkillLoadoutView` 의 `Space`(확정)와 충돌**한다.
구 Input Manager(`KeyCode`)를 쓰는 점도 프로젝트 표준(`Client.inputactions`)에서 벗어난다.

→ `#if UNITY_EDITOR` 나 디버그 플래그로 감싸는 것이 최소 조치.

### 2-2. 미션 UI 의 한글이 □ 로 렌더된다  🔴

콘솔에 `MissionTitle` · `MissionDescription` 의 한글이 `LiberationSans SDF` 에 없어 □ 로 대체된다는
경고가 쌓인다. 해당 TMP 에 한글 SDF 폰트(`Font/SDF/SCDream6 SDF.asset` 등)를 지정해야 한다.
(CLAUDE.md 의 UI 규칙 5번 위반)

### 2-3. `Room_Template.prefab` 에 Missing Script 가 있다

유니티가 저장을 거부해서 이 프리팹만 통과 발판 전환이 반영되지 않았다.
`AssignPools` 는 `_Template` 접미사를 보고 풀에서 제외하므로 던전에는 나오지 않는다.
계속 쓸 템플릿이라면 깨진 스크립트를 정리해야 한다.

### 2-4. 세로로 오르내리는 이동 발판은 아직 안 된다

`MovingPlatform` 은 유니티 2D 표면 마찰로 올라탄 대상을 실어 나른다.
발판이 **중력보다 빠르게 내려가면** 플레이어가 떠서 마찰이 끊긴다.
현재 오비 방의 이동 발판은 전부 가로 왕복이다. 승강기가 필요하면 별도 처리를 추가해야 한다.

### 2-5. 거울 파괴 연출이 일시정지 중에도 진행된다

연출이 `Time.unscaledDeltaTime` 기준이다. 히트스톱이 걸린 채 던전을 나와도 얼어붙지 않는 대신,
연출 도중 일시정지 메뉴를 열면 메뉴 뒤에서 계속 진행된다.
(프로젝트의 다른 연출도 전부 unscaled 기준이라 일관성은 맞는다.)

### 2-6. 같은 씬에서 `F9` 로 불러오면 거울 상태가 갱신되지 않는다

`DungeonGate.Start()` 에서 한 번만 클리어 여부를 확인한다.
`SaveManager.LoadGame()` 은 `Start` 가 지난 뒤에 클리어 목록을 되돌리므로, 같은 씬에서 퀵로드하면
이미 부서진 거울이 멀쩡한 채로 남는다. 씬을 새로 들어가면 정상이다.

→ 필요해지면 `SaveManager` 에 로드 완료 이벤트를 두고 게이트가 구독하도록 넓힌다.

### 2-7. `PlayerInteractor.keyLabel` 이 입력 바인딩과 이중 관리된다

화면에 뜨는 `[E]` 문구는 인스펙터 `keyLabel` 이고, 실제 바인딩은 `Client.inputactions` 에 있다.
키를 리바인드하면 프롬프트가 거짓말을 한다.

→ 고치려면 `PlayerInteractor` 에 `InputActionReference` 를 추가해 씬에서 연결하고,
런타임에 유효 바인딩 표시 문자열을 읽어와야 한다.

### 2-8. `ground` 레이어 관련 주석 표기가 어긋난다

실제 레이어명은 소문자 `ground`(index 31)인데 `Player_move` · `GroundMoveSystem` 주석은
"Ground 레이어" 로 적혀 있고, `Monster_Wraith` 주석은 `"Player(3)"` `"ground(31)"` 처럼
레이어 **인덱스**를 못박아 뒀다. 코드는 정상이고 주석만의 문제다.

---

## 3. 하드코딩처럼 보이지만 그대로 두기로 한 것

다시 조사하지 않도록 근거를 남겨 둔다.

| 항목 | 그대로 두는 이유 |
| --- | --- |
| 곳곳의 `playerTag = "Player"` 필드 | Unity 빌트인 태그이고 전부 인스펙터에 노출돼 있다. 이게 올바른 패턴이다 |
| `SaveManager.fileName = "save.json"` | 인스펙터 필드이고 소유자가 한 곳뿐. 어디에도 중복이 없다 |
| `DungeonGenerator.DefaultSequence()` 의 리터럴 | 인스펙터 `sequence` 가 비었을 때만 쓰이는 기본 리듬 데이터다 |
| `DungeonRoomPrefabBuilder` 의 방 좌표 수치 | 그레이박스 레벨 데이터 자체다. 코드로 옮길 대상이 아니다 |
| `Debug.Log` 계열의 한국어 문구 | 개발자용 진단이라 플레이어에게 노출되지 않는다. 번역하면 디버깅만 나빠진다 |
| `Animator.StringToHash("Windup" / "Strike")` | 애니메이터 파라미터 참조의 표준 관용구. 문자열 없는 API가 없다 |
| `ObjectiveItemView.countFormat` · `MainLobbyView.versionFormat` | 순수 숫자 서식이라 로케일 중립. `LocalizedString` 전환 실익이 없다 |
