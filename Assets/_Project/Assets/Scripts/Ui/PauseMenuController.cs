using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour {
    #region 인스펙터 변수

    [Header("UI Panels")]
    public GameObject mainPanel; // 일시정지 메인창.
    public GameObject settingsPanel; // 설정창.
    public GameObject videoSubPanel; // 설정창 안의 비디오 설정 하위창.

    #endregion
    #region 컴포넌트 변수

    InputAction escapeAction;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        Debug.unityLogger.filterLogType = LogType.Error; // 다른 시스템에서 올라오는 불필요한 로그 억제.
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
    }

    void OnEnable() { escapeAction.Enable(); }
    void OnDisable() { escapeAction.Disable(); }

    void Update() {
        if (!escapeAction.triggered) return;

        // 열려있는 창 중 가장 안쪽(비디오 설정)부터 순서대로 닫는다.
        if (videoSubPanel != null && videoSubPanel.activeSelf) {
            videoSubPanel.SetActive(false);
        }
        else if (settingsPanel != null && settingsPanel.activeSelf) {
            CloseSettings();
        }
        else if (mainPanel != null && mainPanel.activeSelf) {
            Resume();
        }
        else {
            Pause();
        }
    }

    #endregion
    #region 패널 제어 함수

    // 게임으로 돌아가기.
    public void Resume() {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (videoSubPanel != null) videoSubPanel.SetActive(false);
        Time.timeScale = 1f; // 시간 흐름 재개.
    }

    // 일시정지 켜기 (게임 중 ESC를 눌렀을 때).
    public void Pause() {
        if (mainPanel != null) mainPanel.SetActive(true);
        Time.timeScale = 0f; // 시간 정지.
    }

    // [일시정지창의 'Options(Setting)' 버튼] 클릭 시 호출.
    public void GoToOptions() {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (videoSubPanel != null) videoSubPanel.SetActive(false); // 설정창을 처음 열 때는 비디오 하위창을 닫힌 상태로 초기화.
    }

    // [설정창의 '뒤로가기' 버튼] 클릭 시 호출.
    void CloseSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (videoSubPanel != null) videoSubPanel.SetActive(false); // 설정창을 닫을 때 비디오 하위창도 같이 확실히 닫아준다.
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    #endregion
    #region 씬 전환 관련 함수

    // [버튼 연결] 메인 메뉴로 나가기.
    public void GoToMainMenu() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main_menu");
    }

    #endregion
}
