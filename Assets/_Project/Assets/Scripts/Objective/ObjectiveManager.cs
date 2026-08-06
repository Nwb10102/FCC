using System;
using System.Collections.Generic;
using UnityEngine;

// 미션과 목표를 들고 있는 싱글턴.
//
// 계층은 Mission → Objective 두 단계다. 미션은 씬에 놓은 MissionStartZone을 밟는 순간(또는
// Mission.startAutomatically가 켜져 있으면 곧바로) 시작되고, 시작된 미션 안에서 선행 조건이 채워진
// 목표가 하나씩 열린다. 진입점은 예전과 같이 CompleteObjective / AddProgress 둘이라
// 대사·컷신·거울 쪽 호출부는 그대로 쓸 수 있다.
//
// 기획 데이터는 전부 에셋(Mission·ObjectiveDefinition)이고 진행도만 이 클래스가 들고 있다.
public class ObjectiveManager : MonoBehaviour {
    public static ObjectiveManager Instance;

    #region 인스펙터 변수

    [Header("미션")]
    // 이 게임에 존재하는 미션 에셋 전부. **여기 없는 미션은 시작할 수 없습니다.**
    // 순서는 표시에 쓰이지 않는다 (체크리스트는 시작 순서를 따른다).
    public List<Mission> missions = new();

    [Header("디버그")]
    public bool logMissionFlow = true; // 미션 시작·완료를 콘솔에 찍는다. 동선 QA용이라 빌드에선 꺼도 된다.

    #endregion
    #region 이벤트

    public event Action<MissionRuntime> OnMissionStarted;
    public event Action<MissionRuntime> OnMissionCompleted;

    // 목표가 잠금에서 풀려 진행 가능해졌을 때. 체크리스트가 이때 줄을 새로 만든다.
    public event Action<ObjectiveRuntime> OnObjectiveActivated;

    // 진행도나 완료 상태가 바뀌었을 때. 체크리스트가 해당 줄만 다시 그린다.
    public event Action<ObjectiveRuntime> OnObjectiveUpdated;

    public event Action<ObjectiveRuntime> OnObjectiveCompleted;

    // 세이브를 불러 상태를 통째로 갈아치운 뒤. 구독자는 화면을 처음부터 다시 그려야 한다.
    // 복원 중에는 개별 이벤트를 쏘지 않으므로(컷신·보상 재생 방지) 이 신호 하나로 갈음한다.
    public event Action OnStateRestored;

    #endregion
    #region 런타임 변수

    readonly List<MissionRuntime> missionRuntimes = new();
    readonly Dictionary<string, MissionRuntime> missionById = new();
    readonly Dictionary<string, ObjectiveRuntime> objectiveById = new();

    MissionRuntime currentMission;
    int startCounter; // MissionRuntime.startSequence에 찍어줄 번호.

    public IReadOnlyList<MissionRuntime> Missions => missionRuntimes;

    // 체크리스트 헤더가 보는 미션. 가장 최근에 시작됐고 아직 완료되지 않은 것. 없으면 null.
    public MissionRuntime CurrentMission => currentMission;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트에서만 동작 (씬에서는 GAME_MANAGER 하위에 정리용으로 배치되어 있음)
        DontDestroyOnLoad(gameObject);

        Build();
        StartAutomaticMissions();
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 구성

    // 인스펙터에 물린 미션 에셋을 펼쳐 런타임 상태를 만든다.
    // id 중복과 잘못된 선행 조건은 여기서 한 번에 잡는다 — 런타임에 조용히 어긋나면 원인을 찾기 어렵다.
    void Build() {
        missionRuntimes.Clear();
        missionById.Clear();
        objectiveById.Clear();

        foreach (Mission mission in missions) {
            if (mission == null) continue; // 인스펙터에서 비워둔 칸.

            if (missionById.ContainsKey(mission.MissionId)) {
                Debug.LogError($"[ObjectiveManager] 미션 id '{mission.MissionId}'가 중복입니다. 뒤의 '{mission.name}'을 건너뜁니다.", mission);
                continue;
            }

            MissionRuntime missionRuntime = new MissionRuntime(mission);

            foreach (ObjectiveDefinition definition in mission.objectives) {
                if (definition == null) continue;

                if (objectiveById.ContainsKey(definition.ObjectiveId)) {
                    Debug.LogError($"[ObjectiveManager] 목표 id '{definition.ObjectiveId}'가 중복입니다 " +
                        $"(미션 '{mission.MissionId}'). 뒤의 것을 건너뜁니다.", definition);
                    continue;
                }

                ObjectiveRuntime objectiveRuntime = new ObjectiveRuntime(definition);
                missionRuntime.objectives.Add(objectiveRuntime);
                objectiveById.Add(definition.ObjectiveId, objectiveRuntime);
            }

            missionRuntimes.Add(missionRuntime);
            missionById.Add(mission.MissionId, missionRuntime);
        }

        ValidatePrerequisites();
    }

