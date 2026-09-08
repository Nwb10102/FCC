using System.Collections.Generic;
using UnityEngine;

// 던전(뒷세계 등) 입구. 상호작용하면 던전을 새로 생성해 플레이어를 들여보내고, 전투방을 모두
// 클리어하면 기억 조각을 지급한다. 던전 안에서의 낙사·사망 처리는 DungeonRespawnController 에 맡긴다.
//
// **콜라이더(Is Trigger 체크)를 함께 붙여야 PlayerInteractor 가 탐지합니다.**
public class DungeonGate : MonoBehaviour, IInteractable {
    #region 인스펙터 변수

    [Header("식별")]
    // 클리어 여부를 기록하는 고유 id. **씬 안에서 겹치지 않게 지으세요.** 비우면 오브젝트 이름을 쓴다.
    public string dungeonId;

    [Header("프롬프트")]
    public string label = "들어가기";
    public Vector3 promptOffset = new(0f, 1.6f, 0f);

    [Header("던전")]
    public DungeonGenerator generator;

    [Header("복귀")]
    // 던전에서 걸어 나오거나 죽었을 때 플레이어가 설 위치. **빈 오브젝트를 만들어 연결하세요.** 비우면 이 게이트 위치.
    public Transform outsideReturnPoint;

    [Header("목표")]
    // 비어 있지 않으면 던전을 완전히 클리어한 순간 이 목표를 완료 처리한다(SaveMirror·DialogueTriggerZone 과 같은 방식).
    public string objectiveId;

    [Header("보상 · 사망 처리")]
    public int memoryShardReward = 1;

    [Tooltip("던전 안에서 죽으면 자아 게이지를 최대치의 이 비율로 되돌린다. 1이면 전량 회복.")]
    [Range(0f, 1f)] public float deathHealthRestoreRatio = 1f;

    [Tooltip("낙사할 때마다 깎을 자아 게이지. 0이면 페널티 없음.")]
    public int fallDamage = 10;

    [Tooltip("낙사로 복귀한 직후 무적으로 버틸 시간(초). 복귀 지점 근처의 몬스터에게 연달아 맞는 것을 막는다.")]
    public float fallInvincibleTime = 3f;

    [Header("거울 연출")]
    // 클리어하고 나온 순간 부서지는 전신 거울. 비우면 이 오브젝트와 자식에서 찾는다.
    public DungeonGateMirror mirror;

    #endregion
    #region 런타임 변수

    GameObject cachedPlayer;
    int combatRoomsRemaining; // 던전 한 판에 전투방이 여러 개 나올 수 있어 전부 클리어해야 보상을 준다.
    bool inDungeon;

    #endregion
    #region IInteractable

    public string InteractLabel => label;
    public bool CanInteract => !(DungeonManager.Instance != null && DungeonManager.Instance.IsCleared(dungeonId));
    public Vector3 PromptAnchor => transform.position + promptOffset;

