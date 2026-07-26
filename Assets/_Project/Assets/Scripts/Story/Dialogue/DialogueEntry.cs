using UnityEngine;

/// <summary>초상화를 띄울 쪽. 반대쪽 초상화는 숨겨집니다.</summary>
public enum PortraitSide
{
    [InspectorName("왼쪽")]   Left,
    [InspectorName("오른쪽")] Right,
}

/// <summary>
/// 대화 한 칸(한 화면)의 데이터: 화자 이름 + 초상화 + 본문 + 효과음.
/// DialogueTriggerZone / DialogueStep 인스펙터의 Entries 리스트에서 한 칸씩 채웁니다.
/// Speaker가 비어 있으면 이름표를, Portrait가 비어 있으면 초상화를 숨깁니다.
/// </summary>
[System.Serializable]
public struct DialogueEntry
{
    /// <summary>화자 이름. 비우면 이름표를 표시하지 않습니다.</summary>
    public string Speaker;

    /// <summary>화자 초상화. 비우면 초상화를 표시하지 않습니다.</summary>
    public Sprite Portrait;

    /// <summary>초상화를 띄울 쪽. 반대쪽 초상화는 숨겨집니다.</summary>
    public PortraitSide Side;

    /// <summary>본문. DialogueEffect의 마크업 태그(&lt;shake&gt; 등)를 그대로 사용합니다.</summary>
    [TextArea(2, 6)]
    public string Text;

    /// <summary>이 칸을 띄울 때 한 번 재생할 효과음. 비우면 무음.</summary>
    public AudioClip Sound;
}
