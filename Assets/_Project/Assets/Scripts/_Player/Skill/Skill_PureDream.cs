using System.Collections.Generic;
using UnityEngine;

// 주인공의 정화 스킬. 전방에 정화 파동을 터뜨려 범위 안의 대상에게 피해를 주고, 그 대가로 자아 게이지를 조금 회복한다.
// 스킬 틀(SkillBase → SkillManager) 검증용 샘플이라 연출은 기존 전투 파이프라인(HitVfx·HitReactor)에 얹기만 했다.
//
// **에셋 생성: Create ▸ FCC ▸ Skill ▸ Pure Dream**
[CreateAssetMenu(fileName = "Skill_PureDream", menuName = "FCC/Skill/Pure Dream")]
public class Skill_PureDream : SkillBase {
    #region 인스펙터 변수

    [Header("정화 판정")]
    public LayerMask targetLayer; // 정화가 닿는 대상. **몬스터 레이어를 지정하세요.**
    public float radius = 2.2f; // 파동이 퍼지는 반경.
    public float forwardOffset = 1.2f; // 플레이어 기준 전방으로 밀어낼 거리. 0이면 발밑에서 터진다.
    public float heightOffset = 0.5f; // 파동 중심 높이. 플레이어 원점이 발밑이라 조금 올려야 몸통에 걸린다.

    [Header("피해")]
    public int damage = 25;

    [Header("회복")]
    // 발동 시 회복되는 자아 게이지. 정화의 대가로 자기 자신도 조금 정화된다는 컨셉.
    // 데미지가 아닌 경로라 Health.SetHealth를 쓴다 (사망 연출이 도는 TakeDamage와 구분).
    public int selfHealAmount = 5;

    #endregion
    #region 런타임 변수

    // 한 번의 발동에서 이미 때린 대상. 한 몬스터에 콜라이더가 여러 개 붙어 있어도 한 번만 맞게 한다.
    // 매번 새로 할당하지 않도록 재사용한다 (Player_Combat.hitTargets와 같은 방식).
    readonly HashSet<Health> hitTargets = new();

    #endregion
    #region 발동

    public override void Use(Transform owner) {
        Vector2 facing = GetFacingDirection(owner);

        // 파동 중심. 이 좌표가 곧 공격 원점이라 넉백·스파크 방향이 전부 여기서 파생된다.
        Vector2 center = (Vector2)owner.position + facing * forwardOffset + Vector2.up * heightOffset;

        Purify(owner, center);
        HealSelf(owner);
        PlayFeedback(center, facing);
    }

    #endregion
    #region 정화 판정

    void Purify(Transform owner, Vector2 center) {
        // OverlapCircle은 콜라이더를 하나만 돌려줘서 겹쳐 있는 대상 중 한 마리만 맞는다. 범위 안의 전부를 때린다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetLayer);
        if (hits.Length == 0) return;

        Health ownerHealth = owner.GetComponentInParent<Health>(); // 레이어를 잘못 잡아 자기 자신을 때리는 사고 방지용.

        hitTargets.Clear();
        foreach (Collider2D hit in hits) {
            // 콜라이더가 자식에 달려 있어도 본체의 Health를 찾는다.
            Health targetHealth = hit.GetComponentInParent<Health>();

            if (targetHealth == null || targetHealth == ownerHealth) continue;
            if (!hitTargets.Add(targetHealth)) continue;

            // 파동 중심을 원점으로 넘겨 몬스터가 파동 바깥으로 밀려나게 한다.
            targetHealth.TakeDamage(damage, center);
        }
    }

    #endregion
    #region 자기 회복

    void HealSelf(Transform owner) {
        if (selfHealAmount <= 0) return;

        Health ownerHealth = owner.GetComponentInParent<Health>();
        if (ownerHealth == null) return;

        // SetHealth는 최대치를 넘지 않게 알아서 잘라준다.
        ownerHealth.SetHealth(ownerHealth.CurrentHealth + selfHealAmount);
    }

    #endregion
    #region 연출

    // 맞은 대상의 스파크는 HitReactor가 OnDamaged를 받아 알아서 재생한다.
    // 여기서는 아무도 맞지 않아도 스킬을 쓴 티가 나도록 파동 중심에 한 번 터뜨리기만 한다.
    void PlayFeedback(Vector2 center, Vector2 facing) {
        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (HitVfx.Instance != null) HitVfx.Instance.PlaySpark(center, facing);
    }

    #endregion
}
