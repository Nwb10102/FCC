using UnityEngine;

// Health의 OnDamaged/OnDeath 이벤트를 받아 히트스톱/카메라쉐이크/파티클/넉백을 트리거한다.
// 피격 대상 본인의 시각 반응(플래시, 스쿼시)은 HitFlash가 따로 담당한다.
// 플레이어와 몬스터 양쪽에 Health와 함께 부착해서 사용한다.
[RequireComponent(typeof(Health))]
public class HitReactor : MonoBehaviour {
    #region 인스펙터 변수

    [Header("넉백")]
    public float knockbackForce = 6f; // 넉백 세기.

    [Header("이펙트 위치")]
    public Vector2 hitPointOffset = Vector2.zero; // 캐릭터 기준으로 이펙트 위치를 미세 조정.
    public float hitPointPullIn = 0.35f; // 공격이 들어온 쪽으로 얼마나 당겨서 띄울지. 0이면 캐릭터 중심에서 터진다.

    #endregion
    #region 컴포넌트 변수

    Health health;
    GroundMoveSystem groundMove;
    FlyMoveSystem flyMove;
    Player_move playerMove;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        health = GetComponent<Health>();
        groundMove = GetComponent<GroundMoveSystem>();
        flyMove = GetComponent<FlyMoveSystem>();
        playerMove = GetComponent<Player_move>();
    }

    void OnEnable() {
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    void OnDisable() {
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    #endregion
    #region 피격 반응

    void HandleDamaged(int damage, Vector2 sourcePosition) {
        Vector2 hitDir = GetHitDirection(sourcePosition);
        Vector2 hitPoint = GetHitPoint(hitDir);
        bool isLethal = health.CurrentHealth <= 0; // OnDamaged는 체력을 깎은 뒤에 발행되므로 여기서 처치 여부를 알 수 있다.

        // 싱글턴은 파괴된 뒤에도 참조가 남을 수 있어 ?. 대신 Unity의 == 오버로드를 타는 != null로 검사한다.
        if (HitVfx.Instance != null) HitVfx.Instance.PlaySpark(hitPoint, hitDir);

        // 처치했을 때는 HandleDeath에서 더 강한 피드백을 재생하므로 여기서는 건너뛴다 (히트스톱 이중 적용 방지).
        if (!isLethal && HitFeedback.Instance != null) {
            HitFeedback.Instance.PlayHit(hitPoint, hitDir, damage, false);
        }

        ApplyKnockback(hitDir);
    }

    // Health.Die()가 곧바로 Destroy를 호출하므로, 사망 연출은 반드시 이 오브젝트 바깥(HitVfx 싱글턴)에서 재생해야 한다.
    void HandleDeath(Vector2 sourcePosition) {
        Vector2 hitDir = GetHitDirection(sourcePosition);
        Vector2 position = transform.position;

        if (HitFeedback.Instance != null) HitFeedback.Instance.PlayHit(position, hitDir, 0, true);
        if (HitVfx.Instance != null) HitVfx.Instance.PlayDeath(position);
    }

    void ApplyKnockback(Vector2 hitDir) {
        Vector2 force = hitDir * knockbackForce;

        if (groundMove != null) groundMove.ApplyKnockback(force);
        if (flyMove != null) flyMove.ApplyKnockback(force);
        if (playerMove != null) playerMove.ApplyKnockback(force);
    }

    // 공격 원점에서 피격자를 향하는 방향. 넉백과 이펙트/쉐이크가 같은 방향을 공유한다.
    Vector2 GetHitDirection(Vector2 sourcePosition) {
        Vector2 direction = (Vector2)transform.position - sourcePosition;
        if (direction == Vector2.zero) direction = Vector2.right;
        return direction.normalized;
    }

    // 캐릭터 중심에서 공격이 들어온 쪽으로 살짝 당긴 지점. 이펙트가 몸통 한가운데가 아니라 맞은 면에서 터진다.
    Vector2 GetHitPoint(Vector2 hitDir) {
        return (Vector2)transform.position + hitPointOffset - hitDir * hitPointPullIn;
    }

    #endregion
}
