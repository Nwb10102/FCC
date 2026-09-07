using System;
using System.Collections.Generic;
using UnityEngine;

// 던전 몬스터 풀의 한 항목. 방 스폰 엔트리에 몬스터를 비워두면 여기서 가중치로 뽑는다.
[Serializable]
public class DungeonMonsterOption {
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
}

// 역할별 방을 소켓 정렬로 이어 붙이는 던전 생성기.
// 항상 [입구] → [플랫포밍 통로] → (선택)[곁가지 보너스] → [전투방] → [수직 갱도] → [출구] 순서로 조립한다.
// 순서를 고정하고 곁가지 유무만 랜덤화하는 이유는, 2D 플랫포머 특성상 어떤 조합이라도 지형이 항상
// 이어지도록 보장하기 위함이다(순수 알고리즘 타일 생성과 달리 클리어 불가능한 배치가 나올 수 없다).
//
// 방 프리팹은 "몬스터가 놓일 수 있는 자리 / 카메라 / 리스폰" 을 정의하고, 이 생성기는 "그중 얼마를 실제로
// 쓸지(몬스터 배율)" 와 "던전 밖 경계 · 낙사 높이" 같은 던전 단위 값을 정한다.
public class DungeonGenerator : MonoBehaviour {
    #region 인스펙터 변수 — 방 프리팹 풀

    [Header("방 프리팹 풀 (카테고리별 2종 이상 등록 권장)")]
    public List<GameObject> entryRoomPrefabs = new();
    public List<GameObject> hazardRoomPrefabs = new();
    public List<GameObject> verticalRoomPrefabs = new();
    public List<GameObject> combatRoomPrefabs = new();
    public List<GameObject> secretRoomPrefabs = new();
    public List<GameObject> exitRoomPrefabs = new();

    #endregion
    #region 인스펙터 변수 — 방 개수

    [Header("방 개수")]
    [Range(1, 4)] public int minHazardRooms = 1;
    [Range(1, 4)] public int maxHazardRooms = 2;
    [Range(1, 4)] public int minCombatRooms = 1;
    [Range(1, 4)] public int maxCombatRooms = 2;
    [Range(1, 4)] public int minVerticalRooms = 1;
    [Range(1, 4)] public int maxVerticalRooms = 2;
    [Range(0f, 1f)] public float secretBranchChance = 0.6f;

    #endregion
    #region 인스펙터 변수 — 몬스터 양 (던전 단위)

    [Header("몬스터 양")]
    [Tooltip("방에 적힌 스폰 지점 개수에 곱한다. 1이면 방 그대로, 0.5면 절반, 2면 두 배. 같은 방을 챕터마다 다른 밀도로 재사용할 수 있다.")]
    [Range(0f, 3f)] public float monsterCountMultiplier = 1f;

    [Tooltip("배율을 올려도 한 방에 이 수를 넘겨 스폰하지 않는다.")]
    public int maxMonstersPerRoom = 12;

    [Tooltip("방 스폰 엔트리에 몬스터를 비워두면 여기서 가중치로 뽑는다.")]
    public List<DungeonMonsterOption> dungeonMonsterPool = new();

    #endregion
    #region 인스펙터 변수 — 위치 · 경계 · 낙사

    [Header("생성 위치 · 경계")]
    // **오버월드와 겹치지 않는 좌표에 빈 오브젝트를 만들어 연결하세요.**
    public Transform dungeonOrigin;

    // 던전을 나갈 때 카메라를 되돌릴 오버월드 경계. **CoreScene 의 오버월드 Confiner 콜라이더를 연결하세요.**
    public Collider2D outsideBounds;

    [Header("낙사")]
    [Tooltip("플레이어가 이 월드 Y 아래로 떨어지면 현재 방 리스폰 지점으로 되돌린다. 방마다 바닥 높이가 달라도 하나로 처리하려고 던전 단위로 둔다.")]
    public float fallYThreshold = -50f;

    #endregion
    #region 이벤트

    // 플레이어가 입구/출구 이탈 트리거를 밟았을 때. DungeonGate 가 구독해 밖으로 되돌린다.
    public event Action OnDungeonExited;

    #endregion
    #region 런타임 변수

    Transform generatedRoot;

    #endregion
    #region 생성 · 해체

    public List<DungeonRoom> Generate() {
        Teardown(); // 이전 잔여 인스턴스가 있으면 먼저 정리한다.

        generatedRoot = new GameObject("GeneratedDungeon").transform;
        generatedRoot.position = dungeonOrigin != null ? dungeonOrigin.position : Vector3.zero;

        var rooms = new List<DungeonRoom>();

        // 1. 입구
        DungeonRoom current = SpawnRoom(PickPrefab(entryRoomPrefabs), null);
        if (current != null) rooms.Add(current);

        // 2. 플랫포밍 기믹방
        int hazardCount = UnityEngine.Random.Range(minHazardRooms, maxHazardRooms + 1);
        foreach (GameObject prefab in PickPrefabs(hazardRoomPrefabs, hazardCount)) {
            current = SpawnRoom(prefab, current);
            if (current == null) continue;
            rooms.Add(current);
            TrySpawnSecretBranch(current);
        }

        // 3. 전투방
        int combatCount = UnityEngine.Random.Range(minCombatRooms, maxCombatRooms + 1);
        foreach (GameObject prefab in PickPrefabs(combatRoomPrefabs, combatCount)) {
            current = SpawnRoom(prefab, current);
            if (current != null) rooms.Add(current);
        }

        // 4. 수직 갱도
        int verticalCount = UnityEngine.Random.Range(minVerticalRooms, maxVerticalRooms + 1);
        foreach (GameObject prefab in PickPrefabs(verticalRoomPrefabs, verticalCount)) {
            current = SpawnRoom(prefab, current);
            if (current == null) continue;
            rooms.Add(current);
            TrySpawnSecretBranch(current);
        }

        // 5. 출구
        current = SpawnRoom(PickPrefab(exitRoomPrefabs), current);
        if (current != null) rooms.Add(current);

        InjectRoomConfig();
        return rooms;
    }

