using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UI_Panel : MonoBehaviour {
    [Header("UI Panel")]
    public GameObject pauseMenuPanel;

    private bool isPaused = false;
    private InputAction escapeAction;

    private void Awake() {
        // �ٸ� ������ ������ �������� �α� ����
        Debug.unityLogger.filterLogType = LogType.Error;

        // �ý��ۿ� ESC Ű �Է� ���
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
    }

    private void OnEnable() {
        escapeAction.Enable();
    }

    private void OnDisable() {
        escapeAction.Disable();
    }

    void Update() {
        // �� .triggered�� ����ϸ� �����Ӱ� ������� Ű���尡 'Ź!' ���� �� ���� �� �� ���� ��ȣ�� �����ϴ�.
        if (escapeAction.triggered) {

            // ���¸� �ݴ�� �������ϴ� (false -> true / true -> false)
            isPaused = !isPaused;

            if (isPaused) {
                Pause();
            } else {
                Resume();
            }
        }
    }

    public void Resume() {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false; // ��ư Ŭ������ �� ���� ����� ���� Ȯ��
    }

    void Pause() {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true; // ���� Ȯ��
    }

    public void GoToOptions() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SettingScene");
    }

    public void GoToMainMenu() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main_menu");
    }
}