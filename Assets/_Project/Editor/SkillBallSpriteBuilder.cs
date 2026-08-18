#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// 저글러 스킬 투사체(SkillProjectile_Ball) 프리팹에 붙일 임시 스프라이트를 코드로 그려 PNG로 저장한다.
// 정식 아트가 나오기 전까지 형태만 알아볼 수 있게 만든 자리표시용이다 — 서커스 공 느낌의 원(살짝 밝은
// 하이라이트 + 어두운 테두리)이라 방향성이 없으므로 SkillProjectile.Launch()가 회전시켜도 티가 나지 않는다.
//
// 사용법: Tools ▸ FCC ▸ Build Temp Ball Sprite (다시 실행하면 텍스처와 프리팹 연결을 덮어씁니다.)
public static class SkillBallSpriteBuilder {
    #region 경로 · 크기

    const string SpriteDir = "Assets/_Project/Assets/Sprites/Skill";
    const string SpritePath = SpriteDir + "/Ball_Temp.png";
    const string PrefabPath = "Assets/_Project/Assets/Prefabs/Skill/SkillProjectile_Ball.prefab";

    const int Resolution = 32;
    const float PixelsPerUnit = 64f;
    const float ColliderRadius = 0.24f;

    static readonly Color CoreColor = new(0.95f, 0.55f, 0.2f, 1f); // 서커스 공다운 주황색.
    static readonly Color HighlightColor = new(1f, 0.85f, 0.6f, 1f); // 왼쪽 위 하이라이트.
    static readonly Color RimColor = new(0.5f, 0.24f, 0.08f, 1f); // 테두리.

    #endregion
    #region 메뉴 진입점

    [MenuItem("Tools/FCC/Build Temp Ball Sprite")]
    public static void Build() {
        if (!Directory.Exists(SpriteDir)) Directory.CreateDirectory(SpriteDir);

        Texture2D texture = DrawBall();
        File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(SpritePath);

        ConfigureImporter();
        AssignToPrefab();

        Debug.Log($"[SkillBallSpriteBuilder] 임시 공 스프라이트 생성 완료: {SpritePath}");
    }

    #endregion
    #region 텍스처 그리기

    static Texture2D DrawBall() {
        Texture2D texture = new(Resolution, Resolution, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(Resolution - 1, Resolution - 1) * 0.5f;
        float outerRadius = Resolution * 0.5f;
        float rimStart = outerRadius * 0.86f; // 바깥쪽 14%는 테두리로 칠한다.
        Vector2 highlightOffset = new Vector2(-outerRadius * 0.32f, outerRadius * 0.32f); // 왼쪽 위.

        for (int y = 0; y < Resolution; y++) {
            for (int x = 0; x < Resolution; x++) {
                texture.SetPixel(x, y, PixelColor(new Vector2(x, y), center, outerRadius, rimStart, highlightOffset));
            }
        }

        texture.Apply();
        return texture;
    }

    static Color PixelColor(Vector2 pos, Vector2 center, float outerRadius, float rimStart, Vector2 highlightOffset) {
        float dist = Vector2.Distance(pos, center);
        if (dist > outerRadius) return Color.clear;

        if (dist >= rimStart) return RimColor;

        float highlightDist = Vector2.Distance(pos, center + highlightOffset);
        float highlightT = Mathf.Clamp01(1f - highlightDist / (outerRadius * 0.55f));
        return Color.Lerp(CoreColor, HighlightColor, highlightT * highlightT);
    }

    #endregion
    #region 임포트 설정

    static void ConfigureImporter() {
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;

        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    #endregion
    #region 프리팹 연결

    static void AssignToPrefab() {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null) {
            Debug.LogWarning("[SkillBallSpriteBuilder] 스프라이트 로드에 실패해 프리팹에 연결하지 못했습니다.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

        SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white; // 임시 색 채우기 대신 스프라이트 자체 색을 쓰도록 되돌린다.
        }

        CircleCollider2D collider = prefabRoot.GetComponent<CircleCollider2D>();
        if (collider != null) collider.radius = ColliderRadius;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    #endregion
}
#endif
