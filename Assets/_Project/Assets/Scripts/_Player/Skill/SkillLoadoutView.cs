using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 거울(SaveMirror)에서 여는 스킬 정비 화면. 장착 슬롯 3칸과 해금된 스킬 목록을 보여주고 교체시킨다.
//
// 생김새(Canvas·창·슬롯·목록 줄)는 전부 프리팹 Prefabs/UI/SkillLoadout.prefab 에 들어 있고, 이 스크립트는
// 상태 계산과 입력만 맡는다. 예전에는 Canvas를 코드로 지었는데 색·간격 하나를 옮길 때마다 스크립트를
// 고쳐야 했고 씬 뷰에서 결과를 볼 수 없어서 프리팹으로 뺐다. 표시 담당은 SkillSlotView·SkillRowView다.
// 프리팹을 처음부터 다시 찍어내려면 에디터 메뉴 Tools ▸ FCC ▸ Build Skill Loadout Prefab.
//
// **씬에 SkillLoadout 프리팹을 하나만 놓으세요.** (HitFeedback·HitVfx와 같은 싱글턴)
public class SkillLoadoutView : MonoBehaviour {
    public static SkillLoadoutView Instance;

    #region 인스펙터 변수

    [Header("연결 — 프리팹이 채워 둔 값입니다")]
    // 화면 전체를 덮는 어두운 막이자 창의 부모. 열고 닫을 때 이것만 켜고 끈다
    // (Canvas 자체를 끄면 다시 켤 때 레이아웃이 한 프레임 튄다).
    public GameObject windowRoot;
    // 장착 슬롯 3칸. **배열 순서가 그대로 슬롯 번호(0·1·2)입니다.** 순서를 바꾸면 표시도 따라 바뀐다.
    public SkillSlotView[] slotViews = new SkillSlotView[SkillManager.SlotCount];
    public Transform rowContainer; // 목록 줄이 쌓이는 부모. VerticalLayoutGroup이 붙어 있다.
    public SkillRowView rowPrefab; // 목록 한 줄 프리팹. **Prefabs/UI/SkillLoadoutRow.prefab**
    public TMP_Text listTitleLabel; // "보유 스킬". 목록이 비면 emptyListText로 갈아치운다.
    public TMP_Text descriptionLabel; // 커서가 올라간 스킬의 설명문.

    [Header("문구")]
    // 제목·도움말처럼 고정된 문구는 프리팹의 TMP에 직접 적는다. 여기 있는 둘은 상황에 따라 코드가 갈아끼우는 것들.
    public string emptyListText = "아직 해금한 스킬이 없습니다.";
    public string emptySlotText = "비어 있음";

    #endregion
    #region 컴포넌트 변수

    readonly List<SkillRowView> rows = new();

    SkillManager manager;
    Player_move playerMove;
    PauseMenuController pauseMenu; // 열려 있는 동안 꺼둔다. ESC를 서로 먹으면 timeScale이 엉키기 때문.

    string authoredListTitle; // 프리팹에 적혀 있던 목록 제목. 빈 목록 문구로 바꿨다가 되돌릴 때 쓴다.
    int selectedSlot; // 지금 고른 장착 슬롯 (0~2).
    int highlightedRow; // 지금 커서가 올라간 스킬 줄.
    float savedTimeScale = 1f;
    bool isReady; // 프리팹 연결이 온전한지. 어긋난 채로 열면 NullReference가 쏟아지므로 Awake에서 한 번만 검사한다.

