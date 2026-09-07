#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 던전 방 템플릿 프리팹과 씬 던전 리그를 한 번에 찍어내는 에디터 도구.
//
// 방을 스크립트로 조립하지 않는다는 규칙을 지키려면 실물 프리팹이 있어야 한다. 이 도구는 소켓·스폰 지점·
// 카메라 경계·리스폰 지점이 전부 배선된 방 하나를 만들어 준다. 실제 지형·개수·카메라 값은 프리팹을
// 복제해 인스펙터에서 손본다. **다시 실행하면 템플릿 프리팹을 덮어씁니다.**
//
// 사용법: Tools ▸ FCC ▸ Dungeon ▸ Build Room Template → 프리팹 복제 후 편집
//         Tools ▸ FCC ▸ Dungeon ▸ Place Dungeon Rig In Scene → 게이트·생성기·조정자 배치
public static class DungeonRoomPrefabBuilder {
    #region 경로 · 크기

    const string PrefabDir = "Assets/_Project/Assets/Prefabs/Dungeon";
    const string RoomPrefabPath = PrefabDir + "/Room_Template.prefab";

    const float RoomWidth = 48f;
    const float RoomHeight = 24f;

    #endregion
    #region 메뉴 — 방 템플릿

    [MenuItem("Tools/FCC/Dungeon/Build Room Template")]
    public static void BuildRoomTemplate() {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) {
            Debug.LogError("[Dungeon] 프리팹 편집 모드를 닫고 다시 실행하세요. 임시 오브젝트가 편집 중인 프리팹에 섞여 들어갑니다.");
            return;
        }

        EnsureFolder(PrefabDir);

        GameObject root = new GameObject("Room_Template");

        // 방 전체를 덮는 진입 트리거 (루트에 둔다 — 자식에 두면 루트에 Rigidbody2D 가 있어야 트리거 콜백이 온다).
        BoxCollider2D roomTrigger = root.AddComponent<BoxCollider2D>();
        roomTrigger.isTrigger = true;
        roomTrigger.size = new Vector2(RoomWidth, RoomHeight);

        // 소켓 · 리스폰.
        Transform entry = Child(root, "EntryAnchor", new Vector3(-RoomWidth / 2f, 0f, 0f));
        Transform exit = Child(root, "ExitAnchor", new Vector3(RoomWidth / 2f, 0f, 0f));
        Transform branch = Child(root, "BranchAnchor", new Vector3(0f, RoomHeight / 2f, 0f));
        Transform respawn = Child(root, "RespawnPoint", new Vector3(-RoomWidth / 2f + 3f, -RoomHeight / 2f + 3f, 0f));

        // 카메라 경계.
        GameObject camBoundsObj = new GameObject("CameraBounds");
        camBoundsObj.transform.SetParent(root.transform, false);
        BoxCollider2D camBounds = camBoundsObj.AddComponent<BoxCollider2D>();
        camBounds.isTrigger = true;
        camBounds.size = new Vector2(RoomWidth, RoomHeight);

        // 방 안 세부 카메라 구역 (예시 1개).
        GameObject zoneParent = new GameObject("CameraZones");
        zoneParent.transform.SetParent(root.transform, false);
        GameObject zoneObj = new GameObject("Zone_A");
        zoneObj.transform.SetParent(zoneParent.transform, false);
        BoxCollider2D zoneCol = zoneObj.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(RoomWidth / 3f, RoomHeight);
        DungeonCameraZone zone = zoneObj.AddComponent<DungeonCameraZone>();
        zone.settings.orthographicSize = 11f;

        // 스폰 지점 4개.
        var spawnPoints = new Transform[4];
        for (int i = 0; i < spawnPoints.Length; i++) {
            float x = -RoomWidth / 2f + RoomWidth * (i + 1) / (spawnPoints.Length + 1);
            spawnPoints[i] = Child(root, $"SpawnPoint_{i + 1}", new Vector3(x, -RoomHeight / 2f + 2f, 0f));
        }

        // 전투 락인 바리어 (통행 차단 — 비트리거).
        GameObject barrierObj = new GameObject("LockBarrier");
        barrierObj.transform.SetParent(root.transform, false);
        barrierObj.transform.localPosition = new Vector3(RoomWidth / 2f, 0f, 0f);
        BoxCollider2D barrier = barrierObj.AddComponent<BoxCollider2D>();
        barrier.size = new Vector2(1f, RoomHeight);
        barrier.enabled = false; // 입장 시 DungeonRoom 이 켠다.

