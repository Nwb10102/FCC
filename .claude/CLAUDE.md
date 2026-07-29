# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**Final Curtain Call** (부제: 잊혀진 자들의 서커스) — Unity 6000.4.6f1 / URP / 2D 플랫포머 + 심리적 공포 + 액션. PC 타겟.

프로젝트 코드와 에셋은 전부 `Assets/_Project/` 안에 있다. `Assets/_Recovery/` 는 복구 잔해라 참조하지 말 것.

## Antigravity AI와의 협업 파이프라인 (Orca CLI 연동)

이 리포지토리에서는 **Antigravity AI (기획 분석·지시·코드 검토)** 와 **Claude Code (실제 코드 구현)** 가 4단계 협업 프로세스를 통해 작업을 수행합니다:

1. **[기획 분석 & 계획 작성]** Antigravity가 Notion 기획서 및 코드를 읽고 구현 계획을 수립합니다.
2. **[사용자 승인]** 작성된 구현 계획을 사용자에게 공유하여 승인을 받습니다.
3. **[Claude 코드 구현]** 승인이 완료되면 Antigravity가 Orca CLI를 통해 전달하는 승인된 요구사항 및 구체적 명세를 수신하여 Claude가 실제 C# 코드를 구현합니다.
4. **[구현 검토 & 보충 정리]** Claude의 구현이 끝나면 Antigravity가 작성된 코드를 검토하여 구현 완성도(달성률)를 측정하고 미진한 부분을 정리/보충합니다.

**Claude 수칙**:
- Antigravity가 Orca CLI를 통해 전달하는 명세 및 아키텍처 규칙(K&R 중괄호, 전투 파이프라인 등)을 준수하여 C# 코드를 작성할 것.
- 작업 완료 후 작성/수정된 클래스 목록과 주요 구현 내용을 명확히 정리하여 보고할 것.

## 기획안 및 게임 구현 현황 (작업 전 확인)

전체 기획과 게임 구현 관리 페이지는 Notion 문서로 수시 관리되고 있습니다. **게임 로직·시스템·레벨 관련 작업 전 반드시 아래 두 문서를 읽고 구현 진행 상황 및 사양을 확인할 것** (Notion MCP `notion-fetch` 등으로 조회):

1. **통합 기획서 (Notion)**: https://tide-ink-208.notion.site/Ai-3a988e3012b280b58e60dcddb564c86e
2. **게임 구현 관리 현황 (Notion)**: https://tide-ink-208.notion.site/3ac88e3012b28071b3f2fcb8dfb31617?pvs=74

에이전트는 작업을 시작할 때 '게임 구현 관리 현황' 페이지를 읽어 게임 구현이 어디까지 진행되었는지(완료된 기능, 작업 중/미구현 기능)를 정확히 확인하고, 작업 진행 및 완료 후 해당 페이지의 상태를 갱신하고 정리해야 합니다.

코드에 직접 매핑되는 핵심 설정:

- **자아 게이지** = 체력. 0이면 게임오버. 코드상 `Health` 컴포넌트.
- **기억 조각** = 핵심 수집 재화. 회복 아이템 겸 스킬 포인트 재료. 챕터1 10개 / 챕터2 20개 / 챕터4 큰 조각 1개. **수집·소비 로직은 아직 미구현.**
- **거울** = 세이브포인트 겸 스킬 "정비하기" 지점. 코드상 `SaveMirror` (저장 + 자아 게이지 전량 회복까지 구현, 정비 UI는 미구현).
- **오염도 게이지** (챕터2~) = 최대 100, 시간당 누적, "팥" 섭취 시 0으로 초기화. 다 차면 사망. **아직 미구현.** 붙일 때는 `SaveData` 에 필드를 추가하는 식이면 된다 (구버전 세이브 호환은 아래 세이브 항목 참고).
- **스킬 6종** — Pure Dream(주인공, 정화) / Broken Phantasm(칼잡이, 투사체) / Cycle of Fate(저글러, 스택 폭발) / Invisible Reality(마임, 투명 벽) / Bent Spirit(컨토셔니스트, 회피+은신) / Close Call(곡예사, 이동기 🟡미확정). 최대 3개 장착, 기억 조각으로 업그레이드. **`Skill.cs`/`SkillManager.cs` 는 아직 껍데기.**
- **챕터 구조** — 1 서커스 극장 → 2 저승 도시 → 3 지하감옥 → 4 무대(보스전) → 엔딩 2분기.

