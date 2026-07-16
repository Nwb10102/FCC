using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoSettingsManager : MonoBehaviour {
    #region 인스펙터 변수

    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;

    #endregion
    #region 컴포넌트 변수

    Resolution[] resolutions;

    #endregion
    #region 유니티 라이프 사이클

    void Start() {
        // 사용 가능한 해상도 목록 가져오기.
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++) {
            // "1920 x 1080 (60Hz)" 형태 문자열로 만들기.
            string option = resolutions[i].width + " x " + resolutions[i].height + " (" + resolutions[i].refreshRateRatio.value.ToString("0") + "Hz)";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height) {
                currentResolutionIndex = i;
            }
        }

        // 드롭다운에 옵션 채워넣기.
        resolutionDropdown.AddOptions(options);

        // 저장된 설정값 불러오기.
        int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        resolutionDropdown.value = savedResIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = Screen.fullScreen;
        vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
    }

    #endregion
    #region 비디오 설정 관련 함수

    // 해상도 설정.
    public void SetResolution(int resolutionIndex) {
        if (resolutionIndex >= resolutions.Length) return;

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    // 전체화면 토글.
    public void SetFullscreen(bool isFullscreen) {
        Screen.fullScreen = isFullscreen;
    }

    // 수직 동기화 토글.
    public void SetVSync(bool isVSync) {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        PlayerPrefs.SetInt("VSyncState", isVSync ? 1 : 0);
    }

    #endregion
}