    // 등록되지 않은 목표를 선행 조건으로 걸어두면 그 목표는 영원히 열리지 않는다.
    // 런타임에는 열어주는 쪽으로 처리(ArePrerequisitesMet)하므로 게임이 막히지는 않지만, 기획 실수이므로 알린다.
    void ValidatePrerequisites() {
        foreach (ObjectiveRuntime objective in objectiveById.Values) {
            foreach (ObjectiveDefinition prerequisite in objective.Definition.prerequisites) {
                if (prerequisite == null) continue;
                if (objectiveById.ContainsKey(prerequisite.ObjectiveId)) continue;

                Debug.LogWarning($"[ObjectiveManager] 목표 '{objective.Id}'의 선행 조건 '{prerequisite.ObjectiveId}'가 " +
                    "어느 미션에도 들어 있지 않습니다. 선행 조건으로 세지 않고 넘어갑니다.", objective.Definition);
            }
        }
    }

    #endregion
    #region 미션 시작

    // startAutomatically가 켜진 미션을 연다. 챕터 첫 미션처럼 밟을 구역이 없는 경우용.
    void StartAutomaticMissions() {
        foreach (MissionRuntime mission in missionRuntimes) {
            if (mission.Definition.startAutomatically && !mission.isStarted) StartMission(mission);
        }
    }

    public bool StartMission(Mission mission) {
        if (mission == null) return false;
        return StartMission(mission.MissionId);
    }

    // 미션을 시작한다. 이미 시작됐으면 아무 일도 하지 않는다 (구역을 다시 밟아도 진행도가 초기화되지 않게).
    public bool StartMission(string missionId) {
        if (!missionById.TryGetValue(missionId, out MissionRuntime mission)) {
            Debug.LogWarning($"[ObjectiveManager] 미션 '{missionId}'을 찾지 못했습니다. " +
                "ObjectiveManager의 missions 목록에 에셋을 넣었는지 확인하세요.", this);
            return false;
        }

        return StartMission(mission);
    }

    bool StartMission(MissionRuntime mission) {
        if (mission.isStarted) return false;

        mission.isStarted = true;
        mission.startSequence = ++startCounter;

        RefreshActivation(true);
        RefreshCurrentMission();

        if (logMissionFlow) Debug.Log($"[ObjectiveManager] 미션 시작 — '{mission.DisplayName}' ({mission.Id})");
        OnMissionStarted?.Invoke(mission);

        return true;
    }

    #endregion
    #region 목표 진행

    // Boolean 목표(지역 도달, 대화·컷신 완료)를 즉시 완료 처리한다.
    public bool CompleteObjective(string id) {
        if (!TryGetObjective(id, out ObjectiveRuntime objective)) return false;
        if (objective.IsCompleted) return false;

        // 컷신이나 거울이 동선을 앞지르는 경우가 실제로 있어 잠긴 목표도 완료시켜 준다.
        // 다만 미션이 시작조차 안 됐다면 기획 실수일 가능성이 높으므로 알린다.
        MissionRuntime mission = FindMissionOf(objective);
        if (mission != null && !mission.isStarted) {
            Debug.LogWarning($"[ObjectiveManager] 목표 '{id}'를 완료했지만 미션 '{mission.Id}'은 아직 시작되지 않았습니다. " +
                "MissionStartZone을 지나치는 동선인지 확인하세요.", this);
        }

        // 수량을 세는 목표를 즉시 완료시키면 (3/10) 같은 중간 과정이 통째로 건너뛰어진다.
        if (objective.Definition.type == MissionType.CollectItem && objective.TargetCount > 1) {
            Debug.LogWarning($"[ObjectiveManager] 수집형 목표 '{id}'({objective.TargetCount}개)를 CompleteObjective로 " +
                "즉시 완료했습니다. 수집 카운터를 올리려면 AddProgress를 쓰세요.", this);
        }

        objective.currentCount = objective.TargetCount;
        MarkCompleted(objective);
        return true;
    }

