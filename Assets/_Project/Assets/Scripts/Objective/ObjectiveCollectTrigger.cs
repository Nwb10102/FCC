using UnityEngine;

// 수집형 목표(MissionType.CollectItem)의 진행도를 올리는 오브젝트. 기억 조각처럼 필드에 뿌려두고 줍는 것들.
//
// 목표가 아직 진행 가능한 상태가 아니면 아무 일도 하지 않고 그대로 남는다 — 잠긴 동안 주워져 사라지면
// 미션이 열린 뒤에 모을 수 없게 되기 때문이다.
//
// **콜라이더(Is Trigger 체크)를 함께 붙이세요.**
// 기억 조각 아이템 본체(회복·수량 UI·소비)는 아직 별건이다. 여기서는 목표 카운터만 올린다.
public class ObjectiveCollectTrigger : MonoBehaviour {
    #region 인스펙터 변수

    [Header("올릴 목표")]
    public string objectiveId; // **목표 에셋의 id와 똑같이 적으세요.** 틀리면 콘솔에 경고가 찍힌다.
    [Min(1)]
    public int amount = 1; // 한 번에 올릴 수량. 큰 조각처럼 여러 개로 세는 경우에 늘린다.

    [Header("트리거")]
    public string playerTag = "Player";
    public bool destroyOnCollect = true; // 껐다가 다시 켜서 재사용할 수 있도록 파괴 대신 끄고 싶을 때 해제.

    #endregion
    #region 유니티 라이프 사이클

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;

        if (ObjectiveManager.Instance == null) {
            Debug.LogWarning($"[ObjectiveCollectTrigger] '{name}' — 씬에 ObjectiveManager가 없습니다.", this);
            return;
        }

        // 진행도가 실제로 오르지 않았으면(잠김·이미 완료) 오브젝트를 남긴다.
        if (!ObjectiveManager.Instance.AddProgress(objectiveId, amount)) return;

        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    // 수집물은 보통 작아서 구역 트리거처럼 육면체로 그리면 잘 안 보인다. 노란 구로 표시한다.
    void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out Collider2D col)) return;

        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.85f);
        Gizmos.DrawWireSphere(col.bounds.center, Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, 0.2f));
    }
#endif

    #endregion
}
