using System.Collections.Generic;
using UnityEngine;
// 새로운 Input System을 사용하기 위해 필수적인 네임스페이스
using UnityEngine.InputSystem; 

public class SkillManager : MonoBehaviour
{
    // 유니티 인스펙터 창에서 스킬 목록을 추가하고 세팅할 수 있는 리스트
    public List<Skill> skills = new List<Skill>();

    void Update()
    {
        // 현재 연결된 키보드 장치가 없다면 업데이트를 진행하지 않음
        if (Keyboard.current == null) return;

        // 리스트에 등록된 모든 스킬을 매 프레임마다 검사
        foreach (Skill skill in skills)
        {
            // 인스펙터에 지정한 KeyCode가 새로운 인풋 시스템 상에서 눌렸는지 확인
            if (GetNewInputKeyDown(skill.inputKey))
            {
                AttemptUseSkill(skill);
            }
        }
    }

    // 옛날 KeyCode 방식을 새로운 Input System의 Key 방식으로 변환하여 입력 체크하는 함수
    bool GetNewInputKeyDown(KeyCode keyCode)
    {
        string keyStr = keyCode.ToString();

        // KeyCode 문자열(예: "Q")을 새로운 인풋 시스템의 Key 열거형으로 변환 시도 (대소문자 구분X)
        if (System.Enum.TryParse<UnityEngine.InputSystem.Key>(keyStr, true, out UnityEngine.InputSystem.Key outKey))
        {
            // 해당 키가 이번 프레임에 정확히 눌렸는지 판별
            return Keyboard.current[outKey].wasPressedThisFrame;
        }
        
        return false;
    }

    // 스킬 사용을 시도하는 함수 (쿨타임 체크)
    void AttemptUseSkill(Skill skill)
    {
        if (skill.IsReady())
        {
            // 스킬 사용 성공 (캐릭터의 현재 위치/회전 정보를 넘겨줌)
            skill.Use(transform); 
        }
        else
        {
            // 쿨타임이 남았을 때 콘솔창에 남은 시간 표시
            float remainingTime = (skill.lastUsedTime + skill.cooldown) - Time.time;
            Debug.Log($"{skill.skillName}은 아직 쿨타임입니다! (남은 시간: {remainingTime:F1}초)");
        }
    }
}