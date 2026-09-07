#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 던전 방 프리팹 세트를 한 번에 찍어내는 에디터 도구.
//
// 방을 런타임 스크립트로 조립하지 않는다는 규칙을 지키려면 실물 프리팹이 있어야 한다. 이 도구는 소켓·
// 지형·스폰 지점·리스폰 지점이 전부 배선된 방을 카테고리별로 만들어 "그레이박스" 상태까지 올려 준다.
// 실제 아트·미세 배치는 만들어진 프리팹을 열어 인스펙터/씬에서 손본다.
//
// 지형 수치는 플레이어 점프(jumpForce 11, 중력 1배 → 최대 약 6유닛)를 기준으로, 누구나 쉽게 넘어갈 수
// 있도록 단차 2.5 · 간격 3~4유닛 안쪽으로만 잡는다. 오브젝트 수도 방당 한 자릿수로 억제한다.
//
// 사용법:
//   Tools ▸ FCC ▸ Dungeon ▸ Build All Rooms        → 아래 14종 프리팹을 기존 경로에 덮어쓴다(GUID 유지).
//   Tools ▸ FCC ▸ Dungeon ▸ Place Dungeon Rig In Scene → 게이트·생성기·매니저를 씬에 놓고 풀을 채운다.
public static class DungeonRoomPrefabBuilder {
    #region 상수

    const string PrefabDir = "Assets/_Project/Assets/Prefabs/Dungeon";

    // 지형 감각 수치.
    const float Step = 2.5f;      // 한 번에 오르내리는 세로 단차.
    const float PlatThick = 0.6f; // 발판 두께.
    const float WallThick = 1.2f; // 벽·바닥 두께.

    static int groundLayer;

    #endregion
    #region 메뉴 — 전체 빌드

    [MenuItem("Tools/FCC/Dungeon/Build All Rooms")]
    public static void BuildAllRooms() {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) {
            Debug.LogError("[Dungeon] 프리팹 편집 모드를 닫고 다시 실행하세요.");
            return;
        }

        EnsureFolder(PrefabDir);

        groundLayer = LayerMask.NameToLayer("ground");
        if (groundLayer < 0) {
            groundLayer = 0;
            Debug.LogWarning("[Dungeon] 'ground' 레이어를 찾지 못해 지형을 Default 레이어에 만듭니다. 플레이어가 밟지 못할 수 있으니 레이어를 확인하세요.");
        }

        BuildEntry();
        BuildExit();

        BuildHazardA();
        BuildHazardB();
        BuildHazardC();

        BuildVerticalA();
        BuildVerticalB();

        BuildCombat("Room_Combat_A", CombatVariant.OneHighPlatform);
        BuildCombat("Room_Combat_B", CombatVariant.TwoPlatforms);
        BuildCombat("Room_Combat_C", CombatVariant.WideFlat);
        BuildCombat("Room_Combat_D", CombatVariant.TwoTiers);
        BuildCombat("Room_Combat_E", CombatVariant.CenterPeak);

