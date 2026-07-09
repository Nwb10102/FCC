using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundMoveSystem : MonoBehaviour {
    #region 인스펙터 변수

    [Header("무브먼트 설정")]
    public float speed = 4f; // 몬스터 이동 스피드.
    public float maxSpeed = 3f; // 몬스터가 이동할 수 있는 최대 속도.

    [Header("플레이어 감지")]
    public float detectionRange = 5f; // 플레이어를 감지하는 반경.
    public LayerMask playerLayer; // 플레이어 레이어.

    [Header("Ground Check")]
    public Transform groundCheck; // **몬스터 발밑에 빈 오브젝트를 만드세요.**
    public Vector2 groundCheckSize = new(0.5f, 0.1f);
    public LayerMask groundLayer; // Ground 레이어 설정 필요

    [Header("Wall Check")]
    public Transform wallCheck; // **몬스터 진행 방향 앞쪽에 빈 오브젝트를 만드세요.**
    public float wallCheckDistance = 0.2f;

    [Header("Ledge Check")]
    public Transform ledgeCheck; // **몬스터 발밑 앞쪽에 빈 오브젝트를 만드세요.**
    public float ledgeCheckDistance = 0.5f;

    #endregion
    #region 외부 제어용 변수

    [HideInInspector]
    public bool isMovementLocked; // Attack 등 외부 시스템이 공격 중 이동을 멈출 때 사용.

    #endregion
    #region 컴포넌트 변수

    Rigidbody2D rigid;
    MoveState state;
    int facingDirection = 1; // 1: 오른쪽, -1: 왼쪽

    bool isGrounded;
    bool isFacingWall;
    bool isFacingLedge;
    bool isChasing;

    Transform playerTransform;

    #endregion

    enum MoveState { Patrol, Chase }

    #region 유니티 라이프 사이클

    void Awake() {
        rigid = GetComponent<Rigidbody2D>();
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update() {
        CheckGrounded();
        CheckWall();
        CheckLedge();
        DetectPlayer();

        if (state == MoveState.Patrol && (isFacingWall || isFacingLedge)) {
            Flip();
        }
    }

    void FixedUpdate() {
        Move();
    }

    #endregion

    #region 몬스터 움직임 관련 함수

    void Move() {
        if (isMovementLocked) {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocityY);
            return;
        }

        if (!isGrounded) {
            return;
        }

        // 추격 중 낭떠러지 앞에서는 정지 (추락 방지)
        if (state == MoveState.Chase && isFacingLedge) {
            return;
        }

        rigid.AddForce(new Vector2(facingDirection * speed, 0f), ForceMode2D.Impulse);

        // 최고 속도 관리.
        if (rigid.linearVelocityX >= maxSpeed) {
            rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocityY);
        }
        else if (rigid.linearVelocityX <= maxSpeed * (-1)) {
            rigid.linearVelocity = new Vector2(maxSpeed * (-1), rigid.linearVelocityY);
        }
    }

    void CheckGrounded() {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    void CheckWall() {
        if (wallCheck == null) return;
        isFacingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, groundLayer);
    }

    void CheckLedge() {
        if (ledgeCheck == null) {
            isFacingLedge = false;
            return;
        }
        isFacingLedge = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, ledgeCheckDistance, groundLayer);
    }

    void DetectPlayer() {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        isChasing = hit != null;

        if (isChasing) {
            playerTransform = hit.transform;
            state = MoveState.Chase;
            facingDirection = playerTransform.position.x >= transform.position.x ? 1 : -1;
        }
        else {
            state = MoveState.Patrol;
        }
    }

    void Flip() {
        facingDirection *= -1;
        Vector3 newScale = transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * facingDirection;
        transform.localScale = newScale;
    }

    // 감지 범위 표시
    private void OnDrawGizmos() {
        if (groundCheck != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + facingDirection * wallCheckDistance * Vector3.right);
        }

        if (ledgeCheck != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + Vector3.down * ledgeCheckDistance);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    #endregion
}
