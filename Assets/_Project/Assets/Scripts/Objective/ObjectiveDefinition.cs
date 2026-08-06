using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// 인스펙터 드롭다운에 표시할 목표 유형. 완료 판정 자체는 어느 트리거가
// ObjectiveManager.CompleteObjective(즉시 완료)/AddProgress(누적)를 부르느냐로 결정되고,
// 이 enum은 매니저의 잘못된 호출 경고와 체크리스트의 수량 표기 여부에만 쓰인다.
public enum MissionType {
    [InspectorName("특정 지역 도착하기")] ReachZone,
    [InspectorName("특정 물품 수집하기")] CollectItem,
}

// 목표 하나의 기획 데이터.
//
// 씬이 아니라 에셋(ScriptableObject)으로 둔 이유는 SkillBase와 같다 — 체크리스트가 목록을 그대로 뿌려야 하고
// 세이브에는 id로만 기록해야 한다. 씬 컴포넌트에 적어두면 챕터가 늘어날수록 씬 파일이 곧 기획 데이터가 되어
// 머지 충돌 지점이 되고, 씬을 다시 열 때마다 DontDestroyOnLoad로 살아남은 매니저와 어긋난다.
//
// **진행도(currentCount·완료 여부)는 여기에 두지 않는다.** ScriptableObject는 에셋 하나를 공유하므로
// 진행도를 적으면 에디터에서 플레이를 멈춘 뒤에도 완료 상태가 에셋에 남는다 (SkillBase.lastUsedTime이
// 딱 그 문제를 겪어 두 곳에서 씻어내고 있다). 런타임 상태는 ObjectiveRuntime이 따로 들고 있다.
//
// **에셋 생성: Create ▸ FCC ▸ Objective ▸ Objective**
[CreateAssetMenu(fileName = "Obj_", menuName = "FCC/Objective/Objective")]
public class ObjectiveDefinition : ScriptableObject {
    #region 인스펙터 변수

    [Header("식별")]
    // 세이브·트리거가 이 목표를 가리키는 고유 id. **에셋마다 겹치지 않게 지으세요.**
    // 비워두면 에셋 이름을 대신 쓴다 (SkillBase.skillID·SaveMirror.mirrorId와 같은 방식).
    public string objectiveId;

    [Header("표시")]
    // 체크리스트 한 줄에 찍히는 문구. 원문을 여기 적지 않고 String Table 'Objective'의 키를 가리킨다.
    // 키를 고르면 인스펙터에서 언어별 원문을 바로 편집할 수 있다.
    public LocalizedString description = new();

    [Header("달성 조건")]
    public MissionType type = MissionType.ReachZone;
    public int targetCount = 1; // CollectItem일 때 몇 개를 모아야 하는지. ReachZone은 1로 둔다.

    // 이 목표들이 전부 완료되어야 잠금이 풀린다. **비워두면 미션이 시작되는 순간 바로 진행 가능해진다.**
    // 미션 안에서 "도착 → 대화 → 수집" 같은 동선을 코드가 아니라 데이터로 짜기 위한 것이다.
    public List<ObjectiveDefinition> prerequisites = new();

    // 서브 목표. 켜두면 이 목표를 안 해도 미션이 완료된 것으로 판정된다.
    public bool isOptional;

    #endregion
    #region 조회

    // id를 깜빡해도 최소한 에셋 이름으로는 구분되게 한다.
    public string ObjectiveId => string.IsNullOrEmpty(objectiveId) ? name : objectiveId;

    // 현재 언어로 해석한 설명문. 키를 안 골랐거나 번역이 비어 있으면 에셋 이름이라도 띄운다 —
    // 체크리스트에 빈 줄이 뜨면 어느 목표가 잘못됐는지 화면만 봐서는 알 수 없기 때문이다.
    public string Description => LocalizationText.Resolve(description, name);

    #endregion
}
