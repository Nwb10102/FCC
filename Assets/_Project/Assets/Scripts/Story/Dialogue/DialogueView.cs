using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화창 UI 한 묶음을 제어합니다: 패널 루트 + 본문(DialogueEffect) + 이름표 + 초상화.
/// DialogueStep 같은 재생 측에서 Show(entry)로 한 칸씩 표시하고,
/// 타자기 출력 스킵은 IsTyping / CompleteReveal 로 처리합니다.
/// 이름표/초상화 참조는 선택 사항(비워도 동작)입니다.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [SerializeField] private GameObject    _root;           // 켜고 끌 패널 루트 (비우면 이 오브젝트)
    [SerializeField] private DialogueEffect _effect;        // 본문 텍스트 엔진
    [SerializeField] private TMP_Text      _nameText;       // 화자 이름표 (선택)
    [SerializeField] private GameObject    _nameBox;        // 이름 없을 때 통째로 숨길 박스 (선택)
    [SerializeField] private Image         _portraitImage;  // 화자 초상화 (선택)

    /// <summary>본문 타자기 출력이 진행 중인지 여부.</summary>
    public bool IsTyping => _effect != null && _effect.IsTyping;

    /// <summary>대화창을 켜고 한 칸(화자 + 초상화 + 본문)을 표시합니다.</summary>
    public void Show(DialogueEntry entry)
    {
        SetRootActive(true);
        SetSpeaker(entry.Speaker, entry.Portrait);
        if (_effect != null) _effect.SetText(entry.Markup);
    }

    /// <summary>진행 중인 타자기 출력을 즉시 끝냅니다.</summary>
    public void CompleteReveal()
    {
        if (_effect != null) _effect.CompleteReveal();
    }

    /// <summary>대화창을 끕니다.</summary>
    public void Hide() => SetRootActive(false);

    private void SetSpeaker(string speaker, Sprite portrait)
    {
        bool hasName = !string.IsNullOrEmpty(speaker);
        if (_nameText != null) _nameText.text = speaker;
        if (_nameBox  != null) _nameBox.SetActive(hasName);

        if (_portraitImage != null)
        {
            _portraitImage.sprite  = portrait;
            _portraitImage.enabled = portrait != null;
        }
    }

    private void SetRootActive(bool active)
    {
        GameObject target = _root != null ? _root : gameObject;
        target.SetActive(active);
    }
}
