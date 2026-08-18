using UnityEngine;

// 공격 판정 범위를 원형 링 스프라이트로 표시하는 공용 유틸리티. Player_Combat/Attack이 공유해서 사용한다.
public static class AttackRangeIndicator {
    static Sprite ringSprite;

    public static SpriteRenderer Create(float range, Color color, int sortingOrder = 50, float visualScale = 1f) {
        GameObject obj = new GameObject("AttackRangeIndicator");
        float diameter = range * 2f * visualScale; // visualScale은 실제 판정 범위(range)에는 영향을 주지 않고 링의 겉보기 크기만 줄인다.
        obj.transform.localScale = new Vector3(diameter, diameter, 1f);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRingSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        obj.SetActive(false); // 선딜레이가 시작될 때만 보여준다.

        return renderer;
    }

    static Sprite GetRingSprite() {
        if (ringSprite == null) ringSprite = CreateRingSprite(64, 0.15f);
        return ringSprite;
    }

    // 가장자리만 칠해진 원형 텍스처를 만들어 별도 이미지 에셋 없이 판정 범위 링을 그린다.
    static Sprite CreateRingSprite(int resolution, float thicknessRatio) {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(resolution - 1, resolution - 1) * 0.5f;
        float outerRadius = resolution * 0.5f;
        float innerRadius = outerRadius * (1f - thicknessRatio);

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                bool onRing = dist <= outerRadius && dist >= innerRadius;
                texture.SetPixel(x, y, onRing ? Color.white : Color.clear);
            }
        }
        texture.Apply();

        // pixelsPerUnit == resolution: 스케일 1일 때 스프라이트 크기가 정확히 1 월드 유닛이 되어,
        // transform.localScale로 지름을 그대로 지정할 수 있다.
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }
}
