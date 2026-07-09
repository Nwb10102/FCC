using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTest : MonoBehaviour
{
    [SerializeField] private DialogueEffect _dialogueEffect;

    // 다음/스킵 입력 (Client ▸ Ui_Control ▸ NextDialogue)
    [SerializeField] private InputActionReference _advanceAction;

    // 인스펙터에 DialogueScript 에셋을 넣으면 그걸 재생합니다.
    // 비워두면 아래 _markupExamples(마크업 문자열)로 동작합니다.
    [SerializeField] private DialogueScript[] _scripts;

    // 에셋 없이 바로 테스트하기 위한 내장 마크업 예시
    private readonly string[] _markupExamples =
    {
        "안녕하세요, 저는 광대입니다.",

        "<shake>조심해! 폭발한다!!</shake>",

        "<shake a=8 s=50>으아아아아!!!</shake>",

        "<bold>이것은 매우 중요한 정보입니다.</bold>",

        "<wave>바람이 살랑살랑 불어온다.</wave>",

        "<wave a=8 s=8>폭풍이 몰아친다!!</wave>",

        "<rainbow>화려한 마법사가 나타났다!</rainbow>",

        "<rainbow><wave>마법의 파도가 밀려온다!</wave></rainbow>",

        "<rainbow><shake>위험한 마법이 발동됐다!</shake></rainbow>",

        "<shake><wave><rainbow>대혼돈!!</rainbow></wave></shake>",

        // 여러 줄 + 줄마다 다른 강도
        "<wave a=2 s=1.5>부드럽게...</wave>\n" +
        "<wave a=6 s=6>점점 격렬하게!</wave>\n" +
        "<shake a=10 s=50>대폭발!!!!</shake>",

        "<round>빙글빙글 돌아간다~</round>",

        "<round r=8 s=6>어지러워!!</round>",

        "<round><rainbow>회오리 마법!</rainbow></round>",

        // 줄마다 다른 출력 속도
        "<speed=6>느리게... 천천히...</speed>\n" +
        "<speed=40>그리고 갑자기 빠르게!!</speed>",
    };

    private int _index;
    private readonly WaitForSeconds _wait = new(5f);

    // 모든 소스를 "페이지(화면)" 단위로 펼친 목록
    private readonly List<string> _pages = new();

    private void Start()
    {
        BuildPages();
        ShowCurrent();
        StartCoroutine(CycleTexts());
    }

    private void BuildPages()
    {
        _pages.Clear();
        if (_scripts != null && _scripts.Length > 0)
        {
            foreach (DialogueScript script in _scripts)
            {
                if (script == null) continue;
                _pages.AddRange(script.GetPages());
            }
        }
        else
        {
            _pages.AddRange(_markupExamples);
        }
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
        while (true)
        {
            yield return _wait;
            Advance();
        }
    }

    private void Advance()
    {
        if (_pages.Count == 0) return;
        _index = (_index + 1) % _pages.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (_pages.Count == 0) return;
        _dialogueEffect.SetText(_pages[_index]);
        Debug.Log($"[DialogueTest] ({_index + 1}/{_pages.Count})");
    }
}
