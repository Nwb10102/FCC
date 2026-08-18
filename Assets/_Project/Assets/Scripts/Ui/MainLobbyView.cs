using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 메인 로비(커튼콜 정면) 화면. 커튼이 좌우로 열리면서 타이틀과 메뉴 5개가 떠오르고,
// ↑↓ 와 마우스로 항목을 고른다. 와이어프레임 1d 안을 그대로 옮긴 화면이다.
//
// 생김새(커튼·타이틀·메뉴 줄·거울 소품)는 전부 프리팹 Prefabs/UI/MainLobby.prefab 에 들어 있고,
// 이 스크립트는 선택 상태 계산과 입력만 맡는다 (SkillLoadoutView와 같은 구조). 줄 하나의 표시는 MainLobbyItemView 담당.
// 프리팹을 처음부터 다시 찍어내려면 에디터 메뉴 Tools ▸ FCC ▸ Build Main Lobby Prefab.
public class MainLobbyView : MonoBehaviour {
    #region 인스펙터 변수

    [Header("연결 — 프리팹이 채워 둔 값입니다")]
    // 커튼이 열리는 동안 숨겨둘 알맹이(타이틀·메뉴·소품). 커튼 뒤에서 미리 보이면 연출이 죽는다.
    public CanvasGroup contentGroup;
    public RectTransform curtainLeft; // 왼쪽 커튼. 폭을 줄여서 연다 (왼쪽 가장자리에 앵커가 붙어 있다).
    public RectTransform curtainRight; // 오른쪽 커튼.
    // 메뉴 5줄. **배열 순서가 그대로 ↑↓ 이동 순서입니다.** 무엇을 하는 줄인지는 각 줄의 action이 정한다.
    public MainLobbyItemView[] items;
    public TMP_Text versionLabel; // 좌하단 버전 표기. Application.version 을 그대로 찍는다.

    [Header("연결 — 씬에서 직접 이어주세요")]
    // **씬의 설정 패널 오브젝트를 물려주세요.** 비어 있으면 [설정] 줄이 잠긴 채로 표시된다.
    public GameObject settingsPanel;

    [Header("씬 이름")]
    // **빌드 세팅(File ▸ Build Profiles)에 등록된 씬 이름을 정확히 적으세요.**
    public string newGameSceneName = "First"; // [새로 시작] — 0장 프롤로그부터.
    public string memoryRoomSceneName = ""; // [기억의 방] — 씬이 아직 없다. 비워두면 줄이 잠긴 채로 표시된다.

    [Header("문구")]
    public string versionFormat = "v{0}"; // {0} = Application.version.
    public string continueEmptyText = "저장된 기록 없음"; // 세이브가 없을 때 [이어하기] 오른쪽에 붙는 표기.
    // {0} = 저장된 씬 이름, {1} = 저장 시각. 챕터·일차 표기는 SaveData에 해당 필드가 생기면 여기만 바꾸면 된다.
    public string continueFormat = "{0} · {1}";

    [Header("커튼 연출")]
    public bool playOpening = true; // 끄면 커튼이 처음부터 열린 상태로 시작한다 (배치 확인용).
    public float openDuration = 1.2f; // 커튼이 다 열리기까지 걸리는 시간.
    public float contentFadeDelay = 0.35f; // 커튼이 조금 열린 뒤에 알맹이가 떠오르도록 늦추는 시간.
    public float contentFadeDuration = 0.6f;
    public float closedCurtainWidth = 960f; // 닫혔을 때 커튼 한 짝의 폭. 기준 해상도 1920의 절반이라 화면이 꽉 덮인다.

    #endregion
    #region 컴포넌트 변수

    InputAction escapeAction; // 설정창을 닫는 ESC. MainMenuController와 같은 방식으로 코드에서 만든다.

    SaveData continueTarget; // [이어하기]가 열 세이브. 없으면 null이고 그 줄은 잠긴다.
    float leftOpenWidth; // 프리팹에 적혀 있던 커튼 폭. 다 열렸을 때 돌아갈 값이다.
    float rightOpenWidth;
    int selectedIndex = -1;
    bool isReady; // 프리팹 연결이 온전한지. 어긋난 채로 두면 NullReference가 쏟아지므로 Awake에서 한 번만 검사한다.
    bool isBusy; // 씬 전환을 시작한 뒤. 연타로 두 번 넘어가는 것을 막는다.

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        escapeAction = new InputAction(binding: "<Keyboard>/escape");

        isReady = ValidateReferences();
        if (!isReady) return;

        foreach (MainLobbyItemView item in items) {
            item.Bind(HandleItemHovered, HandleItemClicked);
        }

        leftOpenWidth = curtainLeft.rect.width;
        rightOpenWidth = curtainRight.rect.width;

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void OnEnable() { escapeAction.Enable(); }
    void OnDisable() { escapeAction.Disable(); }

    void Start() {
        if (!isReady) return;

        RefreshContinue();
        RefreshVersion();
        RefreshLocks();
        SelectFirstUnlocked();

        StartCoroutine(PlayOpening());
    }

