using System.Collections.Generic;
using UnityEngine;

// 가시 같은 상시 위험 지형. 닿아 있는 동안 reHitInterval 간격으로 계속 데미지를 준다.
// **Is Trigger 콜라이더를 붙이세요.**
public class DamageZone : MonoBehaviour {
    #region 인스펙터 변수

    [Header("데미지")]
    public int damage = 10;
    public float reHitInterval = 0.5f; // 계속 닿아 있을 때 재피격 간격.

    #endregion
    #region 컴포넌트 변수

    readonly Dictionary<Collider2D, float> lastHitTime = new();

    #endregion
    #region 트리거

    void OnTriggerEnter2D(Collider2D other) {
        TryDamage(other);
    }

    void OnTriggerStay2D(Collider2D other) {
        TryDamage(other);
    }

    void TryDamage(Collider2D other) {
        Health health = other.GetComponentInParent<Health>();
        if (health == null) return;

        if (lastHitTime.TryGetValue(other, out float last) && Time.time - last < reHitInterval) return;

        lastHitTime[other] = Time.time;
        health.TakeDamage(damage, transform.position);
    }

    #endregion
}
