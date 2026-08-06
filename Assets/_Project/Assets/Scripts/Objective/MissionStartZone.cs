using UnityEngine;

// 미션이 시작되는 지점. 플레이어가 이 영역에 들어오면 물려둔 미션이 열린다.
//
// 미션 데이터 자체는 에셋(Mission)에 있고 이 컴포넌트는 참조만 든다. 그래서 같은 미션을
// 다른 진입 지점에서도 시작시킬 수 있고, 세이브에는 미션 id만 기록된다.
//
// **콜라이더(Is Trigger 체크)를 함께 붙이세요.** 없으면 플레이어가 지나가도 아무 일도 일어나지 않습니다.
public class MissionStartZone : MonoBehaviour {
    #region 인스펙터 변수

    [Header("시작할 미션")]
    // **이 미션은 ObjectiveManager의 missions 목록에도 들어 있어야 합니다.** 없으면 경고만 찍고 넘어간다.
    public Mission mission;

    [Header("트리거")]
    public bool playOnce = true; // 한 번 시작하면 다시 반응하지 않는다. (미션 쪽에서도 중복 시작은 무시된다)
    public string playerTag = "Player";

    #endregion
    #region 런타임 변수

    bool started;

    #endregion
    #region 유니티 라이프 사이클

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && started) return;

        if (mission == null) {
            Debug.LogWarning($"[MissionStartZone] '{name}'에 시작할 미션이 비어 있습니다.", this);
            return;
        }

        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (ObjectiveManager.Instance == null) {
            Debug.LogWarning($"[MissionStartZone] '{name}' — 씬에 ObjectiveManager가 없어 미션을 시작할 수 없습니다.", this);
            return;
        }

        started = true;
        ObjectiveManager.Instance.StartMission(mission);
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    // 완료 구역(ObjectiveZoneTrigger, 초록)과 구분되도록 파란색으로 그린다.
    void OnDrawGizmos() {
        if (!TryGetComponent<Collider2D>(out Collider2D col)) return;

        Gizmos.color = new Color(0.35f, 0.6f, 1f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.35f, 0.6f, 1f, 0.85f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif

    #endregion
}
