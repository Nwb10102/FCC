#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

// 프리팹 생성 메뉴들이 공유하는 한글 SDF 폰트 조회.
//
// 각 빌더가 폰트 경로를 문자열로 박아두고 있었는데, KwFontAssetBuilder로 폰트를 새로 뽑으면
// 파일 이름이 바뀐다. 경로가 어긋나도 TMP는 조용히 기본 폰트(LiberationSans)로 떨어지고
// 한글이 전부 네모(□)로 렌더되기 때문에, 이름이 안 맞으면 폴더를 훑어서라도 한글 폰트를 물려준다.
public static class PrefabBuilderFont {
    #region 경로

    const string FontDir = "Assets/_Project/Assets/Font/SDF";

    #endregion
    #region 조회

    // logTag는 경고 앞에 붙는 이름(예: "ObjectiveChecklist"). 어느 메뉴가 찍은 경고인지 구분하려고 받는다.
    public static TMP_FontAsset Load(string preferredPath, string logTag) {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(preferredPath);
        if (font != null) return font;

        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontDir })) {
            TMP_FontAsset found = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (found == null) continue;

            Debug.LogWarning($"[{logTag}] 지정한 폰트가 없어({preferredPath}) 같은 폴더의 '{found.name}'을 대신 물렸습니다. " +
                "빌더의 FontPath를 실제 파일 이름으로 고쳐두세요.", found);
            return found;
        }

        Debug.LogError($"[{logTag}] {FontDir} 에 한글 SDF 폰트가 하나도 없습니다. " +
            "Tools ▸ KW Font 로 폰트를 먼저 만드세요. 지금 만든 프리팹의 한글은 전부 네모(□)로 나옵니다.");
        return null;
    }

    #endregion
}
#endif
