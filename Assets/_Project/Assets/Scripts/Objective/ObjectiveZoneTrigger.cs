using UnityEngine;

// 플레이어가 이 영역(Collider2D 트리거)에 들어오면 해당 목표를 완료 처리한다.
// DialogueTriggerZone / CutSceneManager와 동일한 트리거 골격을 사용한다.
public class ObjectiveZoneTrigger : MonoBehaviour {
    #region 인스펙터 변수

    [Header("완료할 목표")]
    public string objectiveId; // **목표 에셋의 id와 똑같이 적으세요.**

    // 켜두면 목표가 진행 가능한 상태일 때만 완료시킨다. 뒷 미션의 구역을 미리 밟아
    // 목표가 동선을 앞질러 완료되는 것을 막는다. **먼저 지나가도 되는 구역이면 끄세요.**
    public bool requireActive = true;

    [Header("트리거")]
    public bool playOnce = true;
    public string playerTag = "Player";

    #endregion
    #region 런타임 변수

    bool played;

    #endregion
    #region 유니티 라이프 사이클

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && played) return;

        if (ObjectiveManager.Instance == null) return;

        // 아직 진행할 수 없는 목표면 아무 일도 하지 않고 played도 세우지 않는다 — 조건이 갖춰진 뒤
        // 다시 지나갈 때 완료되어야 하기 때문.
        if (requireActive && !ObjectiveManager.Instance.IsObjectiveActive(objectiveId)) return;

        played = true;
        ObjectiveManager.Instance.CompleteObjective(objectiveId);
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out Collider2D col)) return;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif

    #endregion
}
