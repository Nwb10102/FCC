using UnityEngine;

// 스프라이트 여러 장을 정해진 프레임 속도로 순환 재생하는 임시 플립북 애니메이터.
// 실제 스프라이트 시트나 Animator로 교체되기 전까지 걷기처럼 반복되는 동작에 쓴다.
// **애니메이션시킬 SpriteRenderer가 붙은 오브젝트에 붙이세요.**
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour {
    #region 인스펙터 변수

    [Header("프레임")]
    public Sprite[] frames; // 재생 순서대로 등록. frames[0]은 정지 시 보여줄 포즈로도 쓰인다.
    public float frameRate = 10f; // 초당 프레임 수.

    #endregion
    #region 컴포넌트 변수

    SpriteRenderer spriteRenderer;
    int frameIndex;
    float frameTimer;
    bool isPlaying;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update() {
        if (!isPlaying || frames.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(frameRate, 0.01f);
        if (frameTimer < frameDuration) return;

        frameTimer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }

    #endregion
    #region 재생 제어

    public void Play() {
        if (isPlaying || frames.Length == 0) return;

        isPlaying = true;
        frameIndex = 0;
        frameTimer = 0f;
        spriteRenderer.sprite = frames[0];
    }

    public void Stop() {
        if (!isPlaying) return;

        isPlaying = false;
        frameTimer = 0f;
        frameIndex = 0;
        if (frames.Length > 0) spriteRenderer.sprite = frames[0];
    }

    #endregion
}
