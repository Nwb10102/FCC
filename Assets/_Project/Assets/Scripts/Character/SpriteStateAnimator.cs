using System;
using System.Collections.Generic;
using UnityEngine;

// 상태 이름 ↔ 스프라이트를 매핑해 두고, 몬스터 로직이 Play(상태 이름)을 호출할 때마다 갈아 끼우는 임시 애니메이션 컴포넌트.
// 프레임 시퀀스가 아니라 상태별 정지 이미지 한 장을 스왑하는 방식이다. 나중에 실제 프레임 애니메이션(Animator)으로
// 교체하더라도 몬스터 로직 쪽 호출부(Play(상태 이름))는 그대로 유지할 수 있도록 인터페이스를 맞춰 두었다.
// **몬스터의 Renderer 오브젝트(SpriteRenderer가 붙은 자식)에 붙이세요.**
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteStateAnimator : MonoBehaviour {
    #region 인스펙터 변수

    [Header("상태별 스프라이트")]
    public List<SpriteState> states = new(); // 상태 이름과 매칭되는 스프라이트 목록. 이름은 Play() 호출부와 정확히 일치해야 한다.

    #endregion
    #region 컴포넌트 변수

    SpriteRenderer spriteRenderer;
    string currentState;
    readonly HashSet<string> warnedStates = new(); // 같은 경고를 반복해서 찍지 않기 위한 기록.

    #endregion

    [Serializable]
    public class SpriteState {
        public string name;
        public Sprite sprite;
    }

    #region 유니티 라이프 사이클

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    #endregion
    #region 재생

    public void Play(string stateName) {
        if (spriteRenderer == null || stateName == currentState) return;

        SpriteState match = states.Find(s => s.name == stateName);
        if (match == null || match.sprite == null) {
            // 예전에는 그냥 return 했다. 상태 이름은 몬스터 코드에 문자열로 박혀 있고 여기 목록은 인스펙터에
            // 손으로 적는 값이라, 오타 하나나 이름 변경만으로 그 연출이 통째로 사라지는데 로그조차 남지 않았다.
            WarnOnce(stateName, match == null ? "목록에 없습니다" : "스프라이트가 비어 있습니다");
            return;
        }

        spriteRenderer.sprite = match.sprite;
        currentState = stateName;
    }

    // 같은 상태를 매 프레임 요청할 수 있으므로 이름당 한 번만 알린다. 콘솔이 도배되면 오히려 못 보고 넘긴다.
    void WarnOnce(string stateName, string reason) {
        if (!warnedStates.Add(stateName)) return;
        Debug.LogWarning($"[SpriteStateAnimator] '{name}' — 상태 '{stateName}' 의 {reason}. 호출부와 인스펙터의 상태 이름을 맞추세요.", this);
    }

    #endregion
}
