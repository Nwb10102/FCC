using UnityEngine;

// 조준 후 발사하는 스킬이 구현하는 인터페이스. 칼잡이(Broken Phantasm)처럼 슬롯 키를 누르고 있는 동안
// 마우스 방향으로 조준선을 보여주다가 키를 떼는 순간 그 방향으로 발동하는 스킬에 쓴다.
// 입력 상태(누름·유지·뗌) 판단은 SkillManager가 맡고, 조준선 연출과 실제 발동 로직은 구현체가 맡는다.
public interface IAimableSkill {
    // 키를 처음 눌렀을 때 한 번 호출된다. 조준선을 켜는 등 준비 동작.
    void BeginAim(Transform owner);

    // 키를 누르고 있는 동안 매 프레임 호출된다. aimPoint는 마우스 커서의 월드 좌표.
    void UpdateAim(Transform owner, Vector2 aimPoint);

    // 키를 뗀 순간 호출된다. 조준선을 끄고 실제 발동(쿨타임 기록 포함)까지 이 안에서 처리한다.
    void ReleaseAim(Transform owner, Vector2 aimPoint);

    // 조준 도중 대사·컷씬 진입 등으로 강제 취소해야 할 때 호출된다. 발동하지 않고 조준선만 끈다.
    void CancelAim(Transform owner);
}
