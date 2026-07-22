using System.Collections.Generic;
using UnityEngine;

// ObjectiveManager의 목표 목록을 체크리스트 UI로 그린다.
// container 아래에 목표당 하나씩 itemPrefab을 생성하고, 갱신 이벤트가 오면 해당 행만 다시 그린다.
public class ObjectiveChecklistView : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private ObjectiveItemView _itemPrefab;

    private readonly Dictionary<string, ObjectiveItemView> _rows = new Dictionary<string, ObjectiveItemView>();

    // OnEnable은 다른 오브젝트의 Awake보다 먼저 실행될 수 있어 ObjectiveManager.Instance가
    // 아직 null일 수 있다. 모든 Awake가 끝난 뒤 보장되는 Start에서 구독/초기 표시를 한다.
    private void Start()
    {
        if (ObjectiveManager.Instance == null) return;

        BuildRows();
        ObjectiveManager.Instance.OnObjectiveUpdated += HandleObjectiveUpdated;
    }

    private void OnDestroy()
    {
        if (ObjectiveManager.Instance == null) return;
        ObjectiveManager.Instance.OnObjectiveUpdated -= HandleObjectiveUpdated;
    }

    private void BuildRows()
    {
        foreach (Transform child in _container) Destroy(child.gameObject);
        _rows.Clear();

        foreach (Objective objective in ObjectiveManager.Instance.Objectives)
        {
            ObjectiveItemView row = Instantiate(_itemPrefab, _container);
            row.Set(objective);
            _rows[objective.id] = row;
        }
    }

    private void HandleObjectiveUpdated(Objective objective)
    {
        if (_rows.TryGetValue(objective.id, out ObjectiveItemView row)) row.Set(objective);
    }
}
