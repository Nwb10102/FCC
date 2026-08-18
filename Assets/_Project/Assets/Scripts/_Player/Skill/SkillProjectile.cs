using System.Collections.Generic;
using UnityEngine;

// 스킬이 날리는 투사체 공용 컴포넌트. 칼잡이(Broken Phantasm)를 시작으로 투사체형 스킬이 함께 재사용할 수 있도록
// 발사 파라미터를 전부 Launch()로 주입받는다. 스킬 쪽(Skill_BrokenPhantasm 등)은 프리팹 참조만 들고 있으면 된다.
//
// obstacleLayer로 지정한 벽·바닥 등에 닿아도 그 자리에서 사라진다 — 유니티 2D 물리는 양쪽 다 Rigidbody2D가
// 없는 정적 콜라이더끼리는 트리거 이벤트를 아예 보내주지 않으므로, 벽 충돌을 감지하려면 이 투사체 쪽에
// Kinematic Rigidbody2D가 반드시 있어야 한다 (직접 transform으로 움직이므로 물리 반응에는 관여하지 않는다).
// **obstacleLayer는 반드시 벽/바닥처럼 실제로 막아야 하는 레이어만 지정해야 한다.** "레이어를 가리지 않고
// 아무 트리거에나" 반응하게 하면 카메라 컨파이너(Cinemachine Confiner의 큰 트리거 콜라이더)처럼 전투와
// 무관한 트리거에도 부딪혀 스폰되자마자 사라지는 사고가 난다 (실제로 겪은 버그).
//
// **프리팹 구성: Kinematic Rigidbody2D + Is Trigger 콜라이더가 필요합니다.** 스프라이트는 +X를 정면으로 그려서
// 붙이세요 (발사 방향으로 회전시킵니다).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SkillProjectile : MonoBehaviour {
    #region 런타임 변수

    Vector2 direction;
    LayerMask targetLayer;
    int damage;
    float speed;
    float remainingLifetime;
    Health ownerHealth; // 발사한 본인을 오폭하지 않기 위한 검사용.
    int remainingHits; // 몇 명을 더 맞힐 수 있는지. 0이 되면 소멸한다.
    LayerMask obstacleLayer; // 이 레이어에 닿으면 그 자리에서 사라진다. 비워두면(0) 벽 충돌 없이 기존처럼 동작한다.
    SkillProjectileExplosion explosion; // enabled가 false면 아무 일도 하지 않는다 (기본값 = default).

    // 폭발 범위 피해 대상 중복 방지용. 명중당 한 번만 쓰고 비우므로 매번 새로 할당하지 않는다.
    static readonly HashSet<Health> explosionHitTargets = new();

    #endregion
    #region 발사

    // pierceCount: 이 투사체가 총 몇 명을 맞히고 사라지는지 (1이면 첫 대상을 맞히고 바로 소멸).
    // obstacleLayer를 생략하면(default, 0) 벽에 부딪혀도 사라지지 않는 기존 동작을 그대로 유지한다
    // (Monster_Wraith 같은 기존 호출부를 건드리지 않기 위해 기본값을 "없음"으로 뒀다).
    // explosionSettings를 생략하면(default) 평소처럼 단일 대상 피해만 준다.
    public void Launch(Vector2 launchDirection, LayerMask hitLayer, int hitDamage, float moveSpeed, float lifetime, Health owner, int pierceCount, LayerMask obstacleLayer = default, SkillProjectileExplosion explosionSettings = default) {
        direction = launchDirection.normalized;
        targetLayer = hitLayer;
        damage = hitDamage;
        speed = moveSpeed;
        remainingLifetime = lifetime;
        ownerHealth = owner;
        remainingHits = Mathf.Max(1, pierceCount);
        this.obstacleLayer = obstacleLayer;
        explosion = explosionSettings;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    #endregion
    #region 유니티 라이프 사이클

    void Update() {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) {
        Health hitHealth = other.GetComponentInParent<Health>();
        if (hitHealth != null && hitHealth == ownerHealth) return; // 발사자 본인의 콜라이더는 무시.

        bool isTarget = ((1 << other.gameObject.layer) & targetLayer) != 0;
        if (isTarget && other.TryGetComponent(out Hurtbox hurtbox) && hurtbox.OwnerHealth != null && hurtbox.OwnerHealth != ownerHealth) {
            hurtbox.OwnerHealth.TakeDamage(damage, transform.position);

            if (HitVfx.Instance != null) HitVfx.Instance.PlaySpark(transform.position, direction);
            if (explosion.enabled) Explode();

            remainingHits--;
            if (remainingHits <= 0) Destroy(gameObject);
            return;
        }

        // obstacleLayer로 지정한 것(벽·바닥 등)에만 반응해 관통 횟수와 상관없이 그 자리에서 사라진다.
        // 그 외 트리거(카메라 컨파이너 등 전투와 무관한 것)는 무시하고 계속 날아간다.
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0) Destroy(gameObject);
    }

    #endregion
    #region 폭발

    void Explode() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosion.radius, explosion.layer);
        if (hits.Length == 0) return;

        explosionHitTargets.Clear();
        foreach (Collider2D hit in hits) {
            if (!hit.TryGetComponent(out Hurtbox hurtbox) || hurtbox.OwnerHealth == null) continue;
            if (hurtbox.OwnerHealth == ownerHealth) continue;
            if (!explosionHitTargets.Add(hurtbox.OwnerHealth)) continue; // 명중 대상과 겹쳐도 두 번 맞지 않게.

            hurtbox.OwnerHealth.TakeDamage(explosion.damage, transform.position);
        }

        // 전용 폭발 이펙트가 아직 없어 기존 사망 파열(원형으로 사방에 퍼지는 연출)을 빌려 쓴다.
        if (HitVfx.Instance != null) HitVfx.Instance.PlayDeath(transform.position);
    }

    #endregion
}