문서에서 🟡 표시는 미확정 항목이다. 특히 **장르(선형 vs 메트로배니아)가 미확정**이므로, 맵 구조나 레벨 디자인을 새로 설계하는 작업은 먼저 확인을 받을 것.

## 빌드 · 실행

CLI 빌드/테스트 스크립트나 CI는 없다. 전부 Unity Editor에서 수행한다.

- MCP for Unity(`com.coplaydev.unity-mcp`)가 설치되어 있어 `mcp__UnityMCP__*` 툴로 에디터를 직접 조작할 수 있다. 스크립트를 수정한 뒤에는 `read_console` 로 컴파일 에러를 확인하고, `editor_state` 의 `isCompiling` 이 끝난 뒤에 새 타입을 사용할 것.
- 한글 TMP 폰트 SDF 생성: 에디터 메뉴 `Tools ▸ KW Font ▸ Build Bold / Build Light` (`Assets/_Project/Editor/KwFontAssetBuilder.cs`, 아틀라스 4096², 한글 음절 11,172자 포함). 결과물 `Assets/_Project/Assets/Font/SDF/*.asset` 은 Git LFS 추적 대상.
- 유닛 테스트는 없다. `com.unity.test-framework` 는 설치돼 있지만 테스트 어셈블리가 없고, `DialogueTest.cs` 는 대사 마크업을 눈으로 확인하는 인게임 컴포넌트다.
- asmdef 없음 — 모든 스크립트가 `Assembly-CSharp` 한 덩어리다. 스크립트 하나만 고쳐도 전체가 재컴파일된다.

## 아키텍처

### 전투 파이프라인 (이벤트 기반)

`Health` 가 허브다. **파일명은 `HealthSystem.cs` 지만 클래스명은 `Health`** 다.

```
Player_Combat.DealDamage()          // OverlapCircleAll + HashSet 중복 방지
  → Health.TakeDamage(damage, sourcePosition)
      → OnDamaged(damage, sourcePosition)  ─┬→ HitReactor  넉백 · HitVfx 스파크 · DamagePopup · HitFeedback
                                            ├→ HitFlash    피격자 본인의 플래시/스쿼시
                                            └→ HealthBar
      → OnDeath(sourcePosition)            ─→ HitReactor  사망 연출
```

지켜야 할 규칙:

- **`sourcePosition`(공격 원점)이 모든 방향 계산의 출발점**이다. 넉백·스파크 방향·이펙트 위치가 전부 여기서 파생되므로 데미지를 주는 쪽은 반드시 정확한 원점을 넘길 것.
- `Health.Die()` 는 이벤트 발행 직후 `Destroy(gameObject)` 를 호출한다. **사망 연출은 반드시 오브젝트 바깥(싱글턴)에서 재생해야 한다.**
- `OnDamaged` 는 치명타여도 항상 발행된다. 처치 여부는 구독자가 `CurrentHealth <= 0` 으로 판단한다. 처치 시 `HitReactor` 는 일반 피드백을 건너뛰고 `OnDeath` 쪽에서 더 강한 피드백을 재생한다 (히트스톱 이중 적용 방지).
- 피격 무적(`invincibleTime`)은 **플레이어만** 걸 것 (0.9 내외). 몬스터에 걸면 공격 쿨타임보다 길어져 때려도 반응 없는 것처럼 보인다.
- 데미지가 아닌 경로(세이브 복원, 거울 회복, 기억 조각 섭취)로 체력을 바꿀 때는 `Health.SetHealth(value)` / `RestoreFull()` 을 쓴다. **`SetHealth(0)` 은 `Die()` 를 돌리지 않는다** — 사망은 `TakeDamage` 를 통해서만 일어나야 연출이 한 번만 재생된다.
- 카메라 쉐이크 파형은 `HitFeedback.ApplyShakeProfile()` 이 코드로 만들어 `CinemachineImpulseSource` 에 덮어쓴다. **인스펙터에서 Impulse Shape / Duration 을 고쳐도 플레이하면 무시된다.** 감각 조정은 `HitFeedback` 의 `shakeDuration` · `shakeOscillations` · `shakeDamping` 으로 할 것 (플레이 중 값을 바꾸면 `OnValidate` 로 즉시 반영된다).

