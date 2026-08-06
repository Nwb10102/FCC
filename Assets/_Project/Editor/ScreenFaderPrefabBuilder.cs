#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 씬 전환용 검은 막(ScreenFader) 프리팹을 한 번 찍어내는 에디터 도구.
//
// 화면을 덮는 막이라 구조는 단순하지만, 코드로 Canvas를 짓지 않는다는 UI 규칙을 지키려면
// 실물 프리팹이 있어야 한다. 색·페이드 시간을 바꾸고 싶으면 프리팹을 인스펙터에서 고치면 되고,
// 처음부터 다시 뽑고 싶을 때만 이 메뉴를 다시 실행하면 된다. **다시 실행하면 프리팹을 덮어씁니다.**
//
// 사용법: Tools ▸ FCC ▸ Build Screen Fader Prefab → Tools ▸ FCC ▸ Place Screen Fader In Scene
public static class ScreenFaderPrefabBuilder {
    #region 경로 · 색

    const string PrefabDir = "Assets/_Project/Assets/Prefabs/UI";
    const string PrefabPath = PrefabDir + "/ScreenFader.prefab";

    // 완전한 검정. 공포 톤이라 푸른 기 없이 순수하게 어두워지는 편이 맞는다.
    static readonly Color CurtainColor = Color.black;

    // 정비 화면(SkillLoadout=200)과 일시정지 메뉴보다 위에 떠야 한다. 전환 중에는 무엇도 보이면 안 되므로 넉넉히 높게.
    const int SortingOrder = 500;

    #endregion
    #region 메뉴

    [MenuItem("Tools/FCC/Build Screen Fader Prefab")]
    public static void BuildPrefab() {
        // 프리팹 편집 모드에서 실행하면 임시로 만든 오브젝트가 그 프리팹 안으로 들어가 버린다.
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) {
            Debug.LogError("[ScreenFader] 프리팹 편집 모드를 닫고 다시 실행하세요. 임시 오브젝트가 편집 중인 프리팹에 섞여 들어갑니다.");
            return;
        }

        EnsureFolder(PrefabDir);

        GameObject rootObj = new GameObject("ScreenFader", typeof(RectTransform));

        Canvas canvas = rootObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // blocksRaycasts로 뒤쪽 버튼을 막으려면 이 캔버스에도 레이캐스터가 있어야 한다.
        rootObj.AddComponent<GraphicRaycaster>();

        CanvasGroup group = rootObj.AddComponent<CanvasGroup>();
        group.alpha = 0f; // 평소에는 투명. 전환이 시작될 때만 ScreenFader가 알파를 올린다.
        group.blocksRaycasts = false;
        group.interactable = false;

        // 화면 전체를 덮는 막. raycastTarget이 켜져 있어야 blocksRaycasts가 실제로 클릭을 막는다.
        Image curtain = CreateImage("Curtain", rootObj.transform, CurtainColor);
        Stretch(curtain.rectTransform);

        ScreenFader fader = rootObj.AddComponent<ScreenFader>();
        fader.fadeGroup = group;

        PrefabUtility.SaveAsPrefabAsset(rootObj, PrefabPath);
        Object.DestroyImmediate(rootObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ScreenFader] 프리팹을 만들었습니다.\n· {PrefabPath}\n" +
            "씬에 올리려면 Tools ▸ FCC ▸ Place Screen Fader In Scene 을 실행하거나 프리팹을 그냥 드래그하세요.");
    }

    // 지금 열려 있는 씬에 프리팹을 올린다. 씬 전환이 일어나는 씬마다 한 번씩 실행하면 된다.
    [MenuItem("Tools/FCC/Place Screen Fader In Scene")]
    public static void PlaceInScene() {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) {
            Debug.LogError($"[ScreenFader] 프리팹이 없습니다({PrefabPath}). 먼저 Tools ▸ FCC ▸ Build Screen Fader Prefab 을 실행하세요.");
            return;
        }

        // 꺼둔 상태로 씬에 남아 있는 것도 찾아야 하므로 Include.
        foreach (ScreenFader existing in Object.FindObjectsByType<ScreenFader>(FindObjectsInactive.Include)) {
            Debug.Log($"[ScreenFader] 씬에 이미 '{existing.name}' 이 있어 새로 놓지 않았습니다.", existing.gameObject);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Screen Fader");

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);

        Debug.Log("[ScreenFader] 씬에 ScreenFader 프리팹을 놓았습니다. 씬을 저장하세요.", instance);
    }

    #endregion
    #region 생성 도우미

    static Image CreateImage(string name, Transform parent, Color color) {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
