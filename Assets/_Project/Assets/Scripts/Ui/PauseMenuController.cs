using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;      // 메인 일시정지 창
    public GameObject settingsPanel;  // 설정 창
    public GameObject videoSubPanel;  // 비디오 설정 상세 창

    private InputAction escapeAction;

    private void Awake()
    {
        Debug.unityLogger.filterLogType = LogType.Error;
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
    }

    private void OnEnable() { escapeAction.Enable(); }
    private void OnDisable() { escapeAction.Disable(); }

    void Update()
    {
        // ESC 키가 눌렸을 때 상태를 확인하고 순서대로 처리
        if (escapeAction.triggered)
        {

            // 비디오 상세 창(videoSubPanel)
            if (videoSubPanel != null && videoSubPanel.activeSelf)
            {
                videoSubPanel.SetActive(false);
            }
            // 설정창(settingsPanel)
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            // 메인창(mainPanel)
            else if (mainPanel != null && mainPanel.activeSelf)
            {
                Resume();
            }
            // 아무것도 안 켜져 있다면
            else
            {
                Pause();
            }
        }
    }

    // 게임으로 돌아가기
    public void Resume()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (videoSubPanel != null) videoSubPanel.SetActive(false); 
        Time.timeScale = 1f; // 시간 흐름 정상화
    }

    // 일시정지 켜기 (게임 중 ESC 눌렀을 때)
    public void Pause()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        Time.timeScale = 0f; // 시간 정지
    }

    // [메인창에서 'Options(Setting)' 버튼을 눌렀을 때 실행
    public void GoToOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false); // 메인창 숨기기
        if (settingsPanel != null) settingsPanel.SetActive(true); // 설정창 띄우기
        if (videoSubPanel != null) videoSubPanel.SetActive(false); // [버그 수정] 설정창 열 때는 비디오 창이 무조건 꺼져있도록 초기화
    }

    // [내부 기능] 설정창에서 빠져나올 때 실행
    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false); // 설정창 숨기기
        if (videoSubPanel != null) videoSubPanel.SetActive(false); // [버그 수정] 설정창이 닫힐 때 비디오 창도 확실하게 같이 끄기
        if (mainPanel != null) mainPanel.SetActive(true); // 다시 메인창 띄우기
    }

    // [버튼 전용] 메인메뉴로 나가기
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main_menu");
    }
}