### 싱글턴

`HitFeedback` · `HitVfx` · `DamagePopup` · `ObjectiveManager` · `SaveManager` 5개. 전부 `Awake()` 에서 중복 검사 후 `DontDestroyOnLoad`.

호출할 때는 **`?.` 대신 `!= null`** 을 쓸 것. 파괴된 뒤에도 C# 참조가 남을 수 있어 Unity의 `==` 오버로드를 타야 한다.

### 스토리 (대사 · 컷씬)

- 재생 루프는 `DialoguePlayer` (static, `IEnumerator Play(...)`) 하나로 모여 있고, **영역 진입형(`DialogueTriggerZone`)과 컷씬형(`DialogueStep`)이 이걸 공유**한다. 진행 규칙(타자기 스킵 / AUTO 대기)을 고칠 때는 여기만 고치면 된다.
- 대사 데이터는 ScriptableObject가 아니라 **컴포넌트 인스펙터의 `List<DialogueEntry>`** 에 직접 적는다. 구 ScriptableObject 방식(`DialogueScript`/`DialogueLine`)은 은퇴해서 `쓰레기통/Dialogue/` 에 있다.
- 표시는 `DialogueView`, 본문 마크업 태그(`<shake>` `<wave>` `<rainbow>` `<round>` `<speed>`)는 `DialogueEffect` 담당.
- 컷씬은 `CutSceneManager` + 자식 오브젝트로 붙인 `CutSceneStep` 들을 순서대로 `yield return step.Execute()` 한다. 새 연출을 추가하려면 `Story/CutScene/Steps/` 에 `CutSceneStep` 파생 클래스를 하나 만들면 된다.

### 목표(퀘스트) 시스템

`ObjectiveManager` 의 진입점은 `CompleteObjective(id)` 와 `AddProgress(id, amount)` 둘뿐이다. `DialogueTriggerZone` · `CutSceneManager` · `SaveMirror` 에는 `objectiveId` 필드가 있어서, 비어 있지 않으면 재생/저장이 끝날 때 자동으로 해당 목표를 완료 처리한다. UI는 `ObjectiveChecklistView` 가 이벤트를 구독해 갱신한다.

세이브용으로 `CaptureState()` / `RestoreState(snapshot)` 가 따로 있다. 진행도(`id`·`currentCount`·`isCompleted`)만 `ObjectiveSaveEntry` 로 떼어 저장하므로, 설명문·목표 수량 같은 기획 데이터를 고쳐도 기존 세이브가 그걸 덮어쓰지 않는다. **`RestoreState` 는 완료 이벤트를 다시 쏘지 않는다** — 불러올 때마다 컷신/보상이 재생되면 안 되기 때문.

### 상호작용 (E키)

`Scripts/Interaction/` 3종이다.

