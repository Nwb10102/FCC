using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬을 바꿀 때 화면을 검게 덮었다가 새 씬에서 다시 걷어내는 전환 연출. 씬 로드까지 여기서 맡는다.
//
// 화면 요소(Canvas·검은 막)는 전부 프리팹 Prefabs/UI/ScreenFader.prefab 에 들어 있고, 이 스크립트는
// 알파 계산과 씬 로드만 맡는다 (SkillLoadoutView와 같은 구조).
// 프리팹을 처음부터 다시 찍어내려면 에디터 메뉴 Tools ▸ FCC ▸ Build Screen Fader Prefab.
//
// **씬 전환이 일어나는 씬마다 ScreenFader 프리팹을 하나씩 놓으세요.**
// 겹쳐도 Awake에서 알아서 정리한다 (HitFeedback·SaveManager와 같은 싱글턴 방식).
public class ScreenFader : MonoBehaviour {
    public static ScreenFader Instance;

    #region 인스펙터 변수

    [Header("연결 — 프리팹이 채워 둔 값입니다")]
    public CanvasGroup fadeGroup; // 화면 전체를 덮는 검은 막. 이 컴포넌트는 alpha만 움직인다.

    [Header("연출")]
    public float fadeOutDuration = 1f; // 화면이 완전히 검어지기까지의 시간(초).
    public float holdDuration = 0.15f; // 검어진 뒤 씬을 넘기기 전 잠깐 멈추는 시간(초). 0이면 바로 넘어간다.
    public float fadeInDuration = 0.8f; // 새 씬에서 화면이 다시 밝아지기까지의 시간(초).

    #endregion
    #region 런타임 변수

    Coroutine transitionRoutine;

    // 도착한 씬이 막을 직접 걷겠다고 예약해 둔 표시. ScreenWakeUp 같은 연출이 Start에서 켠다.
    bool customFadeInRequested;

    // 전환이 도는 중인지 여부. 버튼 연타로 씬이 두 번 로드되는 것을 막는 데 쓴다.
    public bool IsTransitioning => transitionRoutine != null;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (Instance != null && Instance != this) {
            // 씬마다 프리팹을 하나씩 두다 보니 씬을 넘어오면 두 개가 겹친다. **나중에 온 쪽이 물러난다** —
            // 먼저 있던 쪽이 화면을 검게 덮은 채로 넘어왔으므로, 그쪽을 살려야 페이드 인이 이어진다.
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad는 루트 오브젝트에서만 동작한다. 씬에서 정리용으로 다른 오브젝트 밑에
        // 넣어뒀을 수 있으므로 먼저 떼어낸다 (SaveManager와 같은 이유).
        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup == null) {
            Debug.LogError($"[ScreenFader] '{name}' 의 Fade Group 칸이 비어 있어 화면 전환 연출을 쓸 수 없습니다. " +
                "Prefabs/UI/ScreenFader 프리팹을 씬에 놓으세요. 프리팹이 없으면 " +
                "Tools ▸ FCC ▸ Build Screen Fader Prefab 으로 만들 수 있습니다.", this);
            return;
        }

        // 프리팹에 적힌 시작 알파에 blocksRaycasts를 맞춰 둔다. 0이면 막이 클릭을 가로채지 않아야 한다.
        SetAlpha(fadeGroup.alpha);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    #endregion
    #region 씬 전환

    // 씬 전환 버튼은 전부 이것만 부르면 된다. 페이더가 씬에 없어도 게임이 멈추지 않도록,
    // 없으면 연출 없이 그냥 씬을 바꾼다 — 같은 분기를 호출하는 쪽마다 적지 않도록 여기로 모았다.
    public static void LoadScene(string sceneName) {
        if (Instance != null) {
            Instance.FadeToScene(sceneName);
            return;
        }

        Debug.LogWarning($"[ScreenFader] 씬에 ScreenFader가 없어 연출 없이 '{sceneName}' 로 바로 넘어갑니다. " +
            "Tools ▸ FCC ▸ Place Screen Fader In Scene 으로 놓을 수 있습니다.");

        Time.timeScale = 1f; // 일시정지 중에 넘어가면 새 씬이 멈춘 채로 시작한다.
        SceneManager.LoadScene(sceneName);
    }

