using UnityEngine;

// 스킬 조준 방향을 선으로 보여주는 공용 유틸리티. 텍스처 없이 LineRenderer로 그린다.
// 원형 판정을 보여주는 AttackRangeIndicator와 짝을 이루는, 방향성 조준 표시용 버전이다.
//
// 실선이 아니라 점선이며, ScrollDashes()를 매 프레임 불러주면 점선이 진행 방향으로 흐르는 것처럼 보인다
// (조준 중이라는 것을 더 잘 느끼게 하는 연출용 — UpdateAim 안에서 호출하면 된다).
public static class AimLineIndicator {
    static Texture2D dashTexture;

    // pointCount: 2면 직선(칼잡이), 그보다 크면 SetPositions()로 곡선(포물선 등)을 그릴 수 있다.
    // dashesPerUnit: 월드 1유닛에 점선이 몇 번 반복되는지. 클수록 점이 촘촘해진다.
    public static LineRenderer Create(Color color, float width = 0.06f, int sortingOrder = 55, int pointCount = 2, float dashesPerUnit = 4f) {
        GameObject obj = new GameObject("AimLineIndicator");

        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.positionCount = pointCount;
        line.useWorldSpace = true;
        line.widthMultiplier = width;

        // Sprites/Default는 텍스처 오프셋(_MainTex_ST)을 무시해서 점선이 흐르지 않는다 — 전용 셰이더를 쓴다.
        Material material = new Material(Shader.Find("FCC/DashedLine")) { mainTexture = GetDashTexture() };
        material.mainTextureScale = new Vector2(dashesPerUnit, 1f);
        line.material = material;
        line.textureMode = LineTextureMode.Tile; // 선 길이에 맞춰 점선 패턴을 반복시킨다.

        // 뒤(플레이어 쪽, 0번 정점)는 원래 알파로 시작해서 앞(조준 끝)으로 갈수록 완전히 투명해진다.
        Color tipColor = color;
        tipColor.a = 0f;
        line.startColor = color;
        line.endColor = tipColor;
        line.sortingOrder = sortingOrder;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        obj.SetActive(false); // 조준을 시작할 때만 보여준다.
        return line;
    }

    // 점선이 진행 방향으로 흘러가는 것처럼 보이도록 텍스처를 스크롤한다. 조준 중 매 프레임(UpdateAim) 호출한다.
    public static void ScrollDashes(LineRenderer line, float speed = 6f) {
        if (line == null || line.material == null) return;
        line.material.mainTextureOffset = new Vector2(-Time.time * speed, 0f);
    }

    // 절반은 칠하고 절반은 비운 가로 1픽셀 줄무늬. wrapMode Repeat + Tile 모드가 나머지를 알아서 반복해 준다.
    static Texture2D GetDashTexture() {
        if (dashTexture != null) return dashTexture;

        const int resolution = 16;
        dashTexture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);
        dashTexture.filterMode = FilterMode.Bilinear;
        dashTexture.wrapMode = TextureWrapMode.Repeat;

        for (int x = 0; x < resolution; x++) {
            bool solid = x < resolution / 2;
            dashTexture.SetPixel(x, 0, solid ? Color.white : Color.clear);
        }
        dashTexture.Apply();

        return dashTexture;
    }
}