    // 수집형 목표에 진행도를 더한다. 실제로 올랐으면 true (수집 오브젝트가 이걸 보고 사라질지 정한다).
    public bool AddProgress(string id, int amount = 1) {
        if (!TryGetObjective(id, out ObjectiveRuntime objective)) return false;
        if (objective.IsCompleted) return false;

        // 잠긴 목표의 카운터가 미리 오르면 미션을 시작했을 때 이미 절반이 차 있는 상태가 된다.
        if (!objective.IsActive) {
            Debug.LogWarning($"[ObjectiveManager] 목표 '{id}'는 아직 진행할 수 없어 진행도를 올리지 않았습니다 " +
                "(미션이 시작되지 않았거나 선행 목표가 남아 있습니다).", this);
            return false;
        }

        objective.currentCount = Mathf.Min(objective.currentCount + amount, objective.TargetCount);
        OnObjectiveUpdated?.Invoke(objective);

        if (objective.currentCount >= objective.TargetCount) MarkCompleted(objective);
        return true;
    }

    void MarkCompleted(ObjectiveRuntime objective) {
        objective.state = ObjectiveState.Completed;

        OnObjectiveUpdated?.Invoke(objective);
        OnObjectiveCompleted?.Invoke(objective);

        // 이 목표를 선행 조건으로 걸어둔 목표들이 열린다.
        RefreshActivation(true);

        MissionRuntime mission = FindMissionOf(objective);
        if (mission == null || !mission.IsCompleted) return;

        // 헤더를 다음 미션으로 넘기는 것이 이벤트보다 먼저다. 순서를 바꾸면 구독자가 화면을 다시 그릴 때
        // CurrentMission이 아직 방금 완료된 미션이라 완료된 미션이 화면에 남는다.
        RefreshCurrentMission();

        if (logMissionFlow) Debug.Log($"[ObjectiveManager] 미션 완료 — '{mission.DisplayName}' ({mission.Id})");
        OnMissionCompleted?.Invoke(mission);
    }

    #endregion
    #region 활성 계산

    // 시작된 미션의 잠긴 목표 중 선행 조건이 채워진 것을 연다.
    // 한 목표가 열리는 것이 다른 목표의 조건을 채우는 일은 없지만(완료가 아니라 활성이므로),
    // 복원 직후처럼 여러 목표가 이미 완료된 상태에서는 연쇄로 열려야 해서 변화가 없을 때까지 돈다.
    void RefreshActivation(bool fireEvents) {
        bool changed = true;

        while (changed) {
            changed = false;

            foreach (MissionRuntime mission in missionRuntimes) {
                if (!mission.isStarted) continue;

                foreach (ObjectiveRuntime objective in mission.objectives) {
                    if (objective.state != ObjectiveState.Locked) continue;
                    if (!ArePrerequisitesMet(objective)) continue;

                    objective.state = ObjectiveState.Active;
                    changed = true;

                    if (fireEvents) OnObjectiveActivated?.Invoke(objective);
                }
            }
        }
    }

    bool ArePrerequisitesMet(ObjectiveRuntime objective) {
        foreach (ObjectiveDefinition prerequisite in objective.Definition.prerequisites) {
            if (prerequisite == null) continue;

            // 등록되지 않은 선행 조건은 세지 않는다. 그러지 않으면 목표가 영원히 잠겨 진행이 막힌다
            // (Build의 ValidatePrerequisites가 이미 경고를 찍어뒀다).
            if (!objectiveById.TryGetValue(prerequisite.ObjectiveId, out ObjectiveRuntime required)) continue;

            if (!required.IsCompleted) return false;
        }

        return true;
    }

    // 가장 최근에 시작됐고 아직 완료되지 않은 미션을 고른다.
    void RefreshCurrentMission() {
        MissionRuntime best = null;

        foreach (MissionRuntime mission in missionRuntimes) {
            if (!mission.isStarted || mission.IsCompleted) continue;
            if (best == null || mission.startSequence > best.startSequence) best = mission;
        }

        currentMission = best;
    }

    #endregion
    #region 조회

    public ObjectiveRuntime FindObjective(string id) {
        return objectiveById.TryGetValue(id, out ObjectiveRuntime objective) ? objective : null;
    }

    public MissionRuntime FindMission(string missionId) {
        return missionById.TryGetValue(missionId, out MissionRuntime mission) ? mission : null;
    }

    public bool IsObjectiveActive(string id) {
        ObjectiveRuntime objective = FindObjective(id);
        return objective != null && objective.IsActive;
    }

