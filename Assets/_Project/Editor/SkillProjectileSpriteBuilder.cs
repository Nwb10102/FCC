#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// 칼잡이 스킬 투사체(SkillProjectile_Knife) 프리팹에 붙일 임시 스프라이트를 코드로 그려 PNG로 저장한다.
// 정식 아트가 나오기 전까지 형태만 알아볼 수 있게 만든 자리표시용이다 — 손잡이(갈색) + 가드 + 칼날(은색) 실루엣.
// **+X가 칼끝(정면)이 되도록 그린다.** SkillProjectile.Launch()가 발사 방향으로 이 스프라이트를 회전시키기 때문이다.
//
// 사용법: Tools ▸ FCC ▸ Build Temp Knife Sprite (다시 실행하면 텍스처와 프리팹 연결을 덮어씁니다.)
public static class SkillProjectileSpriteBuilder {
    #region 경로 · 크기

    const string SpriteDir = "Assets/_Project/Assets/Sprites/Skill";
    const string SpritePath = SpriteDir + "/Knife_Temp.png";
    const string PrefabPath = "Assets/_Project/Assets/Prefabs/Skill/SkillProjectile_Knife.prefab";

    const int Width = 48;
    const int Height = 16;
    const float PixelsPerUnit = 64f;

    const int HandleWidth = 14; // 손잡이 폭(px).
    const int GuardWidth = 3; // 손잡이와 칼날 사이 코등이 폭(px).
    const int HandleHalf = 4; // 손잡이 반두께(px).
    const int GuardHalf = 6; // 코등이 반두께(px).
    const float BladeHalfStart = 6f; // 칼날이 시작되는 지점의 반두께(px). 끝으로 갈수록 0으로 좁아진다.

    static readonly Color BladeColor = new(0.82f, 0.84f, 0.88f, 1f);
    static readonly Color BladeEdgeColor = new(0.95f, 0.97f, 1f, 1f); // 칼끝 쪽에 밝게 얹어 날카로운 느낌을 낸다.
    static readonly Color HandleColor = new(0.35f, 0.24f, 0.16f, 1f);
    static readonly Color GuardColor = new(0.25f, 0.22f, 0.2f, 1f);

    #endregion
    #region 메뉴 진입점

    [MenuItem("Tools/FCC/Build Temp Knife Sprite")]
    public static void Build() {
        if (!Directory.Exists(SpriteDir)) Directory.CreateDirectory(SpriteDir);

        Texture2D texture = DrawKnife();
        File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(SpritePath);

        ConfigureImporter();
        AssignToPrefab();

        Debug.Log($"[SkillProjectileSpriteBuilder] 임시 칼 스프라이트 생성 완료: {SpritePath}");
    }

    #endregion
    #region 텍스처 그리기

    static Texture2D DrawKnife() {
        Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        int centerY = Height / 2;
        float bladeStart = HandleWidth + GuardWidth;

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                texture.SetPixel(x, y, PixelColor(x, y, centerY, bladeStart));
            }
        }

        texture.Apply();
        return texture;
    }

    static Color PixelColor(int x, int y, int centerY, float bladeStart) {
        int distFromCenter = Mathf.Abs(y - centerY);

        if (x < HandleWidth) {
            return distFromCenter <= HandleHalf ? HandleColor : Color.clear;
        }

        if (x < bladeStart) {
            return distFromCenter <= GuardHalf ? GuardColor : Color.clear;
        }

        // 칼날: bladeStart에서 오른쪽 끝으로 갈수록 반두께가 0까지 좁아지는 삼각형.
        float t = (x - bladeStart) / (Width - bladeStart);
        float half = Mathf.Lerp(BladeHalfStart, 0f, t);
        if (distFromCenter > half) return Color.clear;

        return t > 0.85f ? BladeEdgeColor : BladeColor;
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
            Debug.LogWarning("[SkillProjectileSpriteBuilder] 스프라이트 로드에 실패해 프리팹에 연결하지 못했습니다.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

        SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white; // 임시 색 채우기 대신 스프라이트 자체 색을 쓰도록 되돌린다.
        }

        // 칼날 크기에 맞춰 트리거 반경도 살짝 키운다. 점 하나 크기(0.15)로는 칼날 길이를 못 덮는다.
        CircleCollider2D collider = prefabRoot.GetComponent<CircleCollider2D>();
        if (collider != null) collider.radius = 0.22f;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    #endregion
}
#endif
