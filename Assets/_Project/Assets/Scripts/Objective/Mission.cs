using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// 미션 하나 = 이름 + 짧은 설명 + 그 아래 목표들.
// 체크리스트는 이 이름과 설명을 헤더로 띄우고 목표를 순서대로 그린다.
//
// 미션은 보통 씬에 놓은 MissionStartZone을 플레이어가 밟는 순간 시작된다. 시작되기 전까지는
// 소속 목표가 전부 잠겨 있어 체크리스트에도 나오지 않는다 — 챕터 4 목표가 게임 시작부터
// 화면에 보이던 문제를 이 단위로 막는다.
//
// **에셋 생성: Create ▸ FCC ▸ Objective ▸ Mission**
[CreateAssetMenu(fileName = "Mission_", menuName = "FCC/Objective/Mission")]
public class Mission : ScriptableObject {
    #region 인스펙터 변수

    [Header("식별")]
    // 세이브에 "이 미션은 시작됐다"로 기록되는 고유 id. **에셋마다 겹치지 않게 지으세요.**
    // 비워두면 에셋 이름을 대신 쓴다.
    public string missionId;

    [Header("표시")]
    // 둘 다 String Table 'Objective'의 키를 가리킨다. 키를 고르면 인스펙터에서 언어별 원문을 바로 편집할 수 있다.
    public LocalizedString missionName = new(); // 체크리스트 헤더에 크게 뜨는 이름.
    public LocalizedString description = new(); // 헤더 아래 한두 줄 설명. 비워두면 설명 줄이 꺼진다.

    [Header("목표")]
    // 이 미션에 속한 목표들. **순서가 그대로 체크리스트 표시 순서입니다.**
    // 진행 순서는 각 목표의 prerequisites가 정하므로, 여기 순서를 바꿔도 잠금 흐름은 변하지 않는다.
    public List<ObjectiveDefinition> objectives = new();

    [Header("시작 방식")]
    // 켜면 시작 구역 없이 게임(또는 씬)이 시작될 때 바로 열린다. 챕터의 첫 미션처럼
    // 플레이어가 어디를 밟기 전부터 목표가 떠 있어야 하는 경우에 쓴다.
    // **꺼두면 MissionStartZone을 씬에 놓아야 미션이 시작됩니다.**
    public bool startAutomatically;

    #endregion
    #region 조회

    public string MissionId => string.IsNullOrEmpty(missionId) ? name : missionId;

    // 현재 언어로 해석한 이름. 키를 안 골랐으면 에셋 이름으로 대신한다 (헤더가 빈 칸이 되는 것 방지).
    public string DisplayName => LocalizationText.Resolve(missionName, name);

    // 현재 언어로 해석한 설명. 비어 있으면 체크리스트가 설명 줄을 통째로 끈다.
    public string Description => LocalizationText.Resolve(description);

    #endregion
}
