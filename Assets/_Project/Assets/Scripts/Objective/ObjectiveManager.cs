using System;
using System.Collections.Generic;
using UnityEngine;

// 목표 목록을 들고 있는 싱글턴. CompleteObjective/AddProgress 두 진입점만으로
// 지역 도달, 대화/컷신 완료, (나중에 붙일) 수집형 목표까지 같은 방식으로 처리한다.
public class ObjectiveManager : MonoBehaviour {
    public static ObjectiveManager Instance;

    [SerializeField] List<Objective> objectives = new List<Objective>();

    public event Action<Objective> OnObjectiveUpdated;
    public event Action<Objective> OnObjectiveCompleted;
    public event Action OnAllObjectivesCompleted;

    public IReadOnlyList<Objective> Objectives => objectives;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트에서만 동작 (GAME_MANAGER 하위에 정리용으로 배치되어 있음)
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    public Objective FindObjective(string id) {
        return objectives.Find(o => o.id == id);
    }

    // Boolean 목표(지역 도달, 대화/컷신 완료)를 즉시 완료 처리한다.
    public void CompleteObjective(string id) {
        Objective objective = FindObjective(id);
        if (objective == null || objective.isCompleted) return;

        objective.currentCount = objective.targetCount;
        MarkCompleted(objective);
    }

    // Counter 목표(수집형, 예: 거울 조각)에 진행도를 더한다.
    public void AddProgress(string id, int amount = 1) {
        Objective objective = FindObjective(id);
        if (objective == null || objective.isCompleted) return;

        objective.currentCount = Mathf.Min(objective.currentCount + amount, objective.targetCount);
        OnObjectiveUpdated?.Invoke(objective);

        if (objective.currentCount >= objective.targetCount) {
            MarkCompleted(objective);
        }
    }

    void MarkCompleted(Objective objective) {
        objective.isCompleted = true;
        OnObjectiveUpdated?.Invoke(objective);
        OnObjectiveCompleted?.Invoke(objective);

        if (objectives.TrueForAll(o => o.isCompleted)) {
            OnAllObjectivesCompleted?.Invoke();
        }
    }
}
