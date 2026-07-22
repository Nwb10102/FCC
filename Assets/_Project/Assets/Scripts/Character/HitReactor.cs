using UnityEngine;

// Health의 OnDamaged 이벤트를 받아 히트스톱/카메라쉐이크/넉백을 트리거한다.
// 플레이어와 몬스터 양쪽에 Health와 함께 부착해서 사용한다.
[RequireComponent(typeof(Health))]
public class HitReactor : MonoBehaviour {
    public float knockbackForce = 6f; // 넉백 세기.

    Health health;
    GroundMoveSystem groundMove;
    Player_move playerMove;

    void Awake() {
        health = GetComponent<Health>();
        groundMove = GetComponent<GroundMoveSystem>();
        playerMove = GetComponent<Player_move>();
    }

    void OnEnable() {
        health.OnDamaged += HandleDamaged;
    }

    void OnDisable() {
        health.OnDamaged -= HandleDamaged;
    }

    void HandleDamaged(int damage, Vector2 sourcePosition) {
        if (HitFeedback.Instance != null) {
            HitFeedback.Instance.PlayHit();
        }

        Vector2 knockbackDir = (Vector2)transform.position - sourcePosition;
        if (knockbackDir == Vector2.zero) knockbackDir = Vector2.right;
        knockbackDir.Normalize();

        Vector2 force = knockbackDir * knockbackForce;

        if (groundMove != null) groundMove.ApplyKnockback(force);
        if (playerMove != null) playerMove.ApplyKnockback(force);
    }
}
