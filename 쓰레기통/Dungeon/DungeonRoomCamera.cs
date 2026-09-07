using UnityEngine;

// 방 하나의 기본 카메라 상태. DungeonRoom 이 플레이어 진입 시 이 값을 DungeonCameraDirector 의
// 최하위 레이어로 올린다. 방 안에 DungeonCameraZone 이 있으면 그 위에 덮인다.
//
// DungeonRoom 과 같은 오브젝트에 붙여도 되고 따로 둬도 된다(DungeonRoom 이 GetComponent 로 찾는다).
public class DungeonRoomCamera : MonoBehaviour {
    #region 인스펙터 변수

    [Header("방 기본 카메라")]
    public DungeonCameraSettings settings = new();

    #endregion
    #region 요청 생성

    public DungeonCameraRequest BuildRequest() {
        return settings.ToRequest();
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (settings.bounds != null) {
            Bounds b = settings.bounds.bounds;
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.12f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.7f);
            Gizmos.DrawWireCube(b.center, b.size);
        }

        if (settings.mode == DungeonCameraFollowMode.FixedPoint && settings.fixedPoint != null) {
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.9f);
            Gizmos.DrawWireSphere(settings.fixedPoint.position, 0.4f);
            Gizmos.DrawLine(transform.position, settings.fixedPoint.position);
        }
    }
#endif

    #endregion
}
