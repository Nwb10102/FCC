using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class Settingscript : MonoBehaviour, IPointerClickHandler {

    [Header("씬 이름")]
    [SerializeField] private string sceneName;
    public void OnPointerClick(PointerEventData eventData) {
        SceneManager.LoadScene(sceneName);
    }
} 

