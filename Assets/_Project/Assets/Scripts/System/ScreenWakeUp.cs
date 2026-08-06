using System.Collections;
using UnityEngine;

// 씬이 시작될 때 "정신을 차리며 눈을 뜨는" 연출. 검은 화면을 한동안 유지하다가,
// 눈꺼풀이 껌뻑이듯 몇 번 열렸다 감긴 뒤 마지막에 완전히 뜬다.
//
// 막을 직접 만들지 않고 ScreenFader의 검은 막을 빌려 쓴다. 메인 메뉴에서 넘어올 때 이미 화면이
// 덮여 있는 상태로 도착하므로, 새로 막을 만들면 한 프레임 겹쳐 깜빡이기 때문이다.
//
// **연출을 시작할 씬에 빈 오브젝트를 만들어 붙이세요.** 같은 씬에 ScreenFader 프리팹도 함께 있어야 한다.
public class ScreenWakeUp : MonoBehaviour {
    #region 인스펙터 변수

    [Header("암전")]
    public float blackHoldDuration = 2.5f; // 눈을 뜨기 전, 검은 화면을 그대로 유지하는 시간(초).

    [Header("눈 깜빡임")]
    // 완전히 뜨기 전에 몇 번 껌뻑일지. 0이면 깜빡임 없이 곧장 밝아진다.
    public int blinkCount = 2;
    // 마지막 깜빡임에서 눈이 열리는 정도(0=완전한 암전, 1=완전히 밝음). 앞선 깜빡임은 이보다 좁게 열린다.
    [Range(0f, 1f)]
    public float blinkOpenAmount = 0.5f;
    // 다시 감았을 때 남는 어두움(1이면 완전한 암전). 1보다 조금 낮춰야 의식이 돌아오는 느낌이 난다.
    [Range(0f, 1f)]
    public float blinkCloseAmount = 0.9f;
    public float blinkOpenDuration = 0.4f; // 눈이 열리는 데 걸리는 시간(초).
    public float blinkStayDuration = 0.25f; // 뜬 채로 머무는 시간(초).
    public float blinkCloseDuration = 0.35f; // 다시 감기는 데 걸리는 시간(초).
    public float blinkIntervalDuration = 0.5f; // 감은 뒤 다음 깜빡임까지 쉬는 시간(초).

    [Header("완전히 뜨기")]
    public float finalOpenDuration = 2.5f; // 마지막으로 눈을 완전히 뜨는 데 걸리는 시간(초).

    [Header("플레이어")]
    // 깨어나는 동안 움직이지 못하게 막는다. 검은 화면에서 조작이 먹히면 깨어나는 연출이 무의미해진다.
    // 대사·컷씬과 같은 잠금(Player_move.isMovementLocked)을 쓴다.
    public bool lockPlayerWhileWaking = true;

    #endregion
    #region 컴포넌트 변수

    Player_move playerMove;

    // 깨어나기 연출이 끝났는지 여부. 뒤이어 무언가를 시작하려는 쪽(오프닝 대사 등)이 이것을 본다.
    public bool IsFinished { get; private set; }

    #endregion
    #region 유니티 라이프 사이클

    // Awake가 아니라 Start에서 예약하는 이유:
    // ① 같은 씬의 ScreenFader보다 먼저 Awake가 돌면 Instance가 아직 비어 있다 (실행 순서가 정해져 있지 않음).
    // ② 씬 전환으로 들어온 경우, 새 씬의 Start는 ScreenFader의 전환 코루틴이 다시 깨어나기 전에 돌기 때문에
    //    Start에서 예약해도 자동 페이드 인을 확실히 가로챌 수 있다.
    IEnumerator Start() {
        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (ScreenFader.Instance == null) {
            Debug.LogError($"[ScreenWakeUp] '{name}' — 씬에 ScreenFader가 없어 깨어나기 연출을 재생할 수 없습니다. " +
                "Tools ▸ FCC ▸ Place Screen Fader In Scene 으로 놓으세요.", this);
            yield break;
        }

        ScreenFader fader = ScreenFader.Instance;

        // 메뉴에서 넘어온 전환이 막을 걷어내지 않도록 가로챈다. 여기서부터는 이 연출이 막을 책임진다.
        fader.SuppressAutoFadeIn();

        // 씬을 에디터에서 직접 실행한 경우에는 막이 투명한 채로 시작한다. 첫 프레임이 그려지기 전에 덮어둔다.
        fader.SetCover(1f);

        LockPlayer(true);

        yield return PlayWakeUp(fader);

        // 잠금을 풀기 전에 먼저 끝났다고 알린다. 뒤이어 시작되는 대사가 잠금을 곧바로 이어받도록.
        IsFinished = true;
        LockPlayer(false);
    }

    #endregion
    #region 조회

    // 깨어나기가 끝날 때까지 기다린다. 호출 측 코루틴에서 yield return 하면 된다.
    // 연출이 끝난 뒤에 불러도 곧바로 빠져나오므로 순서를 신경 쓰지 않아도 된다.
    public IEnumerator WaitUntilFinished() {
        while (!IsFinished) yield return null;
    }

    #endregion
    #region 깨어나기 연출

    IEnumerator PlayWakeUp(ScreenFader fader) {
        // 완전한 암전. 히트스톱·일시정지 배속에 끌려가지 않도록 unscaled 기준으로 센다 (ScreenFader와 같은 이유).
        if (blackHoldDuration > 0f) yield return new WaitForSecondsRealtime(blackHoldDuration);

        for (int i = 0; i < blinkCount; i++) {
            // 깜빡일수록 조금씩 더 크게 뜨도록 한다. 매번 같은 폭으로 열리면 기계적으로 보인다.
            float openRatio = (i + 1f) / (blinkCount + 1f);
            float openAlpha = Mathf.Lerp(1f, 1f - blinkOpenAmount, openRatio);

            yield return fader.FadeCover(openAlpha, blinkOpenDuration);
            if (blinkStayDuration > 0f) yield return new WaitForSecondsRealtime(blinkStayDuration);

            yield return fader.FadeCover(blinkCloseAmount, blinkCloseDuration);
            if (blinkIntervalDuration > 0f) yield return new WaitForSecondsRealtime(blinkIntervalDuration);
        }

        yield return fader.FadeCover(0f, finalOpenDuration);
    }

    #endregion
    #region 플레이어 잠금

    void LockPlayer(bool locked) {
        if (!lockPlayerWhileWaking) return;

        // 씬에 플레이어가 없는 연출 전용 씬일 수도 있으므로, 못 찾아도 조용히 넘어간다.
        if (playerMove == null) playerMove = FindAnyObjectByType<Player_move>();
        if (playerMove == null) return;

        playerMove.isMovementLocked = locked;
    }

    #endregion
}
