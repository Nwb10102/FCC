using UnityEngine;

// 시작 위치와 moveOffset 만큼 떨어진 지점 사이를 왕복하는 발판. 위에 올라탄 대상은 함께 실려 간다.
// **Kinematic Rigidbody2D + 비트리거 Collider2D 가 필요합니다.**
//
// 태우는 코드가 따로 없는 이유는, Kinematic 바디를 MovePosition 으로 옮기면 유니티 2D 물리의 표면
// 마찰이 위에 올라탄 다이나믹 바디를 이미 같은 속도로 끌고 가기 때문이다. 여기에 "발판이 움직인
// 만큼 더해 주기"를 얹었더니 플레이어가 발판 속도의 두 배로 움직여 앞쪽 모서리로 밀려 떨어졌다.
//
// 예전 구현처럼 SetParent 로 태우는 것도 안 된다. 두 가지 이유가 있다.
//  - 던전을 나갈 때 DungeonGenerator.Teardown() 이 생성된 방을 통째로 Destroy 하는데, 그 순간
//    플레이어가 발판의 자식이면 플레이어까지 함께 삭제된다.
//  - 발판이 스케일로 크기를 잡는 경우(그레이박스 Quad 가 그렇다) 자식이 된 플레이어에게 그 스케일이
//    곱해져 납작하게 찌그러진다. 플레이어는 좌우 반전도 localScale 로 하고 있어 방향까지 깨진다.
//
// 왕복 구간을 Transform 두 개가 아니라 오프셋으로 잡는 이유는, 발판을 복제하거나 프리팹으로 만들 때
// 따라오지 않는 빈 오브젝트를 함께 챙길 필요가 없기 때문이다.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour {
    #region 인스펙터 변수

    [Header("왕복 구간")]
    public Vector2 moveOffset = new(4f, 0f); // 시작 위치에서 이만큼 떨어진 지점까지 갔다가 되돌아온다.
    public float speed = 2f;                 // 이동 속도(초당 유닛).
    public float waitTime = 0.4f;            // 양 끝에서 잠깐 멈추는 시간. 뛰어 탈 여유를 준다.
    public float startDelay = 0f;            // 처음 출발까지 기다리는 시간. 여러 발판의 위상을 어긋나게 할 때 쓴다.

    #endregion
    #region 컴포넌트 변수

    Rigidbody2D body;
    Vector2 origin;
    Vector2 destination;
    Vector2 target;
    bool originCaptured;
    float waitTimer;

    #endregion
    #region 유니티 라이프 사이클

    void Reset() {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    void Awake() {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; // 다이나믹이면 중력에 발판이 그대로 떨어져 버린다.
    }

    // 물리 이동이므로 FixedUpdate 에서 MovePosition 으로 옮긴다. Update 에서 transform 을 직접 옮기면
    // 물리 갱신 시점과 어긋나 올라탄 쪽이 미세하게 떨리고, 마찰로 실어 나르지도 못한다.
    void FixedUpdate() {
        if (!originCaptured) CaptureOrigin();

        if (waitTimer > 0f) {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        body.MovePosition(next);

        if (Vector2.Distance(next, target) > 0.001f) return;

        target = target == destination ? origin : destination;
        waitTimer = waitTime;
    }

    #endregion
    #region 왕복 기준점

    // 기준점을 첫 물리 스텝에서, 그것도 body.position 이 아니라 transform.position 에서 잡는다.
    // DungeonGenerator 는 방을 Instantiate 한 직후 소켓을 맞춰 통째로 옮기는데,
    //  - Awake 는 그 이동 전에 돌아 프리팹 원점을 기준으로 잡아 버리고,
    //  - 부모를 옮긴 것은 다음 물리 스텝 전까지 Rigidbody2D 에 반영되지 않아 body.position 은
    //    아직 옮기기 전 좌표를 돌려준다.
    // 실제로 body.position 으로 잡았을 때, 발판이 방을 벗어나 월드 원점 쪽으로 계속 날아갔다.
    void CaptureOrigin() {
        originCaptured = true;
        origin = transform.position;
        destination = origin + moveOffset;
        target = destination;
        waitTimer = startDelay; // 첫 출발만 늦춘다. 이후 왕복은 waitTime 을 따른다.
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    // 왕복 구간을 씬 뷰에서 눈으로 확인할 수 있게 그린다. 플레이 전에는 현재 위치가 곧 기준점이다.
    void OnDrawGizmosSelected() {
        Vector3 from = originCaptured ? (Vector3)origin : transform.position;
        Vector3 to = from + (Vector3)moveOffset;

        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(from, 0.25f);
        Gizmos.DrawWireSphere(to, 0.25f);
    }
#endif

    #endregion
}
