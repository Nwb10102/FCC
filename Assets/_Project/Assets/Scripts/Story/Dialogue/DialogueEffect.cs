using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DialogueEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text _textComponent;
    [SerializeField] private float _shakeIntensity = 2f;
    [SerializeField] private float _shakeSpeed = 25f;

    private readonly List<(int start, int end)> _shakeRanges = new();
    private bool _hasShakeEffect;

    public void SetText(string rawText)
    {
        _shakeRanges.Clear();
        _textComponent.text = ParseTags(rawText);
        _hasShakeEffect = _shakeRanges.Count > 0;
    }

    private void Update()
    {
        if (_hasShakeEffect)
            AnimateShake();
    }

    // <shake>, <bold> 커스텀 태그를 파싱해 TMP 호환 텍스트로 변환
    // visible 문자 인덱스 기준으로 shake 범위를 _shakeRanges에 기록
    private string ParseTags(string input)
    {
        var sb = new StringBuilder();
        int visibleCount = 0;
        int shakeStart = -1;
        int i = 0;

        while (i < input.Length)
        {
            if (MatchTag(input, i, "<shake>"))
            {
                shakeStart = visibleCount;
                i += "<shake>".Length;
            }
            else if (MatchTag(input, i, "</shake>"))
            {
                if (shakeStart >= 0)
                {
                    _shakeRanges.Add((shakeStart, visibleCount - 1));
                    shakeStart = -1;
                }
                i += "</shake>".Length;
            }
            else if (MatchTag(input, i, "<bold>"))
            {
                sb.Append("<b>");
                i += "<bold>".Length;
            }
            else if (MatchTag(input, i, "</bold>"))
            {
                sb.Append("</b>");
                i += "</bold>".Length;
            }
            else if (input[i] == '<')
            {
                // 기존 TMP 태그(<b>, <color> 등) — visible count 증가 없이 그대로 복사
                int closeIndex = input.IndexOf('>', i);
                if (closeIndex < 0)
                {
                    sb.Append(input[i]);
                    i++;
                }
                else
                {
                    sb.Append(input, i, closeIndex - i + 1);
                    i = closeIndex + 1;
                }
            }
            else
            {
                sb.Append(input[i]);
                visibleCount++;
                i++;
            }
        }

        return sb.ToString();
    }

    private static bool MatchTag(string input, int pos, string tag)
    {
        if (pos + tag.Length > input.Length) return false;
        for (int i = 0; i < tag.Length; i++)
        {
            if (char.ToLowerInvariant(input[pos + i]) != char.ToLowerInvariant(tag[i]))
                return false;
        }
        return true;
    }

    private void AnimateShake()
    {
        _textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textComponent.textInfo;

        foreach (var (start, end) in _shakeRanges)
        {
            for (int i = start; i <= end; i++)
            {
                if (i >= textInfo.characterCount) break;
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int meshIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

                float t = Time.time * _shakeSpeed;
                Vector3 offset = new(
                    Mathf.Sin(t + i * 1.3f) * _shakeIntensity,
                    Mathf.Cos(t + i * 2.1f) * _shakeIntensity,
                    0f);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
            _textComponent.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
    }
}
