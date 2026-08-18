using UnityEngine;

// 명중 시 범위 피해를 추가로 터뜨리는 투사체가 쓰는 설정. 저글러의 {폭발하는 공}처럼 스택이 다 찼을 때만
// 켜서 넘겨준다. enabled가 false면 아무 일도 하지 않으므로, 이 기능이 필요 없는 투사체(칼잡이 등)는
// 신경 쓰지 않고 기본값(default)을 그대로 넘기면 된다. SkillProjectile과 SkillBouncingProjectile이 공유한다.
public struct SkillProjectileExplosion {
    public bool enabled;
    public float radius;
    public int damage;
    public LayerMask layer; // 보통 투사체의 targetLayer와 같지만, 다른 범위로 터뜨리고 싶을 때를 위해 따로 받는다.
}
