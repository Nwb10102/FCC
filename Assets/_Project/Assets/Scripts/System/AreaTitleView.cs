using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 새 지역에 들어섰을 때 화면 위에서 스르륵 내려오는 지역 이름 표시. 뒷세계 진입이나 챕터·구역
// 경계에서 "여기가 어디인지" 잠깐 알려주는 용도다.
//
// 화면 요소(Canvas·글자)는 전부 프리팹 Prefabs/UI/AreaTitle.prefab 에 들어 있고, 이 스크립트는
// 슬라이드·페이드 타이밍만 맡는다 (ScreenFader 와 같은 구조).
//
// **표시가 필요한 씬마다 AreaTitle 프리팹을 하나씩 놓으세요.** 겹쳐도 Awake 에서 정리한다
// (ScreenFader·SaveManager 와 같은 싱글턴 방식).
public class AreaTitleView : MonoBehaviour {
    public static AreaTitleView Instance;

    #region 인스펙터 변수

    [Header("연결 — 프리팹이 채워 둔 값입니다")]
    public RectTransform panel;    // 내려왔다 올라가는 글자 묶음. anchoredPosition.y 를 움직인다.
    public CanvasGroup group;      // 페이드 인/아웃용. alpha 만 건드린다.
    public TMP_Text titleLabel;    // 지역 이름(큰 글자).
    public TMP_Text subtitleLabel; // 부제(작은 글자). 비어 있으면 오브젝트째 꺼진다.

    [Header("연출")]
    public float dropDistance = 40f;     // 제자리보다 이만큼 위에서 시작해 내려온다.
    public float slideInDuration = 0.6f; // 내려오며 밝아지는 시간.
    public float holdDuration = 2f;      // 다 보인 채로 머무는 시간.
    public float fadeOutDuration = 0.9f; // 사라지는 시간.
    public float riseOnExit = 16f;       // 사라질 때 살짝 떠오르는 거리.

    #endregion
    #region 런타임 변수

    Vector2 restPos;         // 프리팹에 적힌 제자리. 모든 이동은 여기서 파생된다.
    Coroutine playRoutine;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity 의 == 오버로드를 탄다.
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad 는 루트 오브젝트에서만 동작한다. 정리용으로 다른 오브젝트 밑에 넣어뒀을
        // 수 있으므로 먼저 떼어낸다 (SaveManager 와 같은 이유).
        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!ValidateReferences()) return;

        restPos = panel.anchoredPosition;
        group.alpha = 0f;
        panel.gameObject.SetActive(false);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 표시

    // 어디서든 부르는 진입점. 프리팹이 씬에 없어도 게임이 멈추지 않도록, 없으면 조용히 지나간다
    // (ScreenFader.LoadScene 과 같은 방침).
    public static void Announce(string title, string subtitle = null) {
        if (Instance != null) {
            Instance.Show(title, subtitle);
            return;
        }

        Debug.LogWarning($"[AreaTitleView] 씬에 AreaTitle 프리팹이 없어 '{title}' 표시를 건너뜁니다. " +
            "Prefabs/UI/AreaTitle 프리팹을 씬에 놓으세요.");
    }

    public void Show(string title, string subtitle = null) {
        if (panel == null || group == null || titleLabel == null) return;

        // TODO(다국어): 지역 이름도 LocalizedString 으로 옮겨야 한다. 스킬·메뉴 문구와 함께 아직 미전환.
        titleLabel.text = title;

        if (subtitleLabel != null) {
            bool hasSub = !string.IsNullOrWhiteSpace(subtitle);
            subtitleLabel.gameObject.SetActive(hasSub);
            if (hasSub) subtitleLabel.text = subtitle;
        }

        // 짧은 간격으로 두 번 들어와도 글자만 갈아끼우고 처음부터 다시 재생한다 — 연출이 겹쳐 쌓이지 않게.
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayRoutine());
    }

    // 일시정지·히트스톱 중에도 멈추지 않도록 전부 unscaled 기준 (ScreenFader·DamagePopup 과 같은 이유).
    IEnumerator PlayRoutine() {
        panel.gameObject.SetActive(true);

        Vector2 from = restPos + Vector2.up * dropDistance;
        yield return Tween(from, restPos, 0f, 1f, slideInDuration);

        if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);

        yield return Tween(restPos, restPos + Vector2.up * riseOnExit, 1f, 0f, fadeOutDuration);

        group.alpha = 0f;
        panel.gameObject.SetActive(false);
        playRoutine = null;
    }

    // posFrom→posTo 로 옮기며 alpha 도 aFrom→aTo 로 같이 움직인다. 감속 곡선(SmoothStep)으로 스르륵 붙는다.
    IEnumerator Tween(Vector2 posFrom, Vector2 posTo, float aFrom, float aTo, float duration) {
        if (duration <= 0f) {
            panel.anchoredPosition = posTo;
            group.alpha = aTo;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            panel.anchoredPosition = Vector2.LerpUnclamped(posFrom, posTo, t);
            group.alpha = Mathf.Lerp(aFrom, aTo, t);
            yield return null;
        }

        panel.anchoredPosition = posTo;
        group.alpha = aTo;
    }

    #endregion
    #region 검사

    bool ValidateReferences() {
        List<string> missing = new();
        if (panel == null) missing.Add(nameof(panel));
        if (group == null) missing.Add(nameof(group));
        if (titleLabel == null) missing.Add(nameof(titleLabel));

        if (missing.Count == 0) return true;

        Debug.LogError($"[AreaTitleView] 프리팹 연결이 비어 있어 지역 이름 표시를 쓸 수 없습니다 — {string.Join(", ", missing)}. " +
            "Prefabs/UI/AreaTitle 프리팹을 씬에 놓으세요.", this);
        return false;
    }

    #endregion
}
