using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 컷씬에서 대사 한 묶음을 재생하는 스텝.
/// 대사는 이 컴포넌트의 Entries 리스트에서 직접 편집합니다 — 칸마다 화자 / 초상화 / 좌·우 / 본문 / 효과음.
/// advanceAction(Client ▸ Ui ▸ NextDialogue)이 들어오면 타자기 출력 중에는 즉시 완성,
/// 완성된 뒤에는 다음 칸으로 넘어갑니다. 대화창의 AUTO가 켜져 있으면 자동으로 진행됩니다.
/// </summary>
public class DialogueStep : CutSceneStep
{
    [SerializeField] private DialogueView view;
    [SerializeField] private List<DialogueEntry> entries = new();
    [SerializeField] private bool hideOnFinish = false;        // 끝나면 대화창 끄기
    [SerializeField] private InputActionReference advanceAction; // 다음/스킵 입력 (Client ▸ Ui ▸ NextDialogue)

    public override IEnumerator Execute()
    {
        yield return DialoguePlayer.Play(view, entries, advanceAction, hideOnFinish);
    }
}
