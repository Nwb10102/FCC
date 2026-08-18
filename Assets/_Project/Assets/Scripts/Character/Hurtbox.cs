using UnityEngine;

// 공격 판정이 실제로 노려야 할 피격 범위다. 몸통 물리 콜라이더와 분리해 두면
// 이동 충돌용 모양과 피격 판정 모양을 서로 다르게 조정할 수 있고, 공격 쪽에서는
// 이 컴포넌트가 붙어 있는 콜라이더만 골라 때려서 몸통 콜라이더가 실수로 맞는 걸 막는다.
[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour {
    #region 컴포넌트 변수

    public Health OwnerHealth { get; private set; }

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        OwnerHealth = GetComponentInParent<Health>();
    }

    #endregion
}
