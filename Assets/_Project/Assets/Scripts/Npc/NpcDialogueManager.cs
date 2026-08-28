using System.Collections.Generic;
using UnityEngine;

// NPC별로 지금까지 나눈 대화 횟수를 들고 있는 싱글턴. ObjectiveManager와 같은 형태(런타임 Dictionary +
// CaptureState/RestoreState)를 따르되, 사전 등록할 기획 에셋 목록이 없다 — NpcDialogue가 스스로
// npcId를 들고 조회/증가를 요청하는 구조라 Build() 단계가 필요 없다.
public class NpcDialogueManager : MonoBehaviour {
    public static NpcDialogueManager Instance;

    #region 런타임 변수

    readonly Dictionary<string, int> talkCounts = new();

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트에서만 동작 (다른 싱글턴과 동일한 이유)
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 조회 · 갱신

    // 기록이 없는 NPC는 0(=아직 한 번도 대화하지 않음)으로 취급한다.
    public int GetTalkCount(string npcId) {
        return talkCounts.TryGetValue(npcId, out int count) ? count : 0;
    }

    public void IncrementTalkCount(string npcId) {
        talkCounts[npcId] = GetTalkCount(npcId) + 1;
    }

    #endregion
    #region 세이브

    public List<NpcDialogueSaveEntry> CaptureState() {
        List<NpcDialogueSaveEntry> snapshot = new(talkCounts.Count);

        foreach (KeyValuePair<string, int> pair in talkCounts) {
            snapshot.Add(new NpcDialogueSaveEntry {
                npcId = pair.Key,
                talkCount = pair.Value,
            });
        }

        return snapshot;
    }

    // 세이브에서 읽은 상태로 되돌린다. 스냅샷에 없는 NPC의 기록이 남으면 구버전 세이브를 불렀을 때
    // 최근 진행이 섞이므로(ObjectiveManager.RestoreState와 같은 이유) 항상 통째로 비우고 다시 채운다.
    public void RestoreState(List<NpcDialogueSaveEntry> snapshot) {
        talkCounts.Clear();
        if (snapshot == null) return;

        foreach (NpcDialogueSaveEntry entry in snapshot) talkCounts[entry.npcId] = entry.talkCount;
    }

    #endregion
}
