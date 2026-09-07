using System;
using System.Collections.Generic;
using UnityEngine;

// 뒷세계 같은 몬스터 전투 서브 구역의 "클리어 여부" 만 들고 있는 싱글턴.
//
// ObjectiveManager 를 재사용하지 않는 이유: ObjectiveManager 의 CurrentMission 은 한 번에 하나만
// 노출되어 체크리스트 UI 에 그대로 보인다. 던전 클리어는 UI 에 노출될 필요가 없는 순수 플래그라
// Mission/Objective 로 만들면 진행 중인 챕터 미션 표시와 충돌한다.
public class DungeonManager : MonoBehaviour {
    public static DungeonManager Instance;

    #region 이벤트

    public event Action<string> OnDungeonCleared;

    #endregion
    #region 런타임 변수

    readonly HashSet<string> clearedDungeonIds = new();

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad 는 루트에서만 동작 (GAME_MANAGER 하위에 정리용으로 배치됨).
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 조회 · 기록

    public bool IsCleared(string dungeonId) {
        return !string.IsNullOrEmpty(dungeonId) && clearedDungeonIds.Contains(dungeonId);
    }

    // 이미 기록돼 있으면 무시한다 — 같은 구역을 여러 번 클리어 처리해도 보상이 중복 지급되지 않도록
    // 호출부(DungeonGate)가 아니라 여기서 막는다.
    public void MarkCleared(string dungeonId) {
        if (string.IsNullOrEmpty(dungeonId)) return;
        if (!clearedDungeonIds.Add(dungeonId)) return;

        OnDungeonCleared?.Invoke(dungeonId);
    }

    #endregion
    #region 세이브

    public List<string> CaptureClearedDungeons() {
        return new List<string>(clearedDungeonIds);
    }

    // 세이브에서 읽은 상태로 되돌린다. ObjectiveManager.RestoreState 와 마찬가지로 이벤트는 다시 쏘지 않는다
    // (불러올 때마다 보상이 재지급된 것처럼 보이면 안 되기 때문).
    public void RestoreClearedDungeons(List<string> ids) {
        clearedDungeonIds.Clear();
        if (ids == null) return;

        foreach (string id in ids) clearedDungeonIds.Add(id);
    }

    #endregion
}
