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
        if (match == null || match.sprite == null) return;

        spriteRenderer.sprite = match.sprite;
        currentState = stateName;
    }

    #endregion
}
