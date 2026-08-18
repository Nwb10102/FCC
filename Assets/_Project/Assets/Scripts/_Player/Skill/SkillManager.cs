using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// 해금된 스킬 중 최대 3개를 슬롯에 장착하고, 입력에 맞춰 발동시킨다.
// 기획서상 장착 수는 3개 고정이고 교체는 거울(정비하기)에서 한다.
//
// **플레이어 오브젝트에 붙이세요.** 스킬이 owner로 받는 트랜스폼이 이 컴포넌트가 붙은 오브젝트다.
public class SkillManager : MonoBehaviour {
    // 기획서상 최대 장착 수. 배열·UI·세이브가 전부 이 값을 따라가야 하므로 상수로 둔다.
    public const int SlotCount = 3;

    #region 인스펙터 변수

    [Header("보유 스킬")]
    // 해금된 스킬 에셋 목록. 거울 정비 UI가 이 목록을 장착 후보로 뿌린다.
    // **지금은 인스펙터로 채우지만, 기억 조각 해금이 붙으면 세이브에서 UnlockSkill()로 채우게 됩니다.**
    public List<SkillBase> unlockedSkills = new();

    [Header("장착 슬롯")]
    // 슬롯 0·1·2. 인스펙터에 미리 넣어두면 그게 시작 장착 상태가 된다.
    // **크기는 3으로 고정입니다.** 어긋나게 바꿔도 Awake에서 3개로 맞춘다.
    public SkillBase[] equippedSkills = new SkillBase[SlotCount];

    [Header("입력")]
    // 슬롯 0·1·2를 발동시킬 키. Client.inputactions에 아직 스킬 액션이 없어 키보드를 직접 읽는다.
    // **Skill1~3 액션이 생기면 이 배열 대신 InputActionReference로 갈아끼우세요.**
    public Key[] slotKeys = { Key.Q, Key.W, Key.E };

    [Header("디버그")]
    public bool logCooldown = true; // 쿨타임 중에 눌렀을 때 남은 시간을 콘솔에 찍는다. 감각 조정용이라 빌드에선 꺼도 된다.

    #endregion
    #region 이벤트

    // (슬롯 번호, 장착된 스킬) — 해제한 경우 skill이 null로 온다. 정비 UI가 구독해 슬롯 아이콘을 갱신한다.
    public event Action<int, SkillBase> onSkillEquipped;

    // (슬롯 번호, 발동한 스킬) — 쿨타임 게이지 UI가 구독한다.
    public event Action<int, SkillBase> onSkillUsed;

    #endregion
    #region 컴포넌트 변수

    Player_move playerMove; // 대사·컷씬 중에는 스킬도 막아야 해서 이동 잠금 상태를 본다.

    // 슬롯별 조준 중 여부. IAimableSkill(마우스 조준형 스킬)에만 쓰인다 — 누른 순간 true, 뗀 순간 false.
    readonly bool[] isAimingSlot = new bool[SlotCount];

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        playerMove = GetComponentInParent<Player_move>();

        // 인스펙터에서 배열 크기를 잘못 건드려도 슬롯 수가 어긋나지 않게 맞춰 둔다.
        if (equippedSkills == null || equippedSkills.Length != SlotCount) {
            Array.Resize(ref equippedSkills, SlotCount);
        }

        // 인스펙터로 미리 끼워둔 스킬은 해금 목록에도 넣어준다.
        // 안 그러면 정비 UI에서 뺐다가 다시 끼울 수 없고, EquipSkill의 해금 검사에도 걸린다.
        for (int i = 0; i < SlotCount; i++) {
            if (equippedSkills[i] != null) UnlockSkill(equippedSkills[i]);
        }

