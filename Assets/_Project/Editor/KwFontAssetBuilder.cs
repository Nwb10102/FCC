#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class KwFontAssetBuilder
{
    const int SamplingSize = 90;
    const int Padding = 9;
    const int AtlasWidth = 4096;
    const int AtlasHeight = 4096;
    const string OutDir = "Assets/_Project/Assets/Font/SDF";

    static uint[] BuildCharset()
    {
        var list = new List<uint>();
        for (uint c = 0x0020; c <= 0x007E; c++) list.Add(c);   // ASCII
        for (uint c = 0xAC00; c <= 0xD7A3; c++) list.Add(c);   // Hangul syllables (11,172)
        return list.ToArray();
    }

    static void Build(string srcPath, string outName)
    {
        var srcFont = AssetDatabase.LoadAssetAtPath<Font>(srcPath);
        if (srcFont == null) { Debug.LogError("[KwFont] source not found: " + srcPath); return; }

        // 1) Create in DYNAMIC mode so glyphs can be rasterized into the atlas.
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            srcFont, SamplingSize, Padding, GlyphRenderMode.SDFAA,
            AtlasWidth, AtlasHeight, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null) { Debug.LogError("[KwFont] CreateFontAsset null: " + outName); return; }

        // 2) Add all glyphs.
        uint[] charset = BuildCharset();
        uint[] missing;
        bool ok = fontAsset.TryAddCharacters(charset, out missing);

        // 3) Switch to STATIC so it ships as a fixed atlas (no runtime growth).
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        // Make atlas textures no longer readable/clearable as dynamic.
        foreach (var tex in fontAsset.atlasTextures)
            if (tex != null) tex.Apply(false, true);

        // 4) Save asset + sub-assets.
        if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
        string assetPath = OutDir + "/" + outName + " SDF.asset";

        AssetDatabase.CreateAsset(fontAsset, assetPath);

        for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
        {
            var tex = fontAsset.atlasTextures[i];
            if (tex != null && !AssetDatabase.Contains(tex))
            {
                tex.name = outName + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }
        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = outName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);

        int missingCount = missing == null ? 0 : missing.Length;
        Debug.Log("[KwFont] " + outName + " done. glyphs=" + fontAsset.glyphTable.Count
            + " chars=" + fontAsset.characterTable.Count
            + " atlases=" + fontAsset.atlasTextures.Length
            + " missing=" + missingCount + " ok=" + ok
            + " -> " + assetPath);
    }

    [MenuItem("Tools/KW Font/Build Bold")]
    public static void BuildBold()
    {
        Build("Assets/_Project/Assets/Font/NotCreatedAsset/강원교육모두 Bold.ttf", "강원교육모두 Bold");
    }

    [MenuItem("Tools/KW Font/Build Light")]
    public static void BuildLight()
    {
        Build("Assets/_Project/Assets/Font/NotCreatedAsset/강원교육모두 Light.ttf", "강원교육모두 Light");
    }
}
#endif
