using UnityEngine;

// 방 안의 세부 카메라 구역. 트리거에 들어가면 방 기본값 위에 이 설정을 덮고, 나가면 되돌린다.
// 예: 전투방은 기본 줌 8·플레이어 추적 → 그 안 아레나 구역에 들어가면 줌 11·고정점 응시로.
//
// 여러 구역이 겹치거나 중첩돼도 DungeonCameraDirector 가 스택으로 관리하므로 꼬이지 않는다.
// **Is Trigger 콜라이더를 붙이세요.**
[RequireComponent(typeof(Collider2D))]
public class DungeonCameraZone : MonoBehaviour {
    #region 인스펙터 변수

    [Header("구역 카메라 (방 기본값 위에 덮어씀)")]
    public DungeonCameraSettings settings = new();

    [Header("감지")]
    public string playerTag = "Player";

    #endregion
    #region 유니티 라이프 사이클

    void Reset() {
        // 새로 붙였을 때 바로 트리거로 동작하도록.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;
        DungeonCameraDirector.GetOrCreate().PushZone(this, settings.ToRequest());
    }

    void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;
        if (DungeonCameraDirector.Instance != null) DungeonCameraDirector.Instance.PopZone(this);
    }

    // 던전이 통째로 해체되면 OnTriggerExit2D 가 오지 않을 수 있으니 여기서도 확실히 뺀다.
    void OnDisable() {
        if (DungeonCameraDirector.Instance != null) DungeonCameraDirector.Instance.PopZone(this);
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out Collider2D col)) return;

        Gizmos.color = new Color(0.55f, 0.5f, 1f, 0.18f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.55f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

        if (settings.mode == DungeonCameraFollowMode.FixedPoint && settings.fixedPoint != null) {
            Gizmos.DrawLine(col.bounds.center, settings.fixedPoint.position);
            Gizmos.DrawWireSphere(settings.fixedPoint.position, 0.4f);
        }
    }
#endif

    #endregion
}
