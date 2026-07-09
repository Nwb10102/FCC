using UnityEngine;

[RequireComponent(typeof(GroundMoveSystem))]
public class Attack : MonoBehaviour {
    #region 인스펙터 변수

    [Header("공격 판정")]
    public Transform attackPoint; // **몬스터 공격 판정용 빈 오브젝트를 만드세요.**
    public float attackRange = 1f; // 공격이 닿는 범위.
    public LayerMask playerLayer; // 플레이어 레이어.

    [Header("공격 타이밍")]
    public float attackDelay = 0.3f; // 판정이 들어가기 전 선딜레이.
    public float attackCooldown = 1.5f; // 공격 후 다음 공격까지의 대기 시간.

    [Header("공격 데미지")]
    public int attackDamage = 10;

    #endregion
    #region 컴포넌트 변수

    GroundMoveSystem moveSystem;
    AttackState state = AttackState.Idle;
    float stateTimer;

    #endregion

    enum AttackState { Idle, Windup, Cooldown }

    #region 유니티 라이프 사이클

    void Awake() {
        moveSystem = GetComponent<GroundMoveSystem>();
    }

    void Update() {
        switch (state) {
            case AttackState.Idle:
                CheckAttackRange();
                break;
            case AttackState.Windup:
                TickWindup();
                break;
            case AttackState.Cooldown:
                TickCooldown();
                break;
        }
    }

    #endregion

    #region 공격 관련 함수

    void CheckAttackRange() {
        if (attackPoint == null) return;

        bool playerInRange = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (!playerInRange) return;

        StartWindup();
    }

    void StartWindup() {
        state = AttackState.Windup;
        stateTimer = attackDelay;
        moveSystem.isMovementLocked = true; // 선딜레이 동안 제자리에서 공격 준비.
    }

    void TickWindup() {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        DealDamage();
        StartCooldown();
    }

    void DealDamage() {
        // 선딜레이가 끝난 시점에 플레이어가 범위 안에 있는지 다시 확인.
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (hit == null) return;

        if (hit.TryGetComponent(out Health playerHealth)) {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    void StartCooldown() {
        state = AttackState.Cooldown;
        stateTimer = attackCooldown;
        moveSystem.isMovementLocked = false;
    }

    void TickCooldown() {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) {
            state = AttackState.Idle;
        }
    }

    // 공격 판정 범위 표시
    void OnDrawGizmos() {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    #endregion
}
