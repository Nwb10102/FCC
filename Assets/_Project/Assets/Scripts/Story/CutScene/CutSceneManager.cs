using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string objectiveId;                // 비어있지 않으면 컷신 종료 시 해당 목표를 완료 처리

    public static event Action OnCutSceneStart;
    public static event Action OnCutSceneEnd;

    private bool _played;
    private static bool _isPlaying;
    private List<CutSceneStep> _steps;

    private void Awake()
    {
        _steps = new List<CutSceneStep>(GetComponentsInChildren<CutSceneStep>());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && _played) return;
        if (_isPlaying) return;
        StartCoroutine(RunCutScene());
    }

    public void PlayManually()
    {
        if (_isPlaying) return;
        StartCoroutine(RunCutScene());
    }

    private IEnumerator RunCutScene()
    {
        _isPlaying = true;
        _played = true;
        OnCutSceneStart?.Invoke();

        foreach (var step in _steps)
            yield return StartCoroutine(step.Execute());

        _isPlaying = false;
        OnCutSceneEnd?.Invoke();

        if (!string.IsNullOrEmpty(objectiveId)) ObjectiveManager.Instance?.CompleteObjective(objectiveId);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!TryGetComponent<Collider2D>(out var col)) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
