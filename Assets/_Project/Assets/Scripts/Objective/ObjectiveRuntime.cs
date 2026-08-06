// 목표의 현재 상태.
//
// Locked  — 미션이 아직 시작되지 않았거나 선행 목표가 남아 있다. 체크리스트에 줄을 만들지 않는다.
// Active  — 지금 진행 가능. 체크리스트에 보인다.
// Completed — 달성 완료.
//
// **Active는 세이브에 기록하지 않는다.** "미션이 시작됨 + 미완료 + 선행 조건 전부 완료"로 언제든 다시
// 계산되기 때문이다(ObjectiveManager.RefreshActivation). 그래서 선행 조건을 무시하고 강제로 활성시키는
// API는 두지 않는다 — 그런 게 생기면 파생이 불가능해져 세이브에 상태를 적어야 한다.
public enum ObjectiveState {
    Locked,
    Active,
    Completed,
}

// 목표 하나의 런타임 상태. ObjectiveManager가 소유하고, 기획 데이터는 Definition으로 읽기만 한다.
//
// 진행도를 ScriptableObject가 아니라 여기에 두는 이유: 에셋에 적으면 하나를 여러 곳이 공유하므로
// 에디터에서 플레이를 멈춘 뒤에도 완료 상태가 에셋에 남는다 (SkillBase.lastUsedTime이 겪은 문제).
public class ObjectiveRuntime {
    #region 상태

    public readonly ObjectiveDefinition Definition;

    public int currentCount;
    public ObjectiveState state = ObjectiveState.Locked;

    #endregion
    #region 생성

    public ObjectiveRuntime(ObjectiveDefinition definition) {
        Definition = definition;
    }

    #endregion
    #region 조회

    public string Id => Definition.ObjectiveId;

    // 현재 언어로 해석한 설명문. 언어를 바꾸면 체크리스트가 다시 그려지면서 새 언어로 갱신된다.
    public string Description => Definition.Description;

    public int TargetCount => Definition.targetCount;

    public bool IsCompleted => state == ObjectiveState.Completed;

    public bool IsActive => state == ObjectiveState.Active;

    // 세이브 복원·미션 재시작 때 처음 상태로 되돌린다.
    public void Reset() {
        currentCount = 0;
        state = ObjectiveState.Locked;
    }

    #endregion
}
