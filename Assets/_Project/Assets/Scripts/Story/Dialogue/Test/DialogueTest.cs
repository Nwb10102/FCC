using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DialogueEffect의 마크업 태그(&lt;shake&gt; / &lt;wave&gt; / &lt;rainbow&gt; / &lt;round&gt; / &lt;speed&gt; …)를
/// 눈으로 확인하기 위한 테스트 컴포넌트. 대화창(DialogueView)을 거치지 않고 본문 엔진만 직접 굴립니다.
/// 확인할 문구는 인스펙터의 Texts 리스트에 직접 적습니다.
/// </summary>
public class DialogueTest : MonoBehaviour
{
    [SerializeField] private DialogueEffect _dialogueEffect;

    // 다음/스킵 입력 (Client ▸ Ui ▸ NextDialogue)
    [SerializeField] private InputActionReference _advanceAction;

    // 확인할 문구 목록. 한 칸이 한 화면이며, 5초마다 자동으로 다음 칸으로 넘어갑니다.
    [SerializeField, TextArea(2, 6)] private string[] _texts;

    [SerializeField] private float _cycleSeconds = 5f;

    private int _index;

    private void Start()
    {
        ShowCurrent();
        StartCoroutine(CycleTexts());
    }

    private void OnEnable()
    {
        if (_advanceAction != null) _advanceAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_advanceAction != null) _advanceAction.action.Disable();
    }

    private void Update()
    {
        // NextDialogue(스페이스/좌클릭): 출력 중이면 즉시 전체 출력(스킵), 다 됐으면 다음 대사로
        if (_advanceAction != null && _advanceAction.action.WasPerformedThisFrame())
        {
            if (_dialogueEffect.IsTyping) _dialogueEffect.CompleteReveal();
            else                          Advance();
        }
    }

    private IEnumerator CycleTexts()
    {
        var wait = new WaitForSeconds(_cycleSeconds);
        while (true)
        {
            yield return wait;
            Advance();
        }
    }

    private void Advance()
    {
        if (_texts == null || _texts.Length == 0) return;
        _index = (_index + 1) % _texts.Length;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (_texts == null || _texts.Length == 0) return;
        _dialogueEffect.SetText(_texts[_index]);
        Debug.Log($"[DialogueTest] ({_index + 1}/{_texts.Length})");
    }
}