    public void Interact(GameObject interactor) {
        if (inDungeon) return;
        if (generator == null) {
            Debug.LogError($"[DungeonGate] '{name}' — generator 가 연결되지 않아 던전을 생성할 수 없습니다.", this);
            return;
        }

        // PlayerInteractor 가 플레이어 루트가 아닌 자식에 붙어 있어도 동작하도록 루트를 찾아 쓴다 (SaveMirror 와 같은 이유).
        Health health = interactor.GetComponentInParent<Health>();
        cachedPlayer = health != null ? health.gameObject : interactor;

        List<DungeonRoom> rooms = generator.Generate(); // 매번 새 레이아웃.
        if (rooms.Count == 0) {
            Debug.LogError($"[DungeonGate] '{name}' — 생성된 방이 없습니다. 방 프리팹 풀을 확인하세요.", this);
            return;
        }

        inDungeon = true;

        // 전투방 클리어 카운트.
        combatRoomsRemaining = 0;
        foreach (DungeonRoom room in rooms) {
            if (room.role != DungeonRoom.RoomRole.CombatArena || room.spawner == null) continue;
            combatRoomsRemaining++;
            room.spawner.OnAllMonstersDefeated += HandleCombatRoomCleared;
        }

        // 걸어 나가기 처리. 중복 구독 방지.
        generator.OnDungeonExited -= HandleWalkOut;
        generator.OnDungeonExited += HandleWalkOut;

        // 던전 안 낙사·사망 처리 시작.
        DungeonRespawnController.Begin(cachedPlayer, generator.fallYThreshold, fallDamage, fallInvincibleTime, HandlePlayerDeath);

        // 입구방으로 이동 + 첫 방 리스폰.
        DungeonRoom entry = rooms[0];
        WarpTo(cachedPlayer, entry.entryAnchor != null ? entry.entryAnchor.position : entry.transform.position);

        if (DungeonRespawnController.Instance != null) DungeonRespawnController.Instance.SetCurrentRoom(entry);
    }

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (string.IsNullOrEmpty(dungeonId)) dungeonId = name;
        if (mirror == null) mirror = GetComponentInChildren<DungeonGateMirror>(true);
    }

    // 이미 클리어한 던전이면 거울이 처음부터 부서진 상태여야 한다. Awake 가 아니라 Start 인 이유는
    // DungeonManager 가 세이브에서 클리어 목록을 되돌리는 시점보다 뒤여야 하기 때문이다.
    void Start() {
        if (mirror == null) return;
        if (DungeonManager.Instance != null && DungeonManager.Instance.IsCleared(dungeonId)) mirror.SetBrokenImmediate();
    }

    void OnDestroy() {
        if (generator != null) generator.OnDungeonExited -= HandleWalkOut;
    }

    #endregion
    #region 클리어 판정 · 보상

    void HandleCombatRoomCleared() {
        combatRoomsRemaining = Mathf.Max(0, combatRoomsRemaining - 1);
        if (combatRoomsRemaining > 0) return; // 아직 안 끝난 전투방이 남아 있으면 보류.

        if (DungeonManager.Instance != null) DungeonManager.Instance.MarkCleared(dungeonId);

        if (cachedPlayer != null && cachedPlayer.TryGetComponent(out Player_MemoryShardInventory shards)) {
            shards.Add(memoryShardReward);
        }

        if (!string.IsNullOrEmpty(objectiveId) && ObjectiveManager.Instance != null) {
            ObjectiveManager.Instance.CompleteObjective(objectiveId);
        }
    }

    #endregion
    #region 이탈 처리

    // 자아 게이지 0 → 던전 밖으로 되돌리고 체력을 회복시킨다.
    void HandlePlayerDeath() {
        ExitDungeon(restoreHealth: true);
    }

    // 클리어 전에 입구/출구로 걸어 나감 → 그대로 밖으로. 체력은 건드리지 않는다.
    void HandleWalkOut() {
        ExitDungeon(restoreHealth: false);
    }

    void ExitDungeon(bool restoreHealth) {
        if (!inDungeon) return;
        inDungeon = false;

        if (generator != null) generator.Teardown();
        if (DungeonRespawnController.Instance != null) DungeonRespawnController.Instance.Dispose();

        Vector3 pos = outsideReturnPoint != null ? outsideReturnPoint.position : transform.position;
        WarpTo(cachedPlayer, pos);

        if (restoreHealth && cachedPlayer != null && cachedPlayer.TryGetComponent(out Health h)) {
            int target = Mathf.Max(1, Mathf.RoundToInt(h.MaxHealth * deathHealthRestoreRatio));
            h.SetHealth(target);
        }

        // 클리어하고 나왔을 때만 거울을 부순다. 클리어 전에 걸어 나온 것은 다시 들어갈 수 있어야 하므로
        // 입구가 그대로 남아 있어야 한다(CanInteract 도 같은 기준으로 열고 닫힌다).
        if (mirror != null && DungeonManager.Instance != null && DungeonManager.Instance.IsCleared(dungeonId)) {
            mirror.PlayBreak();
        }
    }

    void WarpTo(GameObject go, Vector3 pos) {
        if (go == null) return;

        go.transform.position = pos;
        if (go.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
        Gizmos.DrawWireSphere(PromptAnchor, 0.2f);

        Vector3 ret = outsideReturnPoint != null ? outsideReturnPoint.position : transform.position;
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(ret, 0.3f);
        Gizmos.DrawLine(transform.position, ret);
    }
#endif

    #endregion
}
