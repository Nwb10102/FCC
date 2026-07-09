using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 컷씬에서 대화 한 묶음을 재생하는 스텝.
/// DialogueScript(여러 칸)를 우선 재생하고, 없으면 단발 text를 한 칸으로 재생합니다.
/// advanceAction(예: Ui_Control/NextDialogue)이 들어오면 타자기 출력 중에는 즉시 완성,
/// 완성된 뒤에는 다음 칸으로 넘어갑니다.
/// </summary>
public class DialogueStep : CutSceneStep
{
    [SerializeField] private DialogueView view;
    [SerializeField] private DialogueScript script;            // 우선 재생 (여러 칸/화자/초상화)
    [SerializeField, TextArea(2, 5)] private string text;      // script가 없을 때 쓸 단발 대사
    [SerializeField] private string speaker;                   // 단발 대사의 화자 (선택)
    [SerializeField] private bool hideOnFinish = false;        // 끝나면 대화창 끄기
    [SerializeField] private InputActionReference advanceAction; // 다음/스킵 입력 (Client ▸ Ui_Control ▸ NextDialogue)

    public override IEnumerator Execute()
    {
        DialogueEntry[] entries = BuildEntries();
        if (view == null || entries.Length == 0) yield break;

        InputAction action = advanceAction != null ? advanceAction.action : null;
        bool enabledByUs = action != null && !action.enabled;
        if (enabledByUs) action.Enable();

        foreach (DialogueEntry entry in entries)
        {
            view.Show(entry);
            yield return null;   // 표시 직후 같은 프레임의 입력은 무시

            while (true)
            {
                if (action != null && action.WasPerformedThisFrame())
                {
                    if (view.IsTyping) view.CompleteReveal();   // 출력 중 → 즉시 완성
                    else break;                                 // 완성됨 → 다음 칸으로
                }
                yield return null;
            }
        }

        if (enabledByUs) action.Disable();
        if (hideOnFinish) view.Hide();
    }

    private DialogueEntry[] BuildEntries()
    {
        if (script != null) return script.GetEntries();
        if (!string.IsNullOrEmpty(text))
            return new[] { new DialogueEntry { Speaker = speaker, Portrait = null, Markup = text } };
        return System.Array.Empty<DialogueEntry>();
    }
}