- `IInteractable` — `InteractLabel` / `CanInteract` / `PromptAnchor` / `Interact(interactor)`. **거울·NPC·조사 오브젝트를 새로 만들 땐 이것만 구현하면** 탐지·프롬프트·입력 전달이 자동으로 따라온다.
- `PlayerInteractor` (플레이어에 부착) — 매 프레임 `OverlapCircle` 로 주변을 훑어 가장 가까운 대상을 잡고, `[E] 문구` 프롬프트(`TextMeshPro` 를 코드로 생성)를 띄운다. 거리는 콜라이더가 아니라 `PromptAnchor` 기준으로 잰다.
- `SaveMirror` — 세이브포인트 구현체.

주의할 점:

- 대상 오브젝트에는 **`Is Trigger` 콜라이더**가 있어야 탐지된다 (`ContactFilter2D.useTriggers = true`). 콜라이더가 자식에 있어도 `GetComponentInParent` 로 찾는다.
- `Player_move.isMovementLocked` 가 켜져 있으면(대사·컷씬 중) 상호작용이 통째로 막힌다. 대사 진행키와 겹쳐 대화가 끝나는 순간 같은 입력이 한 번 더 먹히는 것을 막기 위한 것이다.
- 프롬프트 연출은 전부 `Time.unscaledDeltaTime` 기준이다 (히트스톱 중에도 정상 속도로 떠 있어야 하므로).

### 세이브

`SaveManager` 가 파일 입출력(`Write`/`Read`/`DeleteSave`)과 상태 수집·복원(`SaveGame`/`LoadGame`)을 전부 소유한다. 저장 위치는 `Application.persistentDataPath/save.json`, 직렬화는 `JsonUtility`.

```
SaveMirror.Interact()  → Health.RestoreFull()          // 회복이 먼저. 안 그러면 깎인 체력이 기록된다.
                       → SaveManager.SaveGame(mirrorId, respawnPosition)
                             → Health / ObjectiveManager.CaptureState() 수집 → Write → OnSaved
                       → ObjectiveManager.CompleteObjective(objectiveId)   // objectiveId 가 비어있지 않을 때
```

- 정식 저장 지점은 **거울(`SaveMirror`)** 이다. `Player_move` 의 **F5/F9 는 개발용 단축키**이고, `checkpointId` 를 빈 문자열로 넘겨 퀵세이브로 기록한다.
- `SaveGame` 오버로드가 둘이다. `SaveGame(id, respawnPosition)` 은 복귀 지점을 명시(거울용, 플레이어 현재 위치를 쓰면 거울에 붙어 있다 저장했을 때 복귀 위치가 어긋난다), `SaveGame(id)` 는 플레이어가 선 자리를 그대로 쓴다.
- `SaveData` 는 **`JsonUtility` 가 다루므로 전부 public 필드**여야 한다. 필드를 새로 추가해도 기존 세이브는 그대로 읽히고(없는 필드는 0/null), `maxHealth == 0` 이면 체력을 기록하지 않던 구버전 세이브로 보고 체력을 건드리지 않는다. 후속 시스템(오염도·기억 조각)은 여기에 필드만 늘려서 붙인다.
- `LoadGame()` 은 **같은 씬 안에서만** 복원한다. 메인 메뉴 → 이어하기처럼 씬 전환이 필요하면 `Read()` 로 `sceneName` 을 먼저 확인해 씬을 연 뒤 호출할 것.
- `SaveManager.Awake()` 는 `transform.SetParent(null)` 을 먼저 부른다. `DontDestroyOnLoad` 는 루트 오브젝트에서만 동작하는데 씬에서는 `GAME_MANAGER` 하위에 정리용으로 놓여 있기 때문.

### 입력

`Assets/_Project/Assets/Input/Client.inputactions` 를 쓴다 (루트의 `InputSystem_Actions.inputactions` 는 Unity 기본 템플릿이라 미사용).

- `Client ▸ Player` — Move / Jump / Attack / Interact (`E` · 패드 남쪽 버튼)
- `Client ▸ Ui` — NextDialogue (대사 다음/스킵)

