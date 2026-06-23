using UnityEngine;

[System.Serializable]
public class Skill
{
    public string skillName;    // 스킬 이름
    public float cooldown;      // 쿨타임 (초)
    public float damage;        // 데미지
    public KeyCode inputKey;    // 발동할 키 (예: Q, W, E, R)

    [HideInInspector]
    public float lastUsedTime;  // 마지막으로 스킬을 사용한 시간 (쿨타임 계산용)

    // 스킬이 사용 가능한지 확인하는 함수
    public bool IsReady()
    {
        return Time.time >= lastUsedTime + cooldown;
    }

    // 스킬 사용 로직 (실제 이펙트나 데미지는 여기서 발동)
    public void Use(Transform PlayerTransform)
    {
        lastUsedTime = Time.time;
        Debug.Log($"{skillName} 스킬 발동! (데미지: {damage})");

        // TODO: 여기에 실제 스킬 이펙트 생성이나 데미지 처리 로직을 넣습니다.
        // 예: GameObject.Instantiate(파이어볼프리팹, PlayerTransform.position, ...);
    }
}
