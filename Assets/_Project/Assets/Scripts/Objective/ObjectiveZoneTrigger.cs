using UnityEngine;

// 플레이어가 이 영역(Collider2D 트리거)에 들어오면 해당 목표를 완료 처리한다.
// DialogueTriggerZone / CutSceneManager와 동일한 트리거 골격을 사용한다.
public class ObjectiveZoneTrigger : MonoBehaviour
{
    [SerializeField] private string objectiveId;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool _played;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playOnce && _played) return;

        _played = true;
        ObjectiveManager.Instance?.CompleteObjective(objectiveId);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!TryGetComponent<Collider2D>(out var col)) return;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.3f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
#endif
}