`Player_Combat`(`OnAttack`)과 `PlayerInteractor`(`OnInteract`)는 `PlayerInput` 의 SendMessage 방식(`void OnXxx(InputValue)`)이고, 대사 쪽은 `InputActionReference` 를 인스펙터로 주입받는다.

키 바인딩을 바꾸면 `PlayerInteractor.keyLabel`(프롬프트에 표시되는 `[E]`)도 같이 고칠 것. 자동 연동되지 않는다.

### 씬

`BootScene` / `Main_menu` → `CoreScene`(인게임 본편). 빌드 세팅에 활성화된 씬은 `Main_menu` 와 `CoreScene` 뿐이다.

## 코드 컨벤션

`.editorconfig` 기준 **K&R 중괄호**(여는 중괄호 같은 줄). `Assets/_Project/Assets/Scripts/` 아래 `Character/` · `Combat/` · `Objective/` · `Interaction/` · `_Data/` 계열이 표준 스타일이다:

```csharp
public class HitReactor : MonoBehaviour {
    #region 인스펙터 변수

    [Header("넉백")]
    public float knockbackForce = 6f; // 넉백 세기.

    #endregion
    #region 컴포넌트 변수

    Health health;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        health = GetComponent<Health>();
    }

    #endregion
}
```

- `#region` 구획을 한글로 나눈다: `인스펙터 변수` / `컴포넌트 변수` / `유니티 라이프 사이클` / 기능별 구획.
- 인스펙터 노출 필드는 `public` + `[Header]` + **줄 끝 한글 주석으로 용도 설명**. 접근제한자 `private` 는 생략한다.
- 씬에서 사람이 직접 배치·연결해줘야 하는 필드는 `// **몬스터 발밑에 빈 오브젝트를 만드세요.**` 처럼 `**` 로 강조해 둔다.
- 주석은 한글로, "무엇"이 아니라 **"왜 이렇게 했는지"** 를 적는다 (기존 코드가 전부 이 방식).
- 비동기는 전부 코루틴이다. async/await, UniTask, DOTween 미사용 (`Plugins/Demigiant` 는 들어만 있고 코드에서 안 쓴다).
- `Story/` 계열(Allman 중괄호 + `_` 접두사 private 필드 + XML 주석)과 `_Player/Move/Player_move.cs` 는 스타일이 다르다. **레거시로 취급**하고, 새 코드는 위 K&R 스타일로 쓰되 해당 파일을 손볼 때 점진적으로 맞춘다. `_Data/` 는 세이브 시스템 작업 때 이미 K&R로 옮겼다.

## 파일 · 폴더 정리 규칙

원칙은 하나다. **큰 틀(주체 · 시스템)은 폴더로 나누고, 그 안의 세세한 기능은 폴더를 더 파지 말고 스크립트 이름으로 구분한다.**

```
Scripts/
  _Player/      플레이어 주체 (Camera/ · Combat/ · Move/ · Skill/ 로 갈라짐)
  Monster/      몬스터 주체
  (Npc/)        NPC가 생기면 여기에 새로 만든다
  Character/    플레이어·몬스터가 함께 쓰는 공용 컴포넌트 (Health · HitReactor · HitFlash · HealthBar · GroundMoveSystem · FlyMoveSystem)
  Combat/       전투 연출 싱글턴 (HitFeedback · HitVfx · DamagePopup)
  Interaction/  상호작용 (IInteractable · PlayerInteractor · SaveMirror)
  Objective/    목표(퀘스트)
  Story/        대사 · 컷씬 (Dialogue/ · CutScene/Steps/)
  Ui/           메뉴 · 설정 UI
  System/       게임 전역 (GameSpeedController · GameDebugLog)
  _Data/        세이브 데이터
```

