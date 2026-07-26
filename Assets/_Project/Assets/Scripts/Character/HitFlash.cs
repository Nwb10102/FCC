using System.Collections;
using UnityEngine;

// Health의 OnDamaged를 받아 피격 대상 본인의 시각 반응(흰색 플래시 + 스쿼시&스트레치)을 재생한다.
// HealthBar처럼 Health와 함께 부착하는 독립 컴포넌트다.
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour {
    #region 인스펙터 변수

    [Header("피격 플래시")]
    // SpriteRenderer.color는 스프라이트에 곱해지는 값이라, 흰색 스프라이트를 흰색으로 칠하면 아무 변화가 없다.
    // (현재 몬스터는 흰색 Capsule 플레이스홀더다.) 그래서 기본값을 눈에 띄는 붉은색으로 둔다.
    // 어두운 색의 몬스터 아트가 들어오면 Color.white로 바꾸는 편이 더 잘 어울린다.
    public Color flashColor = new(1f, 0.3f, 0.25f, 1f);
    public float flashDuration = 0.07f; // 번쩍인 뒤 원래 색으로 돌아오는 시간.

    [Header("스쿼시 & 스트레치")]
    public Transform visualRoot; // 비워두면 SpriteRenderer가 붙은 자식을 자동으로 찾습니다.
    public float squashAmount = 0.3f; // 0.3 = 가로 130% / 세로 70%로 찌그러진다.
    public float squashDuration = 0.16f;
    // 1 = 최대로 찌그러짐, 0 = 원래 크기, 음수 = 반대로 늘어남.
    // 찌그러졌다가 살짝 늘어나며 되돌아오는 카툰풍 곡선.
    public AnimationCurve squashCurve = new(new Keyframe(0f, 1f), new Keyframe(0.55f, -0.35f), new Keyframe(1f, 0f));

    #endregion
    #region 컴포넌트 변수

    Health health;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    Vector3 baseScale = Vector3.one;
    Coroutine flashRoutine;
    Coroutine squashRoutine;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        health = GetComponent<Health>();

        // HealthBar와 AttackRangeIndicator가 만드는 스프라이트는 자식으로 붙지 않으므로 여기 섞이지 않는다.
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) {
            baseColors[i] = renderers[i].color;
        }

        if (visualRoot == null) visualRoot = ResolveVisualRoot();
        if (visualRoot != null) baseScale = visualRoot.localScale;
    }

    void OnEnable() {
        health.OnDamaged += HandleDamaged;
    }

    void OnDisable() {
        health.OnDamaged -= HandleDamaged;
        RestoreColors(); // 플래시 도중에 꺼져도 흰색으로 굳지 않도록.
    }

    #endregion
    #region 피격 반응

    void HandleDamaged(int damage, Vector2 sourcePosition) {
        if (renderers.Length > 0) {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (visualRoot != null) {
            if (squashRoutine != null) StopCoroutine(squashRoutine);
            squashRoutine = StartCoroutine(SquashRoutine());
        }
    }

    // 히트스톱 중에도 진행돼야 하므로 전부 unscaledDeltaTime 기준으로 돈다.
    IEnumerator FlashRoutine() {
        SetColors(flashColor);

        float elapsed = 0f;
        while (elapsed < flashDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i] != null) renderers[i].color = Color.Lerp(flashColor, baseColors[i], t);
            }
            yield return null;
        }

        RestoreColors();
        flashRoutine = null;
    }

    IEnumerator SquashRoutine() {
        float elapsed = 0f;
        while (elapsed < squashDuration) {
            elapsed += Time.unscaledDeltaTime;
            ApplySquash(squashCurve.Evaluate(Mathf.Clamp01(elapsed / squashDuration)));
            yield return null;
        }

        ApplySquash(0f);
        squashRoutine = null;
    }

    // GroundMoveSystem이 좌우 반전을 위해 localScale.x 부호를 뒤집으므로,
    // 현재 부호를 그대로 유지한 채 크기만 바꾼다. visualRoot가 루트로 잡혀도 방향이 깨지지 않는다.
    void ApplySquash(float squash) {
        Vector3 current = visualRoot.localScale;
        visualRoot.localScale = new Vector3(
            Mathf.Sign(current.x) * Mathf.Abs(baseScale.x) * (1f + squashAmount * squash),
            Mathf.Sign(current.y) * Mathf.Abs(baseScale.y) * (1f - squashAmount * squash),
            baseScale.z);
    }

    void SetColors(Color color) {
        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i] != null) renderers[i].color = color;
        }
    }

    void RestoreColors() {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i] != null) renderers[i].color = baseColors[i];
        }
    }

    // 스프라이트를 가진 가장 바깥쪽 자식을 찾는다. 루트에 직접 붙어 있으면 루트를 그대로 쓴다.
    Transform ResolveVisualRoot() {
        if (renderers.Length == 0) return null;

        Transform target = renderers[0].transform;
        if (target == transform) return transform;

        while (target.parent != transform) {
            target = target.parent;
        }
        return target;
    }

    #endregion
}
