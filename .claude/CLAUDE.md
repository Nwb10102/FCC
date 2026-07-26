# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**Final Curtain Call** (부제: 잊혀진 자들의 서커스) — Unity 6000.4.6f1 / URP / 2D 플랫포머 + 심리적 공포 + 액션. PC 타겟.

프로젝트 코드와 에셋은 전부 `Assets/_Project/` 안에 있다. `Assets/_Recovery/` 는 복구 잔해라 참조하지 말 것.

## 기획안 (작업 전 확인)

전체 기획은 Notion 통합 문서 하나에 정리되어 있다. **게임 로직·시스템·레벨 관련 작업 전에 반드시 이 문서를 읽을 것** (Notion MCP `notion-fetch` 로 조회):

https://tide-ink-208.notion.site/Ai-3a988e3012b280b58e60dcddb564c86e

코드에 직접 매핑되는 핵심 설정:

- **자아 게이지** = 체력. 0이면 게임오버. 코드상 `Health` 컴포넌트.
- **기억 조각** = 핵심 수집 재화. 회복 아이템 겸 스킬 포인트 재료. 챕터1 10개 / 챕터2 20개 / 챕터4 큰 조각 1개.
- **오염도 게이지** (챕터2~) = 최대 100, 시간당 누적, "팥" 섭취 시 0으로 초기화. 다 차면 사망. **아직 미구현.**
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

### 싱글턴

`HitFeedback` · `HitVfx` · `DamagePopup` · `ObjectiveManager` · `SaveManager` 5개. 전부 `Awake()` 에서 중복 검사 후 `DontDestroyOnLoad`.

호출할 때는 **`?.` 대신 `!= null`** 을 쓸 것. 파괴된 뒤에도 C# 참조가 남을 수 있어 Unity의 `==` 오버로드를 타야 한다.

### 스토리 (대사 · 컷씬)

- 재생 루프는 `DialoguePlayer` (static, `IEnumerator Play(...)`) 하나로 모여 있고, **영역 진입형(`DialogueTriggerZone`)과 컷씬형(`DialogueStep`)이 이걸 공유**한다. 진행 규칙(타자기 스킵 / AUTO 대기)을 고칠 때는 여기만 고치면 된다.
- 대사 데이터는 ScriptableObject가 아니라 **컴포넌트 인스펙터의 `List<DialogueEntry>`** 에 직접 적는다. 구 ScriptableObject 방식(`DialogueScript`/`DialogueLine`)은 은퇴해서 `쓰레기통/Dialogue/` 에 있다.
- 표시는 `DialogueView`, 본문 마크업 태그(`<shake>` `<wave>` `<rainbow>` `<round>` `<speed>`)는 `DialogueEffect` 담당.
- 컷씬은 `CutSceneManager` + 자식 오브젝트로 붙인 `CutSceneStep` 들을 순서대로 `yield return step.Execute()` 한다. 새 연출을 추가하려면 `Story/CutScene/Steps/` 에 `CutSceneStep` 파생 클래스를 하나 만들면 된다.

### 목표(퀘스트) 시스템

`ObjectiveManager` 의 진입점은 `CompleteObjective(id)` 와 `AddProgress(id, amount)` 둘뿐이다. `DialogueTriggerZone` 과 `CutSceneManager` 에는 `objectiveId` 필드가 있어서, 비어 있지 않으면 재생이 끝날 때 자동으로 해당 목표를 완료 처리한다. UI는 `ObjectiveChecklistView` 가 이벤트를 구독해 갱신한다.

### 입력

`Assets/_Project/Assets/Input/Client.inputactions` 를 쓴다 (루트의 `InputSystem_Actions.inputactions` 는 Unity 기본 템플릿이라 미사용).

- `Client ▸ Player` — Move / Jump / Attack
- `Client ▸ Ui` — NextDialogue (대사 다음/스킵)

`Player_Combat` 은 `PlayerInput` 의 SendMessage 방식(`void OnAttack(InputValue)`)이고, 대사 쪽은 `InputActionReference` 를 인스펙터로 주입받는다.

### 씬

`BootScene` / `Main_menu` → `CoreScene`(인게임 본편). 빌드 세팅에 활성화된 씬은 `Main_menu` 와 `CoreScene` 뿐이다.

## 코드 컨벤션

`.editorconfig` 기준 **K&R 중괄호**(여는 중괄호 같은 줄). `Assets/_Project/Assets/Scripts/Character/` · `Combat/` · `Objective/` 계열이 표준 스타일이다:

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
- `Story/` · `_Data/` 계열은 Allman 중괄호 + `_` 접두사 private 필드라 스타일이 다르다. **레거시로 취급**하고, 새 코드는 위 K&R 스타일로 쓰되 해당 파일을 손볼 때 점진적으로 맞춘다.

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
