using System;
using System.Collections.Generic;
using UnityEngine;

// 뒷세계 같은 몬스터 전투 서브 구역의 "클리어 여부"만 들고 있는 싱글턴.
//
// ObjectiveManager를 재사용하지 않는 이유: ObjectiveManager의 CurrentMission은 한 번에 하나만
// 노출되어 체크리스트 UI에 그대로 보인다. 뒷세계 클리어는 UI에 노출될 필요가 없는 순수 플래그라
// Mission/Objective로 만들면 진행 중인 챕터 미션 표시와 충돌한다.
public class ArenaManager : MonoBehaviour {
    public static ArenaManager Instance;

    #region 이벤트

    public event Action<string> OnArenaCleared;

    #endregion
    #region 런타임 변수

    readonly HashSet<string> clearedArenaIds = new();

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트에서만 동작 (GAME_MANAGER 하위에 정리용으로 배치되어 있음)
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 조회 · 기록

    public bool IsCleared(string arenaId) {
        return !string.IsNullOrEmpty(arenaId) && clearedArenaIds.Contains(arenaId);
    }

    // 이미 기록돼 있으면 무시한다 — 같은 구역을 여러 번 클리어 처리해도 보상이 중복 지급되지 않도록
    // 호출부(ArenaGate)가 아니라 여기서 막는다.
    public void MarkCleared(string arenaId) {
        if (string.IsNullOrEmpty(arenaId)) return;
        if (!clearedArenaIds.Add(arenaId)) return;

        OnArenaCleared?.Invoke(arenaId);
    }

    #endregion
    #region 세이브

    public List<string> CaptureClearedArenas() {
        return new List<string>(clearedArenaIds);
    }

    // 세이브에서 읽은 상태로 되돌린다. ObjectiveManager.RestoreState와 마찬가지로 이벤트는 다시 쏘지 않는다
    // (불러올 때마다 보상이 재지급된 것처럼 보이면 안 되기 때문).
    public void RestoreClearedArenas(List<string> ids) {
        clearedArenaIds.Clear();
        if (ids == null) return;

        foreach (string id in ids) clearedArenaIds.Add(id);
    }

    #endregion
}
