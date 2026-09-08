using System.Collections;
using UnityEngine;

// Health가 무적인 동안 스프라이트를 깜빡여, 지금 맞지 않는 상태라는 것을 눈으로 알려 준다.
// HealthBar·HitFlash처럼 Health와 함께 붙이는 독립 컴포넌트다.
//
// 색이 아니라 SpriteRenderer.enabled를 껐다 켜는 방식인 이유는, 같은 스프라이트의 color를
// HitFlash가 피격 플래시로 덮어쓰기 때문이다. 둘 다 색을 만지면 무적 중에 맞았을 때
// 서로의 값을 되돌려 놓아 어중간한 색으로 굳는다.
[RequireComponent(typeof(Health))]
public class InvincibleBlink : MonoBehaviour {
    #region 인스펙터 변수

    [Header("깜빡임")]
    public float blinkInterval = 0.09f; // 보였다 사라지는 한 단계의 길이. 짧을수록 빠르게 깜빡인다.

    #endregion
    #region 컴포넌트 변수

    Health health;
    SpriteRenderer[] renderers;
    bool[] wasEnabled; // 원래 꺼져 있던 스프라이트를 깜빡임이 멋대로 켜 버리지 않도록 시작 상태를 기억해 둔다.
    Coroutine blinkRoutine;

    #endregion
    #region 유니티 라이프 사이클

    void Awake() {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        wasEnabled = new bool[renderers.Length];
    }

    void OnEnable() {
        health.OnInvincibleChanged += HandleInvincibleChanged;

        // 무적 도중에 이 컴포넌트가 켜졌다면 이미 이벤트는 지나간 뒤라 깜빡임이 시작되지 않는다.
        if (health.IsInvincible) HandleInvincibleChanged(true);
    }

    void OnDisable() {
        health.OnInvincibleChanged -= HandleInvincibleChanged;
        StopBlink(); // 꺼진 상태로 굳지 않도록 반드시 되돌린다.
    }

    #endregion
    #region 깜빡임

    void HandleInvincibleChanged(bool invincible) {
        if (invincible) {
            if (blinkRoutine != null) return; // 무적이 연장된 경우. 이미 돌고 있는 깜빡임을 끊지 않는다.
            blinkRoutine = StartCoroutine(BlinkRoutine());
        } else {
            StopBlink();
        }
    }

    // 히트스톱으로 시간이 멈춘 동안에도 깜빡여야 하므로 unscaled 기준으로 돈다.
    IEnumerator BlinkRoutine() {
        for (int i = 0; i < renderers.Length; i++) {
            wasEnabled[i] = renderers[i] != null && renderers[i].enabled;
        }

        bool visible = false;
        while (true) {
            SetVisible(visible);
            visible = !visible;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }
    }

    void StopBlink() {
        if (blinkRoutine != null) {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        SetVisible(true);
    }

    void SetVisible(bool visible) {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i] == null || !wasEnabled[i]) continue;
            renderers[i].enabled = visible;
        }
    }

    #endregion
}
