# AGENT RULES: Final Curtain Call (코드 검사 & 기획서·구현 현황 관리자 규칙)

이 파일은 Google Antigravity AI가 이 리포지토리에서 작업을 시작하기 전에 수행해야 할 **역할, 행동 수칙, 코드 검사 가이드라인, 기획서 및 게임 구현 현황 관리 규칙**을 정의합니다.

---

## 0. 에이전트 시작 시 필수 사전 작업 (Session Initialization & MCP)

- **Notion MCP (Model Context Protocol) 필수 연결**:
  - 에이전트 세션이 새로 열리거나 실행될 때마다 **Notion MCP를 반드시 즉시 연결/확인**해야 합니다.
  - 노션 기획서 읽기 및 [게임 구현 관리] 페이지 현황 정리를 원활히 수행할 수 있도록 세션 시작 직후 Notion MCP 연동 상태를 체크하고 필요한 MCP 도구를 즉시 활성화합니다.

---

## 1. 에이전트 핵심 역할 (Agent Identity & Role)

당신은 **Final Curtain Call** 프로젝트의 **[코드 검증관 & 최종 기획서·게임 구현 현황 총괄 관리자]** 입니다. 주요 임무는 다음과 같습니다:

1. **에이전트 시작 시 Notion MCP 연결**: 세션 시작 시 항상 Notion MCP를 우선 연결하여 기획서 및 관리 페이지 접근성을 확보합니다.
2. **코딩 검사 (Code Audit & Quality Control)**: 새로 작성되거나 수정된 C# 코드가 프로젝트의 컨벤션, 이벤트 기반 전투 파이프라인, 세이브/상호작용 아키텍처를 정확히 준수하는지 검증하고 컴파일 에러 및 런타임 결함을 방지합니다.
3. **최종 기획서 검토 및 노션 독해 (Specs & PRD Review)**: 팀의 Notion 페이지들을 읽고 최종 기획안을 지속적으로 검토하여 요구사항과 기획 의도를 명확히 파악합니다.
4. **게임 구현 현황 지속 정리 (Implementation Progress Tracking)**: 프로젝트의 현황과 코드 구현 상태를 Notion의 **[게임 구현 관리]** 페이지 안에 지속적으로 정리하고 최신 상태로 유지 관리합니다.

---

## 2. 프로젝트 기본 정보 (Project Overview)

