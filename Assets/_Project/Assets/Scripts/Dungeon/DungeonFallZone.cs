using UnityEngine;

// 구덩이·가시밭 아래에 깔아 두는 낙사 트리거. 닿으면 현재 방 리스폰 지점으로 되돌린다.
// DungeonGenerator.fallYThreshold 로도 같은 처리가 되지만, 방마다 바닥 높이가 다를 때
// 지점별로 정확히 막고 싶으면 이 트리거를 쓴다.
// **Is Trigger 콜라이더를 붙이세요.**
[RequireComponent(typeof(Collider2D))]
public class DungeonFallZone : MonoBehaviour {
    #region 인스펙터 변수

    [Header("감지")]
    public string playerTag = "Player";

    #endregion
    #region 유니티 라이프 사이클

    void Reset() {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;
        if (DungeonRespawnController.Instance != null) DungeonRespawnController.Instance.RespawnAtCurrentRoom();
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out Collider2D col)) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.18f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif

    #endregion
}
