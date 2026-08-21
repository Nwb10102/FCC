using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// 뒷세계(반전 세계) 입구. 상호작용하면 던전을 새로 생성해 플레이어를 들여보내고,
// 전투방을 모두 클리어하면 기억 조각을 지급한다.
//
// **콜라이더(Is Trigger 체크)를 함께 붙여야 PlayerInteractor가 탐지합니다.**
public class ArenaGate : MonoBehaviour, IInteractable {
    #region 인스펙터 변수

    [Header("식별")]
    // 클리어 여부를 기록하는 고유 id. **씬 안에서 겹치지 않게 지으세요.** 비워두면 오브젝트 이름을 쓴다.
    public string arenaId;

    [Header("프롬프트")]
    public string label = "들어가기";
    public Vector3 promptOffset = new(0f, 1.6f, 0f);

    [Header("던전")]
    public ArenaDungeonGenerator generator;

    [Header("보상")]
    public int memoryShardReward = 1;

    #endregion
    #region 컴포넌트 변수

    CinemachineConfiner2D confiner;
    GameObject cachedInteractor;
    int combatRoomsRemaining; // 던전 한 판에 CombatArena가 여러 개 나올 수 있어(min~maxCombatRooms) 전부 클리어해야 보상을 준다.

    #endregion
    #region IInteractable

    public string InteractLabel => label;
    public bool CanInteract => !(ArenaManager.Instance != null && ArenaManager.Instance.IsCleared(arenaId));
    public Vector3 PromptAnchor => transform.position + promptOffset;

    public void Interact(GameObject interactor) {
        if (generator == null) {
            Debug.LogError($"[ArenaGate] '{name}' — generator가 연결되지 않아 던전을 생성할 수 없습니다.", this);
            return;
        }

        // PlayerInteractor가 플레이어 루트가 아닌 자식에 붙어 있어도 동작하도록 루트를 찾아 쓴다 (SaveMirror와 같은 이유).
        Health health = interactor.GetComponentInParent<Health>();
        cachedInteractor = health != null ? health.gameObject : interactor;

        List<DungeonRoom> rooms = generator.Generate(); // 매번 새 레이아웃 (던전을 세이브에 고정하지 않는다).

        // 몬스터 스폰은 더 이상 여기서 하지 않는다 — 플레이어가 실제로 각 CombatArena 방에 걸어 들어간
        // 순간 DungeonRoom.StartCombatLockIn이 스폰과 락인 바리어를 함께 처리한다.
        combatRoomsRemaining = 0;
        foreach (DungeonRoom room in rooms) {
            if (room.role != DungeonRoom.RoomRole.CombatArena || room.spawner == null) continue;

            combatRoomsRemaining++;
            room.spawner.OnAllMonstersDefeated += HandleCombatRoomCleared;
        }

        WarpToEntry(cachedInteractor, rooms[0]);
        ApplyConfiner(rooms[0]);
    }

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        var go = GameObject.Find("Player_Camera");
        if (go != null) confiner = go.GetComponent<CinemachineConfiner2D>();

        if (string.IsNullOrEmpty(arenaId)) arenaId = name;
    }

    #endregion
    #region 진입 처리

    void WarpToEntry(GameObject interactor, DungeonRoom entryRoom) {
        if (entryRoom.entryAnchor == null) return;

        interactor.transform.position = entryRoom.entryAnchor.position;
        if (interactor.TryGetComponent(out Rigidbody2D rigid)) rigid.linearVelocity = Vector2.zero;
    }

    void ApplyConfiner(DungeonRoom entryRoom) {
        if (confiner == null || entryRoom.cameraBounds == null) return;

        confiner.BoundingShape2D = entryRoom.cameraBounds;
        confiner.InvalidateBoundingShapeCache();
    }

    #endregion
    #region 클리어 판정 · 보상

    void HandleCombatRoomCleared() {
        combatRoomsRemaining = Mathf.Max(0, combatRoomsRemaining - 1);
        if (combatRoomsRemaining > 0) return; // 아직 안 끝난 CombatArena가 남아 있으면 보상 보류.

        if (ArenaManager.Instance != null) ArenaManager.Instance.MarkCleared(arenaId);

        if (cachedInteractor != null && cachedInteractor.TryGetComponent(out Player_MemoryShardInventory shards))
            shards.Add(memoryShardReward);
    }

    #endregion
}
