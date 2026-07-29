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
  - 🟡 표시: 미확정 항목 (선형 vs 메트로배니아 장르 구조 등)
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
   - 🟡(미확정) 항목과 연관된 구조 변경(예: 맵 구조, 레벨 디자인)은 독단적으로 결정하지 말고 사용자에게 확인을 받습니다.
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

---

## 6. Antigravity & Claude 4단계 협업 프로세스 (Orca CLI 연동)

Antigravity(기획 분석·지시·검토 관리자)와 Claude Code(C# 코드 구현 전담 에이전트)는 아래 **4단계 워크플로우**에 따라 협업을 진행합니다:

### [Step 1] 기획 분석 및 구현 계획 작성 (Antigravity)
* Notion 통합 기획서 및 구현 현황 페이지, 기존 C# 코드를 분석합니다.
* 클래스 구조, 이벤트 연동, K&R 컨벤션을 포함한 세부 구현 계획을 정리합니다.

### [Step 2] 사용자 승인 (User Approval)
* 작성된 구현 계획을 사용자에게 제시하고 승인/수정 의견을 수령합니다.
* 승인이 완료되기 전까지는 실제 코드 변경 지시를 내리지 않습니다.

### [Step 3] Claude 구현 지시 및 작업 수행 (Claude via Orca CLI)
* 승인 완료 시, Antigravity가 **Orca CLI**를 통해 Claude 에이전트 터미널로 승인된 요구사항 및 C# 구현 명세를 전송합니다.
  - Orca 명령어 활용 예시: `orca terminal send --terminal <handle> --text "<구현지침>" --enter`
  - 필요 시 워크트리 분리: `orca worktree create --name <task-name> --agent claude`
* Claude가 `Assets/_Project/` 내 C# 스크립트 작성 및 기능 구현을 진행합니다.

### [Step 4] 코드 검토 및 보충 정리 (Antigravity)
* Claude가 구현을 마치면 Antigravity가 작성된 C# 코드를 종합 검토합니다 (컴파일 에러, K&R 컨벤션, 이벤트 파이프라인, 싱글턴 준수 여부).
* 어느 정도 구현되었는지(구현 달성률 및 미진한 파트) 분석하여 사용자에게 보고하고, 부족한 부분은 보충 지시 또는 직접 보정합니다.
* 최종 구현 결과를 Notion **[게임 구현 관리]** 페이지에 지속적으로 업데이트합니다.

