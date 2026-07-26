using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 대사 리스트를 한 칸씩 재생하는 공용 루프.
/// DialogueTriggerZone(영역 진입)과 DialogueStep(컷씬)이 같은 재생 규칙을 쓰도록 여기로 모았습니다.
///
/// 한 칸의 진행 규칙:
///   입력(Client ▸ Ui ▸ NextDialogue) — 타자기 출력 중이면 즉시 완성, 완성된 뒤면 다음 칸으로
///   AUTO 켜짐                        — 출력이 끝나고 DialogueView.AutoAdvanceDelay 초가 지나면 자동으로 다음 칸으로
/// </summary>
public static class DialoguePlayer
{
    /// <summary>entries를 순서대로 view에 표시합니다. 호출 측 코루틴에서 yield return 하세요.</summary>
    public static IEnumerator Play(DialogueView view, IList<DialogueEntry> entries,
                                   InputActionReference advanceAction, bool hideOnFinish)
    {
        if (view == null || entries == null || entries.Count == 0) yield break;

        InputAction action = advanceAction != null ? advanceAction.action : null;
        bool enabledByUs = action != null && !action.enabled;
        if (enabledByUs) action.Enable();

        foreach (DialogueEntry entry in entries)
        {
            view.Show(entry);
            yield return null;   // 표시 직후 같은 프레임의 입력은 무시

            float autoTimer = 0f;

            while (true)
            {
                // BlockAdvanceThisFrame: AUTO 버튼을 누른 클릭이 "다음 칸"으로도 먹히는 것을 막습니다.
                bool advanced = action != null
                             && action.WasPerformedThisFrame()
                             && !view.BlockAdvanceThisFrame;

                if (advanced)
                {
                    if (view.IsTyping) view.CompleteReveal();   // 출력 중 → 즉시 완성
                    else break;                                 // 완성됨 → 다음 칸으로
                }

                if (view.IsTyping)
                {
                    autoTimer = 0f;                             // 아직 출력 중이면 자동 진행 대기를 미룸
                }
                else if (view.IsAutoAdvance)
                {
                    autoTimer += Time.deltaTime;
                    if (autoTimer >= view.AutoAdvanceDelay) break;
                }

                yield return null;
            }
        }

        if (enabledByUs) action.Disable();
        if (hideOnFinish) view.Hide();
    }
}