    public bool IsOpen { get; private set; }

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        if (Instance != null && Instance != this) {
            // 코드로 UI를 짓던 시절에는 컴포넌트만 지웠지만, 이제는 프리팹 뿌리째 들어오므로
            // 오브젝트를 통째로 지워야 한다. 안 그러면 안 쓰는 Canvas가 화면에 남는다.
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad는 루트 오브젝트에서만 동작한다. 씬에서 정리용으로 다른 오브젝트 밑에
        // 넣어뒀을 수 있으므로 먼저 떼어낸다 (SaveManager와 같은 이유).
        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        isReady = ValidateReferences();
        if (!isReady) return;

        if (listTitleLabel != null) authoredListTitle = listTitleLabel.text;

        // 슬롯 번호는 배열 순서에서만 나온다. 여기서 한 번 물려두면 클릭이 알아서 SelectSlot으로 들어온다.
        for (int i = 0; i < SkillManager.SlotCount; i++) {
            slotViews[i].Bind(i, SelectSlot);
        }

        windowRoot.SetActive(false);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void Update() {
        if (!IsOpen) return;
        HandleInput();
    }

    #endregion
    #region 프리팹 연결 검사

    // 프리팹을 쓰지 않고 컴포넌트만 붙였거나, 프리팹을 손보다 참조를 끊었을 때 조용히 죽지 않도록
    // 무엇이 비었는지 이름으로 찍어준다. 하나라도 비면 화면을 아예 열지 않는다.
    bool ValidateReferences() {
        List<string> missing = new();

        if (windowRoot == null) missing.Add(nameof(windowRoot));
        if (rowContainer == null) missing.Add(nameof(rowContainer));
        if (rowPrefab == null) missing.Add(nameof(rowPrefab));

        if (slotViews == null || slotViews.Length < SkillManager.SlotCount) {
            missing.Add($"{nameof(slotViews)}(칸 {SkillManager.SlotCount}개 필요)");
        }
        else {
            for (int i = 0; i < SkillManager.SlotCount; i++) {
                if (slotViews[i] == null) missing.Add($"{nameof(slotViews)}[{i}]");
            }
        }

        if (missing.Count == 0) return true;

        Debug.LogError($"[SkillLoadoutView] 프리팹 연결이 비어 있어 정비 화면을 쓸 수 없습니다 — {string.Join(", ", missing)}. " +
            "Prefabs/UI/SkillLoadout 프리팹을 씬에 놓으세요. 프리팹이 없으면 Tools ▸ FCC ▸ Build Skill Loadout Prefab 으로 만들 수 있습니다.", this);
        return false;
    }

    #endregion
    #region 열기 · 닫기

    // SaveMirror가 저장을 마친 뒤 호출한다. manager가 없으면 열지 않는다.
    public void Open(SkillManager skillManager) {
        if (IsOpen || !isReady) return;

        if (skillManager == null) {
            Debug.LogWarning("[SkillLoadoutView] SkillManager를 찾지 못해 정비 화면을 열 수 없습니다. 플레이어에 SkillManager를 붙이세요.", this);
            return;
        }

        manager = skillManager;
        playerMove = manager.GetComponentInParent<Player_move>();

        // 정비 화면이 떠 있는 동안 플레이어가 움직이거나 거울에 다시 말을 거는 것을 막는다.
        // PlayerInteractor·SkillManager가 둘 다 이 잠금을 보고 입력을 무시한다 (대사·컷씬과 같은 방식).
        if (playerMove != null) playerMove.isMovementLocked = true;

        // ESC를 서로 먹으면 한쪽은 정비 화면을 닫고 다른 쪽은 일시정지를 켜서 timeScale이 어긋난다.
        pauseMenu = FindAnyObjectByType<PauseMenuController>();
        if (pauseMenu != null) pauseMenu.enabled = false;

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f; // 정비 중에는 몬스터도 멈춘다.

        selectedSlot = 0;
        highlightedRow = 0;

        BuildRows();
        Refresh();

        windowRoot.SetActive(true);
        IsOpen = true;
    }

    public void Close() {
        if (!IsOpen) return;

        IsOpen = false;
        windowRoot.SetActive(false);

        Time.timeScale = savedTimeScale;

        if (pauseMenu != null) pauseMenu.enabled = true;
        if (playerMove != null) playerMove.isMovementLocked = false;

        manager = null;
        playerMove = null;
        pauseMenu = null;
    }

    #endregion
    #region 입력 처리

    // EventSystem의 내비게이션 액션에 기대지 않고 키보드를 직접 읽는다.
    // UI 모듈 설정이 씬마다 달라도 똑같이 동작해야 하기 때문 (마우스 클릭은 Button이 따로 받는다).
    void HandleInput() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) {
            Close();
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2);

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) MoveHighlight(-1);
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) MoveHighlight(1);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) {
            EquipHighlighted();
        }

        if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame) {
            UnequipSelectedSlot();
        }
    }

    #endregion
    #region 조작

    void SelectSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= SkillManager.SlotCount) return;

        selectedSlot = slotIndex;
        Refresh();
    }

    void MoveHighlight(int delta) {
        if (rows.Count == 0) return;

        // 목록 끝에서 반대편으로 넘어가게 감싼다. 스킬이 몇 개 없어 위아래로 자주 오가기 때문.
        highlightedRow = (highlightedRow + delta + rows.Count) % rows.Count;
        Refresh();
    }

    void EquipHighlighted() {
        if (manager == null || highlightedRow < 0 || highlightedRow >= rows.Count) return;

        SkillBase skill = rows[highlightedRow].Skill;

        // 고른 슬롯에 이미 그 스킬이 들어있으면 해제로 동작한다. 같은 키로 넣고 빼는 편이 손에 익는다.
        if (manager.GetSkillInSlot(selectedSlot) == skill) {
            manager.UnequipSlot(selectedSlot);
        }
        else {
            manager.EquipSkill(selectedSlot, skill);
        }

        Refresh();
    }

    void UnequipSelectedSlot() {
        if (manager == null) return;

        manager.UnequipSlot(selectedSlot);
        Refresh();
    }

    // 마우스로 줄을 누르면 커서를 그 줄로 옮기고 바로 장착까지 한다. 키보드 흐름과 결과가 같도록.
    void HandleRowClicked(SkillBase skill) {
        int index = rows.FindIndex(row => row.Skill == skill);
        if (index < 0) return;

        highlightedRow = index;
        EquipHighlighted();
    }

    #endregion
    #region 표시 갱신

    // 해금 목록에 맞춰 줄을 새로 찍는다. 열 때마다 목록이 달라질 수 있어 매번 다시 짓는다
    // (스킬은 최대 6종이라 재사용 풀을 둘 만큼 무겁지 않다).
    void BuildRows() {
        foreach (SkillRowView row in rows) {
            if (row != null) Destroy(row.gameObject);
        }
        rows.Clear();

        foreach (SkillBase skill in manager.unlockedSkills) {
            if (skill == null) continue; // 인스펙터에서 비워둔 칸.

            SkillRowView row = Instantiate(rowPrefab, rowContainer);
            row.Bind(skill, HandleRowClicked);
            rows.Add(row);
        }

        highlightedRow = Mathf.Clamp(highlightedRow, 0, Mathf.Max(0, rows.Count - 1));
    }

    void Refresh() {
        RefreshSlots();
        RefreshRows();
        RefreshDescription();
    }

    void RefreshSlots() {
        for (int i = 0; i < SkillManager.SlotCount; i++) {
            slotViews[i].Set(manager.GetSkillInSlot(i), emptySlotText);
            slotViews[i].SetSelected(i == selectedSlot);
        }
    }

    void RefreshRows() {
        for (int i = 0; i < rows.Count; i++) {
            rows[i].Refresh(manager.GetSlotOf(rows[i].Skill));
            rows[i].SetHighlighted(i == highlightedRow);
        }

        if (listTitleLabel != null) listTitleLabel.text = rows.Count > 0 ? authoredListTitle : emptyListText;
    }

    void RefreshDescription() {
        if (descriptionLabel == null) return;

        if (rows.Count == 0 || highlightedRow < 0 || highlightedRow >= rows.Count) {
            descriptionLabel.text = string.Empty;
            return;
        }

        descriptionLabel.text = rows[highlightedRow].Skill.description;
    }

    #endregion
}
