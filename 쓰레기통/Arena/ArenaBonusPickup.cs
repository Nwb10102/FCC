using UnityEngine;

// SecretBranch 방에 놓는 보너스 기억 조각. 던전이 재입장마다 통째로 재생성되므로
// 별도 세이브·1회성 플래그 없이 SetActive(false)만으로 충분하다.
// **Is Trigger 콜라이더를 붙이세요.**
public class ArenaBonusPickup : MonoBehaviour {
    #region 인스펙터 변수

    [Header("보상")]
    public int memoryShardAmount = 1;

    #endregion
    #region 트리거

    void OnTriggerEnter2D(Collider2D other) {
        Player_MemoryShardInventory shards = other.GetComponentInParent<Player_MemoryShardInventory>();
        if (shards == null) return;

        shards.Add(memoryShardAmount);
        gameObject.SetActive(false);
    }

    #endregion
}
