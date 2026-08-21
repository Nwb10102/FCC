using Unity.Cinemachine;
using UnityEngine;

// 던전 입구방(뒤로 빠져나가기)과 출구방(클리어 후 나가기) 양쪽에 배치하는 공용 이탈 트리거.
// 카메라 경계를 오버월드로 되돌리고 생성된 던전을 통째로 해체한다.
//
// 전투방을 다 클리어하기 전에 이 트리거를 밟으면(입구방 뒷문) 보상 없이 나가며, 다음 입장 시
// 완전히 새로운 레이아웃이 생성된다 — 중간 상태를 보존하지 않는다(사용자 확정 사항 5번).
public class ArenaExitZone : MonoBehaviour {
    #region 인스펙터 변수

    // 방 프리팹 안에 들어 있는 경우 아래 둘은 씬 오브젝트를 가리킬 수 없어 비어 있다.
    // 생성 직후 ArenaDungeonGenerator.Configure가 채워 넣는다.
    public ArenaDungeonGenerator generator;
    public Collider2D outsideBounds; // 던전 밖(오버월드) 카메라 경계.

    [Tooltip("비워두면 씬에서 'Player_Camera'를 자동으로 찾습니다.")]
    public CinemachineConfiner2D confiner;
    public string playerTag = "Player";

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (confiner == null) {
            var go = GameObject.Find("Player_Camera");
            if (go != null) confiner = go.GetComponent<CinemachineConfiner2D>();
        }
    }

    // 프리팹은 씬 오브젝트를 참조할 수 없으므로 생성기가 인스턴스화 직후 주입한다.
    public void Configure(ArenaDungeonGenerator owner, Collider2D bounds) {
        generator = owner;
        outsideBounds = bounds;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;

        if (confiner != null && outsideBounds != null) {
            confiner.BoundingShape2D = outsideBounds;
            confiner.InvalidateBoundingShapeCache();
        }

        if (generator != null) generator.Teardown();
    }

    #endregion
}
