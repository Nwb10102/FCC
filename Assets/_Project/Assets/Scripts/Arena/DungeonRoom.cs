using Unity.Cinemachine;
using UnityEngine;

// 뒷세계 던전을 이루는 방 프리팹 하나. entryAnchor / exitAnchor로 소켓을 규격화해
// ArenaDungeonGenerator가 방을 순서대로 이어 붙일 수 있게 한다. 정렬은 두 앵커의 월드 좌표를
// 맞추는 것뿐이라 좌우뿐 아니라 상하로도 그대로 동작한다(VerticalClimb는 바닥→천장으로 앵커를 둔다).
// 플레이어가 이 방에 들어오면 카메라 Confiner를 이 방의 경계로 즉시 스왑한다.
public class DungeonRoom : MonoBehaviour {
    public enum RoomRole { Entry, PlatformingHazard, CombatArena, VerticalClimb, SecretBranch, Exit } // 방 종류.

    #region 인스펙터 변수

    [Header("역할")]
    public RoomRole role;

    [Header("소켓")]
    public Transform entryAnchor; // 이전 방의 exitAnchor에 맞춰 이 방이 배치되는 기준점.
    public Transform exitAnchor;  // 다음 방이 이 지점에 맞춰 배치됨.
    public Transform branchAnchor; // 이 방에서 SecretBranch가 갈라지는 지점. 비워두면 이 방은 분기를 지원하지 않는다.

    [Header("카메라")]
    public Collider2D cameraBounds; // 이 방에 머무는 동안 카메라를 가둘 콜라이더 (Confiner용).
    public Collider2D roomTrigger;  // 플레이어 진입 감지용 (Is Trigger). 비워두면 자기 자신의 Collider2D를 쓴다.

    [Header("전투 (역할이 CombatArena일 때만)")]
    public ArenaMonsterSpawner spawner;
    public Collider2D lockBarrier; // 입장 시 활성화(통행 차단)되고, 전멸 시 비활성화된다.

    #endregion
    #region 컴포넌트 변수

    CinemachineConfiner2D confiner;
    bool combatStarted; // 같은 방에 다시 들어와도 몬스터가 중복 스폰되지 않도록 막는 가드.

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        var go = GameObject.Find("Player_Camera");
        if (go != null) confiner = go.GetComponent<CinemachineConfiner2D>();

        if (roomTrigger == null) roomTrigger = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        if (confiner != null && cameraBounds != null) {
            confiner.BoundingShape2D = cameraBounds;
            confiner.InvalidateBoundingShapeCache();
        }

        if (role == RoomRole.CombatArena) StartCombatLockIn();
    }

    #endregion
    #region 전투방 락인

    // 플레이어가 실제로 이 방에 걸어 들어온 순간 몬스터를 스폰하고 진행 방향 바리어를 잠근다.
    // 게이트 진입 즉시 전체 선스폰하지 않는 이유는, 락인 연출과 맞물리려면 "방에 들어온 순간"이어야 하기 때문이다.
    void StartCombatLockIn() {
        if (combatStarted || spawner == null) return;
        combatStarted = true;

        if (lockBarrier != null) lockBarrier.enabled = true;
        spawner.OnAllMonstersDefeated += HandleCombatCleared;
        spawner.SpawnAll();
    }

    void HandleCombatCleared() {
        if (lockBarrier != null) lockBarrier.enabled = false;
    }

    #endregion
}
