using UnityEngine;

// 마임의 Invisible Reality가 설치하는 투명 벽. 정적 콜라이더 하나로 몬스터·투사체·플레이어 이동을
// 전부 물리적으로 막는다 — Rigidbody2D가 없어도 유니티 2D 물리가 알아서 Rigidbody2D를 가진 쪽(플레이어·몬스터)의
// 이동을 막아 주므로 이 오브젝트에는 Rigidbody2D가 필요 없다. 몬스터 투사체(SkillProjectile)는 자체적으로
// Kinematic Rigidbody2D + Is Trigger 콜라이더를 갖고 있어, 이 벽이 obstacleLayer에 포함되기만 하면
// 트리거 이벤트가 정상적으로 발생해 그 자리에서 사라진다.
//
// **프리팹 구성: 비트리거 BoxCollider2D(크기 1x1 그대로 두세요) + SpriteRenderer(1x1 월드 유닛 스프라이트)가 필요합니다.**
// Rigidbody2D는 붙이지 않습니다. 크기 조절은 transform.localScale 하나로 처리한다 — BoxCollider2D.size는
// 로컬 스케일을 그대로 따라가므로(유니티 2D 물리 기본 동작), 콜라이더와 스프라이트가 항상 같은 크기를 유지한다.
[RequireComponent(typeof(Collider2D))]
public class SkillInvisibleWall : MonoBehaviour {
    #region 런타임 변수

    float remainingDuration;

    #endregion
    #region 유니티 라이프 사이클

    void Update() {
        // SkillProjectile.Update()와 같은 패턴 — 설치 스킬 쪽(Skill_InvisibleReality)이 별도 코루틴을 관리하지
        // 않아도 되도록 소멸 타이머를 이 오브젝트가 스스로 들고 있는다.
        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f) Destroy(gameObject);
    }

    #endregion
    #region 설치

    // length: 벽이 뻗는 방향(로컬 Y축)의 길이. thickness: 그 수직 방향(로컬 X축)의 두께.
    // duration: 이 시간이 지나면 자동으로 사라진다. layer: 물리 충돌·시야 차단·투사체 차단 판정에 쓸 레이어.
    public void Initialize(float length, float thickness, float duration, int layer) {
        remainingDuration = duration;
        gameObject.layer = layer;
        transform.localScale = new Vector3(thickness, length, 1f);
    }

    #endregion
}
