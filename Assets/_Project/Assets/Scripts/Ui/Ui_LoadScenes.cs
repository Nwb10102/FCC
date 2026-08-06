using UnityEngine;
using UnityEngine.EventSystems;

// 클릭하면 지정한 씬으로 넘어가는 버튼. Button 컴포넌트 없이 클릭만 받으면 되는 단순한 메뉴 항목용이다.
//
// 씬 로드를 직접 하지 않고 ScreenFader를 거치는 이유는, 화면이 검게 덮인 뒤 넘어가야 하기 때문이다.
// 씬에 ScreenFader가 없으면 ScreenFader.LoadScene이 알아서 연출 없이 바로 넘긴다.
public class Ui_LoadScenes : MonoBehaviour, IPointerClickHandler {
    #region 인스펙터 변수

    [Header("씬 이름")]
    // **빌드 세팅(File ▸ Build Profiles)에 등록된 씬 이름을 정확히 적으세요.** 확장자 없이 이름만 적는다.
    [SerializeField] string sceneName;

    #endregion
    #region 입력 처리

    public void OnPointerClick(PointerEventData eventData) {
        ScreenFader.LoadScene(sceneName);
    }

    #endregion
}
