using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour {
    [Header("UI Panels")]
    public GameObject settingsPanel; // 1단계에서 가져온 설정창 프리팹

    private InputAction escapeAction;

    private void Awake() {
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
    }

    private void OnEnable() { escapeAction.Enable(); }
    private void OnDisable() { escapeAction.Disable(); }

    void Update() {
        // 메인 메뉴에서도 ESC를 누르면 켜져있는 설정창이 닫히도록 만듭니다.
        if (escapeAction.triggered) {
            if (settingsPanel != null && settingsPanel.activeSelf) {
                CloseSettings();
            }
        }
    }

    // 1. [Setting] 버튼을 눌렀을 때 실행
    public void OpenSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(true); // 설정창 켜기
    }

    // [내부 기능] 설정창 닫기
    private void CloseSettings() {
        if (settingsPanel != null) settingsPanel.SetActive(false); // 설정창 숨기기
    }

    // 2. [Game Start] 버튼을 눌렀을 때 실행
    public void StartGame() {
        // "master_scene"으로 이동합니다. (씬 이름이 다르면 여기를 수정하세요)
        SceneManager.LoadScene("master_scene");
    }

    // 3. [Exit] 버튼을 눌렀을 때 실행
    public void ExitGame() {
        Debug.Log("게임 종료!");
        Application.Quit(); // 실제 빌드된 게임파일에서 게임을 꺼줍니다.
    }
}