        // 바닥 · 천장 (스케일 Quad + 콜라이더 — 기존 Arena 방과 같은 방식).
        Quad(root, "Floor", new Vector3(0f, -RoomHeight / 2f, 0f), new Vector3(RoomWidth, 1f, 1f));
        Quad(root, "Ceiling", new Vector3(0f, RoomHeight / 2f, 0f), new Vector3(RoomWidth, 1f, 1f));

        // 컴포넌트 배선.
        DungeonRoomCamera roomCam = root.AddComponent<DungeonRoomCamera>();
        roomCam.settings.bounds = camBounds;
        roomCam.settings.orthographicSize = 8f;

        DungeonRoomSpawner spawner = root.AddComponent<DungeonRoomSpawner>();
        foreach (Transform p in spawnPoints) {
            spawner.entries.Add(new DungeonRoomSpawner.SpawnEntry { point = p });
        }

        DungeonRoom room = root.AddComponent<DungeonRoom>();
        room.role = DungeonRoom.RoomRole.CombatArena;
        room.entryAnchor = entry;
        room.exitAnchor = exit;
        room.branchAnchor = branch;
        room.respawnPoint = respawn;
        room.roomTrigger = roomTrigger;
        room.lockBarrier = barrier;
        room.roomCamera = roomCam;
        room.spawner = spawner;

        PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Dungeon] 방 템플릿을 만들었습니다.\n· {RoomPrefabPath}\n" +
            "이 프리팹을 카테고리별로 복제(Room_Combat_A 등)해 지형·스폰·카메라 값을 편집한 뒤 DungeonGenerator 풀에 등록하세요.");
    }

    #endregion
    #region 메뉴 — 씬 던전 리그

    [MenuItem("Tools/FCC/Dungeon/Place Dungeon Rig In Scene")]
    public static void PlaceDungeonRig() {
        foreach (DungeonGenerator existing in Object.FindObjectsByType<DungeonGenerator>(FindObjectsInactive.Include)) {
            Debug.Log($"[Dungeon] 씬에 이미 '{existing.name}' 이 있어 새로 놓지 않았습니다.", existing.gameObject);
            return;
        }

        GameObject rig = new GameObject("DungeonRig");
        Undo.RegisterCreatedObjectUndo(rig, "Place Dungeon Rig");

        GameObject origin = new GameObject("DungeonOrigin");
        origin.transform.SetParent(rig.transform, false);
        origin.transform.position = new Vector3(500f, -500f, 0f); // **오버월드와 겹치지 않는 곳으로 옮기세요.**

        var generator = rig.AddComponent<DungeonGenerator>();
        generator.dungeonOrigin = origin.transform;

        rig.AddComponent<DungeonCameraDirector>();
        rig.AddComponent<DungeonManager>();

        GameObject gateObj = new GameObject("DungeonGate_Backworld");
        gateObj.transform.SetParent(rig.transform, false);
        BoxCollider2D gateCol = gateObj.AddComponent<BoxCollider2D>();
        gateCol.isTrigger = true;
        gateCol.size = new Vector2(2f, 3f);
        var gate = gateObj.AddComponent<DungeonGate>();
        gate.generator = generator;

        Selection.activeGameObject = rig;
        EditorSceneManager.MarkSceneDirty(rig.scene);

        Debug.Log("[Dungeon] 씬에 DungeonRig(생성기·조정자·매니저·게이트)를 놓았습니다. " +
            "DungeonOrigin 위치, 방 프리팹 풀, outsideBounds, 게이트 위치를 채운 뒤 씬을 저장하세요.");
    }

    #endregion
    #region 생성 도우미

    static Transform Child(GameObject parent, string name, Vector3 localPos) {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = localPos;
        return obj.transform;
    }

    static void Quad(GameObject parent, string name, Vector3 localPos, Vector3 localScale) {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = name;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        // 3D 콜라이더는 2D 물리에서 안 쓰이므로 교체한다.
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.AddComponent<BoxCollider2D>();
    }

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    #endregion
}
#endif