        // 쿨타임이 ScriptableObject 에셋에 기록되는 구조라, 에디터에서 플레이를 다시 켜면 직전 판의 값이 남아 있다.
        ResetAllCooldowns();
    }

    void Update() {
        if (Keyboard.current == null) return; // 패드만 연결된 상황 등.

        if (IsInputBlocked()) {
            CancelAllAiming(); // 조준 도중 대사·컷씬이 끼어들면 조준선이 화면에 남지 않도록 정리한다.
            return;
        }

        for (int i = 0; i < SlotCount; i++) {
            HandleSlotInput(i);
        }
    }

    #endregion
    #region 입력 처리

    // 대사·컷씬 중에는 스킬을 막는다. PlayerInteractor가 상호작용을 막는 것과 같은 기준.
    bool IsInputBlocked() {
        return playerMove != null && playerMove.isMovementLocked;
    }

    void HandleSlotInput(int slotIndex) {
        if (slotKeys == null || slotIndex >= slotKeys.Length) return;

        Key key = slotKeys[slotIndex];
        if (key == Key.None) return; // Keyboard.current[Key.None]은 예외를 던진다.

        SkillBase skill = equippedSkills[slotIndex];
        if (skill == null) return;

        // 조준형 스킬(IAimableSkill)은 누름·유지·뗌을 전부 스킬에 넘겨준다. 아니면 기존처럼 누르는 즉시 발동.
        if (skill is IAimableSkill aimable) {
            HandleAimableInput(slotIndex, skill, aimable, Keyboard.current[key]);
        }
        else if (Keyboard.current[key].wasPressedThisFrame) {
            UseSkillInSlot(slotIndex);
        }
    }

    void HandleAimableInput(int slotIndex, SkillBase skill, IAimableSkill aimable, ButtonControl control) {
        if (control.wasPressedThisFrame) {
            if (!skill.IsReady()) {
                if (logCooldown) {
                    Debug.Log($"[SkillManager] '{skill.DisplayName}' 쿨타임 — 남은 시간 {skill.GetRemainingCooldown():F1}초");
                }
                return;
            }

            isAimingSlot[slotIndex] = true;
            aimable.BeginAim(transform);
            return;
        }

        if (!isAimingSlot[slotIndex]) return; // 쿨타임 중이라 조준을 시작하지 못했던 경우 등.

        if (control.wasReleasedThisFrame) {
            isAimingSlot[slotIndex] = false;
            aimable.ReleaseAim(transform, GetMouseWorldPosition());
            onSkillUsed?.Invoke(slotIndex, skill);
        }
        else if (control.isPressed) {
            aimable.UpdateAim(transform, GetMouseWorldPosition());
        }
    }

    void CancelAllAiming() {
        for (int i = 0; i < SlotCount; i++) {
            if (!isAimingSlot[i]) continue;

            isAimingSlot[i] = false;
            if (equippedSkills[i] is IAimableSkill aimable) aimable.CancelAim(transform);
        }
    }

    // 마우스 커서 위치를 플레이어와 같은 z 평면의 월드 좌표로 변환한다.
    Vector2 GetMouseWorldPosition() {
        if (Mouse.current == null || Camera.main == null) return transform.position;

        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    #endregion
    #region 스킬 장착

    // 슬롯에 스킬을 장착한다. skill에 null을 넣으면 해제.
    // 다른 슬롯에 이미 끼워져 있는 스킬을 넣으면 두 슬롯의 내용을 맞바꾼다 (같은 스킬이 두 칸을 먹지 않도록).
    public bool EquipSkill(int slotIndex, SkillBase skill) {
        if (!IsValidSlot(slotIndex)) {
            Debug.LogWarning($"[SkillManager] 슬롯 번호 {slotIndex}는 범위 밖입니다. (0~{SlotCount - 1})", this);
            return false;
        }

        // 해금하지 않은 스킬이 UI 버그나 잘못된 호출로 끼워지는 것을 막는다.
        if (skill != null && !unlockedSkills.Contains(skill)) {
            Debug.LogWarning($"[SkillManager] '{skill.DisplayName}'은 아직 해금되지 않아 장착할 수 없습니다.", this);
            return false;
        }

        if (equippedSkills[slotIndex] == skill) return true; // 이미 같은 상태. 이벤트를 헛돌리지 않는다.

        SkillBase displaced = equippedSkills[slotIndex]; // 이 슬롯에 원래 있던 스킬. 자리를 맞바꿀 때 돌려보낸다.
        int previousSlot = GetSlotOf(skill); // 스킬이 null이면 -1이 나와 교체 처리를 건너뛴다.

        equippedSkills[slotIndex] = skill;
        onSkillEquipped?.Invoke(slotIndex, skill);

        // 다른 슬롯에 있던 스킬을 가져온 경우, 원래 자리에는 이 슬롯에 있던 것을 넣어 맞바꾼다.
        // displaced가 null이면 그 슬롯은 그냥 비워진다.
        if (previousSlot >= 0 && previousSlot != slotIndex) {
            equippedSkills[previousSlot] = displaced;
            onSkillEquipped?.Invoke(previousSlot, displaced);
        }

        return true;
    }

    public bool UnequipSlot(int slotIndex) {
        return EquipSkill(slotIndex, null);
    }

    // 기억 조각으로 새 스킬을 얻었을 때 호출. 이미 있으면 아무 일도 하지 않는다.
    public bool UnlockSkill(SkillBase skill) {
        if (skill == null || unlockedSkills.Contains(skill)) return false;

        unlockedSkills.Add(skill);
        return true;
    }

    #endregion
    #region 스킬 발동

    // 슬롯의 스킬을 발동한다. 빈 슬롯이거나 쿨타임 중이면 false.
    public bool UseSkillInSlot(int slotIndex) {
        if (!IsValidSlot(slotIndex)) return false;

        SkillBase skill = equippedSkills[slotIndex];
        if (skill == null) return false;

        if (!skill.IsReady()) {
            if (logCooldown) {
                Debug.Log($"[SkillManager] '{skill.DisplayName}' 쿨타임 — 남은 시간 {skill.GetRemainingCooldown():F1}초");
            }
            return false;
        }

        // owner로 이 컴포넌트가 붙은 트랜스폼을 넘긴다. 스킬의 판정 위치·방향이 전부 여기서 파생된다.
        if (!skill.TryUse(transform)) return false;

        onSkillUsed?.Invoke(slotIndex, skill);
        return true;
    }

    #endregion
    #region 조회

    public bool IsValidSlot(int slotIndex) {
        return slotIndex >= 0 && slotIndex < SlotCount && equippedSkills != null && slotIndex < equippedSkills.Length;
    }

    public SkillBase GetSkillInSlot(int slotIndex) {
        return IsValidSlot(slotIndex) ? equippedSkills[slotIndex] : null;
    }

    // 스킬이 장착된 슬롯 번호. 장착돼 있지 않으면 -1.
    public int GetSlotOf(SkillBase skill) {
        if (skill == null || equippedSkills == null) return -1;

        for (int i = 0; i < equippedSkills.Length; i++) {
            if (equippedSkills[i] == skill) return i;
        }
        return -1;
    }

    public bool IsEquipped(SkillBase skill) {
        return GetSlotOf(skill) >= 0;
    }

    // 장착·해금 스킬 전부의 쿨타임을 초기화. 리스폰이나 거울 정비 후처럼 판을 새로 시작할 때 쓴다.
    public void ResetAllCooldowns() {
        foreach (SkillBase skill in unlockedSkills) {
            if (skill != null) skill.ResetCooldown();
        }

        for (int i = 0; i < equippedSkills.Length; i++) {
            if (equippedSkills[i] != null) equippedSkills[i].ResetCooldown();
        }
    }

    #endregion
}
