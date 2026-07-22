using TMPro;
using UnityEngine;

// 체크리스트 한 줄: 설명 텍스트 + 완료 체크 표시.
public class ObjectiveItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    public void Set(Objective objective)
    {
        if (_label == null) return;

        string checkbox = objective.isCompleted ? "☑" : "☐";
        string countSuffix = objective.type == MissionType.CollectItem
            ? $" ({objective.currentCount}/{objective.targetCount})"
            : string.Empty;

        _label.text = $"{checkbox} {objective.description}{countSuffix}";
        _label.fontStyle = objective.isCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}
