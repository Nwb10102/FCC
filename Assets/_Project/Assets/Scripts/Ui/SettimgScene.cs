using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class NewMonoBehaviourScript : MonoBehaviour
{

public class Settingscript : MonoBehaviour, IPointerClickHandler {

    [Header("SettingScene")]
    [SerializeField] private string sceneName;
    public void OnPointerClick(PointerEventData eventData) {
        SceneManager.LoadScene(sceneName);
    }
} 
}