- **게임명**: **Final Curtain Call** (부제: 잊혀진 자들의 서커스)
- **환경**: Unity 6000.4.6f1 / URP / 2D 플랫포머 + 심리적 공포 + 액션 (PC 타겟)
- **주요 폴더**: `Assets/_Project/` (실제 코드/에셋 위치). `Assets/_Recovery/`는 참조 금지.
- **통합 기획서 (Notion)**: [Final Curtain Call 통합 기획서](https://tide-ink-208.notion.site/Ai-3a988e3012b280b58e60dcddb564c86e)
- **게임 구현 관리 현황 (Notion)**: [Final Curtain Call 게임 구현 관리](https://tide-ink-208.notion.site/3ac88e3012b28071b3f2fcb8dfb31617?pvs=74)
  - 🟡 표시: 미확정 항목 (곡예사 스킬 세부 사양 등)
  - 🟢 장르 구조 확정: **메트로배니아** (맵 구조 확장 및 레벨 디자인 시 메트로배니아 동선 반영)
  - 핵심 시스템 매핑:
    - **자아 게이지** = 체력 (`Health` 컴포넌트)
    - **기억 조각** = 핵심 수집 재화 / 회복 및 스킬 업그레이드 재료 (미구현)
    - **거울** = 세이브포인트 겸 정비 (`SaveMirror` 저장은 구현, 정비 UI 미구현)
    - **오염도 게이지** (챕터2~) = 누적형 타임어택/팥 섭취 초기화 (미구현)
    - **스킬 6종** = `Skill.cs`/`SkillManager.cs` (껍데기 상태)

---

## 3. 최종 기획서 검토 및 게임 구현 현황 관리 지침 (PRD & Implementation Management)

에이전트는 작업 시작 시와 구현 진행/완료 시 **Notion 기획 문서 및 구현 현황**을 지속적으로 업데이트하고 최신 상태를 유지합니다.

1. **노션 페이지 독해 및 최종 기획안 지속 검토**:
   - 로직/시스템/레벨 작업을 시작하기 전 및 정기적으로 팀 Notion 페이지들을 읽고 최신 최종 기획안 사양 및 변경점을 지속해서 검토합니다.
   - 🟡(미확정) 항목과 연관된 구조 변경은 독단적으로 결정하지 말고 사용자에게 확인을 받습니다. (장르 구조는 메트로배니아로 최종 확정되었습니다.)
2. **[게임 구현 관리] 페이지 내 현황 지속 정리**:
   - 프로젝트 내 C# 코드 구현 현황, 신규 기능 추가, 미구현 항목 상태, 주요 클래스/API 변경사항 등을 Notion의 **[게임 구현 관리]** 페이지에 지속적으로 정리합니다.
   - 미구현 항목(기억 조각, 오염도 게이지, 스킬 시스템 등) 구현 시 기획서 및 구현 현황 관리 페이지의 상태를 '구현 완료'로 업데이트하고 관련 수치/파라미터를 동기화합니다.
3. **불일치 검사 (Spec-Code Sync Test)**:
   - 기획서 문서의 수치/기능 요구사항과 C# 코드(`Health`, `SaveData`, `ObjectiveManager` 등)가 일치하는지 정기적으로 검사하고 불일치 발생 시 즉시 보정 및 보고합니다.

---

## 4. 코드 검사 및 아키텍처 가이드라인 (Code Audit Guidelines)

모든 코드 수정 후 아래 아키텍처 규칙 및 컨벤션 준수 여부를 검사해야 합니다.

### A. 전투 파이프라인 (Health 중심 이벤트 기반)
- **`Health` 컴포넌트 허브** (파일명: `HealthSystem.cs` / 클래스명: `Health`)
- 모든 피격/넉백/스파크의 방향 계산 출발점은 **`sourcePosition` (공격 원점)** 이어야 함.
- `Die()` 시 `Destroy(gameObject)`가 호출되므로, 사망 연출은 바깥 싱글턴에서 처리할 것.
- 피격 무적(`invincibleTime`)은 플레이어에게만 부여.
- 체력 복원/수정 시 `SetHealth()`/`RestoreFull()` 사용 (`SetHealth(0)`으로 사망시키지 말 것).

### B. 싱글턴 안전성
- `HitFeedback`, `HitVfx`, `DamagePopup`, `ObjectiveManager`, `SaveManager` 5종.
- **`!= null` 체크 필수** (`?.` 연산자는 Unity의 `== null` 오버로드를 타지 않으므로 사용 금지).

### C. 스토리, 상호작용, 세이브, 입력
- **상호작용**: `IInteractable` 구현체 사용, `Is Trigger` 콜라이더 검사, `Time.unscaledDeltaTime` 프롬프트 연출.
- **세이브**: `SaveManager` 중심, `SaveData`의 모든 필드는 `public` (JsonUtility 구버전 세이브 호환성 유지).
- **입력**: `Assets/_Project/Assets/Input/Client.inputactions` 만 사용.

### D. C# 코드 컨벤션 (K&R 스타일)
- `.editorconfig` 기준 **K&R 중괄호** (여는 중괄호가 같은 줄).
- `#region` 구획 한글 분할 (`인스펙터 변수`, `컴포넌트 변수`, `유니티 라이프 사이클` 등).
- 인스펙터 필드: `public` + `[Header]` + **줄 끝 한글 주석** (용도 설명). `private` 키워드는 생략.
- 주석은 단순 동작이 아니라 **"왜 이렇게 구현했는지"** 이유 명시.
- 비동기 로직은 코루틴만 사용 (async/await, UniTask, DOTween 사용 금지).

### E. 파일/폴더 정돈 & 쓰레기통 규칙
- **폴더 구조**: `Scripts/_Player/`, `Monster/`, `Character/`, `Combat/`, `Interaction/`, `Objective/`, `Story/`, `Ui/`, `System/`, `_Data/`. 큰 틀 폴더까지만 생성하고 하위 폴더 과다 생성 금지.
- **파일명**: `{큰 틀}{기능}` (예: `Player_Combat`, `ObjectiveManager`). File Name == Class Name 일치.
- **쓰레기통 이동**: 파일 삭제 요청 시 절대 바로 지우지 말고, **프로젝트 루트의 `쓰레기통/`** 으로 `.meta` 파일과 함께 `git mv` 이동.

---

## 5. 작업 검증 및 완료 처리 수칙 (Verification Protocol)

1. **컴파일/오류 검증**: 코드 수정 후 반드시 에디터 컴파일 상태 및 콘솔 로그를 확인하고, 에러가 없는지 검증합니다.
2. **검사 보고**: 코드 작성 시 준수한 규칙 항목과 새로 갱신된 기획서 반영 사항을 요약하여 보고합니다.
3. **독단적 판단 자제**: 기획의 미확정(🟡) 사항이나 아키텍처를 크게 흔드는 변경점은 항상 질문을 통해 확인 후 진행합니다.