    // 화면을 검게 덮은 뒤 씬을 바꾸고, 새 씬에서 다시 밝힌다.
    public void FadeToScene(string sceneName) {
        if (string.IsNullOrEmpty(sceneName)) {
            Debug.LogError("[ScreenFader] 넘어갈 씬 이름이 비어 있습니다. 버튼의 씬 이름 칸을 채우세요.", this);
            return;
        }

        // 연타로 씬이 두 번 로드되는 것을 막는다. 첫 클릭의 전환이 끝날 때까지 나머지는 무시한다.
        if (IsTransitioning) return;

        transitionRoutine = StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName) {
        yield return FadeCover(1f, fadeOutDuration);

        // 검어진 상태를 잠깐 유지한다. 페이드가 끝나자마자 새 씬이 튀어나오면 전환이 뚝 끊긴 느낌이 든다.
        if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);

        // 일시정지(timeScale 0) 상태로 씬을 넘기면 새 씬이 멈춘 채로 시작한다. 넘기기 전에 되돌린다.
        Time.timeScale = 1f;

        // 동기 LoadScene은 로딩 동안 프레임이 통째로 멈춰 덮어둔 막이 끊겨 보인다. 그래서 비동기로 넘긴다.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

        if (load == null) {
            // 빌드 세팅에 없는 씬 이름. 검은 화면에 갇히지 않도록 막을 걷어내고 끝낸다.
            Debug.LogError($"[ScreenFader] '{sceneName}' 씬을 불러오지 못했습니다. " +
                "File ▸ Build Profiles 의 씬 목록에 들어 있는지, 이름이 정확한지 확인하세요.", this);

            yield return FadeCover(0f, fadeInDuration);
            transitionRoutine = null;
            yield break;
        }

        while (!load.isDone) yield return null;

        // 도착한 씬이 직접 걷겠다고 했으면 검은 화면 그대로 넘긴다 (ScreenWakeUp의 깨어나기 연출 등).
        // 새 씬의 Start는 이 코루틴이 다시 깨어나기 전에 돌기 때문에, 여기서 읽으면 이미 예약이 들어와 있다.
        if (customFadeInRequested) {
            customFadeInRequested = false;
            transitionRoutine = null;
            yield break;
        }

        yield return FadeCover(0f, fadeInDuration);
        transitionRoutine = null;
    }

    #endregion
    #region 페이드

    // 도착한 씬에서 막을 직접 걷고 싶을 때 Start에서 부른다. 켜두면 전환이 막을 그대로 두고 손을 뗀다.
    // **부른 쪽이 반드시 막을 걷어야 합니다.** 안 그러면 검은 화면에 갇힌다.
    public void SuppressAutoFadeIn() {
        customFadeInRequested = true;
    }

    // 막의 짙기를 즉시 바꾼다(0=투명, 1=완전한 검정). 씬이 시작되자마자 어둡게 덮을 때 쓴다.
    public void SetCover(float alpha) {
        if (fadeGroup == null) return;
        SetAlpha(alpha);
    }

    // 막의 짙기를 duration 초에 걸쳐 target까지 옮긴다. 호출 측 코루틴에서 yield return 하면 된다.
    // 일시정지나 히트스톱 중에도 연출이 멈추지 않도록 전부 unscaled 기준으로 돈다
    // (SaveMirror 번쩍임·DamagePopup과 같은 이유).
    public IEnumerator FadeCover(float target, float duration) {
        if (fadeGroup == null) yield break;

        if (duration <= 0f) {
            SetAlpha(target);
            yield break;
        }

        float start = fadeGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(target);
    }

    void SetAlpha(float alpha) {
        fadeGroup.alpha = alpha;

        // 막이 조금이라도 덮여 있으면 뒤쪽 버튼이 눌리지 않게 한다.
        // 페이드가 도는 동안 다른 버튼을 눌러 씬이 엇갈리게 로드되는 사고를 막는다.
        fadeGroup.blocksRaycasts = alpha > 0.001f;
    }

    #endregion
}