    // 생성된 방을 통째로 치운다. generatedRoot 하나만 Destroy 하면 그 아래 방·몬스터가 전부 함께 사라진다.
    public void Teardown() {
        if (generatedRoot == null) return;

        Destroy(generatedRoot.gameObject);
        generatedRoot = null;
    }

    // 입구/출구 이탈 트리거가 호출한다. 게이트에 알린 뒤 던전을 해체한다.
    public void NotifyExit() {
        OnDungeonExited?.Invoke();
        Teardown();
    }

    #endregion
    #region 던전 값 주입

    // 프리팹은 씬·던전 값을 참조할 수 없으므로, 생성 직후 모든 스폰어·이탈 트리거에 던전 값을 넣어 준다.
    void InjectRoomConfig() {
        if (generatedRoot == null) return;

        foreach (DungeonRoomSpawner spawner in generatedRoot.GetComponentsInChildren<DungeonRoomSpawner>(true)) {
            spawner.injectedPool = dungeonMonsterPool;
            spawner.countMultiplier = monsterCountMultiplier;
            spawner.perRoomCap = maxMonstersPerRoom;
            spawner.monsterParent = generatedRoot;
        }

        foreach (DungeonExitZone zone in generatedRoot.GetComponentsInChildren<DungeonExitZone>(true)) {
            zone.Configure(this);
        }
    }

    #endregion
    #region 방 배치

    // previous 가 있으면 새 방의 entryAnchor 를 previous 의 exitAnchor 위치에 맞춰 통째로 옮긴다.
    DungeonRoom SpawnRoom(GameObject prefab, DungeonRoom previous) {
        if (prefab == null) {
            Debug.LogWarning("[DungeonGenerator] 방 프리팹 풀 중 하나가 비어 있어 그 단계를 건너뜁니다.", this);
            return null;
        }

        GameObject instance = Instantiate(prefab, generatedRoot);
        DungeonRoom room = instance.GetComponent<DungeonRoom>();
        if (room == null) {
            Debug.LogError($"[DungeonGenerator] '{prefab.name}' 에 DungeonRoom 이 없습니다.", this);
            Destroy(instance);
            return null;
        }

        if (previous == null) {
            instance.transform.position = generatedRoot.position;
        }
        else if (room.entryAnchor != null && previous.exitAnchor != null) {
            instance.transform.position += previous.exitAnchor.position - room.entryAnchor.position;
        }
        else {
            Debug.LogWarning($"[DungeonGenerator] '{prefab.name}' 또는 이전 방에 소켓(entryAnchor/exitAnchor)이 비어 있어 정렬하지 못했습니다.", this);
        }

        return room;
    }

    void TrySpawnSecretBranch(DungeonRoom parent) {
        if (parent == null || parent.branchAnchor == null) return;
        if (UnityEngine.Random.value >= secretBranchChance) return;

        GameObject prefab = PickPrefab(secretRoomPrefabs);
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, generatedRoot);
        DungeonRoom room = instance.GetComponent<DungeonRoom>();

        if (room != null && room.entryAnchor != null) {
            instance.transform.position += parent.branchAnchor.position - room.entryAnchor.position;
        }
        else {
            Debug.LogWarning($"[DungeonGenerator] 곁가지 '{prefab.name}' 에 entryAnchor 가 없어 정렬하지 못했습니다.", this);
        }
    }

    #endregion
    #region 랜덤 선택

    GameObject PickPrefab(List<GameObject> pool) {
        if (pool == null || pool.Count == 0) return null;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    // count 만큼 뽑는다. 셔플 뭉치 방식이라 풀을 다 쓰기 전까지는 같은 방이 연속으로 나오지 않는다.
    List<GameObject> PickPrefabs(List<GameObject> pool, int count) {
        var result = new List<GameObject>(Mathf.Max(0, count));
        if (pool == null || pool.Count == 0 || count <= 0) return result;

        var bucket = new List<GameObject>();
        while (result.Count < count) {
            if (bucket.Count == 0) {
                bucket.AddRange(pool);
                Shuffle(bucket);
            }
            int last = bucket.Count - 1;
            result.Add(bucket[last]);
            bucket.RemoveAt(last);
        }
        return result;
    }

    void Shuffle(List<GameObject> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion
    #region 에디터 표시

#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (dungeonOrigin != null) {
            Gizmos.color = new Color(1f, 0.4f, 0.9f, 0.9f);
            Gizmos.DrawWireCube(dungeonOrigin.position, new Vector3(3f, 3f, 0f));
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 left = new(-1000f, fallYThreshold, 0f);
        Vector3 right = new(1000f, fallYThreshold, 0f);
        Gizmos.DrawLine(left, right); // 낙사 기준선.
    }
#endif

    #endregion
}
