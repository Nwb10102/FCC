#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 목표 체크리스트 프리팹을 씬에 올려주는 에디터 도구.
//
// 예전에는 프리팹을 코드로 찍어내는 Tools ▸ FCC ▸ Build Objective Checklist Prefab 메뉴가 여기 함께 있었다.
// **일부러 걷어냈다** — 체크리스트 프리팹은 이미 인스펙터에서 손으로 다듬은 상태(배경 패널 제거, 제목·설명·목록
// 자유 배치)라, 메뉴를 한 번만 잘못 눌러도 그 작업이 통째로 초기화되기 때문이다.
//
// **생김새는 이제 프리팹에서만 고칩니다.** 프리팹을 망가뜨렸다면 코드로 다시 찍지 말고 git 이력에서
// Prefabs/UI/ObjectiveChecklist.prefab · ObjectiveItemRow.prefab 을 되돌리세요.
public static class ObjectivePrefabBuilder {
    #region 경로

    const string PanelPrefabPath = "Assets/_Project/Assets/Prefabs/UI/ObjectiveChecklist.prefab";

    #endregion
    #region 메뉴

    // 씬에 프리팹을 올린다. 손으로 만들어둔 구버전 체크리스트가 있으면 같이 걷어낸다
    // (그대로 두면 둘이 겹쳐 뜨고, 어느 쪽이 갱신되는지 알 수 없다).
    [MenuItem("Tools/FCC/Place Objective Checklist In Scene")]
    public static void PlaceInScene() {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        if (prefab == null) {
            Debug.LogError($"[ObjectiveChecklist] 프리팹이 없습니다({PanelPrefabPath}). git 이력에서 되돌리세요.");
            return;
        }

        bool alreadyPlaced = false;

        // 꺼둔 상태로 씬에 남아 있는 것도 찾아야 하므로 Include.
        foreach (ObjectiveChecklistView existing in Object.FindObjectsByType<ObjectiveChecklistView>(FindObjectsInactive.Include)) {
            // 이미 프리팹 인스턴스라면 정상적으로 올라간 것이므로 손대지 않는다.
            if (PrefabUtility.GetCorrespondingObjectFromSource(existing) != null) {
                alreadyPlaced = true;
                continue;
            }

            // 구버전은 Canvas 뿌리째 손으로 만든 것이라 컴포넌트만 지우면 빈 Canvas가 화면에 남는다.
            Debug.Log($"[ObjectiveChecklist] 손으로 만든 구버전 체크리스트 '{existing.name}' 을 제거했습니다.", existing.gameObject);
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        if (alreadyPlaced) {
            Debug.Log("[ObjectiveChecklist] 씬에 이미 ObjectiveChecklist 프리팹이 있어 새로 놓지 않았습니다.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Objective Checklist");

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);

        Debug.Log("[ObjectiveChecklist] 씬에 ObjectiveChecklist 프리팹을 놓았습니다. 씬을 저장하세요.", instance);
    }

    #endregion
}
#endif