1. 새 스크립트는 **"누구의 기능인가"** 를 먼저 보고 폴더를 고른다. 플레이어 전용이면 `_Player/`, 몬스터 전용이면 `Monster/`, **둘 다 붙는 공용이면 `Character/`**.
2. **폴더는 큰 틀에서 멈춘다.** 파일 두세 개 때문에 하위 폴더를 또 만들지 말 것. 한 폴더의 파일이 여덟 개를 넘고 성격이 뚜렷하게 갈릴 때만 쪼갠다 (`_Player/` 와 `Story/` 가 그렇게 갈라진 것이다).
3. 파일 이름은 **`{큰 틀}{기능}`** 꼴로 소속을 앞에 붙인다 — `Player_Combat` · `ObjectiveManager` · `DialogueView` · `CutSceneStep` · `SaveManager`. 검색 결과나 인스펙터에서 폴더가 안 보여도 어디 소속인지 드러나게 하기 위함이다. `_Player/` 계열만 `Player_` 처럼 밑줄을 쓰고 나머지는 붙여 쓴다 (기존 파일이 그렇게 되어 있다).
4. **파일명 = 클래스명.** 지금 어긋나 있는 `HealthSystem.cs`(클래스 `Health`) 와 `Ui/Ui_LoadScenes.cs`(클래스 `Settingscript`) 는 레거시이므로 따라하지 말 것. `Monster/Attack.cs` 도 이름이 규칙 이전 것이라 너무 일반적이니, 손볼 일이 생기면 `Monster_Attack` 으로 맞춘다.
5. 인터페이스는 `I` 접두사(`IInteractable`), 파생 클래스는 베이스 이름을 접미사로 둔다(`CutSceneStep` ← `DialogueStep` · `CameraStep` · `WaitStep` · `ImageStep`).
6. 에셋도 같은 원칙이다 — `Prefabs/Monster` · `Prefabs/UI` · `Prefabs/VFX` · `Sprites/Player` 처럼 큰 틀만 폴더로 두고 나머지는 파일 이름으로 구분한다.
7. 폴더 이름의 `_` 접두사(`_Player` · `_Data`)는 프로젝트 창에서 위로 정렬시키려는 것뿐이고 다른 의미는 없다. **새 큰 틀 폴더에는 붙이지 말 것** (`Monster/` · `Interaction/` · `Objective/` 가 그렇다).
8. 파일을 옮길 때는 반드시 **`.meta` 를 짝으로 함께** 옮긴다(`git mv`). 빠뜨리면 GUID가 새로 발급돼 씬·프리팹에 걸린 컴포넌트 연결이 전부 끊긴다. Unity 에디터를 켠 채라면 프로젝트 창에서 드래그하는 편이 안전하다.

## 쓰레기통 규칙

1. 필요없는 스크립트/에셋 삭제를 요청하면 삭제하지 않고 `쓰레기통/` 으로 `git mv` 한다.
2. 쓰레기통 위치는 반드시 **프로젝트 루트** (= `Assets/` 바깥).
   `Assets/` 안에 두면 `.cs` 가 계속 컴파일되고 짝 잃은 `.asset` 도 살아남아 정리가 안 된다.
3. 바로 넣지 말고, 원래 파일의 **부모 폴더 이름**으로 하위 폴더를 만들어 그 안에 넣는다.
   (예: `Scripts/Story/Dialogue/DialogueLine.cs` → `쓰레기통/Dialogue/DialogueLine.cs`)
   단, 같은 시스템에 속한 파일이면 부모 폴더가 달라도 그 시스템 폴더로 함께 묶는다.
   (예: `Assets/Story/NewDialogue.asset` 은 Dialogue 시스템 에셋이므로 `쓰레기통/Dialogue/`)
4. `.meta` 파일도 반드시 짝으로 함께 옮긴다.
5. 쓰레기통과 프로젝트에 같은 이름·같은 내용의 파일이 있으면,
   쓰레기통 것만 남기고 프로젝트 쪽을 지운다.
