using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Settingscript : MonoBehaviour, IPointerClickHandler {
    #region 인스펙터 변수

    [Header("씬 이름")]
    [SerializeField] string sceneName;

    #endregion
    #region 입력 처리

    public void OnPointerClick(PointerEventData eventData) {
        SceneManager.LoadScene(sceneName);
    }

    #endregion
}
