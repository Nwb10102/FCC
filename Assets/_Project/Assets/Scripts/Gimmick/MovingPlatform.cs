using UnityEngine;

// 지정된 두 지점 사이를 왕복하며, 위에 올라탄 플레이어를 함께 실어 나른다.
// **Rigidbody2D(Kinematic) + 비트리거 Collider2D가 필요합니다** (OnCollision 이벤트가 뜨려면 Kinematic 바디가 있어야 한다).
public class MovingPlatform : MonoBehaviour {
    #region 인스펙터 변수

    [Header("왕복 구간")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    #endregion
    #region 컴포넌트 변수

    Vector3 target;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        target = pointB != null ? pointB.position : transform.position;
    }

    void Update() {
        if (pointA == null || pointB == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f)
            target = target == pointA.position ? pointB.position : pointA.position;
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.collider.CompareTag("Player")) collision.transform.SetParent(transform);
    }

    void OnCollisionExit2D(Collision2D collision) {
        if (collision.collider.CompareTag("Player")) collision.transform.SetParent(null);
    }

    #endregion
}