    void Update() {
        if (!isReady || isBusy) return;

        // 설정창이 떠 있는 동안에는 로비 커서가 움직이면 안 된다. ESC로 닫는 것만 받는다.
        if (settingsPanel != null && settingsPanel.activeSelf) {
            if (escapeAction.triggered) settingsPanel.SetActive(false);
            return;
        }

        HandleInput();
    }

    #endregion
    #region 프리팹 연결 검사

    // 프리팹을 쓰지 않고 컴포넌트만 붙였거나, 프리팹을 손보다 참조를 끊었을 때 조용히 죽지 않도록
    // 무엇이 비었는지 이름으로 찍어준다. 하나라도 비면 화면을 아예 굴리지 않는다.
    bool ValidateReferences() {
        List<string> missing = new();

        if (contentGroup == null) missing.Add(nameof(contentGroup));
        if (curtainLeft == null) missing.Add(nameof(curtainLeft));
        if (curtainRight == null) missing.Add(nameof(curtainRight));

        if (items == null || items.Length == 0) {
            missing.Add($"{nameof(items)}(메뉴 줄이 하나도 없습니다)");
        }
        else {
            for (int i = 0; i < items.Length; i++) {
                if (items[i] == null) missing.Add($"{nameof(items)}[{i}]");
            }
        }

        if (missing.Count == 0) return true;

        Debug.LogError($"[MainLobbyView] 프리팹 연결이 비어 있어 로비를 열 수 없습니다 — {string.Join(", ", missing)}. " +
            "Prefabs/UI/MainLobby 프리팹을 씬에 놓으세요. 프리팹이 없으면 Tools ▸ FCC ▸ Build Main Lobby Prefab 으로 만들 수 있습니다.", this);
        return false;
    }

    #endregion
    #region 커튼 연출

    // 커튼을 좌우로 밀어 열고, 조금 늦게 알맹이를 띄운다.
    // 전부 unscaledDeltaTime 기준이다 — 로비에서 timeScale이 0으로 남아 들어와도 연출이 멈추면 안 되기 때문.
    IEnumerator PlayOpening() {
        if (!playOpening || openDuration <= 0f) {
            SetCurtainWidth(leftOpenWidth, rightOpenWidth);
            contentGroup.alpha = 1f;
            yield break;
        }

        SetCurtainWidth(closedCurtainWidth, closedCurtainWidth);
        contentGroup.alpha = 0f;

        StartCoroutine(FadeInContent());

        for (float elapsed = 0f; elapsed < openDuration; elapsed += Time.unscaledDeltaTime) {
            // 끝에서 부드럽게 멈추도록 감속을 준다. 실제 커튼도 등속으로 열리지 않는다.
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            SetCurtainWidth(Mathf.Lerp(closedCurtainWidth, leftOpenWidth, t), Mathf.Lerp(closedCurtainWidth, rightOpenWidth, t));
            yield return null;
        }

        SetCurtainWidth(leftOpenWidth, rightOpenWidth);
    }

    IEnumerator FadeInContent() {
        yield return new WaitForSecondsRealtime(contentFadeDelay);

        for (float elapsed = 0f; elapsed < contentFadeDuration; elapsed += Time.unscaledDeltaTime) {
            contentGroup.alpha = Mathf.Clamp01(elapsed / contentFadeDuration);
            yield return null;
        }

        contentGroup.alpha = 1f;
    }

