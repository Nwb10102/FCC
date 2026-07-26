using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 이 영역(Collider2D 트리거)에 들어오면 Entries 리스트의 대사를 순서대로 재생합니다.
/// 대사는 이 컴포넌트의 인스펙터에서 직접 편집합니다 — 칸마다 화자 / 초상화 / 좌·우 / 본문 / 효과음.
/// CutSceneManager의 트리거 골격 + DialoguePlayer의 재생 루프를 합친 컴포넌트입니다.
/// </summary>
public class DialogueTriggerZone : MonoBehaviour
{
    [SerializeField] private DialogueView view;
    [SerializeField] private List<DialogueEntry> entries = new();
    [SerializeField] private bool hideOnFinish = true;         // 끝나면 대화창 끄기
    [SerializeField] private InputActionReference advanceAction; // 다음/스킵 입력 (Client ▸ Ui ▸ NextDialogue)

    [SerializeField] private bool playOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool lockPlayerMovement = true;   // 대화 중 플레이어 이동 잠금 (Player_move.isMovementLocked 재사용)
    [SerializeField] private string objectiveId;                // 비어있지 않으면 대화 종료 시 해당 목표를 완료 처리

    public static event Action OnDialogueStart;
    public static event Action OnDialogueEnd;

    private bool _played;
    private static bool _isPlaying;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && _played) return;
        if (_isPlaying) return;
        StartCoroutine(RunDialogue(other));
    }

    private IEnumerator RunDialogue(Collider2D playerCollider)
    {
        if (view == null)
        {
            Debug.LogError($"[DialogueTriggerZone] '{name}' 의 View 칸이 비어 있어 대화를 재생할 수 없습니다.", this);
            yield break;
        }
        if (entries.Count == 0)
        {
            Debug.LogWarning($"[DialogueTriggerZone] '{name}' 의 Entries 리스트가 비어 있습니다.", this);
            yield break;
        }

        _isPlaying = true;
        _played = true;
        OnDialogueStart?.Invoke();

        Player_move playerMove = lockPlayerMovement ? playerCollider.GetComponent<Player_move>() : null;
        if (playerMove != null) playerMove.isMovementLocked = true;

        yield return DialoguePlayer.Play(view, entries, advanceAction, hideOnFinish);

        if (playerMove != null) playerMove.isMovementLocked = false;

        _isPlaying = false;
        OnDialogueEnd?.Invoke();

        if (!string.IsNullOrEmpty(objectiveId)) ObjectiveManager.Instance?.CompleteObjective(objectiveId);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!TryGetComponent<Collider2D>(out var col)) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
