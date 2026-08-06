using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// 화면 한쪽에 뜨는 목표 체크리스트. 지금 진행 중인 미션의 이름·설명을 헤더로 띄우고
// 그 아래에 목표를 한 줄씩 그린다.
//
// 생김새는 전부 프리팹 Prefabs/UI/ObjectiveChecklist.prefab 에 있고 이 스크립트는 상태 갱신만 한다.
// 줄 프리팹은 ObjectiveItemView가 맡는다. 처음부터 다시 찍어내려면
// 에디터 메뉴 Tools ▸ FCC ▸ Build Objective Checklist Prefab.
//
// 잠긴(Locked) 목표는 줄을 만들지 않는다 — 뒷 미션 목표까지 처음부터 노출되던 문제를 막기 위함.
public class ObjectiveChecklistView : MonoBehaviour {
    #region 인스펙터 변수

    [Header("연결 — 프리팹이 채워 둔 값입니다")]
    // 진행 중인 미션이 없을 때 통째로 꺼지는 뿌리. 비워두면 아무것도 끄지 않는다.
    public GameObject panelRoot;
    public TMP_Text missionTitleLabel; // 미션 이름.
    public TMP_Text missionDescriptionLabel; // 미션 설명. 설명이 비어 있으면 오브젝트째로 꺼진다.
    public Transform container; // 목표 줄이 쌓이는 부모. VerticalLayoutGroup이 붙어 있다.
    public ObjectiveItemView itemPrefab; // 목표 한 줄 프리팹. **Prefabs/UI/ObjectiveItemRow.prefab**

    #endregion
    #region 런타임 변수

    readonly Dictionary<string, ObjectiveItemView> rows = new();

    // 구독한 매니저를 그대로 들고 있다가 같은 것에서 해제한다. 체크리스트는 씬 오브젝트이고 매니저는
    // DontDestroyOnLoad라, 씬을 다시 열면 Instance가 다른 것으로 바뀌어 있을 수 있다.
    ObjectiveManager manager;

    MissionRuntime shownMission; // 지금 헤더에 띄워둔 미션. 바뀌면 줄을 전부 다시 만든다.
    bool isReady;

    #endregion
    #region 유니티 라이프 사이클

    // OnEnable은 다른 오브젝트의 Awake보다 먼저 실행될 수 있어 ObjectiveManager.Instance가
    // 아직 null일 수 있다. 모든 Awake가 끝난 뒤 보장되는 Start에서 구독/초기 표시를 한다.
    void Start() {
        isReady = ValidateReferences();
        if (!isReady) return;

        if (ObjectiveManager.Instance == null) {
            Debug.LogWarning("[ObjectiveChecklistView] 씬에 ObjectiveManager가 없어 체크리스트를 그릴 수 없습니다.", this);
            SetPanelVisible(false);
            return;
        }

        manager = ObjectiveManager.Instance;
        manager.OnMissionStarted += HandleMissionChanged;
        manager.OnMissionCompleted += HandleMissionChanged;
        manager.OnObjectiveActivated += HandleObjectiveChanged;
        manager.OnObjectiveUpdated += HandleObjectiveChanged;
        manager.OnObjectiveCompleted += HandleObjectiveCompleted;
        manager.OnStateRestored += Rebuild;

        // 언어를 바꾸면 미션 이름·설명과 목표 문구를 전부 새 언어로 다시 받아와야 한다.
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        StartCoroutine(DrawWhenLocalizationReady());
    }

    // String Table이 로드되기 전에 그리면 첫 조회가 초기화까지 통째로 끌어와 한 프레임 끊긴다.
    // 한 번만 기다렸다가 그린다 — 이후 언어 전환은 이미 로드된 상태라 곧바로 갱신된다.
    IEnumerator DrawWhenLocalizationReady() {
        yield return LocalizationText.WaitForInitialization();
        Rebuild();
    }

    void HandleLocaleChanged(Locale locale) {
        Rebuild();
    }

