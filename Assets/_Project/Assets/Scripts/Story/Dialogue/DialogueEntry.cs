using UnityEngine;

/// <summary>
/// 대화 한 칸(한 화면)의 데이터: 화자 이름 + 초상화 + 본문 마크업.
/// DialogueScript의 Entries 리스트에서 사용합니다.
/// Speaker가 비어 있으면 이름표를 숨기고, Portrait가 비어 있으면 초상화를 숨깁니다.
/// </summary>
[System.Serializable]
public struct DialogueEntry
{
    /// <summary>화자 이름. 비우면 이름표를 표시하지 않습니다.</summary>
    public string Speaker;

    /// <summary>화자 초상화. 비우면 초상화를 표시하지 않습니다.</summary>
    public Sprite Portrait;

    /// <summary>본문. DialogueEffect의 마크업 태그(&lt;shake&gt; 등)를 그대로 사용합니다.</summary>
    [TextArea(2, 6)]
    public string Markup;
}
