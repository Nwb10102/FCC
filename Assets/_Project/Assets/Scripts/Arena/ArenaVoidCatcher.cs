using UnityEngine;

// 방 하나의 바닥 아래 깔리는 낙사 안전망. ArenaDungeonGenerator가 방을 배치한 직후 생성해
// 자기 소속 방으로 Configure한다(프리팹 안에서는 자기 방을 스스로 참조할 수 없기 때문).
// 던전은 재입장마다 통째로 재생성되므로 별도 세이브 없이 room 참조만 들고 있으면 충분하다.
public class ArenaVoidCatcher : MonoBehaviour {
    #region 컴포넌트 변수

    DungeonRoom ownerRoom;

    #endregion
    #region 설정

    public void Configure(DungeonRoom room) {
        ownerRoom = room;
    }

    #endregion
    #region 트리거

    // ArenaGate.WarpToEntry와 같은 방식(위치 대입 + Rigidbody2D 속도 초기화)으로 방 입구에 되돌려놓는다.
    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player") || ownerRoom == null || ownerRoom.entryAnchor == null) return;

        other.transform.position = ownerRoom.entryAnchor.position;
        if (other.attachedRigidbody != null) other.attachedRigidbody.linearVelocity = Vector2.zero;
    }

    #endregion
}
