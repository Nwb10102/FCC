using System.Collections.Generic;

// 미션 하나의 런타임 상태. ObjectiveManager가 소유한다.
//
// isStarted만은 목표 진행도에서 파생되지 않아 세이브에 따로 기록해야 한다 (SaveData.startedMissions).
// 시작 구역을 밟고 저장했다면 그 사실 자체를 기억해야 하기 때문이다.
public class MissionRuntime {
    #region 상태

    public readonly Mission Definition;
    public readonly List<ObjectiveRuntime> objectives = new();

    public bool isStarted;

    // 몇 번째로 시작됐는지. 체크리스트 헤더가 "가장 최근에 시작된 미완료 미션"을 골라야 하는데,
    // 미션이 인스펙터 목록 순서와 다르게 시작될 수 있어 목록 순서로는 판별할 수 없다.
    public int startSequence;

    #endregion
    #region 생성

    public MissionRuntime(Mission definition) {
        Definition = definition;
    }

    #endregion
    #region 조회

    public string Id => Definition.MissionId;

    public string DisplayName => Definition.DisplayName;

    public string Description => Definition.Description;

    // 필수 목표가 전부 완료됐는지. isOptional 목표는 판정에서 빠진다.
    // 필수 목표가 하나도 없는 미션(전부 선택 목표)은 완료로 보지 않는다 — 시작하자마자
    // 완료 처리되어 헤더가 사라지는 것을 막기 위함.
    public bool IsCompleted {
        get {
            bool hasRequired = false;

            foreach (ObjectiveRuntime objective in objectives) {
                if (objective.Definition.isOptional) continue;

                hasRequired = true;
                if (!objective.IsCompleted) return false;
            }

            return hasRequired;
        }
    }

    // 체크리스트에 줄로 보여줄 목표들. 잠긴 것은 감춘다.
    public bool HasVisibleObjective {
        get {
            foreach (ObjectiveRuntime objective in objectives) {
                if (objective.state != ObjectiveState.Locked) return true;
            }
            return false;
        }
    }

    #endregion
}
