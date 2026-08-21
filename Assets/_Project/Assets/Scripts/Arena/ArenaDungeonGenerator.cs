using System.Collections.Generic;
using UnityEngine;

// 역할별 고정 슬롯을 소켓 정렬로 이어 붙이는 던전 생성기.
// 항상 [입구] → [플랫포밍 통로] → (선택)[곁가지 보너스방] → [전투방] → [수직 갱도] → [출구]
// 순서로 조립한다 — 순서 자체는 고정하고 곁가지 유무만 랜덤화하는 이유는, 2D 플랫포머 특성상
// 어떤 조합이라도 지형이 항상 이어지도록 보장하기 위함이다(순수 알고리즘 타일 생성과 달리
// 클리어 불가능한 배치가 나올 수 없다).
//
// 소켓 정렬(entryAnchor/exitAnchor 좌표 맞추기)은 방향에 무관하게 동작하므로, 방 프리팹의
// 앵커를 좌우가 아니라 상하에 둬도 그대로 이어붙는다(VerticalClimb가 이 방식을 쓴다).
public class ArenaDungeonGenerator : MonoBehaviour {
    #region 인스펙터 변수

    [Header("방 프리팹 풀 (각 카테고리별 2종 이상 등록 권장)")]
    public List<GameObject> entryRoomPrefabs = new();
    public List<GameObject> hazardRoomPrefabs = new();
    public List<GameObject> verticalRoomPrefabs = new();
    public List<GameObject> combatArenaRoomPrefabs = new();
    public List<GameObject> secretBranchRoomPrefabs = new();
    public List<GameObject> exitRoomPrefabs = new();

    [Header("단일 프리팹 (풀이 비어있을 때 사용하는 레거시/폴백)")]
    public GameObject entryRoomPrefab;
    public GameObject hazardRoomPrefab;
    public GameObject combatArenaRoomPrefab;
    public GameObject verticalClimbRoomPrefab;
    public GameObject secretBranchRoomPrefab;
    public GameObject exitRoomPrefab;

    [Header("생성 수량 및 확률 설정")]
    [Range(1, 4)] public int minHazardRooms = 1;
    [Range(1, 4)] public int maxHazardRooms = 2;
    [Range(1, 4)] public int minCombatRooms = 1;
    [Range(1, 4)] public int maxCombatRooms = 2;
    [Range(1, 4)] public int minVerticalRooms = 1;
    [Range(1, 4)] public int maxVerticalRooms = 2;
    [Range(0f, 1f)] public float secretBranchChance = 0.6f;

    [Header("생성 위치")]
    public Transform dungeonOrigin; // **오버월드와 겹치지 않는 좌표에 빈 오브젝트를 만들어 연결하세요.**

    [Header("연결")]
    // 던전을 나갈 때 카메라를 되돌릴 오버월드 경계. **CoreScene의 TestConfider 콜라이더를 연결하세요.**
    // 방 프리팹 안의 ArenaExitZone은 씬 오브젝트를 참조할 수 없어, 생성 직후 이 값을 주입해 준다.
    public Collider2D outsideBounds;

    #endregion
    #region 런타임 변수

    Transform generatedRoot;

    #endregion
    #region 생성 · 해체

    public List<DungeonRoom> Generate() {
        Teardown(); // 이전 잔여 인스턴스가 있으면 먼저 정리한다.

        generatedRoot = new GameObject("GeneratedDungeon").transform;
        generatedRoot.position = dungeonOrigin != null ? dungeonOrigin.position : Vector3.zero;

        List<DungeonRoom> rooms = new();

        // 1. [도입] 입구방
        GameObject entryPrefab = PickPrefab(entryRoomPrefabs, entryRoomPrefab);
        DungeonRoom current = SpawnRoom(entryPrefab, null);
        if (current != null) rooms.Add(current);

        // 2. [전개] 플랫포밍 기믹방 (1~N개)
        int hazardCount = Random.Range(minHazardRooms, maxHazardRooms + 1);
        foreach (GameObject prefab in PickPrefabs(hazardRoomPrefabs, hazardRoomPrefab, hazardCount)) {
            current = SpawnRoom(prefab, current);
            if (current != null) {
                rooms.Add(current);
                TrySpawnSecretBranch(current);
            }
        }

        // 3. [전투] 아레나 락인 전투방 (1~N개)
        int combatCount = Random.Range(minCombatRooms, maxCombatRooms + 1);
        foreach (GameObject prefab in PickPrefabs(combatArenaRoomPrefabs, combatArenaRoomPrefab, combatCount)) {
            current = SpawnRoom(prefab, current);
            if (current != null) rooms.Add(current);
        }

        // 4. [심화] 수직 상승/하강 갱도 (1~N개)
        int verticalCount = Random.Range(minVerticalRooms, maxVerticalRooms + 1);
        foreach (GameObject prefab in PickPrefabs(verticalRoomPrefabs, verticalClimbRoomPrefab, verticalCount)) {
            current = SpawnRoom(prefab, current);
            if (current != null) {
                rooms.Add(current);
                TrySpawnSecretBranch(current);
            }
        }

        // 5. [절정/출구] 보스 전야 / 탈출 출구방
        GameObject exitPrefab = PickPrefab(exitRoomPrefabs, exitRoomPrefab);
        current = SpawnRoom(exitPrefab, current);
        if (current != null) rooms.Add(current);

        return rooms;
    }