        BuildSecretA();
        BuildSecretB();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Dungeon] 방 프리팹 14종을 다시 만들었습니다. DungeonGenerator 풀에 이미 연결돼 있으면 그대로 쓰입니다.");
    }

    #endregion
    #region 방 빌드 — 입구 · 출구

    static void BuildEntry() {
        const float w = 38f, h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Entry", w, h, out BoxCollider2D trigger);
        Frame(root, w, h, g, leftWall: true, rightWall: false, ceiling: true);

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 8f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 8f, a, 0f));

        // 뒤로 걸어 나가는 이탈 트리거. 스폰 지점(EntryAnchor)과 3유닛 이상 떨어뜨려 입장 직후 오발동을 막는다.
        TriggerVolume(root, "WalkOutZone", new Vector3(-w / 2f + 3f, g + h / 2f - 1f, 0f), new Vector2(2.5f, h - 2f))
            .AddComponent<DungeonExitZone>();

        WireRoom(root, DungeonRoom.RoomRole.Entry, entry, exit, null, respawn, trigger, null, null);
        Save(root, "Room_Entry");
    }

    static void BuildExit() {
        const float w = 38f, h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Exit", w, h, out BoxCollider2D trigger);
        Frame(root, w, h, g, leftWall: false, rightWall: true, ceiling: true);
        Solid(root, "Step", new Vector3(2f, g + Step * 0.5f, 0f), new Vector2(6f, PlatThick));

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 1.5f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        // 던전을 클리어하고 나가는 이탈 트리거.
        TriggerVolume(root, "ClearExitZone", new Vector3(w / 2f - 3f, g + h / 2f - 1f, 0f), new Vector2(2.5f, h - 2f))
            .AddComponent<DungeonExitZone>();

        WireRoom(root, DungeonRoom.RoomRole.Exit, entry, exit, null, respawn, trigger, null, null);
        Save(root, "Room_Exit");
    }

    #endregion
    #region 방 빌드 — 플랫포밍 기믹

    // 두 발판으로 건너는 넓은 틈. 떨어져도 죽지 않고 현재 방 리스폰으로 되돌린다.
    static void BuildHazardA() {
        const float w = 38f, h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Hazard_A", w, h, out BoxCollider2D trigger);
        Ceiling(root, w, h);
        Solid(root, "Floor_L", new Vector3(-w / 2f + (w / 2f - 6f) / 2f, g - WallThick / 2f, 0f), new Vector2(w / 2f - 6f, WallThick));
        Solid(root, "Floor_R", new Vector3(w / 2f - (w / 2f - 6f) / 2f, g - WallThick / 2f, 0f), new Vector2(w / 2f - 6f, WallThick));

        Solid(root, "Plat_1", new Vector3(-2.5f, g + Step, 0f), new Vector2(3f, PlatThick));
        Solid(root, "Plat_2", new Vector3(2.5f, g + Step, 0f), new Vector2(3f, PlatThick));

        TriggerVolume(root, "FallZone", new Vector3(0f, g - 5f, 0f), new Vector2(13f, 3f)).AddComponent<DungeonFallZone>();

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 1.5f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform branch = Anchor(root, "BranchAnchor", new Vector3(w / 2f - 5f, a + Step, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.PlatformingHazard, entry, exit, branch, respawn, trigger, null, null);
        Save(root, "Room_Hazard_A");
    }

    // 올라갔다 내려오는 3단 계단.
    static void BuildHazardB() {
        const float w = 38f, h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Hazard_B", w, h, out BoxCollider2D trigger);
        Ceiling(root, w, h);
        Solid(root, "Floor_L", new Vector3(-w / 2f + (w / 2f - 4f) / 2f, g - WallThick / 2f, 0f), new Vector2(w / 2f - 4f, WallThick));
        Solid(root, "Floor_R", new Vector3(w / 2f - (w / 2f - 10f) / 2f, g - WallThick / 2f, 0f), new Vector2(w / 2f - 10f, WallThick));

        Solid(root, "Plat_1", new Vector3(-1f, g + Step, 0f), new Vector2(3f, PlatThick));
        Solid(root, "Plat_2", new Vector3(3f, g + Step * 2f, 0f), new Vector2(3f, PlatThick));
        Solid(root, "Plat_3", new Vector3(7f, g + Step, 0f), new Vector2(3f, PlatThick));

        TriggerVolume(root, "FallZone", new Vector3(3f, g - 5f, 0f), new Vector2(15f, 3f)).AddComponent<DungeonFallZone>();

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 1.5f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform branch = Anchor(root, "BranchAnchor", new Vector3(3f, a + Step * 2f, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.PlatformingHazard, entry, exit, branch, respawn, trigger, null, null);
        Save(root, "Room_Hazard_B");
    }

    // 작은 틈 두 개를 한 번씩 톡톡 건넌다.
    static void BuildHazardC() {
        const float w = 38f, h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Hazard_C", w, h, out BoxCollider2D trigger);
        Ceiling(root, w, h);
        float seg = (w - 8f) / 3f;
        Solid(root, "Floor_L", new Vector3(-w / 2f + seg / 2f, g - WallThick / 2f, 0f), new Vector2(seg, WallThick));
        Solid(root, "Floor_M", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(seg, WallThick));
        Solid(root, "Floor_R", new Vector3(w / 2f - seg / 2f, g - WallThick / 2f, 0f), new Vector2(seg, WallThick));

        float gapX = seg / 2f + 2f;
        Solid(root, "Plat_L", new Vector3(-gapX, g + 1.5f, 0f), new Vector2(2.5f, PlatThick));
        Solid(root, "Plat_R", new Vector3(gapX, g + 1.5f, 0f), new Vector2(2.5f, PlatThick));

        TriggerVolume(root, "FallZone_L", new Vector3(-gapX, g - 5f, 0f), new Vector2(4.5f, 3f)).AddComponent<DungeonFallZone>();
        TriggerVolume(root, "FallZone_R", new Vector3(gapX, g - 5f, 0f), new Vector2(4.5f, 3f)).AddComponent<DungeonFallZone>();

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 1.5f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform branch = Anchor(root, "BranchAnchor", new Vector3(0f, a + Step, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.PlatformingHazard, entry, exit, branch, respawn, trigger, null, null);
        Save(root, "Room_Hazard_C");
    }

    #endregion
    #region 방 빌드 — 수직 갱도

    // 발판 하나의 배치값. YUp 은 방 바닥면(g) 기준 상대 높이.
    readonly struct VPlat {
        public readonly float X, YUp, W;
        public VPlat(float x, float yUp, float w) { X = x; YUp = yUp; W = w; }
    }

    // A: 오른쪽으로 올랐다가 왼쪽으로 되꺾어 다시 오른쪽 출구로 가는 갈지자 경사로.
    // 중간에 넓은 쉼터(6번)를 두고, 되꺾이는 지점에 곁가지 니치를 붙였다. 헛디뎌도 아래 경사면에
    // 걸리거나 바닥으로 떨어져 다시 오르면 되도록 단차는 2.4 이하로만 둔다.
    static void BuildVerticalA() {
        VPlat[] path = {
            new(-8f, 2.6f, 5f),  new(-3.5f, 4.6f, 5f), new(1f, 6.6f, 5f),   new(5.5f, 8.6f, 6f),
            new(8f, 11f, 4f),    new(3f, 12.8f, 7f),
            new(-2.5f, 14.8f, 5f), new(-7.5f, 16.8f, 5f), new(-9f, 19.2f, 4f),
            new(-4.5f, 21f, 4f), new(0.5f, 22.9f, 4f), new(5.5f, 24.9f, 5f), new(9.5f, 26.9f, 5f),
        };
        BuildVertical("Room_Vertical_A", path, alcove: new VPlat(-11f, 20f, 3f));
    }

    // B: S자로 크게 휘어 오르며, 가운데에 좁은 디딤돌 구간(6~8번)을 끼워 리듬에 강약을 준다.
    // 넓은 쉼터를 두 곳(5번, 10번) 둔다.
    static void BuildVerticalB() {
        VPlat[] path = {
            new(7f, 2.4f, 5f),   new(2.5f, 4.4f, 6f),  new(-2.5f, 6.2f, 6f), new(-8f, 8.2f, 5f),
            new(-4.5f, 10.4f, 7f),
            new(0.5f, 12.4f, 3f), new(5f, 14.2f, 2.5f), new(9f, 16f, 4f),
            new(4.5f, 18f, 6f),  new(-0.5f, 20f, 8f),
            new(-5.5f, 22f, 5f), new(-1f, 24f, 4f),   new(4f, 26f, 5f),     new(9f, 28f, 5f),
        };
        BuildVertical("Room_Vertical_B", path, alcove: new VPlat(-11.5f, 9.2f, 3f));
    }

    static void BuildVertical(string prefabName, VPlat[] path, VPlat alcove) {
        const float w = 28f, h = 38f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom(prefabName, w, h, out BoxCollider2D trigger);
        Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));

        for (int i = 0; i < path.Length; i++) {
            VPlat p = path[i];
            Solid(root, $"Plat_{i + 1}", new Vector3(p.X, g + p.YUp, 0f), new Vector2(p.W, PlatThick));
        }

        // 오르는 길에서 살짝 벗어난 막다른 니치. 곁가지(비밀방)가 여기 붙고, 없어도 잠깐 쉬어 가는 자리.
        Solid(root, "Alcove", new Vector3(alcove.X, g + alcove.YUp, 0f), new Vector2(alcove.W, PlatThick));

        float topY = g + path[path.Length - 1].YUp;

        // 맨 위 발판에서 오른쪽 퇴장 구멍까지 이어 주는 선반. 없으면 꼭대기에서 다음 방까지 허공이 뜬다.
        Solid(root, "ExitLedge", new Vector3(w / 4f + 1f, topY, 0f), new Vector2(w / 2f, PlatThick));

        // 옆벽은 헛디딤 방지용이지만 통째로 세우면 앞뒤 방과 이어지지 않는다.
        // 왼쪽은 아래(입장 통로)를, 오른쪽은 위(퇴장 통로)를 비운 반쪽짜리로 세운다.
        float roomTop = h / 2f;
        float roomBottom = -h / 2f;
        float entryOpeningTop = g + 4.5f;          // 왼쪽 아래로 걸어 들어오는 구멍의 천장.
        float exitOpeningBottom = topY - 1.5f;     // 오른쪽 위로 걸어 나가는 구멍의 바닥(맨 위 발판 아래).

        Solid(root, "Wall_L", new Vector3(-w / 2f + WallThick / 2f, (entryOpeningTop + roomTop) / 2f, 0f),
            new Vector2(WallThick, roomTop - entryOpeningTop));
        Solid(root, "Wall_R", new Vector3(w / 2f - WallThick / 2f, (roomBottom + exitOpeningBottom) / 2f, 0f),
            new Vector2(WallThick, exitOpeningBottom - roomBottom));

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 3f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(0f, topY + 1.4f, 0f));
        Transform branch = Anchor(root, "BranchAnchor", new Vector3(alcove.X, g + alcove.YUp + 0.3f, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.VerticalClimb, entry, exit, branch, respawn, trigger, null, null);
        Save(root, prefabName);
    }

    #endregion
    #region 방 빌드 — 전투방

    enum CombatVariant { OneHighPlatform, TwoPlatforms, WideFlat, TwoTiers, CenterPeak }

    static void BuildCombat(string prefabName, CombatVariant variant) {
        float w = variant == CombatVariant.WideFlat ? 48f : 44f;
        const float h = 18f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom(prefabName, w, h, out BoxCollider2D trigger);
        Ceiling(root, w, h);

        var spawns = new List<Vector3>();

        switch (variant) {
            case CombatVariant.OneHighPlatform:
                Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
                Solid(root, "Plat_C", new Vector3(0f, g + Step, 0f), new Vector2(9f, PlatThick));
                spawns.Add(new Vector3(-15f, g + 1f, 0f));
                spawns.Add(new Vector3(-8f, g + 1f, 0f));
                spawns.Add(new Vector3(0f, g + Step + 1f, 0f));
                spawns.Add(new Vector3(8f, g + 1f, 0f));
                spawns.Add(new Vector3(15f, g + 1f, 0f));
                break;

            case CombatVariant.TwoPlatforms:
                Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
                Solid(root, "Plat_L", new Vector3(-10f, g + Step, 0f), new Vector2(8f, PlatThick));
                Solid(root, "Plat_R", new Vector3(10f, g + Step, 0f), new Vector2(8f, PlatThick));
                spawns.Add(new Vector3(-16f, g + 1f, 0f));
                spawns.Add(new Vector3(-10f, g + Step + 1f, 0f));
                spawns.Add(new Vector3(-4f, g + 1f, 0f));
                spawns.Add(new Vector3(4f, g + 1f, 0f));
                spawns.Add(new Vector3(10f, g + Step + 1f, 0f));
                spawns.Add(new Vector3(16f, g + 1f, 0f));
                break;

            case CombatVariant.WideFlat:
                Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
                for (int i = 0; i < 6; i++) {
                    float x = -20f + i * 8f;
                    spawns.Add(new Vector3(x, g + 1f, 0f));
                }
                break;

            case CombatVariant.TwoTiers:
                // 양 끝은 바닥 높이로 두어 앞뒤 방과 이어지게 하고, 가운데만 한 단 올린 대(臺)를 둔다.
                Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
                Solid(root, "Dais", new Vector3(4f, g + Step - PlatThick / 2f, 0f), new Vector2(18f, PlatThick));
                Solid(root, "Dais_Step", new Vector3(-6.5f, g + Step * 0.5f, 0f), new Vector2(3f, PlatThick));
                spawns.Add(new Vector3(-16f, g + 1f, 0f));
                spawns.Add(new Vector3(-9f, g + 1f, 0f));
                spawns.Add(new Vector3(2f, g + Step + 1f, 0f));
                spawns.Add(new Vector3(9f, g + Step + 1f, 0f));
                spawns.Add(new Vector3(18f, g + 1f, 0f));
                break;

            case CombatVariant.CenterPeak:
                Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
                Solid(root, "Step_L", new Vector3(-6f, g + Step * 0.8f, 0f), new Vector2(3f, PlatThick));
                Solid(root, "Step_R", new Vector3(6f, g + Step * 0.8f, 0f), new Vector2(3f, PlatThick));
                Solid(root, "Peak", new Vector3(0f, g + Step * 1.8f, 0f), new Vector2(6f, PlatThick));
                spawns.Add(new Vector3(-16f, g + 1f, 0f));
                spawns.Add(new Vector3(-6f, g + Step * 0.8f + 1f, 0f));
                spawns.Add(new Vector3(0f, g + Step * 1.8f + 1f, 0f));
                spawns.Add(new Vector3(6f, g + Step * 0.8f + 1f, 0f));
                spawns.Add(new Vector3(16f, g + 1f, 0f));
                break;
        }

        // 진행 방향(오른쪽) 차단 바리어 — 입장 시 켜지고 전멸하면 꺼진다.
        GameObject barrierObj = new("LockBarrier");
        barrierObj.transform.SetParent(root.transform, false);
        barrierObj.transform.localPosition = new Vector3(w / 2f - 1.5f, g + h / 2f - 1f, 0f);
        var barrier = barrierObj.AddComponent<BoxCollider2D>();
        barrier.size = new Vector2(1f, h - 2f);
        barrier.enabled = false;

        DungeonRoomSpawner spawner = AddSpawner(root, spawns);

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 1.5f, a, 0f));
        Transform exit = Anchor(root, "ExitAnchor", new Vector3(w / 2f - 1.5f, a, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 3f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.CombatArena, entry, exit, null, respawn, trigger, barrier, spawner);
        Save(root, prefabName);
    }

    #endregion
    #region 방 빌드 — 곁가지

    static void BuildSecretA() {
        const float w = 16f, h = 12f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Secret_A", w, h, out BoxCollider2D trigger);
        Frame(root, w, h, g, leftWall: true, rightWall: true, ceiling: true);

        GameObject pickup = TriggerVolume(root, "BonusShard", new Vector3(0f, g + 1.5f, 0f), new Vector2(1.6f, 1.6f));
        pickup.AddComponent<DungeonBonusPickup>();

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 2f, a, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 2f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.SecretBranch, entry, null, null, respawn, trigger, null, null);
        Save(root, "Room_Secret_A");
    }

    static void BuildSecretB() {
        const float w = 16f, h = 12f;
        float g = GroundY(h);
        float a = AnchorY(h);

        GameObject root = NewRoom("Room_Secret_B", w, h, out BoxCollider2D trigger);
        Frame(root, w, h, g, leftWall: true, rightWall: true, ceiling: true);
        Solid(root, "Plat", new Vector3(3f, g + Step, 0f), new Vector2(4f, PlatThick));

        GameObject pickup = TriggerVolume(root, "BonusShard", new Vector3(3f, g + Step + 1.4f, 0f), new Vector2(1.6f, 1.6f));
        pickup.AddComponent<DungeonBonusPickup>();

        Transform entry = Anchor(root, "EntryAnchor", new Vector3(-w / 2f + 2f, a, 0f));
        Transform respawn = Anchor(root, "RespawnPoint", new Vector3(-w / 2f + 2f, a, 0f));

        WireRoom(root, DungeonRoom.RoomRole.SecretBranch, entry, null, null, respawn, trigger, null, null);
        Save(root, "Room_Secret_B");
    }

    #endregion
    #region 씬 던전 리그

    [MenuItem("Tools/FCC/Dungeon/Place Dungeon Rig In Scene")]
    public static void PlaceDungeonRig() {
        DungeonGenerator generator = Object.FindAnyObjectByType<DungeonGenerator>(FindObjectsInactive.Include);

        if (generator == null) {
            GameObject rig = new("DungeonRig");
            Undo.RegisterCreatedObjectUndo(rig, "Place Dungeon Rig");

            GameObject origin = new("DungeonOrigin");
            origin.transform.SetParent(rig.transform, false);
            origin.transform.position = new Vector3(500f, -500f, 0f); // **오버월드와 겹치지 않는 곳으로 옮기세요.**

            generator = rig.AddComponent<DungeonGenerator>();
            generator.dungeonOrigin = origin.transform;
            rig.AddComponent<DungeonManager>();

            GameObject gateObj = new("DungeonGate_Backworld");
            gateObj.transform.SetParent(rig.transform, false);
            var gateCol = gateObj.AddComponent<BoxCollider2D>();
            gateCol.isTrigger = true;
            gateCol.size = new Vector2(2f, 3f);
            gateObj.AddComponent<DungeonGate>().generator = generator;

            Selection.activeGameObject = rig;
        }

        AssignPools(generator);
        EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        Debug.Log("[Dungeon] DungeonGenerator 풀을 Prefabs/Dungeon 의 방 프리팹으로 채웠습니다. DungeonOrigin·outsideBounds·게이트 위치를 확인한 뒤 씬을 저장하세요.");
    }

    static void AssignPools(DungeonGenerator g) {
        g.entryRoomPrefabs = Load("Room_Entry");
        g.exitRoomPrefabs = Load("Room_Exit");
        g.hazardRoomPrefabs = Load("Room_Hazard_A", "Room_Hazard_B", "Room_Hazard_C");
        g.verticalRoomPrefabs = Load("Room_Vertical_A", "Room_Vertical_B");
        g.combatRoomPrefabs = Load("Room_Combat_A", "Room_Combat_B", "Room_Combat_C", "Room_Combat_D", "Room_Combat_E");
        g.secretRoomPrefabs = Load("Room_Secret_A", "Room_Secret_B");
        EditorUtility.SetDirty(g);
    }

    static List<GameObject> Load(params string[] names) {
        var list = new List<GameObject>();
        foreach (string n in names) {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{n}.prefab");
            if (go != null) list.Add(go);
            else Debug.LogWarning($"[Dungeon] '{n}.prefab' 을 찾지 못했습니다. 먼저 Build All Rooms 를 실행하세요.");
        }
        return list;
    }

    #endregion
    #region 생성 도우미

    static float GroundY(float roomHeight) => -roomHeight / 2f + 1f;

    // 소켓·리스폰·워프 목표 Y. 플레이어 트랜스폼 피벗이 콜라이더(높이 약 2) 한가운데라, 바닥 표면보다
    // 몸통 반쯤 위에 두어야 입장·복귀 시 바닥에 파묻히지 않는다.
    static float AnchorY(float roomHeight) => GroundY(roomHeight) + 1.1f;

    static GameObject NewRoom(string name, float w, float h, out BoxCollider2D roomTrigger) {
        GameObject root = new(name);
        roomTrigger = root.AddComponent<BoxCollider2D>();
        roomTrigger.isTrigger = true;
        roomTrigger.size = new Vector2(w, h);
        return root;
    }

    // 바닥 전폭 + 선택적 벽·천장.
    static void Frame(GameObject root, float w, float h, float g, bool leftWall, bool rightWall, bool ceiling) {
        Solid(root, "Floor", new Vector3(0f, g - WallThick / 2f, 0f), new Vector2(w, WallThick));
        if (ceiling) Ceiling(root, w, h);
        if (leftWall) Solid(root, "Wall_L", new Vector3(-w / 2f + WallThick / 2f, 0f, 0f), new Vector2(WallThick, h));
        if (rightWall) Solid(root, "Wall_R", new Vector3(w / 2f - WallThick / 2f, 0f, 0f), new Vector2(WallThick, h));
    }

    static void Ceiling(GameObject root, float w, float h) {
        Solid(root, "Ceiling", new Vector3(0f, h / 2f - WallThick / 2f, 0f), new Vector2(w, WallThick));
    }

    static void Solid(GameObject parent, string name, Vector3 center, Vector2 size) {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = name;
        obj.layer = groundLayer;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = center;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        Object.DestroyImmediate(obj.GetComponent<Collider>()); // 3D 콜라이더는 2D 물리에서 안 쓰인다.
        obj.AddComponent<BoxCollider2D>();
    }

    static GameObject TriggerVolume(GameObject parent, string name, Vector3 center, Vector2 size) {
        GameObject obj = new(name);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = center;
        var col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;
        return obj;
    }

    static Transform Anchor(GameObject parent, string name, Vector3 local) {
        GameObject obj = new(name);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = local;
        return obj.transform;
    }

    static DungeonRoomSpawner AddSpawner(GameObject root, List<Vector3> points) {
        var spawner = root.AddComponent<DungeonRoomSpawner>();
        for (int i = 0; i < points.Count; i++) {
            Transform p = Anchor(root, $"SpawnPoint_{i + 1}", points[i]);
            spawner.entries.Add(new DungeonRoomSpawner.SpawnEntry { point = p });
        }
        return spawner;
    }

    static void WireRoom(GameObject root, DungeonRoom.RoomRole role,
        Transform entry, Transform exit, Transform branch, Transform respawn,
        Collider2D roomTrigger, Collider2D lockBarrier, DungeonRoomSpawner spawner) {
        var room = root.AddComponent<DungeonRoom>();
        room.role = role;
        room.entryAnchor = entry;
        room.exitAnchor = exit;
        room.branchAnchor = branch;
        room.respawnPoint = respawn;
        room.roomTrigger = roomTrigger;
        room.lockBarrier = lockBarrier;
        room.spawner = spawner;
    }

    static void Save(GameObject root, string prefabName) {
        string path = $"{PrefabDir}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path); // 경로가 이미 있으면 GUID 를 유지한 채 덮어쓴다.
        Object.DestroyImmediate(root);
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