    void OnDestroy() {
        // 씬 오브젝트가 사라져도 static 이벤트에는 구독이 남아 죽은 오브젝트를 갱신하려 든다.
        // manager 구독 해제보다 먼저, 그리고 아래 early return 바깥에서 반드시 떼어낸다.
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        // 파괴된 뒤에도 C# 참조가 남을 수 있어 ?. 대신 != null 로 Unity의 == 오버로드를 탄다.
        if (manager == null) return;

        manager.OnMissionStarted -= HandleMissionChanged;
        manager.OnMissionCompleted -= HandleMissionChanged;
        manager.OnObjectiveActivated -= HandleObjectiveChanged;
        manager.OnObjectiveUpdated -= HandleObjectiveChanged;
        manager.OnObjectiveCompleted -= HandleObjectiveCompleted;
        manager.OnStateRestored -= Rebuild;
    }

    bool ValidateReferences() {
        List<string> missing = new();

        if (container == null) missing.Add(nameof(container));
        if (itemPrefab == null) missing.Add(nameof(itemPrefab));

        if (missing.Count == 0) return true;

        Debug.LogError($"[ObjectiveChecklistView] 프리팹 연결이 비어 있어 체크리스트를 쓸 수 없습니다 — {string.Join(", ", missing)}. " +
            "Prefabs/UI/ObjectiveChecklist 프리팹을 씬에 놓으세요. " +
            "없으면 Tools ▸ FCC ▸ Build Objective Checklist Prefab 으로 만들 수 있습니다.", this);
        return false;
    }

    #endregion
    #region 이벤트 처리

    void HandleMissionChanged(MissionRuntime mission) {
        Rebuild();
    }

    void HandleObjectiveChanged(ObjectiveRuntime objective) {
        // 다른 미션의 목표가 바뀐 것이면 화면에 그릴 것이 없다.
        if (!BelongsToShownMission(objective)) return;

        ObjectiveItemView row = GetOrCreateRow(objective);
        if (row != null) row.Set(objective);
    }

    void HandleObjectiveCompleted(ObjectiveRuntime objective) {
        if (!BelongsToShownMission(objective)) return;

        ObjectiveItemView row = GetOrCreateRow(objective);
        if (row != null) row.PlayCompleted();
    }

    #endregion
    #region 표시 갱신

    // 미션이 바뀌었거나 세이브를 불러왔을 때. 헤더를 갈아끼우고 줄을 처음부터 다시 만든다.
    void Rebuild() {
        if (!isReady || manager == null) return;

        shownMission = manager.CurrentMission;

        ClearRows();

        if (shownMission == null) {
            SetPanelVisible(false);
            return;
        }

        SetPanelVisible(true);
        RefreshHeader(shownMission);

        // 잠긴 목표는 건너뛴다. 나중에 활성되면 OnObjectiveActivated로 줄이 새로 생긴다.
        foreach (ObjectiveRuntime objective in shownMission.objectives) {
            if (objective.state == ObjectiveState.Locked) continue;
            CreateRow(objective);
        }
    }

    void RefreshHeader(MissionRuntime mission) {
        if (missionTitleLabel != null) missionTitleLabel.text = mission.DisplayName;

        if (missionDescriptionLabel == null) return;

        // 설명을 비워둔 미션이면 빈 줄이 자리를 먹지 않게 오브젝트를 끈다.
        bool hasDescription = !string.IsNullOrWhiteSpace(mission.Description);
        missionDescriptionLabel.gameObject.SetActive(hasDescription);
        if (hasDescription) missionDescriptionLabel.text = mission.Description;
    }

    // 이미 줄이 있으면 그것을, 없으면 새로 만들어 돌려준다.
    // 예전에는 Start에서 한 번만 줄을 지어서 도중에 활성된 목표가 화면에 나타나지 못했다.
    ObjectiveItemView GetOrCreateRow(ObjectiveRuntime objective) {
        if (rows.TryGetValue(objective.Id, out ObjectiveItemView existing)) return existing;

        // 아직 잠긴 목표는 줄을 만들지 않는다.
        if (objective.state == ObjectiveState.Locked) return null;

        return CreateRow(objective);
    }

    ObjectiveItemView CreateRow(ObjectiveRuntime objective) {
        ObjectiveItemView row = Instantiate(itemPrefab, container);
        row.Set(objective);
        rows[objective.Id] = row;
        return row;
    }

    void ClearRows() {
        foreach (ObjectiveItemView row in rows.Values) {
            if (row != null) Destroy(row.gameObject);
        }
        rows.Clear();
    }

    void SetPanelVisible(bool visible) {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }

    bool BelongsToShownMission(ObjectiveRuntime objective) {
        return shownMission != null && shownMission.objectives.Contains(objective);
    }

    #endregion
}
