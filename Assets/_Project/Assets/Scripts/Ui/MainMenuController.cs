using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour {
    #region 인스펙터 변수

    [Header("UI Panels")]
    public GameObject settingsPanel; // 메인 메뉴에서 열리는 설정창.

    #endregion
    #region 컴포넌트 변수

    InputAction escapeAction;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
    }

    void OnEnable() { escapeAction.Enable(); }
    void OnDisable() { escapeAction.Disable(); }

    void Update() {
        // 메인 메뉴에서 ESC를 누르면 열려있는 설정창을 닫아준다.
        if (escapeAction.triggered && settingsPanel != null && settingsPanel.activeSelf) {
            CloseSettings();
        }
    }

    #endregion
    #region 설정창 관련 함수

    // [Setting] 버튼 클릭 시 호출.
    public void OpenSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    void CloseSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    #endregion
    #region 씬 전환 관련 함수

    // [Game Start] 버튼 클릭 시 호출.
    public void StartGame() {
        SceneManager.LoadScene("master_scene"); // 씬 이름이 바뀌면 여기를 수정.
    }

    // [Exit] 버튼 클릭 시 호출.
    public void ExitGame() {
        Debug.Log("게임 종료!");
        Application.Quit(); // 에디터에서는 동작하지 않고 빌드에서만 실제로 종료됨.
    }

    #endregion
}
