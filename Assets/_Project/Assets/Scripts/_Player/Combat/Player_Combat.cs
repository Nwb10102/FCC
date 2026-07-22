using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Combat : MonoBehaviour {
    #region 인스펙터 변수

    [Header("공격 판정")]
    public Transform attackPoint; // **플레이어 공격 판정용 빈 오브젝트를 만드세요.**
    public float attackRange = 1f; // 공격이 닿는 범위.
    public LayerMask enemyLayer; // 몬스터 레이어.

    [Header("공격 타이밍")]
    public float attackDelay = 0.15f; // 판정이 들어가기 전 선딜레이.
    public float attackCooldown = 0.4f; // 공격 후 다음 공격까지의 대기 시간.

    [Header("공격 데미지")]
    public int attackDamage = 10;

    [Header("공격 범위 표시")]
    public Color rangeIndicatorColor = new(1f, 0f, 0f, 0.85f); // 선딜레이 동안 보여줄 판정 범위 색상.

    #endregion
    #region 컴포넌트 변수

    AttackState state = AttackState.Idle;
    float stateTimer;
    SpriteRenderer rangeIndicator;
    Vector3 currentAttackWorldPos; // 공격이 시작된 순간의 월드 좌표. 판정 도중 플레이어가 움직여도 이 위치에 고정된다.

    #endregion

    enum AttackState { Idle, Windup, Cooldown }

    #region 유니티 라이프 사이클

    void Awake() {
        BuildRangeIndicator();
    }

    void Update() {
        switch (state) {
            case AttackState.Windup:
                TickWindup();
                break;
            case AttackState.Cooldown:
                TickCooldown();
                break;
        }
    }

    #endregion
    #region 입력 처리

    void OnAttack(InputValue value) {
        if (!value.isPressed) return;
        if (state != AttackState.Idle) return;

        StartWindup();
    }

    #endregion
    #region 공격 관련 함수

    void StartWindup() {
        state = AttackState.Windup;
        stateTimer = attackDelay;
        currentAttackWorldPos = attackPoint.position; // 공격 시작 순간의 위치를 고정.
        ShowRangeIndicator();
    }

    void TickWindup() {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        DealDamage();
        StartCooldown();
    }

void DealDamage() {
        if (attackPoint == null) return;

        Collider2D hit = Physics2D.OverlapCircle(currentAttackWorldPos, attackRange, enemyLayer);
        if (hit == null) return;

        Health enemyHealth = hit.GetComponentInParent<Health>();
        if (enemyHealth != null) {
            enemyHealth.TakeDamage(attackDamage, currentAttackWorldPos);
        }
    }

    void StartCooldown() {
        state = AttackState.Cooldown;
        stateTimer = attackCooldown;
        HideRangeIndicator();
    }

    void TickCooldown() {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) {
            state = AttackState.Idle;
        }
    }

    // 공격 판정 범위 표시 (에디터 전용)
    void OnDrawGizmos() {
        if (attackPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    #endregion
    #region 공격 범위 표시

    void BuildRangeIndicator() {
        if (attackPoint == null) return;

        rangeIndicator = AttackRangeIndicator.Create(attackRange, rangeIndicatorColor);
    }

    void ShowRangeIndicator() {
        if (rangeIndicator == null) return;
        rangeIndicator.transform.position = currentAttackWorldPos;
        rangeIndicator.gameObject.SetActive(true);
    }

    void HideRangeIndicator() {
        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(false);
    }

    #endregion
}
