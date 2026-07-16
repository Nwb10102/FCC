using UnityEngine;

[RequireComponent(typeof(Health))]
public class HealthBar : MonoBehaviour {
    #region 인스펙터 변수

    [Header("체력바 위치/크기")]
    public Vector3 offset = new(0f, 1f, 0f); // 캐릭터 머리 위 오프셋.
    public Vector2 size = new(1f, 0.12f); // 체력바 월드 크기.
    public int sortingOrder = 100; // 다른 스프라이트보다 위에 그려지도록.

    [Header("체력바 색상")]
    public Color backgroundColor = Color.black;
    public Color fillColor = Color.red;

    #endregion
    #region 컴포넌트 변수

    Health health;
    Transform barRoot;
    Transform fillTransform;

    static Sprite centerPivotSprite;
    static Sprite leftPivotSprite;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        health = GetComponent<Health>();
        BuildBar();
    }

    void LateUpdate() {
        barRoot.position = transform.position + offset; // 캐릭터를 따라다니게 위치 갱신.

        float ratio = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0f;
        fillTransform.localScale = new Vector3(size.x * Mathf.Clamp01(ratio), size.y, 1f);
    }

    void OnDestroy() {
        if (barRoot != null) Destroy(barRoot.gameObject); // 캐릭터가 사라져도 체력바만 남지 않도록 정리.
    }

    #endregion
    #region 체력바 생성

    void BuildBar() {
        barRoot = new GameObject(gameObject.name + "_HealthBar").transform;
        barRoot.position = transform.position + offset; // 부모로 붙이지 않아 캐릭터의 좌우 반전(스케일) 영향을 받지 않음.

        SpriteRenderer background = CreateBarPiece("Background", GetCenterPivotSprite(), backgroundColor, sortingOrder);
        background.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer fill = CreateBarPiece("Fill", GetLeftPivotSprite(), fillColor, sortingOrder + 1);
        fillTransform = fill.transform;
        fillTransform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f); // 배경의 왼쪽 끝에 맞춤.
    }

    SpriteRenderer CreateBarPiece(string pieceName, Sprite sprite, Color color, int order) {
        GameObject obj = new GameObject(pieceName);
        obj.transform.SetParent(barRoot);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    static Sprite GetCenterPivotSprite() {
        if (centerPivotSprite == null) centerPivotSprite = CreateWhiteSprite(new Vector2(0.5f, 0.5f));
        return centerPivotSprite;
    }

    static Sprite GetLeftPivotSprite() {
        if (leftPivotSprite == null) leftPivotSprite = CreateWhiteSprite(new Vector2(0f, 0.5f));
        return leftPivotSprite;
    }

    // 1x1 흰색 텍스처로 스프라이트를 만들어 별도 이미지 에셋 없이 색상만으로 막대를 그린다.
    static Sprite CreateWhiteSprite(Vector2 pivot) {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
    }

    #endregion
}