    void TrySpawnSecretBranch(DungeonRoom parent) {
        if (parent == null || parent.branchAnchor == null) return;
        if (Random.value < secretBranchChance) {
            GameObject secretPrefab = PickPrefab(secretBranchRoomPrefabs, secretBranchRoomPrefab);
            if (secretPrefab != null) {
                SpawnBranch(secretPrefab, parent);
            }
        }
    }

    // 생성된 방을 통째로 치운다. generatedRoot 하나만 Destroy하면 그 아래 방·몬스터가 전부 함께 사라진다.
    public void Teardown() {
        if (generatedRoot == null) return;

        Destroy(generatedRoot.gameObject);
        generatedRoot = null;
    }

    #endregion
    #region 방 배치

    // previous가 있으면 새 방의 entryAnchor를 previous의 exitAnchor 위치에 맞춰 통째로 옮긴다.
    // 소켓 정렬 방식이라 어떤 방 조합이라도 지형이 항상 이어진다.
    DungeonRoom SpawnRoom(GameObject prefab, DungeonRoom previous) {
        GameObject instance = Instantiate(prefab, generatedRoot);
        DungeonRoom room = instance.GetComponent<DungeonRoom>();

        if (previous == null) {
            instance.transform.position = generatedRoot.position;
        }
        else if (room.entryAnchor != null && previous.exitAnchor != null) {
            Vector3 delta = previous.exitAnchor.position - room.entryAnchor.position;
            instance.transform.position += delta;
        }
        else {
            Debug.LogWarning($"[ArenaDungeonGenerator] '{prefab.name}' 또는 이전 방에 소켓(entryAnchor/exitAnchor)이 비어 있어 정렬하지 못했습니다.", this);
        }

        ConfigureExitZones(instance);
        return room;
    }

    // 막다른 곁가지 방. parent의 branchAnchor에 맞춰 배치하고, 메인 경로 rooms 목록에는 넣지 않는다
    // (전투방 카운트·클리어 판정과 무관한 순수 보너스이기 때문).
    void SpawnBranch(GameObject prefab, DungeonRoom parent) {
        if (parent.branchAnchor == null) return; // 이 방은 분기를 지원하지 않음.

        GameObject instance = Instantiate(prefab, generatedRoot);
        DungeonRoom room = instance.GetComponent<DungeonRoom>();

        if (room.entryAnchor != null) {
            Vector3 delta = parent.branchAnchor.position - room.entryAnchor.position;
            instance.transform.position += delta;
        }
        else {
            Debug.LogWarning($"[ArenaDungeonGenerator] 곁가지 '{prefab.name}'에 entryAnchor가 없어 정렬하지 못했습니다.", this);
        }

        ConfigureExitZones(instance);
    }

    void ConfigureExitZones(GameObject instance) {
        foreach (ArenaExitZone zone in instance.GetComponentsInChildren<ArenaExitZone>(true)) {
            zone.Configure(this, outsideBounds);
        }
    }

    #endregion
    #region 랜덤 선택

    // 풀에 프리팹이 있으면 그중 하나를 무작위로, 비어 있으면 폴백 하나를 쓴다.
    GameObject PickPrefab(List<GameObject> pool, GameObject fallback) {
        if (pool != null && pool.Count > 0) return pool[Random.Range(0, pool.Count)];
        return fallback;
    }

    // count만큼 뽑는다. 셔플 뭉치 방식이라 풀을 다 쓰기 전까지는 같은 방이 연속으로 나오지 않는다.
    List<GameObject> PickPrefabs(List<GameObject> pool, GameObject fallback, int count) {
        List<GameObject> result = new(count);
        List<GameObject> source = (pool != null && pool.Count > 0) ? pool : (fallback != null ? new List<GameObject> { fallback } : new List<GameObject>());
        if (source.Count == 0) return result;

        List<GameObject> bucket = new();
        while (result.Count < count) {
            if (bucket.Count == 0) {
                bucket.AddRange(source);
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
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion
}