    void SetCurtainWidth(float left, float right) {
        curtainLeft.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, left);
        curtainRight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, right);
    }

    #endregion
    #region 표시 갱신

    // 세이브 파일을 읽어 [이어하기] 줄에 어느 지점인지 적어둔다.
    // SaveManager는 씬을 넘어 살아남는 싱글턴이라 로비 씬에 없을 수도 있는데, 그때는 세이브가 없는 것으로 본다.
    void RefreshContinue() {
        continueTarget = SaveManager.Instance != null ? SaveManager.Instance.Read() : null;

        MainLobbyItemView item = FindItem(MainLobbyAction.Continue);
        if (item == null) return;

        if (continueTarget == null || string.IsNullOrEmpty(continueTarget.sceneName)) {
            continueTarget = null;
            item.SetSuffix(continueEmptyText);
            return;
        }

        item.SetSuffix(string.Format(continueFormat, continueTarget.sceneName, continueTarget.savedAt));
    }

    void RefreshVersion() {
        if (versionLabel != null) versionLabel.text = string.Format(versionFormat, Application.version);
    }

    // 아직 열 수 없는 줄을 잠근다. 지우지 않고 흐리게 남기는 이유는 앞으로 무엇이 생길지 보여주기 위해서다.
    void RefreshLocks() {
        foreach (MainLobbyItemView item in items) {
            item.SetUnlocked(IsUnlocked(item.action));
        }
    }

    bool IsUnlocked(MainLobbyAction action) {
        return action switch {
            MainLobbyAction.Continue => continueTarget != null,
            MainLobbyAction.MemoryRoom => !string.IsNullOrEmpty(memoryRoomSceneName), // 기억의 방 씬이 생기기 전까지는 잠김.
            MainLobbyAction.Settings => settingsPanel != null,
            _ => true,
        };
    }

    MainLobbyItemView FindItem(MainLobbyAction action) {
        foreach (MainLobbyItemView item in items) {
            if (item.action == action) return item;
        }
        return null;
    }

    #endregion
    #region 선택 이동

    void SelectFirstUnlocked() {
        for (int i = 0; i < items.Length; i++) {
            if (!items[i].IsUnlocked) continue;

            Select(i);
            return;
        }
    }

    void Select(int index) {
        selectedIndex = index;

        for (int i = 0; i < items.Length; i++) {
            items[i].SetSelected(i == selectedIndex);
        }
    }

    // 잠긴 줄은 건너뛰고, 끝에 닿으면 반대편으로 감싼다. 항목이 5개뿐이라 위아래로 자주 오간다.
    void MoveSelection(int delta) {
        if (items.Length == 0) return;

        int index = selectedIndex;
        for (int step = 0; step < items.Length; step++) {
            index = (index + delta + items.Length) % items.Length;
            if (!items[index].IsUnlocked) continue;

            Select(index);
            return;
        }
    }

    #endregion
    #region 입력 처리

    // EventSystem의 내비게이션에 기대지 않고 키보드를 직접 읽는다. UI 모듈 설정이 씬마다 달라도
    // 똑같이 동작해야 하기 때문 (마우스는 MainLobbyItemView가 따로 받는다). SkillLoadoutView와 같은 방식.
    void HandleInput() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) MoveSelection(-1);
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) MoveSelection(1);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) {
            Activate();
        }
    }

    void HandleItemHovered(MainLobbyItemView item) {
        if (isBusy) return;
        if (settingsPanel != null && settingsPanel.activeSelf) return;

        int index = System.Array.IndexOf(items, item);
        if (index >= 0) Select(index);
    }

    void HandleItemClicked(MainLobbyItemView item) {
        if (isBusy) return;
        if (settingsPanel != null && settingsPanel.activeSelf) return;

        HandleItemHovered(item); // 클릭한 줄을 먼저 고른 뒤 실행한다. 키보드 흐름과 결과가 같도록.
        Activate();
    }

    #endregion
    #region 항목 실행

    void Activate() {
        if (selectedIndex < 0 || selectedIndex >= items.Length) return;

        MainLobbyItemView item = items[selectedIndex];
        if (!item.IsUnlocked) return;

        switch (item.action) {
            case MainLobbyAction.Continue: Continue(); break;
            case MainLobbyAction.NewGame: LoadScene(newGameSceneName); break;
            case MainLobbyAction.MemoryRoom: LoadScene(memoryRoomSceneName); break;
            case MainLobbyAction.Settings: OpenSettings(); break;
            case MainLobbyAction.Quit: Quit(); break;
        }
    }

    // 세이브에 적힌 씬을 연 뒤에 상태를 되돌린다.
    // SaveManager.LoadGame()은 같은 씬 안에서만 복원하므로, 씬이 다 뜬 다음에 불러야 한다.
    void Continue() {
        if (continueTarget == null) return;

        SceneManager.sceneLoaded += RestoreAfterLoad;
        LoadScene(continueTarget.sceneName);
    }

    // 씬 전환과 함께 이 컴포넌트는 사라지므로 인스턴스 메서드로는 복원 시점을 받을 수 없다. 그래서 static이다.
    static void RestoreAfterLoad(Scene scene, LoadSceneMode mode) {
        SceneManager.sceneLoaded -= RestoreAfterLoad; // 한 번만 받고 곧바로 뗀다. 안 떼면 이후 모든 씬 로드에서 되돌린다.

        if (SaveManager.Instance == null) {
            Debug.LogWarning("[MainLobbyView] SaveManager가 없어 이어하기 상태를 복원하지 못했습니다. " +
                "세이브를 쓰는 씬에는 SaveManager가 살아 있어야 합니다.");
            return;
        }

        SaveManager.Instance.LoadGame();
    }

    // 씬 로드를 직접 하지 않고 ScreenFader를 거친다 — 커튼이 닫히듯 화면이 덮인 뒤에 넘어가야 하기 때문.
    // 씬에 ScreenFader가 없으면 ScreenFader.LoadScene이 알아서 연출 없이 바로 넘긴다.
    void LoadScene(string sceneName) {
        if (string.IsNullOrEmpty(sceneName)) {
            Debug.LogWarning("[MainLobbyView] 씬 이름이 비어 있어 넘어가지 않았습니다. 인스펙터의 씬 이름을 채우세요.", this);
            return;
        }

        isBusy = true;
        ScreenFader.LoadScene(sceneName);
    }

    void OpenSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    void Quit() {
        Debug.Log("게임 종료!");
        Application.Quit(); // 에디터에서는 동작하지 않고 빌드에서만 실제로 종료됨.
    }

    #endregion
}