    // id 오타가 조용히 무시되면 트리거를 몇 개씩 놓고도 왜 안 되는지 알 수 없다. 못 찾으면 반드시 알린다.
    bool TryGetObjective(string id, out ObjectiveRuntime objective) {
        if (string.IsNullOrEmpty(id)) {
            objective = null;
            return false; // 트리거의 objectiveId를 일부러 비워둔 경우 — 정상이므로 조용히 넘어간다.
        }

        if (objectiveById.TryGetValue(id, out objective)) return true;

        Debug.LogWarning($"[ObjectiveManager] 목표 '{id}'를 찾지 못했습니다. 목표 에셋의 id와 " +
            "미션(missions 목록)에 들어 있는지 확인하세요.", this);
        return false;
    }

    MissionRuntime FindMissionOf(ObjectiveRuntime objective) {
        foreach (MissionRuntime mission in missionRuntimes) {
            if (mission.objectives.Contains(objective)) return mission;
        }
        return null;
    }

    #endregion
    #region 세이브

    // 진행도 스냅샷. Objective 에셋을 직렬화하면 설명문·목표 수량 같은 기획 데이터까지 세이브에 굳어버려서,
    // 진행 상태만 떼어 담는다.
    public List<ObjectiveSaveEntry> CaptureObjectives() {
        List<ObjectiveSaveEntry> snapshot = new List<ObjectiveSaveEntry>(objectiveById.Count);

        foreach (ObjectiveRuntime objective in objectiveById.Values) {
            snapshot.Add(new ObjectiveSaveEntry {
                id = objective.Id,
                currentCount = objective.currentCount,
                isCompleted = objective.IsCompleted,
            });
        }

        return snapshot;
    }

    // 시작된 미션 id. Active 상태는 선행 조건으로 파생되지만 "시작됨"은 파생되지 않아 따로 기록해야 한다.
    public List<string> CaptureStartedMissions() {
        List<string> started = new();

        foreach (MissionRuntime mission in missionRuntimes) {
            if (mission.isStarted) started.Add(mission.Id);
        }

        return started;
    }

    // 세이브에서 읽은 상태로 되돌린다.
    //
    // 개별 이벤트(완료·활성)는 쏘지 않고 마지막에 OnStateRestored 하나만 발행한다 — 불러올 때마다
    // 이미 본 컷신이나 보상이 재생되면 안 되기 때문. 구독자는 이 신호를 받고 화면을 처음부터 다시 그린다.
    public void RestoreState(List<ObjectiveSaveEntry> snapshot, List<string> startedMissions) {
        // 스냅샷에 없는 목표의 진행도가 남으면, 구버전 세이브를 불렀는데 최근 진행도가 섞이는 일이 생긴다.
        // DontDestroyOnLoad로 살아남은 인스턴스라면 특히 그렇다.
        ResetAllProgress();

        if (snapshot != null) {
            foreach (ObjectiveSaveEntry entry in snapshot) {
                if (!objectiveById.TryGetValue(entry.id, out ObjectiveRuntime objective)) continue; // 세이브 이후 사라진 목표.

                objective.currentCount = entry.currentCount;
                objective.state = entry.isCompleted ? ObjectiveState.Completed : ObjectiveState.Locked;
            }
        }

        if (startedMissions != null) {
            foreach (string missionId in startedMissions) {
                if (missionById.TryGetValue(missionId, out MissionRuntime mission)) MarkStartedOnRestore(mission);
            }
        }

        // startedMissions 필드가 없던 구버전 세이브 보정 — 진행 흔적이 있는 미션은 시작된 것으로 본다.
        foreach (MissionRuntime mission in missionRuntimes) {
            if (mission.isStarted) continue;

            foreach (ObjectiveRuntime objective in mission.objectives) {
                if (!objective.IsCompleted && objective.currentCount <= 0) continue;

                MarkStartedOnRestore(mission);
                break;
            }
        }

        // 세이브 이후에 startAutomatically로 새로 추가된 미션도 열어준다.
        foreach (MissionRuntime mission in missionRuntimes) {
            if (mission.Definition.startAutomatically && !mission.isStarted) MarkStartedOnRestore(mission);
        }

        RefreshActivation(false);
        RefreshCurrentMission();

        OnStateRestored?.Invoke();
    }

    void MarkStartedOnRestore(MissionRuntime mission) {
        if (mission.isStarted) return;

        mission.isStarted = true;
        mission.startSequence = ++startCounter;
    }

    // 새 게임을 시작할 때. 싱글턴이 DontDestroyOnLoad로 살아남아 직전 판의 진행도를 물고 있을 수 있다.
    public void ResetAllProgress() {
        foreach (MissionRuntime mission in missionRuntimes) {
            mission.isStarted = false;
            mission.startSequence = 0;

            foreach (ObjectiveRuntime objective in mission.objectives) {
                objective.Reset();
            }
        }

        currentMission = null;
        startCounter = 0;
    }

    #endregion
}
