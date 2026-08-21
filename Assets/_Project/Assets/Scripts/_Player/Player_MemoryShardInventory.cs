using UnityEngine;

// 플레이어가 보유한 기억 조각 수량. SaveManager가 이 값을 세이브 파일과 동기화한다.
// 회복 소비·스킬 포인트 전환 등 후속 기능은 아직 없고, 지금은 보유 수량만 들고 있다.
public class Player_MemoryShardInventory : MonoBehaviour {
    #region 인스펙터 변수

    [Header("기억 조각")]
    public int count; // 보유한 기억 조각 수.

    #endregion
    #region 조회

    public int Count => count;

    #endregion
    #region 증감

    public void Add(int amount = 1) {
        count = Mathf.Max(0, count + amount);
    }

    // 세이브 복원 전용. 직접 증감이 아니라 값을 통째로 맞출 때 쓴다.
    public void SetCount(int value) {
        count = Mathf.Max(0, value);
    }

    #endregion
}
