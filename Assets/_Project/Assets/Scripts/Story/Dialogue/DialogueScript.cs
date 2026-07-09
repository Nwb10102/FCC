using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 대사 한 묶음(여러 화면)을 마크업 텍스트로 담는 에셋.
/// Project 창에서 [Create ▸ Dialogue ▸ Dialogue Script] 로 생성하고
/// 인스펙터의 Markup 칸에 글처럼 작성합니다.
///
/// 한 줄에 "---" 만 적으면 페이지(화면)가 나뉩니다. 예:
///   안녕, 나는 광대야.
///   ---
///   <![CDATA[<shake>조심해!!</shake>]]>
///
/// 지원 태그:
///   &lt;shake a=8 s=50&gt; … &lt;/shake&gt;     a=강도   s=속도
///   &lt;wave a=8 s=8 f=1.5&gt; … &lt;/wave&gt;  a=진폭   s=속도   f=주파수
///   &lt;round r=8 s=6&gt; … &lt;/round&gt;      r=반경   s=속도
///   &lt;rainbow s=2&gt; … &lt;/rainbow&gt;      s=속도
///   &lt;speed=6&gt; … &lt;/speed&gt;            타자기 출력 속도(초당 글자 수)
///   &lt;bold&gt; … &lt;/bold&gt;                굵게
/// 파라미터는 생략하면 DialogueEffect 컴포넌트의 기본값을 사용합니다.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Dialogue Script", fileName = "NewDialogue")]
public class DialogueScript : ScriptableObject
{
    /// <summary>화자/초상화까지 포함한 대화 칸 목록. 이 리스트가 비어 있지 않으면
    /// 우선 사용되고, 비어 있으면 아래 Markup("---" 분할)을 사용합니다(레거시).</summary>
    public List<DialogueEntry> Entries = new();

    [TextArea(5, 20)]
    public string Markup;

    /// <summary>대화를 (화자 + 초상화 + 마크업) 칸 단위로 반환합니다.
    /// Entries가 있으면 그대로, 없으면 Markup을 "---"로 나눠 화자 없는 칸으로 변환합니다.</summary>
    public DialogueEntry[] GetEntries()
    {
        if (Entries != null && Entries.Count > 0)
            return Entries.ToArray();

        string[] pages = GetPages();
        var result = new DialogueEntry[pages.Length];
        for (int i = 0; i < pages.Length; i++)
            result[i] = new DialogueEntry { Speaker = string.Empty, Portrait = null, Markup = pages[i] };
        return result;
    }

    /// <summary>"---" 구분자로 나눈 페이지(화면) 목록. 빈 페이지는 제외됩니다.</summary>
    public string[] GetPages()
    {
        if (string.IsNullOrEmpty(Markup)) return System.Array.Empty<string>();

        string[] lines = Markup.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var pages = new List<string>();
        var sb    = new StringBuilder();

        foreach (string line in lines)
        {
            if (line.Trim() == "---")
            {
                pages.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
        }
        pages.Add(sb.ToString().Trim());

        pages.RemoveAll(string.IsNullOrEmpty);
        return pages.ToArray();
    }
}
